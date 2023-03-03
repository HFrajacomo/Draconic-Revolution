using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile(FloatPrecision.High, FloatMode.Strict)]
public struct GenerateHellChunkJob: IJob{
    public ChunkPos pos;
    public NativeArray<ushort> blockData;
    public NativeArray<ushort> stateData;
    public NativeArray<ushort> hpData;
    public NativeArray<float> heightMap;
    public NativeArray<float> ceilingMap;
    public bool pregen;

    // Noises
    [ReadOnly]
    public NativeArray<byte> baseNoise;
    [ReadOnly]
    public NativeArray<byte> erosionNoise;
    [ReadOnly]
    public NativeArray<byte> peakNoise;
    [ReadOnly]
    public NativeArray<byte> caveNoise;
    [ReadOnly]
    public NativeArray<byte> cavemaskNoise;
    [ReadOnly]
    public NativeArray<byte> temperatureNoise;

    public void Execute(){
        int lavaLevel = 136;


        // Lower part
        GenerateHeightPivots();
        BilinearIntepolateMaps(heightMap, ceilingMap);
        ApplyMap(lavaLevel);
        AddBedrockLayer();
    }

    public void GenerateHeightPivots(){
        float height, ceilingHeight;
        float erosionMultiplier;
        float peakAdd, peakCeiling;
        float groundSpikiness, ceilingSpikiness;

        int baseCeilingHeight = Chunk.chunkDepth - 8;
        int ceilingBoost = 8;
        int hDiv = 15;


        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                height = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep1, baseNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep2, baseNoise))), NoiseMap.BASE, ChunkDepthID.HELL);
                ceilingHeight = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep3, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep3, baseNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep4, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep4, baseNoise))), NoiseMap.BASE, ChunkDepthID.HELL);
                erosionMultiplier = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep1, erosionNoise), NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep2, erosionNoise)), NoiseMap.EROSION, ChunkDepthID.HELL);
                peakAdd = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep5, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep5, peakNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep6, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep6, peakNoise))), NoiseMap.PEAK, ChunkDepthID.HELL);
                peakCeiling = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep3, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep3, peakNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep4, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep4, peakNoise))), NoiseMap.PEAK, ChunkDepthID.HELL);
                groundSpikiness = RemapIntervalGround(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.temperatureNoiseStep3, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.temperatureNoiseStep4, temperatureNoise));
                ceilingSpikiness = RemapIntervalCeiling(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.temperatureNoiseStep5, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.temperatureNoiseStep6, temperatureNoise));

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(BedrockClamp(Mathf.Clamp(Mathf.CeilToInt((height + (peakAdd * groundSpikiness)) * erosionMultiplier), 0, Chunk.chunkDepth)));
                ceilingMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.CeilToInt(baseCeilingHeight - ZeroClamp(((peakCeiling * ceilingSpikiness) + ceilingBoost + (ceilingHeight/hDiv)) * erosionMultiplier)));
            }
        }
    }

    public void BilinearIntepolateMaps(NativeArray<float> map1, NativeArray<float> map2){
        int xIndex, zIndex;
        float xInterp, zInterp;

        for(int x=0; x < Chunk.chunkWidth; x++){
            xIndex = x/4;
            xInterp = (x%4)*0.25f;

            for(int z=0; z < Chunk.chunkWidth; z++){
                zIndex = z/4;
                zInterp = (z%4)*0.25f;

                map1[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(map1[xIndex*4*(Chunk.chunkWidth+1)+zIndex*4]*(1-xInterp)*(1-zInterp) + map1[(xIndex+1)*4*(Chunk.chunkWidth+1)+zIndex*4]*(xInterp)*(1-zInterp) + map1[xIndex*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(1-xInterp)*(zInterp) + map1[(xIndex+1)*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(xInterp)*(zInterp)));
                map2[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(map2[xIndex*4*(Chunk.chunkWidth+1)+zIndex*4]*(1-xInterp)*(1-zInterp) + map2[(xIndex+1)*4*(Chunk.chunkWidth+1)+zIndex*4]*(xInterp)*(1-zInterp) + map2[xIndex*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(1-xInterp)*(zInterp) + map2[(xIndex+1)*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(xInterp)*(zInterp)));
            }
        }
    }

    public void ApplyMap(int lavaLevel){
        ushort lavaBlock = (ushort)BlockID.LAVA;
        ushort hellMarble = (ushort)BlockID.HELL_MARBLE;

        if(!pregen){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    for(int y=1; y < Chunk.chunkDepth; y++){ 
                        if(y >= heightMap[x*(Chunk.chunkWidth+1)+z]){
                            if(y < ceilingMap[x*(Chunk.chunkWidth+1)+z]){
                                if(y <= lavaLevel){
                                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = lavaBlock;
                                }
                                else
                                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                            }
                            else{
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = hellMarble;
                            }
                        } 
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = hellMarble;
                        }

                        stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                        hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;       
                    }
                }
            } 
        }
        else{
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    for(int y=1; y < Chunk.chunkDepth; y++){ 
                        if(y >= heightMap[x*(Chunk.chunkWidth+1)+z]){
                            if(y >= ceilingMap[x*(Chunk.chunkWidth+1)+z]){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = hellMarble;
                                stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;                                
                            }
                            else if(y <= lavaLevel && blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = lavaBlock;
                                stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;
                            }
                        } 
                        else if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = hellMarble;     
                            stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                            hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;
                        }
                    }
                }
            }             
        }
    }

    public void AddBedrockLayer(){
        ushort bedrock = (ushort)BlockID.STONE; // Needs to set it to bedrock once block is added
        int index;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                index = x*Chunk.chunkWidth*Chunk.chunkDepth+z;

                blockData[index] = bedrock;
                stateData[index] = 0;
                hpData[index] = ushort.MaxValue;
            }
        }
    }

    // Calculates the cumulative distribution function of a Normal Distribution
    private float TransformOctaves(float a, float b){
        float c = (a+b)/2f;

        return (2f/(1f + Mathf.Exp(-c*4.1f)))-1;
    }

    // Clamps negative numbers to 1
    private float ZeroClamp(float number){
        if(number <= 0)
            return 1;
        return number;
    }

    // Turns heights below 30 to 0
    private float BedrockClamp(float number){
        if(number <= 30)
            return 0;
        return number;
    }

    // Remaps -1 to 1 interval to 0.35 to 1
    private float RemapIntervalGround(float number){
        return Mathf.Lerp(0.35f, 1f, (number+1)/2);
    }

    // Remaps -1 to 1 interval to 0.7 to 1
    private float RemapIntervalCeiling(float number){
        return Mathf.Lerp(0.7f, 1f, (number+1)/2);
    }
}