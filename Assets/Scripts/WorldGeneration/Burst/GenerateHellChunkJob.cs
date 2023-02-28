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

    public void Execute(){
        int lavaLevel = 136;


        // Lower part
        GenerateHeightPivots();
        BilinearIntepolateMap(heightMap);

        // Ceiling part
        GenerateCeilingPivots();
        BilinearIntepolateMap(ceilingMap);

        ApplyMap(lavaLevel);
    }

    public void GenerateHeightPivots(){
        float height;
        float erosionMultiplier;
        float peakAdd;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                height = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep1, baseNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep2, baseNoise))), NoiseMap.BASE, ChunkDepthID.HELL);
                erosionMultiplier = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep1, erosionNoise), NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep2, erosionNoise)), NoiseMap.EROSION, ChunkDepthID.HELL);
                peakAdd = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep1, peakNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep2, peakNoise))), NoiseMap.PEAK, ChunkDepthID.HELL);

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.CeilToInt((height + peakAdd) * erosionMultiplier));
            }
        }
    }

    public void GenerateCeilingPivots(){
        float erosionMultiplier;
        float peakAdd;

        int baseCeilingHeight = Chunk.chunkDepth - 8;
        int ceilingBoost = 8;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                erosionMultiplier = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep1, erosionNoise), NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep2, erosionNoise)), NoiseMap.EROSION, ChunkDepthID.HELL);
                peakAdd = NoiseMaker.FindSplineHeight(TransformOctaves(NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep2, peakNoise), (NoiseMaker.Noise2D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep2, peakNoise))), NoiseMap.PEAK, ChunkDepthID.HELL);

                ceilingMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.CeilToInt(baseCeilingHeight - ZeroClamp((peakAdd + ceilingBoost) * erosionMultiplier)));
            }
        }
    }

    public void BilinearIntepolateMap(NativeArray<float> map){
        int xIndex, zIndex;
        float xInterp, zInterp;

        for(int x=0; x < Chunk.chunkWidth; x++){
            xIndex = x/4;
            xInterp = (x%4)*0.25f;

            for(int z=0; z < Chunk.chunkWidth; z++){
                zIndex = z/4;
                zInterp = (z%4)*0.25f;

                map[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(map[xIndex*4*(Chunk.chunkWidth+1)+zIndex*4]*(1-xInterp)*(1-zInterp) + map[(xIndex+1)*4*(Chunk.chunkWidth+1)+zIndex*4]*(xInterp)*(1-zInterp) + map[xIndex*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(1-xInterp)*(zInterp) + map[(xIndex+1)*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(xInterp)*(zInterp)));
            }
        }
    }

    public void ApplyMap(int lavaLevel){
        ushort lavaBlock = (ushort)BlockID.LAVA;
        ushort hellMarble = (ushort)BlockID.HELL_MARBLE;

        if(!pregen){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    for(int y=0; y < Chunk.chunkDepth; y++){ 
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
                        else
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = hellMarble;

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

    // Calculates the cumulative distribution function of a Normal Distribution
    private float TransformOctaves(float a, float b){
        float c = (a+b)/2f;

        return (2f/(1f + Mathf.Exp(-c*4.1f)))-1;
    }

    private float ZeroClamp(float number){
        if(number <= 0)
            return 1;
        return number;
    }
}