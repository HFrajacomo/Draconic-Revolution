using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
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
    [ReadOnly]
    public NativeArray<byte> warpNoise;

    [ReadOnly]
    public ushort hellMarbleBlockID;
    [ReadOnly]
    public ushort acasterBlockID;

    public void Execute(){
        GenerateHeightPivots();
        BilinearIntepolateMaps(heightMap, ceilingMap);
        ApplyMap();
        AddAcasterLayer();
    }

    public void GenerateHeightPivots(){
        TerrainSample sample;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                sample = TerrainShape.SampleHell(pos.x*Chunk.chunkWidth+x, pos.z*Chunk.chunkWidth+z, baseNoise, erosionNoise, peakNoise, temperatureNoise, warpNoise);

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)sample.height;
                ceilingMap[x*(Chunk.chunkWidth+1)+z] = (ushort)sample.secondary;
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

    public void ApplyMap(){
        if(!pregen){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    for(int y=1; y < Chunk.chunkDepth; y++){ 
                        if(y >= heightMap[x*(Chunk.chunkWidth+1)+z]){
                            if(y < ceilingMap[x*(Chunk.chunkWidth+1)+z]){
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                            }
                            else{
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.hellMarbleBlockID;
                            }
                        } 
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.hellMarbleBlockID;
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
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.hellMarbleBlockID;
                                stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                                hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;                                
                            }
                        } 
                        else if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.hellMarbleBlockID;     
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
                index = x*Chunk.chunkWidth*Chunk.chunkDepth+z;

                blockData[index] = this.acasterBlockID;
                stateData[index] = 0;
                hpData[index] = ushort.MaxValue;
            }
        }
    }
}