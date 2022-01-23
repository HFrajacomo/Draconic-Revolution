using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;

public static class LightMapper
{
    public static ComputeShader cs;
    private static ComputeBuffer dataComputeBuffer;
    private static ComputeBuffer shadowComputeBuffer;
    private static uint[] shadowMap;
    private static byte[] lightMap;


    private enum BlockLightBehaviour : byte{
        PROPAGATE = 0,  // Propagates current light level at full strength (Sky light)
        DRAIN = 1,      // Propagates but drains 1 level of light (Air/Transparent blocks)
        STOP = 2        // Does not propagate light at all
    }
}

/*
[BurstCompile]
public struct CreateShadowMapJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<uint> shadowMap;
    [ReadOnly]
    public NativeArray<ushort> data;
    [ReadOnly]
    public NativeArray<byte> heightMap;
    [ReadOnly]
    public NativeArray<bool> isTransparent;
    [ReadOnly]
    public int chunkWidth;
    [ReadOnly]
    public int chunkDepth;


    public void Execute(int index){
        ushort blockCode;

        for(int z=0; z < chunkWidth; z++){
            for(int y=chunkDepth-1; y >= 0; y--){
                blockCode = data[index*chunkWidth*chunkDepth+y*chunkWidth+z];

                if(heightMap[index*chunkWidth+z])
            }
        }
    }
}
*/