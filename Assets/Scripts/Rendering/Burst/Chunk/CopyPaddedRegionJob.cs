using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

/*
Copies the voxels of a chunk that touch the padded snapshot of another chunk (see PaddedChunk).
offset is the position of the source chunk relative to the snapshot chunk, being -1, 0 or 1 in each axis.
Runs synchronously while the source arrays are pinned
*/
[BurstCompile]
public unsafe struct CopyPaddedRegionJob : IJob{
	[NativeDisableUnsafePtrRestriction]
	public ushort* srcBlocks;
	[NativeDisableUnsafePtrRestriction]
	public byte* srcLight;
	[NativeDisableUnsafePtrRestriction]
	public byte* srcHeight; // Only read when the source is in the same layer
	[ReadOnly]
	public int3 offset;

	public NativeArray<ushort> blocks;
	public NativeArray<byte> light;
	public NativeArray<byte> heights;

	public void Execute(){
		int xStart, xEnd, yStart, yEnd, zStart, zEnd;
		int dx = offset.x * Chunk.chunkWidth;
		int dy = offset.y * Chunk.chunkDepth;
		int dz = offset.z * Chunk.chunkWidth;
		int srcIndex, dstIndex;

		GetRange(offset.x, Chunk.chunkWidth, out xStart, out xEnd);
		GetRange(offset.y, Chunk.chunkDepth, out yStart, out yEnd);
		GetRange(offset.z, Chunk.chunkWidth, out zStart, out zEnd);

		for(int x=xStart; x < xEnd; x++){
			for(int y=yStart; y < yEnd; y++){
				srcIndex = x*Chunk.chunkWidth*Chunk.chunkDepth + y*Chunk.chunkWidth;
				dstIndex = PaddedChunk.Index(x+dx, y+dy, dz);

				for(int z=zStart; z < zEnd; z++){
					blocks[dstIndex+z] = srcBlocks[srcIndex+z];
					light[dstIndex+z] = srcLight[srcIndex+z];
				}
			}
		}

		if(offset.y != 0 || srcHeight == null)
			return;

		for(int x=xStart; x < xEnd; x++){
			for(int z=zStart; z < zEnd; z++){
				heights[PaddedChunk.MapIndex(x+dx, z+dz)] = srcHeight[x*Chunk.chunkWidth+z];
			}
		}
	}

	// Range [start, end) of the source chunk that is copied in a given axis
	private void GetRange(int axisOffset, int size, out int start, out int end){
		if(axisOffset < 0){
			start = size-1;
			end = size;
		}
		else if(axisOffset > 0){
			start = 0;
			end = 1;
		}
		else{
			start = 0;
			end = size;
		}
	}
}
