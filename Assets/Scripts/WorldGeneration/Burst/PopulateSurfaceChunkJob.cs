using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile]
public struct PopulateSurfaceChunkJob : IJob{
    public NativeArray<ushort> blockData;
    [ReadOnly]
    public NativeArray<float> heightMap;
    [ReadOnly]
    public NativeArray<byte> patchNoise;
    [ReadOnly]
    public NativeArray<byte> blendNoise;

    [ReadOnly]
    public ChunkPos pos;
    [ReadOnly]
    public BiomeNeighborhood biomes;

    [ReadOnly]
    public NativeArray<ushort> decorationBlock; // 0: Water, 1: Grass, 2: Dirt, 3: Stone, 4: Sand, 5: Sandstone, 6: Ice, 7: Snow, 8: Clay 

    public void Execute(){
        ApplySurfaceDecoration();
        ApplyWaterBodyFloor();
    }

    // Every column is decorated by its own biome, which blends the biome borders (see BiomeBlend)
    public void ApplySurfaceDecoration(){
        byte columnBiome;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                columnBiome = BiomeBlend.GetColumnBiome(pos.x*Chunk.chunkWidth+x, pos.z*Chunk.chunkWidth+z, pos, in biomes, blendNoise);
                DecorateColumn(x, z, (BiomeCode)columnBiome);
            }
        }
    }

    private void DecorateColumn(int x, int z, BiomeCode code){
        switch(code){
            case BiomeCode.PLAINS:
            case BiomeCode.FOREST:
                DecorateGrass(x, z, false);
                return;
            case BiomeCode.GRASSY_HIGHLANDS:
                DecorateGrass(x, z, GetPatchNoise(x, z) >= 0.1f);
                return;
            case BiomeCode.OCEAN:
                DecorateOcean(x, z);
                return;
            case BiomeCode.DESERT:
                DecorateDesert(x, z);
                return;
            case BiomeCode.SNOWY_PLAINS:
            case BiomeCode.SNOWY_FOREST:
                DecorateSnow(x, z, GetPatchNoise(x, z) >= -0.2f, false);
                return;
            case BiomeCode.ICE_OCEAN:
                DecorateSnow(x, z, GetPatchNoise(x, z) >= 0f, false);
                return;
            case BiomeCode.SNOWY_HIGHLANDS:
                float pNoise = GetPatchNoise(x, z);
                DecorateSnow(x, z, pNoise >= -0.2f, pNoise >= 0.1f);
                return;
            default:
                return;
        }
    }

    private float GetPatchNoise(int x, int z){
        return NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise);
    }

    // Grass on top of Dirt. Stone floors replace both
    private void DecorateGrass(int x, int z, bool isStoneFloor){
        ushort blockCode;
        int depth = 0;

        for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
            blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

            if(blockCode == this.decorationBlock[0])
                depth++;
            else if(isStoneFloor && depth < 5){
                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[3];
                depth++;
            }
            else if(depth == 0){
                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[1];
                depth++;
            }
            else{
                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[2];
                depth++;
            }

            if(depth == 5){
                break;
            }
        }
    }

    private void DecorateOcean(int x, int z){
        ushort blockCode;
        int depth = 0;

        for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
            blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

            if(blockCode == this.decorationBlock[0])
                depth++;
            else{
                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[4];
                depth++; 
            }

            if(depth == 5){
                break;
            }
        }
    }

    // Sand on top of Sandstone
    private void DecorateDesert(int x, int z){
        ushort blockCode;
        int depth = 0;

        for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
            blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

            if(blockCode == this.decorationBlock[0])
                depth++;
            else{
                if(depth != 5)
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[4];
                else
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[5];

                depth++; 
            }

            if(depth == 6){
                break;
            }
        }
    }

    // Snow, with Ice over underwater floors and optional Stone floors
    private void DecorateSnow(int x, int z, bool isIceFloor, bool isStoneFloor){
        ushort blockCode;
        int depth = 0;

        for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
            blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

            if(blockCode == this.decorationBlock[0]){
                depth++;
            }
            else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                if(isIceFloor)
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = this.decorationBlock[6];
                break;
            }
            else if(isStoneFloor && depth < 5){
                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[3];
                depth++;
            }
            else{
                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[7];
                depth++;
            }

            if(depth == 5){
                break;
            }
        }
    }

    public void ApplyWaterBodyFloor(){
        int depth = 0;
        int finalDepth = 5;
        ushort blockCode;
        int height;
        bool hasClay;
        float clayNoise;
        float clayThreshold = 0.42f;
        float clayThreshold2 = -0.2f;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                hasClay = false;

                depth = 0;
                height = (int)heightMap[x*(Chunk.chunkWidth+1)+z];
                clayNoise = NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep1, patchNoise);

                if(clayNoise >= clayThreshold)
                    hasClay = true;

                blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+height*Chunk.chunkWidth+z];

                if(blockCode != this.decorationBlock[0])
                    continue;
                else{
                    for(int y=height-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode != this.decorationBlock[0]){
                            depth++;
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[4];

                            if(hasClay && y >= Constants.WORLD_CLAY_MIN_LEVEL && y <= Constants.WORLD_CLAY_MAX_LEVEL){
                                if(NoiseMaker.PatchNoise1D((x ^ z)*y*GenerationSeed.patchNoiseStep4, patchNoise) >= clayThreshold2){
                                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.decorationBlock[8];
                                }
                            }
                        }

                        if(depth == finalDepth)
                            break;
                    }
                }
            }
        }          
    }
}
