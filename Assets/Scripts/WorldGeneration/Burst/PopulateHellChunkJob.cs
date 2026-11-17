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
    public NativeArray<byte> blendNoise;

    [ReadOnly]
    public ChunkPos pos;
    [ReadOnly]
    public BiomeNeighborhood biomes;

    [ReadOnly]
    public NativeArray<ushort> decorationBlock; // 0: Basalt, 1: Hell Marble, 2: Lava


    public void Execute(int index){
        int lavaLevel = 136;

        ApplySurfaceDecoration(index);

        // Lava is decided per chunk, so lava lakes don't get walls where a column without lava meets them
        if(HasLava())
            GenerateLava(lavaLevel, index);
    }

    // Every column is decorated by its own biome, which blends the biome borders (see BiomeBlend)
    private void ApplySurfaceDecoration(int index){
        int x = index;

        for(int z=0; z < Chunk.chunkWidth; z++){
            if((BiomeCode)BiomeBlend.GetColumnBiome(pos.x*Chunk.chunkWidth+x, pos.z*Chunk.chunkWidth+z, pos, in biomes, blendNoise) == BiomeCode.VOLCANIC_HIGHLANDS)
                DecorateVolcanicColumn(x, z);
        }
    }

    // Basalt or Hell Marble floor and ceiling
    private void DecorateVolcanicColumn(int x, int z){
        float basaltThreshold = -0.1f;
        bool isBasaltFloor;
        int depth = 0;
        int depthCeil = 0;

        isBasaltFloor = NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= basaltThreshold;

        // Ground
        for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
            if(depth <= 5){
                if(isBasaltFloor)
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[0];
                else
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[1];

                depth++;
            }

            if(depth > 5){
                break;
            }
        }

        // Ceiling
        for(int yC = (int)ceilingMap[x*(Chunk.chunkWidth+1)+z]; yC < Chunk.chunkDepth; yC++){
            if(depthCeil <= 5){
                if(isBasaltFloor)
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+yC*Chunk.chunkWidth+z] = this.decorationBlock[0];
                else
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+yC*Chunk.chunkWidth+z] = this.decorationBlock[1];

                depthCeil++;
            }

            if(depthCeil > 5){
                break;
            }
        }
    }

    // Deep Cliffs only have lava at their borders with other biomes
    private bool HasLava(){
        BiomeCode code = (BiomeCode)biomes.xz;

        if(code == BiomeCode.HELL_PLAINS || code == BiomeCode.LAVA_OCEAN || code == BiomeCode.BONE_VALLEY)
            return true;
        if(code == BiomeCode.DEEP_CLIFF)
            return !biomes.IsUniform();

        return false;
    }

    private void GenerateLava(int lavaLevel, int index){
        int x = index;
        int height;

        for(int z=0; z < Chunk.chunkWidth; z++){
            height = (int)heightMap[x*(Chunk.chunkWidth+1)+z];

            if(height <= lavaLevel){
                for(int y=height; y <= lavaLevel; y++){
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[2];
                }
            }
        }
    }
}
