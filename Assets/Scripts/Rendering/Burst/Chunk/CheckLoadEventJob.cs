using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct CheckLoadEventJob : IJob{
	[ReadOnly]
	public NativeArray<ushort> blocks; // Padded snapshot
	[ReadOnly]
	public NativeArray<bool> blockLoad;
	[ReadOnly]
	public NativeArray<bool> objectLoad;

	public NativeList<int3> loadCoords;

	public void Execute(){
		ushort blockCode;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int z=0; z < Chunk.chunkWidth; z++){
				for(int y=0; y < Chunk.chunkDepth; y++){
					blockCode = blocks[PaddedChunk.Index(x, y, z)];

					if(blockCode <= ushort.MaxValue/2){
						if(blockLoad[blockCode]){
							loadCoords.Add(new int3(x, y, z));
						}
					}
					else{
						if(objectLoad[ushort.MaxValue-blockCode]){
							loadCoords.Add(new int3(x, y, z));
						}
					}
				}
			}
		}
	}
}
