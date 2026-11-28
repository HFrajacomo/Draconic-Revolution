using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
public struct GenerateSurfaceChunkJob: IJob{
    public int chunkX;
    public int chunkZ;
    public NativeArray<ushort> blockData;
    public NativeArray<ushort> stateData;
    public NativeArray<ushort> hpData;
    public NativeArray<float> heightMap;
    public bool pregen;

    // Noises
    [ReadOnly]
    public NativeArray<byte> baseNoise;
    [ReadOnly]
    public NativeArray<byte> erosionNoise;
    [ReadOnly]
    public NativeArray<byte> peakNoise;
    [ReadOnly]
    public NativeArray<byte> warpNoise;

    public void Execute(){
        int waterLevel = Constants.WORLD_WATER_LEVEL;
        GeneratePivots();
        BilinearIntepolateMap();
        ApplyMap(waterLevel);
    }

    public void GeneratePivots(){
        TerrainSample sample;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                sample = TerrainShape.SampleSurface(chunkX*Chunk.chunkWidth+x, chunkZ*Chunk.chunkWidth+z, baseNoise, erosionNoise, peakNoise, warpNoise);

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.CeilToInt(sample.height));
            }
        }
    }
    

    public void BilinearIntepolateMap(){
        int xIndex, zIndex;
        float xInterp, zInterp;

        for(int x=0; x < Chunk.chunkWidth; x++){
            xIndex = x/4;
            xInterp = (x%4)*0.25f;

            for(int z=0; z < Chunk.chunkWidth; z++){
                zIndex = z/4;
                zInterp = (z%4)*0.25f;

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(heightMap[xIndex*4*(Chunk.chunkWidth+1)+zIndex*4]*(1-xInterp)*(1-zInterp) + heightMap[(xIndex+1)*4*(Chunk.chunkWidth+1)+zIndex*4]*(xInterp)*(1-zInterp) + heightMap[xIndex*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(1-xInterp)*(zInterp) + heightMap[(xIndex+1)*4*(Chunk.chunkWidth+1)+(zIndex+1)*4]*(xInterp)*(zInterp)));
            }
        }
    }

    public void ApplyMap(int waterLevel){
        if(!pregen){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    for(int y=0; y < Chunk.chunkDepth; y++){ 
                        if(y >= heightMap[x*(Chunk.chunkWidth+1)+z]){
                            if(y <= waterLevel){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 6;
                            }
                            else
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                        } 
                        else
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 3;

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
                            if(y <= waterLevel && blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 6;
                                stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;
                            }
                        } 
                        else if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 3;     
                            stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                            hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;
                        }
                    }
                }
            }             
        }
    }
}