using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

/*
Persistent native buffers of one chunk generation.
The WorldGenerator schedules the generation jobs of a chunk into a slot and later finishes it, copying the result into the chunk
*/
public class GenerationSlot{
	private static readonly int VOXEL_COUNT = Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth;
	private static readonly int MAP_SIZE = (Chunk.chunkWidth+1)*(Chunk.chunkWidth+1);

	public NativeArray<ushort> voxels;
	public NativeArray<ushort> state;
	public NativeArray<ushort> hp;
	public NativeArray<float> heightMap;
	public NativeArray<float> auxMap; // Ceiling Map in Hell and Bottom Map in Core

	public JobHandle handle;
	public Chunk chunk;
	public byte biome;
	public bool isActive = false;


	public GenerationSlot(){
		this.voxels = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.state = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.hp = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.heightMap = new NativeArray<float>(MAP_SIZE, Allocator.Persistent);
		this.auxMap = new NativeArray<float>(MAP_SIZE, Allocator.Persistent);
	}

	// Prepares the slot to generate a chunk. Pre-generated chunks keep their data, so generation only fills the empty voxels
	public void Begin(Chunk target, bool isPregen){
		this.chunk = target;
		this.isActive = true;

		if(isPregen){
			this.voxels.CopyFrom(target.data.GetData());
			this.state.CopyFrom(target.metadata.GetStateData());
			this.hp.CopyFrom(target.metadata.GetHPData());
		}
		else{
			Clear(this.voxels);
			Clear(this.state);
			Clear(this.hp);
		}

		Clear(this.heightMap);
		Clear(this.auxMap);
	}

	// Waits for the generation jobs and copies the generated voxels into the chunk arrays
	public void CompleteInto(ushort[] blocks, ushort[] states, ushort[] hps, float[] managedHeightMap){
		this.handle.Complete();

		this.voxels.CopyTo(blocks);
		this.state.CopyTo(states);
		this.hp.CopyTo(hps);
		this.heightMap.CopyTo(managedHeightMap);
	}

	public void Release(){
		this.handle = default(JobHandle);
		this.chunk = null;
		this.isActive = false;
	}

	public void Dispose(){
		this.handle.Complete();

		this.voxels.Dispose();
		this.state.Dispose();
		this.hp.Dispose();
		this.heightMap.Dispose();
		this.auxMap.Dispose();
	}

	private static unsafe void Clear<T>(NativeArray<T> array) where T : struct{
		UnsafeUtility.MemClear(array.GetUnsafePtr(), (long)array.Length * UnsafeUtility.SizeOf<T>());
	}
}
