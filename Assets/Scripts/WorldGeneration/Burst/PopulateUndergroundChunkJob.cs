using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile]
public struct PopulateUndergroundChunkJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockData;
    [ReadOnly]
    public NativeArray<float> heightMap;
    [ReadOnly]
    public NativeArray<byte> patchNoise;

    [ReadOnly]
    public ChunkPos pos;
    [ReadOnly]
    public byte biome;

    public void Execute(int index){
        ApplySurfaceDecoration(index, biome);
    }

    private void ApplySurfaceDecoration(int x, byte biome){
        if((BiomeCode)biome == BiomeCode.CAVERNS){
            return;
        }
        else if((BiomeCode)biome == BiomeCode.BASALT_CAVES){
            float basaltThreshold = -0.33f;

            for(int z=0; z < Chunk.chunkWidth; z++){
                for(int y=(int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                    if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == (ushort)BlockID.STONE){
                        if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2 + (pos.y*Chunk.chunkDepth+y)*GenerationSeed.patchNoiseStep3, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= basaltThreshold){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.BASALT;
                        }
                    }
                }
            }
        }
        else if((BiomeCode)biome == BiomeCode.UNDERWATER_CAVES){
            int waterLevel = 200;

            for(int z=0; z < Chunk.chunkWidth; z++){
                for(int y=Chunk.chunkDepth-1; y > 0; y--){
                    if(y > waterLevel)
                        continue;

                    if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0)
                        blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.WATER;
                }
            }            
        }
        else if((BiomeCode)biome == BiomeCode.ICE_CAVES){
            float snowThreshold = -0.33f;
            float minIce = 0.1f;
            float maxIce = 0.2f;
            float val;

            bool topBlock;
            bool bottomBlock;

            for(int z=0; z < Chunk.chunkWidth; z++){
                for(int y=(int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                    if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == (ushort)BlockID.STONE){

                        if(y < Chunk.chunkDepth-1)
                            topBlock = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z] == 0;
                        else
                            topBlock = false;

                        if(y > 0)
                            bottomBlock = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z] == 0;
                        else
                            bottomBlock = false;
                

                        val = NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2 + (pos.y*Chunk.chunkDepth+y)*GenerationSeed.patchNoiseStep3, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise);

                        if(!topBlock && !bottomBlock)
                            continue;

                        if(val >= snowThreshold){
                            if(val >= minIce && val <= maxIce){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            }
                            else{
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            }
                        }
                    }
                }
            }
        }
    }
}
