using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile(FloatPrecision.High, FloatMode.Strict)]
public struct GenerateCoreChunkJob: IJob{
    public ChunkPos pos;

    public NativeArray<ushort> blockData;
    public NativeArray<ushort> stateData;
    public NativeArray<ushort> hpData;
    public NativeArray<float> heightMap;
    public NativeArray<float> bottomMap;
    public bool pregen;

    // Noises
    [ReadOnly]
    public NativeArray<byte> baseNoise;
    [ReadOnly]
    public NativeArray<byte> erosionNoise;
    [ReadOnly]
    public NativeArray<byte> peakNoise;
    [ReadOnly]
    public ushort moonstoneBlockID;
    [ReadOnly]
    public ushort acasterBlockID;

    public void Execute(){
        GenerateHeightPivots();
        BilinearIntepolateMaps();
        ApplyMap();
        AddAcasterLayer();
    }

    public void GenerateHeightPivots(){
        float height;
        float erosionMultiplier;
        float bottomPeak;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                height = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep5, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep5, baseNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep6, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep6, baseNoise))), NoiseMap.BASE, ChunkDepthID.CORE);
                erosionMultiplier = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep1, erosionNoise), NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep2, erosionNoise)), NoiseMap.EROSION, ChunkDepthID.CORE);
                bottomPeak = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep7, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep7, peakNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep8, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep8, peakNoise))), NoiseMap.PEAK, ChunkDepthID.CORE);
            
                heightMap[x*(Chunk.chunkWidth+1)+z] = Mathf.CeilToInt(height * erosionMultiplier);
                bottomMap[x*(Chunk.chunkWidth+1)+z] = Mathf.CeilToInt(bottomPeak);
            }
        }
    }

    public void BilinearIntepolateMaps(){
        int xIndex, zIndex;
        float xInterp, zInterp;

        for(int x=0; x < Chunk.chunkWidth; x++){
            xIndex = x/4;
            xInterp = (x%4)*0.25f;

            for(int z=0; z < Chunk.chunkWidth; z++){
                zIndex = z/4;
                zInterp = (z%4)*0.25f;

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(heightMap[xIndex*4*(Chunk.chunkWidth+1)+zIndex*4]*(1-xInterp)*(1-zInterp) + heightMap[(xIndex+1)*4*(Chunk.chunkWidth+1)+zIndex*4]*(xInterp)*(1-zInterp) + heightMap[xIndex*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(1-xInterp)*(zInterp) + heightMap[(xIndex+1)*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(xInterp)*(zInterp)));
                bottomMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(bottomMap[xIndex*4*(Chunk.chunkWidth+1)+zIndex*4]*(1-xInterp)*(1-zInterp) + bottomMap[(xIndex+1)*4*(Chunk.chunkWidth+1)+zIndex*4]*(xInterp)*(1-zInterp) + bottomMap[xIndex*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(1-xInterp)*(zInterp) + bottomMap[(xIndex+1)*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(xInterp)*(zInterp)));
            }
        }
    }

    public void ApplyMap(){
        if(!pregen){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    for(int y=0; y < Chunk.chunkDepth; y++){ 
                        if(y <= heightMap[x*(Chunk.chunkWidth+1)+z]){
                            if(y >= bottomMap[x*(Chunk.chunkWidth+1)+z]){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = moonstoneBlockID;
                            }
                            else{
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                            }
                        } 
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
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
                    for(int y=0; y < Chunk.chunkDepth; y++){ 
                        if(y <= heightMap[x*(Chunk.chunkWidth+1)+z]){
                            if(y >= bottomMap[x*(Chunk.chunkWidth+1)+z]){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = moonstoneBlockID;
                                stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;                                
                            }
                            else{
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;                                 
                            }
                        } 
                        else if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;     
                            stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                            hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;
                        }
                    }
                }
            }             
        }
    }

    public void AddAcasterLayer(){
        int index;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                index = x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z;

                blockData[index] = this.acasterBlockID;
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
}