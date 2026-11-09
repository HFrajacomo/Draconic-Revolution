using System.Collections.Generic;
using Unity.Jobs;

/*
Calculates the light of loaded chunks in worker threads, nearest to the player first.
Every chunk has at most one calculation queued or running. Chunk data is read when a calculation starts and written back once it ends.
Anything that needs the light of a chunk right away calls EnsureLit, which finishes its calculation in the main thread
*/
public class LightingSystem{
	private ChunkLoader loader;
	private LightSlot[] slots;
	private ChunkPriorityQueue queue = new ChunkPriorityQueue(DistanceMetric.EDGE_LIMITING, edgeLimit:World.renderDistance);
	private HashSet<ChunkPos> updateBelow = new HashSet<ChunkPos>(); // Queued chunks that rebuild the chunk below once lit
	private Dictionary<ChunkPos, LightSlot> running = new Dictionary<ChunkPos, LightSlot>();
	private HashSet<ChunkPos> rerun = new HashSet<ChunkPos>(); // Running chunks requested again, which are calculated again once done


	public LightingSystem(ChunkLoader loader){
		this.loader = loader;
		this.slots = new LightSlot[Constants.CLIENT_LIGHT_SLOTS];

		for(int i=0; i < this.slots.Length; i++)
			this.slots[i] = new LightSlot();
	}

	public void SetPlayerPosition(ChunkPos pos){
		this.queue.SetPlayerPosition(pos);
	}

	// Queues the light calculation of a chunk. updateBelow rebuilds the chunk below once done
	public void Enqueue(ChunkPos pos, bool updateBelow){
		if(updateBelow)
			this.updateBelow.Add(pos);

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

	// Applies finished calculations and starts queued ones
	public void Update(){
		LightSlot slot;

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
				ChunkPos pos = this.queue.Pop();

				if(slot.Start(pos, this.updateBelow.Remove(pos), this.loader)){
					this.running.Add(pos, slot);
					break;
				}
			}

			if(this.queue.GetSize() == 0)
				break;
		}

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
			CalculateNow(pos, this.updateBelow.Remove(pos));
		}
	}

	// Calculates the light of a chunk right away, discarding queued or running calculations of it
	public void RecalcNow(ChunkPos pos){
		LightSlot slot;
		bool below = this.updateBelow.Remove(pos);

		if(this.running.TryGetValue(pos, out slot)){
			slot.Complete();
			this.running.Remove(pos);
			slot.Release();
		}

		this.rerun.Remove(pos);
		this.queue.Remove(pos);

		CalculateNow(pos, below);
	}

	// Forgets the light calculation of an unloaded chunk
	public void Cancel(ChunkPos pos){
		LightSlot slot;

		this.queue.Remove(pos);
		this.updateBelow.Remove(pos);
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
		this.updateBelow.Clear();
		this.running.Clear();
		this.rerun.Clear();
	}

	public void Dispose(){
		Clear();

		for(int i=0; i < this.slots.Length; i++)
			this.slots[i].Dispose();
	}

	private void Finish(LightSlot slot){
		ChunkPos pos = slot.pos;
		bool below = slot.updateBelow;
		bool applied = slot.ApplyResults(this.loader);

		this.running.Remove(pos);
		slot.Release();

		// Requested again while running, so its data may have changed
		if(this.rerun.Remove(pos)){
			this.queue.Add(pos);

			if(below)
				this.updateBelow.Add(pos);

			return;
		}

		if(applied)
			OnLit(pos, below);
	}

	private void CalculateNow(ChunkPos pos, bool below){
		if(!this.loader.Contains(pos))
			return;

		Chunk c = this.loader.Get(pos);
		c.data.CalculateLightMap(c.metadata);

		OnLit(pos, below);
	}

	private void OnLit(ChunkPos pos, bool below){
		ChunkPos down = new ChunkPos(pos.x, pos.z, pos.y-1);

		if(below && pos.y > 0 && this.loader.Contains(down))
			this.loader.AddToUpdate(down);

		this.loader.OnLightReady(pos);
	}
}
