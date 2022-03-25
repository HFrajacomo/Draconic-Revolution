using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System.IO;
using System.Text;


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
	private	ushort[] cacheVoxdata = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataHP = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataState = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];

    // Native Noise Maps
    private NativeArray<byte> baseMap;
    private NativeArray<byte> erosionMap;

    public WorldGenerator(int worldSeed, float dispersionSeed, float offsetHash, float generationSeed, BiomeHandler biomeReference, StructureHandler structHandler, ChunkLoader_Server reference){
    	this.worldSeed = worldSeed;
    	this.dispersionSeed = dispersionSeed;
    	this.biomeHandler = biomeReference;
    	this.offsetHash = offsetHash;
    	this.generationSeed = generationSeed;
    	this.cl = reference;
    	this.structHandler = structHandler;

        baseMap = new NativeArray<byte>(GenerationSeed.baseNoise, Allocator.Persistent);
        erosionMap = new NativeArray<byte>(GenerationSeed.erosionNoise, Allocator.Persistent);
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

    public void DestroyNativeMemory(){
        this.baseMap.Dispose();
        this.erosionMap.Dispose();
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
        NativeArray<ushort> stateData = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
        NativeArray<ushort> hpData = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
        NativeArray<float> heightMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        GenerateChunkJob gcj = new GenerateChunkJob{
            chunkX = pos.x,
            chunkZ = pos.z,
            blockData = voxelData,
            stateData = stateData,
            hpData = hpData,
            heightMap = heightMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap
        };
        JobHandle job = gcj.Schedule();
        job.Complete();

        cacheVoxdata = NativeTools.CopyToManaged(voxelData);
        cacheMetadataState = NativeTools.CopyToManaged(stateData);
        cacheMetadataHP = NativeTools.CopyToManaged(hpData);

        voxelData.Dispose();
        stateData.Dispose();
        hpData.Dispose();
        heightMap.Dispose();
    }

    /*
    Testing only
    */
    private void PrintMap(NativeArray<ushort> heightMap){
        StringBuilder sb = new StringBuilder();

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth ; z++){
                sb.Append(heightMap[x*Chunk.chunkWidth+z]);
                sb.Append("  ");
            }
            sb.Append("\n");
        }

        File.WriteAllBytes("testNB.txt", Encoding.ASCII.GetBytes(sb.ToString()));
    }
}


// =====================================================================



/*
MULTITHREADING JOBS
*/
[BurstCompile(FloatPrecision.High, FloatMode.Strict)]
public struct GenerateChunkJob: IJob{
    public int chunkX;
    public int chunkZ;
    public NativeArray<ushort> blockData;
    public NativeArray<ushort> stateData;
    public NativeArray<ushort> hpData;
    public NativeArray<float> heightMap;

    // Noises
    [ReadOnly]
    public NativeArray<byte> baseNoise;
    [ReadOnly]
    public NativeArray<byte> erosionNoise;

    public void Execute(){
        int waterLevel = 80;
        GeneratePivots();
        BilinearIntepolateMap();
        ApplyMap(waterLevel);
        //GeneratePerlin(80);
    }

    public void GeneratePerlin(int waterLevel){
        float height;
        float erosionMultiplier;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                height = FindSplineHeight((Noise((chunkX*Chunk.chunkWidth+x)*0.0023f, (chunkZ*Chunk.chunkWidth+z)*0.0023f, NoiseMap.BASE) + Noise((chunkX*Chunk.chunkWidth+x)*0.017f, (chunkZ*Chunk.chunkWidth+z)*0.017f, NoiseMap.BASE))/2f, NoiseMap.BASE);
                erosionMultiplier = FindSplineHeight((Noise((chunkX*Chunk.chunkWidth+x)*0.001f, (chunkZ*Chunk.chunkWidth+z)*0.001f, NoiseMap.EROSION) + Noise((chunkX*Chunk.chunkWidth+x)*0.007f, (chunkZ*Chunk.chunkWidth+z)*0.007f, NoiseMap.EROSION))/2f, NoiseMap.EROSION);

                for(int y=0; y < Chunk.chunkDepth; y++){
                    if(y >= height * erosionMultiplier){
                        if(y <= waterLevel){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 6;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                        }
                    }
                    else{
                        blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 3;                        
                    }

                    stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
                    hpData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = ushort.MaxValue;
                }
            }
        }
    }
    
    public void GeneratePivots(){
        float height;
        float erosionMultiplier;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                height = FindSplineHeight((Noise((chunkX*Chunk.chunkWidth+x)*0.0023f, (chunkZ*Chunk.chunkWidth+z)*0.0023f, NoiseMap.BASE)+Noise((chunkX*Chunk.chunkWidth+x)*0.017f, (chunkZ*Chunk.chunkWidth+z)*0.017f, NoiseMap.BASE))/2f, NoiseMap.BASE);
                erosionMultiplier = FindSplineHeight((Noise((chunkX*Chunk.chunkWidth+x)*0.001f, (chunkZ*Chunk.chunkWidth+z)*0.001f, NoiseMap.EROSION) + Noise((chunkX*Chunk.chunkWidth+x)*0.007f, (chunkZ*Chunk.chunkWidth+z)*0.007f, NoiseMap.EROSION))/2f, NoiseMap.EROSION);
            
                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.CeilToInt(height * erosionMultiplier));
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
        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                for(int y=0; y < Chunk.chunkDepth; y++){ 
                    if(y >= heightMap[x*(Chunk.chunkWidth+1)+z]){
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
    

    private float FindSplineHeight(float noiseValue, NoiseMap type){
        int  index = GenerationSeed.baseNoiseSplineX.Length-2;

        if(type == NoiseMap.BASE){
            for(int i=1; i < GenerationSeed.baseNoiseSplineX.Length; i++){
                if(GenerationSeed.baseNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - GenerationSeed.baseNoiseSplineX[index])/(GenerationSeed.baseNoiseSplineX[index+1] - GenerationSeed.baseNoiseSplineX[index]) ;

            if(GenerationSeed.baseNoiseSplineY[index] > GenerationSeed.baseNoiseSplineY[index+1])
                return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));
        }
        else if(type == NoiseMap.EROSION){
            for(int i=1; i < GenerationSeed.erosionNoiseSplineX.Length; i++){
                if(GenerationSeed.erosionNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - GenerationSeed.erosionNoiseSplineX[index])/(GenerationSeed.erosionNoiseSplineX[index+1] - GenerationSeed.erosionNoiseSplineX[index]) ;

            if(GenerationSeed.erosionNoiseSplineY[index] > GenerationSeed.erosionNoiseSplineY[index+1])
                return Mathf.Lerp(GenerationSeed.erosionNoiseSplineY[index], GenerationSeed.erosionNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
            else
                return Mathf.Lerp(GenerationSeed.erosionNoiseSplineY[index], GenerationSeed.erosionNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));
        }
        else{
            for(int i=1; i < GenerationSeed.erosionNoiseSplineX.Length; i++){
                if(GenerationSeed.erosionNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - GenerationSeed.baseNoiseSplineX[index])/(GenerationSeed.baseNoiseSplineX[index+1] - GenerationSeed.baseNoiseSplineX[index]) ;

            if(GenerationSeed.baseNoiseSplineY[index] > GenerationSeed.baseNoiseSplineY[index+1])
                return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));            
        }
    }  


    /*
    Noises
    */

    public float Noise(float x, NoiseMap type)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        if(type == NoiseMap.BASE)
            return Lerp(u, Grad(GenerationSeed.baseNoise[X], x), Grad(GenerationSeed.baseNoise[X+1], x-1)) * 2;
        else if(type == NoiseMap.EROSION)
            return Lerp(u, Grad(GenerationSeed.erosionNoise[X], x), Grad(GenerationSeed.erosionNoise[X+1], x-1)) * 2;
        else
            return Lerp(u, Grad(GenerationSeed.baseNoise[X], x), Grad(GenerationSeed.baseNoise[X+1], x-1)) * 2;
    }

    public float Noise(float x, float y, NoiseMap type)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        if(type == NoiseMap.BASE){
            int A = (baseNoise[X  ] + Y) & 0xff;
            int B = (baseNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(baseNoise[A  ], x, y  ), Grad(baseNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(baseNoise[A+1], x, y-1), Grad(baseNoise[B+1], x-1, y-1)));
        
        }
        else if(type == NoiseMap.EROSION){
            int A = (erosionNoise[X  ] + Y) & 0xff;
            int B = (erosionNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(erosionNoise[A  ], x, y  ), Grad(erosionNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(erosionNoise[A+1], x, y-1), Grad(erosionNoise[B+1], x-1, y-1)));
        }
        else{
            int A = (baseNoise[X  ] + Y) & 0xff;
            int B = (baseNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(baseNoise[A  ], x, y  ), Grad(baseNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(baseNoise[A+1], x, y-1), Grad(baseNoise[B+1], x-1, y-1)));
        }
        
    }

    public float Noise(float x, float y, float z, NoiseMap type)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        int Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);

        if(type == NoiseMap.BASE){        
            int A  = (GenerationSeed.baseNoise[X  ] + Y) & 0xff;
            int B  = (GenerationSeed.baseNoise[X+1] + Y) & 0xff;
            int AA = (GenerationSeed.baseNoise[A  ] + Z) & 0xff;
            int BA = (GenerationSeed.baseNoise[B  ] + Z) & 0xff;
            int AB = (GenerationSeed.baseNoise[A+1] + Z) & 0xff;
            int BB = (GenerationSeed.baseNoise[B+1] + Z) & 0xff;
            return Lerp(w, Lerp(v, Lerp(u, Grad(GenerationSeed.baseNoise[AA  ], x, y  , z  ), Grad(GenerationSeed.baseNoise[BA  ], x-1, y  , z  )),
                                   Lerp(u, Grad(GenerationSeed.baseNoise[AB  ], x, y-1, z  ), Grad(GenerationSeed.baseNoise[BB  ], x-1, y-1, z  ))),
                           Lerp(v, Lerp(u, Grad(GenerationSeed.baseNoise[AA+1], x, y  , z-1), Grad(GenerationSeed.baseNoise[BA+1], x-1, y  , z-1)),
                                   Lerp(u, Grad(GenerationSeed.baseNoise[AB+1], x, y-1, z-1), Grad(GenerationSeed.baseNoise[BB+1], x-1, y-1, z-1))));
        }
        else if(type == NoiseMap.EROSION){        
            int A  = (GenerationSeed.erosionNoise[X  ] + Y) & 0xff;
            int B  = (GenerationSeed.erosionNoise[X+1] + Y) & 0xff;
            int AA = (GenerationSeed.erosionNoise[A  ] + Z) & 0xff;
            int BA = (GenerationSeed.erosionNoise[B  ] + Z) & 0xff;
            int AB = (GenerationSeed.erosionNoise[A+1] + Z) & 0xff;
            int BB = (GenerationSeed.erosionNoise[B+1] + Z) & 0xff;
            return Lerp(w, Lerp(v, Lerp(u, Grad(GenerationSeed.erosionNoise[AA  ], x, y  , z  ), Grad(GenerationSeed.erosionNoise[BA  ], x-1, y  , z  )),
                                   Lerp(u, Grad(GenerationSeed.erosionNoise[AB  ], x, y-1, z  ), Grad(GenerationSeed.erosionNoise[BB  ], x-1, y-1, z  ))),
                           Lerp(v, Lerp(u, Grad(GenerationSeed.erosionNoise[AA+1], x, y  , z-1), Grad(GenerationSeed.erosionNoise[BA+1], x-1, y  , z-1)),
                                   Lerp(u, Grad(GenerationSeed.erosionNoise[AB+1], x, y-1, z-1), Grad(GenerationSeed.erosionNoise[BB+1], x-1, y-1, z-1))));
        }
        else{
            int A  = (GenerationSeed.baseNoise[X  ] + Y) & 0xff;
            int B  = (GenerationSeed.baseNoise[X+1] + Y) & 0xff;
            int AA = (GenerationSeed.baseNoise[A  ] + Z) & 0xff;
            int BA = (GenerationSeed.baseNoise[B  ] + Z) & 0xff;
            int AB = (GenerationSeed.baseNoise[A+1] + Z) & 0xff;
            int BB = (GenerationSeed.baseNoise[B+1] + Z) & 0xff;
            return Lerp(w, Lerp(v, Lerp(u, Grad(GenerationSeed.baseNoise[AA  ], x, y  , z  ), Grad(GenerationSeed.baseNoise[BA  ], x-1, y  , z  )),
                                   Lerp(u, Grad(GenerationSeed.baseNoise[AB  ], x, y-1, z  ), Grad(GenerationSeed.baseNoise[BB  ], x-1, y-1, z  ))),
                           Lerp(v, Lerp(u, Grad(GenerationSeed.baseNoise[AA+1], x, y  , z-1), Grad(GenerationSeed.baseNoise[BA+1], x-1, y  , z-1)),
                                   Lerp(u, Grad(GenerationSeed.baseNoise[AB+1], x, y-1, z-1), Grad(GenerationSeed.baseNoise[BB+1], x-1, y-1, z-1))));            
        }
    }

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    private float Grad(int hash, float x)
    {
        return (hash & 1) == 0 ? x : -x;
    }

    private float Grad(int hash, float x, float y)
    {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    private float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}