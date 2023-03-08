using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile]
public struct PopulateHellChunkJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockData;
    [ReadOnly]
    public NativeArray<float> heightMap;
    [ReadOnly]
    public NativeArray<float> ceilingMap;
    [ReadOnly]
    public NativeArray<byte> patchNoise;
    [ReadOnly]
    public NativeArray<ushort> blendingBlock;

    [ReadOnly]
    public ChunkPos pos;
    [ReadOnly]
    public byte biome;
    [ReadOnly]
    public byte xmBiome, xpBiome, zpBiome, zmBiome;
    [ReadOnly]
    public byte xmzmBiome, xmzpBiome, xpzmBiome, xpzpBiome;

    public void Execute(int index){
        int lavaLevel = 136;

        ApplySurfaceDecoration(biome, lavaLevel, index);
        ApplyBiomeBlending(biome, lavaLevel, index);
    }

    private void ApplySurfaceDecoration(byte biome, int lavaLevel, int index){
        BiomeCode code = (BiomeCode)biome;
        ushort blockCode;
        int depth = 0;
        int depthCeil = 0;
        int x = index;

        if(code == BiomeCode.HELL_PLAINS){
            GenerateLava(lavaLevel, index);
        }
        else if(code == BiomeCode.DEEP_CLIFF){
            return;
        }
        else if(code == BiomeCode.LAVA_OCEAN){
            GenerateLava(lavaLevel, index);
        }
        else if(code == BiomeCode.HELL_HIGHLANDS){
            return;
        }
        else if(code == BiomeCode.BONE_VALLEY){
            GenerateLava(lavaLevel, index);
        }
        else if(code == BiomeCode.VOLCANIC_HIGHLANDS){
            float basaltThreshold = -0.1f;
            bool isBasaltFloor = false;

            for(int z=0; z < Chunk.chunkWidth; z++){
                depth = 0;
                depthCeil = 0;

                if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= basaltThreshold)
                    isBasaltFloor = true;

                // Ground
                for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                    blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                    if(depth <= 5){
                        if(isBasaltFloor)
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.BASALT;
                        else
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.HELL_MARBLE;

                        depth++;
                    }

                    if(depth > 5){
                        break;
                    }
                }

                // Ceiling
                for(int yC = (int)ceilingMap[x*(Chunk.chunkWidth+1)+z]; yC < Chunk.chunkDepth; yC++){
                    blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+yC*Chunk.chunkWidth+z];

                    if(depthCeil <= 5){
                        if(isBasaltFloor)
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+yC*Chunk.chunkWidth+z] = (ushort)BlockID.BASALT;
                        else
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+yC*Chunk.chunkWidth+z] = (ushort)BlockID.HELL_MARBLE;

                        depthCeil++;
                    }

                    if(depthCeil > 5){
                        break;
                    }
                }
            }
        }
    }

    private void ApplyBiomeBlending(byte biome, int lavaLevel, int index){
        BiomeCode code = (BiomeCode)biome;

        if(code == BiomeCode.DEEP_CLIFF){
            if(biome != xmBiome || biome != xpBiome || biome != zmBiome || biome != zpBiome || biome != xmzmBiome || biome != xmzpBiome || biome != xpzmBiome || biome != xpzpBiome){
                GenerateLava(lavaLevel, index);
            }
        }
        else{
            return;
        }
    }

    private void GenerateLava(int lavaLevel, int index){
        int x = index;
        int height;

        for(int z=0; z < Chunk.chunkWidth; z++){
            height = (int)heightMap[x*(Chunk.chunkWidth+1)+z];

            if(height <= lavaLevel){
                for(int y=height; y <= lavaLevel; y++){
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.LAVA;
                }
            }
        }
    }
}