using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

/*
Client only. Keeps the Animated Objects of every chunk (ChunkAnimatedObjects) in sync with the voxels, builds their meshes and uploads their slots.
Animated Objects are referenced by chunk and in-chunk position. Owned by the ChunkLoader
*/
public class AnimatedObjectHandler{
	private ChunkLoader cl;
	private AnimatedMeshBuildSlot[] buildSlots;
	private Queue<ChunkAnimatedObjects> buildQueue = new Queue<ChunkAnimatedObjects>();
	private List<ChunkAnimatedObjects> uploadList = new List<ChunkAnimatedObjects>();
	private NativeList<int> foundVoxels;
	private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
	private bool isDisposed = false;


	public AnimatedObjectHandler(ChunkLoader cl){
		this.cl = cl;
		this.buildSlots = new AnimatedMeshBuildSlot[Constants.CLIENT_ANIMATED_BUILD_SLOTS];
		this.foundVoxels = new NativeList<int>(Allocator.Persistent);

		for(int i=0; i < this.buildSlots.Length; i++)
			this.buildSlots[i] = new AnimatedMeshBuildSlot();
	}

	// Registers the Animated Objects of a chunk received from the Server. Received again, Objects that are gone are removed
	public void OnChunkLoaded(Chunk chunk){
		ChunkAnimatedObjects animated;
		int x, y, z;

		FindAnimatedVoxels(chunk);

		if(this.foundVoxels.Length == 0){
			if(chunk.animated != null)
				chunk.animated.RemoveMissing(this.foundVoxels);
			return;
		}

		animated = GetOrCreate(chunk);

		if(animated.Count() > 0)
			animated.RemoveMissing(this.foundVoxels);

		for(int i=0; i < this.foundVoxels.Length; i++){
			GetCoords(this.foundVoxels[i], out x, out y, out z);
			animated.Set(x, y, z, chunk.data.GetCell(x, y, z), chunk.metadata.GetState(x, y, z), SampleLight(chunk.pos, x, y, z));
		}
	}

	// Must be called whenever a voxel of a loaded chunk is changed by the Server
	public void OnVoxelChanged(Chunk chunk, int x, int y, int z, ushort code, ushort state){
		if(VoxelLoader.IsAnimatedObject(code))
			GetOrCreate(chunk).Set(x, y, z, code, state, SampleLight(chunk.pos, x, y, z));
		else if(chunk.animated != null)
			chunk.animated.Remove(x, y, z);
	}

	// Plays an animation of the Animated Object of a voxel that changed to state. animation is its index in the Object's model identity
	public void Play(Chunk chunk, int x, int y, int z, ushort code, ushort state, int animation, bool continueNormalized){
		ChunkAnimatedObjects animated;

		if(!VoxelLoader.IsAnimatedObject(code))
			return;

		animated = GetOrCreate(chunk);

		if(!animated.Contains(x, y, z))
			animated.Set(x, y, z, code, state, SampleLight(chunk.pos, x, y, z));

		animated.Play(x, y, z, state, animation, continueNormalized);
	}

	public void RequestBuild(ChunkAnimatedObjects animated){
		if(animated.queuedBuild)
			return;

		animated.queuedBuild = true;
		this.buildQueue.Enqueue(animated);
	}

	public void RequestUpload(ChunkAnimatedObjects animated){
		if(animated.queuedUpload)
			return;

		animated.queuedUpload = true;
		this.uploadList.Add(animated);
	}

	public void Update(){
		FinishBuilds();
		ScheduleBuilds();
		Upload();
	}

	public void Dispose(){
		if(this.isDisposed)
			return;

		for(int i=0; i < this.buildSlots.Length; i++)
			this.buildSlots[i].Dispose();

		this.foundVoxels.Dispose();
		this.buildQueue.Clear();
		this.uploadList.Clear();
		this.isDisposed = true;
	}

	// Applies finished builds until the frame budget is spent (at least one per frame)
	private void FinishBuilds(){
		long budget = (long)(Constants.CLIENT_ANIMATED_FINISH_BUDGET_MS * System.Diagnostics.Stopwatch.Frequency / 1000f);
		bool appliedAny = false;

		this.stopwatch.Restart();

		for(int i=0; i < this.buildSlots.Length; i++){
			if(!this.buildSlots[i].IsDone())
				continue;
			if(appliedAny && this.stopwatch.ElapsedTicks >= budget)
				continue;

			this.buildSlots[i].Apply();
			appliedAny = true;
		}
	}

	// Chunks changed while building are queued again and wait for their build to end
	private void ScheduleBuilds(){
		int attempts = this.buildQueue.Count;
		ChunkAnimatedObjects animated;

		for(int i=0; i < this.buildSlots.Length; i++){
			if(!this.buildSlots[i].IsFree())
				continue;

			while(attempts > 0 && this.buildQueue.Count > 0){
				attempts--;
				animated = this.buildQueue.Dequeue();
				animated.queuedBuild = false;

				if(animated.IsDestroyed() || !animated.IsMeshDirty())
					continue;

				if(animated.IsBuilding()){
					RequestBuild(animated);
					continue;
				}

				this.buildSlots[i].Schedule(animated);
				break;
			}
		}
	}

	private void Upload(){
		for(int i=0; i < this.uploadList.Count; i++){
			this.uploadList[i].queuedUpload = false;

			if(!this.uploadList[i].IsDestroyed())
				this.uploadList[i].Upload();
		}

		this.uploadList.Clear();
	}

	private ChunkAnimatedObjects GetOrCreate(Chunk chunk){
		if(chunk.animated == null)
			chunk.animated = new ChunkAnimatedObjects(chunk, this);

		return chunk.animated;
	}

	private unsafe void FindAnimatedVoxels(Chunk chunk){
		ushort[] blocks = chunk.data.GetData();

		this.foundVoxels.Clear();

		if(blocks == null)
			return;

		fixed(ushort* blockPtr = blocks){
			FindAnimatedVoxelsJob job = new FindAnimatedVoxelsJob{
				blocks = blockPtr,
				objectMaterial = BlockEncyclopediaECS.objectMaterial,
				voxels = this.foundVoxels
			};

			job.Run();
		}
	}

	// Light of the voxel if it's already calculated. Otherwise the next static build of the chunk gives it (ChunkBuildSlot.ApplyMeshes)
	private int SampleLight(ChunkPos pos, int x, int y, int z){
		CastCoord coord = new CastCoord(pos, x, y, z);
		int natural = this.cl.GetLight(coord);
		int extra = this.cl.GetLight(coord, isNatural:false);

		if(natural < 0 || extra < 0)
			return 0;

		return AnimatedSlot.PackLight(natural, extra, 0);
	}

	private static void GetCoords(int index, out int x, out int y, out int z){
		x = index / (Chunk.chunkWidth*Chunk.chunkDepth);
		y = (index / Chunk.chunkWidth) % Chunk.chunkDepth;
		z = index % Chunk.chunkWidth;
	}
}
