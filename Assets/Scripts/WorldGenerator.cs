using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;


public class WorldGenerator
{
	// World Gen Settings
	public int worldSeed;
	public float dispersionSeed;
	public float offsetHash;
	public float generationSeed;
	public BiomeHandler biomeHandler;
	public ChunkLoader_Server cl;

    // Prefab System
    public StructureHandler structHandler;

	// Cached Data
	private ushort[] cacheHeightMap = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private ushort[] cacheHeightMap2 = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private ushort[] cacheHeightMap3 = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private ushort[] cacheHeightMap4 = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];

    private ushort[] cachePivotMap = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];

	private	ushort[] cacheVoxdata = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataHP = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataState = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];

    public WorldGenerator(int worldSeed, float dispersionSeed, float offsetHash, float generationSeed, BiomeHandler biomeReference, StructureHandler structHandler, ChunkLoader_Server reference){
    	this.worldSeed = worldSeed;
    	this.dispersionSeed = dispersionSeed;
    	this.biomeHandler = biomeReference;
    	this.offsetHash = offsetHash;
    	this.generationSeed = generationSeed;
    	this.cl = reference;
    	this.structHandler = structHandler;
    }

    public void SetVoxdata(ushort[] data){
    	this.cacheVoxdata = (ushort[])data.Clone();
    }
    public void SetCacheHP(ushort[] data){
    	this.cacheMetadataHP = (ushort[])data.Clone();
    }
    public void SetCacheState(ushort[] data){
    	this.cacheMetadataState = (ushort[])data.Clone();
    }
    public ushort[] GetVoxdata(){
    	return this.cacheVoxdata;
    }
    public ushort[] GetCacheHP(){
    	return this.cacheMetadataHP;
    }
    public ushort[] GetCacheState(){
    	return this.cacheMetadataState;
    }

    public void ClearCaches(){
        Array.Clear(this.cacheVoxdata, 0, this.cacheVoxdata.Length);
        Array.Clear(this.cacheMetadataHP, 0, this.cacheMetadataHP.Length);
        Array.Clear(this.cacheMetadataState, 0, this.cacheMetadataState.Length);
    }


    // Applies Structures to a chunk
    /*
    Depth Values represent how deep below heightmap things will go.
    Range represents if structure always spawn at given Depth, or if it spans below as well
    */
    /*
    private void GenerateStructures(ChunkPos pos, float xhash, float zhash, byte biome, int structureCode, int depth, int heightlimit=0, bool range=false){
        // Gets index of amount and percentage
        int index = BiomeHandler.GetBiomeStructs(biome).IndexOf(structureCode);
        int amount = BiomeHandler.GetBiomeAmounts(biome)[index];

        float percentage = BiomeHandler.GetBiomePercentages(biome)[index];

        int x,y,z;
        int offsetX, offsetZ;
        int rotation = 0;
        float chance;

        // Offset
        offsetX = structHandler.LoadStructure(structureCode).offsetX;
        offsetZ = structHandler.LoadStructure(structureCode).offsetZ;

        // If structure is static at given heightmap depth
        if(!range){            
            for(int i=1; i <= amount; i++){
                chance = Perlin.Noise((pos.x ^ pos.z)*(zhash/xhash)*(i*0.17f)*structureCode);

                if(chance > percentage)
                    continue;

                rotation = Mathf.FloorToInt(Perlin.Noise((pos.z+pos.x)*xhash*zhash+i*generationSeed)*3.99f);

                x = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.z)*xhash*pos.x*generationSeed)*Chunk.chunkWidthMult);
                z = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.x)*zhash*pos.z*generationSeed)*Chunk.chunkWidthMult);

                // All >
                if(x + offsetX > 15 && z + offsetZ > 15){
                    y = HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, bothAxis:true) - depth;
                }
                // X >
                else if(x + offsetX > 15 && z + offsetZ <= 15){
                    y = HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, xAxis:true) - depth;
                }
                // Z >
                else if(x + offsetX <= 15 && z + offsetZ > 15){
                    y = HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, zAxis:true) - depth;
                }
                // All <
                else{
                    y = cacheHeightMap[(x+offsetX)*(Chunk.chunkWidth+1)+(z+offsetZ)] - depth;
                }

                // Ignores structure on hard limit
                if(y <= heightlimit)
                    continue;

                
                this.structHandler.LoadStructure(structureCode).Apply(this.cl, pos, cacheVoxdata, cacheMetadataHP, cacheMetadataState, x, y, z, rotation:rotation);
            }
        }
        // If can be placed in a range
        else{
            for(int i=1; i <= amount; i++){
                chance = Perlin.Noise((pos.x ^ pos.z)*(zhash/xhash)*(i*0.17f)*structureCode);

                if(chance > percentage)
                    continue;

                rotation = Mathf.FloorToInt(Perlin.Noise((pos.z+pos.x)*xhash*zhash+i*generationSeed)*3.99f);

                x = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.z)*xhash*pos.x*generationSeed)*Chunk.chunkWidthMult);
                z = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.x)*zhash*pos.z*generationSeed)*Chunk.chunkWidthMult);
                float yMult = Perlin.Noise((i + structureCode + (pos.z & pos.x))*xhash*zhash);              

                // All >
                if(x + offsetX > 15 && z + offsetZ > 15){
                    y = (int)(heightlimit + yMult*(HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, bothAxis:true) - depth));
                }
                // X >
                else if(x + offsetX > 15 && z + offsetZ <= 15){
                    y = (int)(heightlimit + yMult*(HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, xAxis:true) - depth));
                }
                // Z >
                else if(x + offsetX <= 15 && z + offsetZ > 15){
                    y = (int)(heightlimit + yMult*(HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, zAxis:true) - depth));
                }
                // All <
                else{
                    y = (int)(heightlimit + yMult*(cacheHeightMap[(x+offsetX)*(Chunk.chunkWidth+1)+(z+offsetZ)] - depth));
                }

                // Ignores structure on hard limit
                if(y <= heightlimit)
                    continue;

                this.structHandler.LoadStructure(structureCode).Apply(this.cl, pos, cacheVoxdata, cacheMetadataHP, cacheMetadataState, x, y, z, rotation:rotation);
            }            
        }
    }

    // Returns the mean height for a given structure
    private int HalfConvolute(ushort[] heightmap, int x, int z, int offsetX, int offsetZ, int code, bool xAxis=false, bool zAxis=false, bool bothAxis=false){
        int sum=0;
        int amount=0;
        
        if(bothAxis){
            for(int i=x; i < Chunk.chunkWidth; i++){
                for(int c=z; c < Chunk.chunkWidth; c++){
                    sum += heightmap[i*(Chunk.chunkWidth+1)+c];
                    amount++;
                }
            }
            if(amount > 0)
                return (int)(sum / amount); 
            else
                return (int)heightmap[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)-1];        
        }
        else if(xAxis){
            int size = structHandler.LoadStructure(code).sizeZ;

            for(int i=x; i < Chunk.chunkWidth; i++){
                for(int c=z; c < Mathf.Min(z+size, Chunk.chunkWidth); c++){
                    sum += heightmap[i*(Chunk.chunkWidth+1)+c];
                    amount++;
                }
            }
            if(amount > 0)
                return (int)(sum / amount);
            else
                return (int)heightmap[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)-1]; 
        }
        else if(zAxis){
            int size = structHandler.LoadStructure(code).sizeX;

            for(int i=z; i < Chunk.chunkWidth; i++){
                for(int c=x; c < Mathf.Min(x+size, Chunk.chunkWidth); c++){
                    sum += heightmap[c*(Chunk.chunkWidth+1)+i];
                    amount++; 
                }
            }
            if(amount > 0)
                return (int)(sum / amount);
            else
                return (int)heightmap[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)-1];         
        }
        
        
        return heightmap[x*(Chunk.chunkWidth+1)+z]-1;
    }
    */


    // Generates a Chunk
    public void GenerateChunk(ChunkPos pos){
        NativeArray<ushort> voxelData = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);

        GenerateChunkJob gcj = new GenerateChunkJob{
            chunkX = pos.x,
            chunkZ = pos.z,
            blockData = voxelData
        };
        JobHandle job = gcj.Schedule();
        job.Complete();

        cacheVoxdata = NativeTools.CopyToManaged(voxelData);

        voxelData.Dispose();
    }
}


// =====================================================================



/*
MULTITHREADING JOBS
*/
//[BurstCompile]
public struct GenerateChunkJob: IJob{
    public int chunkX;
    public int chunkZ;
    public NativeArray<ushort> blockData;

    public void Execute(){
        int waterLevel = 80;
        GeneratePerlin(waterLevel);
    }

    public void GeneratePerlin(int waterLevel){
        int height;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                height = FindSplineHeight((Perlin.Noise((chunkX*Chunk.chunkWidth+x)*0.005f, (chunkZ*Chunk.chunkWidth+z)*0.005f, NoiseMap.BASE)+Perlin.Noise((chunkX*Chunk.chunkWidth+x)*0.017f, (chunkZ*Chunk.chunkWidth+z)*0.017f, NoiseMap.BASE))/2f, NoiseMap.BASE);

                for(int y=0; y < Chunk.chunkDepth; y++){
                    if(y >= height){
                        if(y <= waterLevel)
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 6;
                        else
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                    }
                    else
                        blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 3;                        
                }
            }
        }        
    }

    private int FindSplineHeight(float noiseValue, NoiseMap type){
        int  index = World.baseNoiseSplineX.Length-2;

        if(type == NoiseMap.BASE){
            for(int i=1; i < World.baseNoiseSplineX.Length; i++){
                if(World.baseNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - World.baseNoiseSplineX[index])/(World.baseNoiseSplineX[index+1] - World.baseNoiseSplineX[index]) ;

            if(World.baseNoiseSplineY[index] > World.baseNoiseSplineY[index+1])
                return Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                return Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));
        }
        else{
            for(int i=1; i < World.baseNoiseSplineX.Length; i++){
                if(World.baseNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - World.baseNoiseSplineX[index])/(World.baseNoiseSplineX[index+1] - World.baseNoiseSplineX[index]) ;

            if(World.baseNoiseSplineY[index] > World.baseNoiseSplineY[index+1])
                return Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                return Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));            
        }
    }
}