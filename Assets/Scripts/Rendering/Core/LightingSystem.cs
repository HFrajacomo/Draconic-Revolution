using System.Collections.Generic;
using Unity.Jobs;

/*
Calculates the light of loaded chunks in worker threads, nearest to the player first.
Every chunk has at most one calculation queued or running. Chunk data is read when a calculation starts and written back once it ends.
Anything that needs the light of a chunk right away calls EnsureLit, which finishes its calculation in the main thread.

Direct sunlight enters a chunk through the columns where it leaves the bottom of the chunk above (its SunExitMask).
The last mask of every lit chunk position is kept, even after the chunk is unloaded, so chunks whose upper layer isn't loaded
still know where sunlight comes from. Positions that were never lit count as the open sky.
Chunks always use the kept mask of the chunk above, and the chunk below is calculated again whenever a kept mask changes
*/
public class LightingSystem{
	private ChunkLoader loader;
	private LightSlot[] slots;
	private ChunkPriorityQueue queue = new ChunkPriorityQueue(DistanceMetric.EDGE_LIMITING, edgeLimit:World.renderDistance);
	private Dictionary<ChunkPos, LightSlot> running = new Dictionary<ChunkPos, LightSlot>();
	private HashSet<ChunkPos> rerun = new HashSet<ChunkPos>(); // Running chunks requested again, which are calculated again once done
	private Dictionary<ChunkPos, SunExitMask> sunExits = new Dictionary<ChunkPos, SunExitMask>(); // Last known SunExitMask of every lit chunk position
	private HashSet<ChunkPos> waitingAbove = new HashSet<ChunkPos>(); // Chunks popped in this Update that wait for the light of the chunk above


	// slotCount is the amount of light calculations running at the same time
	public LightingSystem(ChunkLoader loader, int slotCount){
		this.loader = loader;
		this.slots = new LightSlot[slotCount];

		for(int i=0; i < this.slots.Length; i++)
			this.slots[i] = new LightSlot();
	}

	public void SetPlayerPosition(ChunkPos pos){
		this.queue.SetPlayerPosition(pos);
	}

	// Queues the light calculation of a chunk
	public void Enqueue(ChunkPos pos){
		if(this.running.ContainsKey(pos)){
			this.rerun.Add(pos);
			return;
		}

		this.queue.Add(pos);
	}

	// True if the chunk has no light calculation queued or running
	public bool IsLit(ChunkPos pos){
		return !this.queue.Contains(pos) && !this.running.ContainsKey(pos) && !this.rerun.Contains(pos);
	}

	// Columns where direct sunlight enters the top of a chunk
	public SunExitMask GetSkyFromAbove(ChunkPos pos){
		SunExitMask mask;

		if(pos.y >= Chunk.chunkMaxY)
			return SunExitMask.ALL;
		if(this.sunExits.TryGetValue(new ChunkPos(pos.x, pos.z, pos.y+1), out mask))
			return mask;

		return SunExitMask.ALL;
	}

	// Applies finished calculations and starts queued ones
	public void Update(){
		LightSlot slot;
		ChunkPos pos;

		for(int i=0; i < this.slots.Length; i++){
			slot = this.slots[i];

			if(slot.IsBusy() && slot.IsDone())
				Finish(slot);
		}

		for(int i=0; i < this.slots.Length; i++){
			slot = this.slots[i];

			if(slot.IsBusy())
				continue;

			while(this.queue.GetSize() > 0){
				pos = this.queue.Pop();

				// The chunk above is lit first, since this one needs to know where its sunlight comes from
				if(IsAboveUnlit(pos)){
					this.waitingAbove.Add(pos);
					continue;
				}

				if(slot.Start(pos, GetSkyFromAbove(pos), this.loader)){
					this.running.Add(pos, slot);
					break;
				}
			}

			if(this.queue.GetSize() == 0)
				break;
		}

		foreach(ChunkPos waiting in this.waitingAbove)
			this.queue.Add(waiting);

		this.waitingAbove.Clear();

		JobHandle.ScheduleBatchedJobs();
	}

	// Makes sure the light of a chunk is calculated, finishing it in the main thread if needed
	public void EnsureLit(ChunkPos pos){
		LightSlot slot;

		if(this.running.TryGetValue(pos, out slot)){
			slot.Complete();
			Finish(slot);
		}

		if(this.queue.Contains(pos)){
			this.queue.Remove(pos);
			CalculateNow(pos);
		}
	}

	// Calculates the light of a chunk right away, discarding queued or running calculations of it
	public void RecalcNow(ChunkPos pos){
		LightSlot slot;

		if(this.running.TryGetValue(pos, out slot)){
			slot.Complete();
			this.running.Remove(pos);
			slot.Release();
		}

		this.rerun.Remove(pos);
		this.queue.Remove(pos);

		CalculateNow(pos);
	}

	// Forgets the light calculation of an unloaded chunk. Its SunExitMask is kept for the chunk below
	public void Cancel(ChunkPos pos){
		LightSlot slot;

		this.queue.Remove(pos);
		this.rerun.Remove(pos);

		if(this.running.TryGetValue(pos, out slot)){
			slot.Complete();
			this.running.Remove(pos);
			slot.Release();
		}
	}

	// Discards every queued and running calculation
	public void Clear(){
		for(int i=0; i < this.slots.Length; i++){
			if(!this.slots[i].IsBusy())
				continue;

			this.slots[i].Complete();
			this.slots[i].Release();
		}

		this.queue.Clear();
		this.running.Clear();
		this.rerun.Clear();
	}

	public void Dispose(){
		Clear();
		this.sunExits.Clear();

		for(int i=0; i < this.slots.Length; i++)
			this.slots[i].Dispose();
	}

	private void Finish(LightSlot slot){
		ChunkPos pos = slot.pos;
		bool applied = slot.ApplyResults(this.loader);

		this.running.Remove(pos);
		slot.Release();

		// Requested again while running, so its data may have changed
		if(this.rerun.Remove(pos)){
			this.queue.Add(pos);
			return;
		}

		if(applied)
			OnLit(pos);
	}

	private void CalculateNow(ChunkPos pos){
		if(!this.loader.Contains(pos))
			return;

		Chunk c = this.loader.Get(pos);
		c.data.CalculateLightMap(c.metadata);

		OnLit(pos);
	}

	private void OnLit(ChunkPos pos){
		UpdateSunExit(pos);
		this.loader.OnLightReady(pos);
	}

	// Chunks waiting for the chunk above in this Update count as unlit too, since they were popped from the queue
	private bool IsAboveUnlit(ChunkPos pos){
		ChunkPos up = new ChunkPos(pos.x, pos.z, pos.y+1);

		return pos.y < Chunk.chunkMaxY && this.loader.Contains(up) && (!IsLit(up) || this.waitingAbove.Contains(up));
	}

	// Keeps where direct sunlight leaves the bottom of a lit chunk. If it changed, the chunk below is calculated again
	private void UpdateSunExit(ChunkPos pos){
		SunExitMask mask, previous;
		ChunkPos down;
		byte[] shadowMap;

		if(pos.y == 0 || !this.loader.Contains(pos))
			return;

		shadowMap = this.loader.Get(pos).data.GetShadowMap();

		if(shadowMap == null)
			return;

		mask = SunExitMask.FromShadowMap(shadowMap);

		if(!this.sunExits.TryGetValue(pos, out previous))
			previous = SunExitMask.ALL;

		if(mask == previous)
			return;

		this.sunExits[pos] = mask;
		down = new ChunkPos(pos.x, pos.z, pos.y-1);

		if(!this.loader.Contains(down))
			return;

		// Queued calculations read the new mask once they start
		if(this.running.ContainsKey(down))
			this.rerun.Add(down);
		else if(!this.queue.Contains(down))
			this.loader.AddToUpdate(down);
	}
}
