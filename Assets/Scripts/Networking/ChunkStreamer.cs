using System;
using System.Collections.Generic;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

/*
Server-side Chunk Streaming
	Every client reports the center of its StreamWindow (STREAMCENTER) and the Server pushes every chunk inside it, nearest first
	The amount of chunks sent but not yet consumed by a client is limited by an adaptive send window, fed back by CHUNKACK
	Chunks that aren't loaded are requested to the ChunkLoader_Server generation queue and sent as soon as they are ready
	Chunks that leave a window are unloaded for that client by the Server itself
*/
public class ChunkStreamer
{
	private Server server;
	private ChunkLoader_Server cl;

	private Dictionary<ulong, ClientStreamState> states = new Dictionary<ulong, ClientStreamState>();
	private List<ulong> clientOrder = new List<ulong>();
	private int roundRobinIndex = 0;

	// Chunks in the generation queue and the clients waiting for them
	private Dictionary<ChunkPos, HashSet<ulong>> waiting = new Dictionary<ChunkPos, HashSet<ulong>>();

	// Cache
	private List<ChunkPos> cacheAdded = new List<ChunkPos>();
	private List<ChunkPos> cacheRemoved = new List<ChunkPos>();
	private List<ChunkPos> cacheWaitingKeys = new List<ChunkPos>();
	private Stopwatch stopwatch = new Stopwatch();


	public ChunkStreamer(Server server, ChunkLoader_Server cl){
		this.server = server;
		this.cl = cl;
	}

	public bool IsStreaming(ulong id){
		return this.states.ContainsKey(id);
	}

	// Receives the new center of a client's StreamWindow
	public void SetStreamCenter(ulong id, ChunkPos center, int verticalLayer){
		int renderDistance;
		ClientStreamState state;

		if(!this.server.playerRenderDistances.TryGetValue(id, out renderDistance)){
			Debug.Log("STREAMCENTER received from client " + id + " before its Render Distance");
			return;
		}

		StreamWindow newWindow = new StreamWindow(center, verticalLayer, ChunkStreaming.GetRadius(renderDistance));

		// First window of this client
		if(!this.states.TryGetValue(id, out state)){
			state = new ClientStreamState(id, newWindow, Constants.CHUNK_STREAM_WINDOW_MIN);
			this.states.Add(id, state);
			this.clientOrder.Add(id);

			ChunkStreaming.FillWindow(newWindow, this.cacheAdded);

			for(int i=0; i < this.cacheAdded.Count; i++)
				state.pending.Add(this.cacheAdded[i]);

			return;
		}

		if(state.window == newWindow)
			return;

		ChunkStreaming.Diff(state.window, newWindow, this.cacheAdded, this.cacheRemoved);

		bool centerChanged = state.window.center != newWindow.center;
		state.window = newWindow;

		for(int i=0; i < this.cacheRemoved.Count; i++){
			state.pending.Remove(this.cacheRemoved[i]);

			if(state.sent.Remove(this.cacheRemoved[i]))
				this.server.UnloadChunkForClient(this.cacheRemoved[i], id);
			else
				ReleaseIfOrphan(this.cacheRemoved[i]);
		}

		if(centerChanged)
			state.pending.SetPlayerPosition(newWindow.center);

		for(int i=0; i < this.cacheAdded.Count; i++){
			if(!state.sent.Contains(this.cacheAdded[i]))
				state.pending.Add(this.cacheAdded[i]);
		}
	}

	// Receives the amount of streamed chunks consumed by a client and how many it still has to load
	public void ChunkAck(ulong id, int consumed, int backlog){
		ClientStreamState state;

		if(!this.states.TryGetValue(id, out state))
			return;

		state.inFlight = Math.Max(0, state.inFlight - consumed);

		// Client is receiving faster than it can load: avoid piling chunks on it
		if(backlog > state.sendWindow/2)
			state.sendWindow = Math.Max(Constants.CHUNK_STREAM_WINDOW_MIN, (state.sendWindow*3)/4);
		// Client keeps up and the window was the bottleneck
		else if(backlog == 0 && state.windowLimited)
			state.sendWindow = Math.Min(Constants.CHUNK_STREAM_WINDOW_MAX, state.sendWindow + Constants.CHUNK_STREAM_WINDOW_STEP);

		state.windowLimited = false;
	}

	// Client reported that a chunk of its window is missing
	public void Resync(ulong id, ChunkPos pos){
		ClientStreamState state;

		if(!this.states.TryGetValue(id, out state))
			return;
		if(!state.window.Contains(pos))
			return;

		// The previous send of this chunk will never be acknowledged
		if(state.sent.Remove(pos))
			state.inFlight = Math.Max(0, state.inFlight - 1);

		state.front.Enqueue(pos);
	}

	// Client unloaded a chunk on its own (legacy REQUESTCHUNKUNLOAD)
	public void ForgetChunk(ulong id, ChunkPos pos){
		ClientStreamState state;

		if(this.states.TryGetValue(id, out state))
			state.sent.Remove(pos);
	}

	public void RemoveClient(ulong id){
		HashSet<ulong> ids;

		if(!this.states.Remove(id))
			return;

		this.clientOrder.Remove(id);

		this.cacheWaitingKeys.Clear();

		foreach(KeyValuePair<ChunkPos, HashSet<ulong>> item in this.waiting){
			item.Value.Remove(id);

			if(item.Value.Count == 0)
				this.cacheWaitingKeys.Add(item.Key);
		}

		for(int i=0; i < this.cacheWaitingKeys.Count; i++){
			if(this.waiting.TryGetValue(this.cacheWaitingKeys[i], out ids) && ids.Count == 0)
				this.waiting.Remove(this.cacheWaitingKeys[i]);
		}
	}

	// Returns true if any client still wants a chunk that is in the generation queue
	public bool IsAwaited(ChunkPos pos){
		HashSet<ulong> ids;
		ClientStreamState state;

		if(!this.waiting.TryGetValue(pos, out ids))
			return false;

		foreach(ulong id in ids){
			if(this.states.TryGetValue(id, out state) && state.window.Contains(pos))
				return true;
		}

		return false;
	}

	// Called by the generation queue whenever a chunk leaves it, loaded or not
	public void OnChunkLoaded(ChunkPos pos, bool loaded){
		HashSet<ulong> ids;
		ClientStreamState state;

		if(!this.waiting.TryGetValue(pos, out ids))
			return;

		this.waiting.Remove(pos);

		foreach(ulong id in ids){
			if(!this.states.TryGetValue(id, out state))
				continue;

			state.waitingCount--;

			if(loaded && state.window.Contains(pos) && !state.sent.Contains(pos))
				state.front.Enqueue(pos);
		}
	}

	// Sends chunks to clients in round-robin, until all of them are idle or the frame budget is spent
	public void Tick(){
		if(this.clientOrder.Count == 0)
			return;

		long budget = (long)(Constants.SERVER_CHUNK_STREAM_BUDGET_MS * Stopwatch.Frequency / 1000f);
		int idleClients = 0;
		ClientStreamState state;

		this.stopwatch.Restart();

		while(idleClients < this.clientOrder.Count && this.stopwatch.ElapsedTicks < budget){
			if(this.roundRobinIndex >= this.clientOrder.Count)
				this.roundRobinIndex = 0;

			state = this.states[this.clientOrder[this.roundRobinIndex]];
			this.roundRobinIndex++;

			if(StepClient(state))
				idleClients = 0;
			else
				idleClients++;
		}
	}

	// Sends or requests the generation of the next chunk of a client. Returns false if nothing could be done
	private bool StepClient(ClientStreamState state){
		ChunkPos pos;
		bool fromFront;

		// The connection can't keep up, so chunks wait instead of piling up in memory
		if(this.server.GetPendingBytes(state.id) > Constants.NET_STREAM_MAX_PENDING_BYTES)
			return false;

		while(true){
			if(state.inFlight >= state.sendWindow){
				state.windowLimited = true;
				return false;
			}

			if(state.front.Count > 0){
				pos = state.front.Dequeue();
				fromFront = true;
			}
			else if(state.pending.GetSize() > 0){
				pos = state.pending.Pop();
				fromFront = false;
			}
			else{
				return false;
			}

			if(!state.window.Contains(pos) || state.sent.Contains(pos))
				continue;

			if(this.cl.Contains(pos)){
				this.server.SendChunkToClient(pos, state.id, stream:true);
				state.sent.Add(pos);
				state.inFlight++;
				return true;
			}

			// Will be added to front once it's loaded
			if(IsWaiting(pos, state.id))
				continue;

			// Limits how far ahead of the client the generation can go, so it follows the client's priority
			if(state.waitingCount >= Constants.CHUNK_STREAM_GENERATION_LOOKAHEAD){
				if(fromFront)
					state.front.Enqueue(pos);
				else
					state.pending.Add(pos);

				return false;
			}

			AddWaiting(pos, state);
			this.cl.RequestLoad(pos);
			return true;
		}
	}

	private bool IsWaiting(ChunkPos pos, ulong id){
		HashSet<ulong> ids;

		if(!this.waiting.TryGetValue(pos, out ids))
			return false;

		return ids.Contains(id);
	}

	private void AddWaiting(ChunkPos pos, ClientStreamState state){
		HashSet<ulong> ids;

		if(!this.waiting.TryGetValue(pos, out ids)){
			ids = new HashSet<ulong>();
			this.waiting.Add(pos, ids);
		}

		if(ids.Add(state.id))
			state.waitingCount++;
	}

	// Frees a loaded chunk that was never sent and no client wants anymore
	private void ReleaseIfOrphan(ChunkPos pos){
		if(!this.cl.Contains(pos))
			return;
		if(this.cl.loadedChunks.ContainsKey(pos))
			return;

		foreach(ClientStreamState state in this.states.Values){
			if(state.window.Contains(pos))
				return;
		}

		this.cl.ReleaseChunk(pos);
	}
}
