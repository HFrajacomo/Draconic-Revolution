using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;


[BurstCompile]
public struct GenerateCaveJob : IJobParallelFor{
    [ReadOnly]
    public ChunkPos pos;

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockData;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> stateData;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> heightMap;

    [ReadOnly]
    public NativeArray<byte> caveNoise;
    [ReadOnly]
    public NativeArray<byte> cavemaskNoise;

    [ReadOnly]
    public NativeHashSet<ushort> caveFreeBlocks;

    public void Execute(int index){
        GenerateNoiseTunnel(index);
    }

    // Creates a Noise Tunnels in the Chunk
    public void GenerateNoiseTunnel(int x){
        // Dig Caves and destroy underground rocks variables
        float val;
        float lowerCaveLimit = 0.3f;
        float upperCaveLimit = 0.37f;
        int bottomLimit = 10;
        int upperCompensation = -1;
        float maskThreshold = 0.2f;

        for(int z=0; z < Chunk.chunkWidth; z++){ 
            for(int y=(int)heightMap[x*(Chunk.chunkWidth+1)+z]+upperCompensation; y > bottomLimit; y--){
                if(caveFreeBlocks.Contains(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){
                    continue;
                }

                if(y < Chunk.chunkDepth-1){
                    if(caveFreeBlocks.Contains(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z])){
                        continue;
                    }
                }

                if(NoiseMaker.NoiseMask((pos.x*Chunk.chunkWidth+x)*GenerationSeed.cavemaskNoiseStep1, y*GenerationSeed.cavemaskYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.cavemaskNoiseStep1, cavemaskNoise) < maskThreshold)
                    continue;

                val = TransformOctaves(NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep1, y*GenerationSeed.caveYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep1, caveNoise), NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep2, y*GenerationSeed.caveYStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep2, caveNoise));
            

                if(lowerCaveLimit <= val && val <= upperCaveLimit){
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                    stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                }
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


    private int Abs(int x){
        if(x > 0)
            return x;
        else
            return -x;
    }

    // Calculates the cumulative distribution function of a Normal Distribution
    private float TransformOctaves(float a, float b){
        float c = (a+b)/2f;

        return (2f/(1f + Mathf.Exp(-c*4.1f)))-1;
    }
}