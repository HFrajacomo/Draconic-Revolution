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
    [ReadOnly]
    public ChunkDepthID cid;

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
    public ushort waterBlockID;

    [ReadOnly]
    public NativeParallelHashSet<ushort> caveFreeBlocks;

    public void Execute(int index){
        GenerateNoiseTunnel(index);
    }

    // Creates a Noise Tunnels in the Chunk
    public void GenerateNoiseTunnel(int x){
        // Dig Caves and destroy underground rocks variables
        float val;
        float lowerCaveLimit;
        float upperCaveLimit;
        int bottomLimit;
        int upperCompensation;
        float maskThreshold;

        if(cid == ChunkDepthID.HELL){
            lowerCaveLimit = 0.0f;
            upperCaveLimit = 0.1f;
            bottomLimit = 1;
            upperCompensation = -1;
            maskThreshold = 0f;    
        }
        else{
            lowerCaveLimit = 0.3f;
            upperCaveLimit = 0.37f;
            bottomLimit = 1;
            upperCompensation = -1;
            maskThreshold = 0.2f;            
        }

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

                val = TransformOctaves(NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep1, y*GenerationSeed.caveYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep2, caveNoise), NoiseMaker.Noise3D((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep2, y*GenerationSeed.caveYStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep1, caveNoise));
            

                if(lowerCaveLimit <= val && val <= upperCaveLimit){
                    blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                    stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                }
            }
            
            // Updates heightdata
            if(cid == ChunkDepthID.SURFACE)
                SetHeightMapData(x, z);
        }
    }

    private void SetHeightMapData(int x, int z){
        for(int y = Chunk.chunkDepth-1; y > 0; y--){
            if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] != 0 && blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] != this.waterBlockID){
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