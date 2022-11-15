using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile(FloatPrecision.High, FloatMode.Strict)]
public struct GenerateUndergroundChunkJob: IJobParallelFor{
    public ChunkPos pos;

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockData;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> stateData;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> hpData;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> heightMap;

    public bool pregen;

    // Noises
    [ReadOnly]
    public NativeArray<byte> caveNoise;
    [ReadOnly]
    public NativeArray<byte> cavemaskNoise;
    [ReadOnly]
    public NativeArray<byte> peakNoise;

    public void Execute(int index){
        GenerateUnderground(index);
    }

    // Creates a Noise Tunnels in the Chunk
    public void GenerateUnderground(int x){
        float base_;
        float mask, peak;

        float baseRange = 0.34f;
        float logBase = 1.5f;
        float minMask, maxMask;
        float rangeMultiplier = 0.22f;
        float maskThreshold = 0.64f;


        for(int z=0; z < Chunk.chunkWidth; z++){ 
            for(int y=Chunk.chunkDepth-1; y > 0; y--){
                base_ = TransformOctaves(NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep1, y*GenerationSeed.caveYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep1, caveNoise), NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep2, y*GenerationSeed.caveYStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep2, caveNoise));
                mask = Normalize(NoiseMaker.NoiseMask((pos.x*Chunk.chunkWidth+x)*GenerationSeed.cavemaskNoiseStep1, y*GenerationSeed.cavemaskYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.cavemaskNoiseStep1, cavemaskNoise));
                peak = NormalizePeak(TransformOctaves(NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep1, y*GenerationSeed.peakYStep, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep1, peakNoise), NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep2, y*GenerationSeed.peakYStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep2, peakNoise)), logBase);
            
                minMask = baseRange - peak*rangeMultiplier;
                maxMask = baseRange + peak*rangeMultiplier;

                if(base_ <= maxMask && base_ >= minMask && mask >= maskThreshold)
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                else
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.STONE;
            }
            SetHeightMapData(x, z);
        }
    }

    private void SetHeightMapData(int x, int z){
        for(int y = Chunk.chunkDepth-1; y > 0; y--){
            if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] != 0 && blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] != (ushort)BlockID.WATER){
                heightMap[x*(Chunk.chunkWidth+1)+z] = y+1;
                return;
            }
        }            
    }

    // Calculates the cumulative distribution function of a Normal Distribution
    private float TransformOctaves(float a, float b){
        float c = (a+b)/2f;

        return (2f/(1f + Mathf.Exp(-c*4.1f)))-1;
    }

    private float NormalizeErosion(float x){
        return (x+1)/10f;
    }

    private float Normalize(float x){
        return (x+1)/2f;
    }

    private float NormalizePeak(float x, float logBase){
        return Mathf.Clamp(Mathf.Log(((x+1)/2f) + 1, logBase), 0f, 1f);
    }

    private float ApplyPeakTransformation(float base_, float peak, float baseRange){
        return base_*(1-peak) + baseRange*peak;
    }
}