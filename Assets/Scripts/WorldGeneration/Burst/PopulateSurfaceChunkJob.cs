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
    public NativeArray<ushort> blendingBlock;

    [ReadOnly]
    public ChunkPos pos;
    [ReadOnly]
    public byte biome;
    [ReadOnly]
    public byte xpBiome, zpBiome, zmBiome;
    [ReadOnly]
    public byte xzpBiome, xpzmBiome, xmzpBiome;

    public void Execute(){
        ApplySurfaceDecoration(biome);
        ApplyBiomeBlending();
        ApplyWaterBodyFloor();
    }

    public void ApplySurfaceDecoration(byte biome){
        BiomeCode code = (BiomeCode)biome;
        ushort blockCode;
        int depth = 0;

        if(code == BiomeCode.PLAINS){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else if(depth == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.GRASS;
                            depth++;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.DIRT;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }
        }
        else if(code == BiomeCode.GRASSY_HIGHLANDS){
            float stoneThreshold = 0.1f;
            bool isStoneFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    isStoneFloor = false;
                    depth = 0;

                    if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= stoneThreshold)
                        isStoneFloor = true;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else if(isStoneFloor && depth < 5){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.STONE;
                            depth++;
                        } 
                        else if(depth == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.GRASS;
                            depth++;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.DIRT;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }       
        }
        else if(code == BiomeCode.OCEAN){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SAND;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }             
        }
        else if(code == BiomeCode.FOREST){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else if(depth == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.GRASS;
                            depth++;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.DIRT;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }         
        }
        else if(code == BiomeCode.DESERT){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else{
                            if(depth != 5)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SAND;
                            else
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SANDSTONE;

                            depth++; 
                        }

                        if(depth == 6){
                            break;
                        }
                    }
                }
            }          
        }
        else if(code == BiomeCode.SNOWY_PLAINS){
            float iceThreshold = -0.2f;
            bool isIceFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    isIceFloor = false;

                    if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= iceThreshold){
                        isIceFloor = true;
                    }

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }              
        }
        else if(code == BiomeCode.SNOWY_HIGHLANDS){
            float stoneThreshold = 0.1f;
            bool isStoneFloor = false;
            float iceThreshold = -0.2f;
            bool isIceFloor = false;
            float pNoise;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    isStoneFloor = false;
                    isIceFloor = false;
                    depth = 0;

                    pNoise = NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise);
                    isStoneFloor = pNoise >= stoneThreshold;
                    isIceFloor = pNoise >= iceThreshold;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else if(isStoneFloor && depth < 5){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.STONE;
                            depth++;
                        } 
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }
        }
        else if(code == BiomeCode.ICE_OCEAN){
            float iceThreshold = 0f;
            bool isIceFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    isIceFloor = false;

                    if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= iceThreshold)
                        isIceFloor = true;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }        
        }
        else if(code == BiomeCode.SNOWY_FOREST){
            float iceThreshold = -0.2f;
            bool isIceFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    isIceFloor = false;

                    if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) >= iceThreshold)
                        isIceFloor = true;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
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

                if(blockCode != (ushort)BlockID.WATER)
                    continue;
                else{
                    for(int y=height-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode != (ushort)BlockID.WATER){
                            depth++;
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SAND;

                            if(hasClay && y >= Constants.WORLD_CLAY_MIN_LEVEL && y <= Constants.WORLD_CLAY_MAX_LEVEL){
                                if(NoiseMaker.PatchNoise1D((x ^ z)*y*GenerationSeed.patchNoiseStep4, patchNoise) >= clayThreshold2){
                                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.CLAY;
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

    public void ApplyBiomeBlending(){
        int blendingAmount = Chunk.chunkWidth - 3;
        int exageratedBlendingAmount = blendingAmount - (Chunk.chunkWidth - blendingAmount);
        float blendingSafety = 0f;

        if(biome == xpBiome && biome == zpBiome)
            return;
        
        int y;

        // X+ Side
        if(blendingBlock[biome] != blendingBlock[xpBiome]){
            // If needs rounded borders at the top
            if(biome != xpBiome && xpBiome != xzpBiome){
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(x-z > 0 && NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }                
            }
            // If needs rounded borders at the bottom
            else if(biome != xpBiome && xpBiome != xpzmBiome){
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(x+z >= Chunk.chunkWidth && NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }
            }
            // Both edges are rounded
            else if(biome != xpBiome && xpBiome != xzpBiome && xpBiome != xpzmBiome){
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(x+z <= blendingAmount*2 && x-z <= blendingAmount && NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }
            }

            // "Straight" line through border
            else{
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }                
            }
        }

        // Z+ Side
        if(blendingBlock[biome] != blendingBlock[zpBiome]){
            // If needs rounded borders at the right
            if(biome != zpBiome && zpBiome != xzpBiome){
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=0; x < Chunk.chunkWidth; x++){
                        if(x-z <= blendingAmount - Chunk.chunkWidth && NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }                
            }
            // If needs rounded borders at the left
            else if(biome != zpBiome && zpBiome != xmzpBiome){
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=0; x < Chunk.chunkWidth; x++){
                        if(x+z >= Chunk.chunkWidth-1 && NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }
            }
            // Both ends are rounded
            else if(biome != zpBiome && zpBiome != xzpBiome && zpBiome != xmzpBiome){
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=0; x < Chunk.chunkWidth; x++){
                        if(x-z <= blendingAmount - Chunk.chunkWidth && x+z >= Chunk.chunkWidth-1 && NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }                
            }
            // "Straight" line through border
            else{
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=Chunk.chunkWidth-1; x >= 0; x--){
                        if(NoiseMaker.PatchNoise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2, patchNoise) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }               
            }
        }

    }
}