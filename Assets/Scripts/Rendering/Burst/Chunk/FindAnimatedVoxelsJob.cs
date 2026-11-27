using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

/*
Finds the voxels of a chunk that hold Animated Objects, writing their voxel indices (x*chunkWidth*chunkDepth + y*chunkWidth + z).
Runs synchronously while the block array is pinned
*/
[BurstCompile]
public unsafe struct FindAnimatedVoxelsJob : IJob{
	[NativeDisableUnsafePtrRestriction]
	public ushort* blocks;
	[ReadOnly]
	public NativeArray<ShaderIndex> objectMaterial;

	public NativeList<int> voxels;

	public void Execute(){
		int voxelCount = Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth;
		int objectCount = objectMaterial.Length;
		ushort code;
		int obj;

		for(int i=0; i < voxelCount; i++){
			code = blocks[i];

			if(code <= ushort.MaxValue/2)
				continue;

			obj = ushort.MaxValue - code;

			if(obj < objectCount && objectMaterial[obj] == ShaderIndex.ANIMATED_OBJECTS)
				voxels.Add(i);
		}
	}
}
