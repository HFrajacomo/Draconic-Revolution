﻿using System.Collections;
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
	private List<ushort[]> cacheMaps = new List<ushort[]>();
	private List<ushort> cacheBlockCodes = new List<ushort>();
    private ChunkPos cachePos = new ChunkPos(0,0);
    private Chunk cacheChunk;
    private ushort[] cacheMetadataHP = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataState = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private Dictionary<ushort, ushort> cacheStateDict = new Dictionary<ushort, ushort>();


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


    // Returns the biome that should be assigned to a given chunk
    public VoxelData AssignBiome(ChunkPos pos, bool pregen=false){
        byte biome = biomeHandler.Assign(pos);

        structHandler.LoadBiome(biome);

        cl.chunks[pos].biomeName = BiomeHandler.ByteToBiome(biome);
        cl.chunks[pos].features = biomeHandler.GetFeatures(pos);


        if(biome == 0)
            return GeneratePlainsBiome(pos.x, pos.z, pregen:pregen);
        else if(biome == 1)
            return GenerateGrassyHighLandsBiome(pos.x, pos.z, pregen:pregen);
        else if(biome == 2)
            return GenerateOceanBiome(pos.x, pos.z, pregen:pregen);
        else if(biome == 3)
            return GenerateForestBiome(pos.x, pos.z, pregen:pregen);
        else 
            return GeneratePlainsBiome(pos.x, pos.z, pregen:pregen);
        
    }

    /*
    Used for Cave system generation and above ground turbulence
    */
    private NativeArray<ushort> GenerateRidgedMultiFractal3D(int chunkX, int chunkZ, float xhash, float yhash, float zhash, float threshold, int ceiling=20, float maskThreshold=0f){

        NativeArray<ushort> turbulanceMap = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);

        RidgedMultiFractalJob rmfJob = new RidgedMultiFractalJob{
            chunkX = chunkX*Chunk.chunkWidth,
            chunkZ = chunkZ*Chunk.chunkWidth,
            xhash = xhash,
            yhash = yhash,
            zhash = zhash,
            threshold = threshold,
            generationSeed = generationSeed,
            ceiling = ceiling,
            maskThreshold = maskThreshold,
            turbulenceMap = turbulanceMap
        };

        JobHandle handle = rmfJob.Schedule(Chunk.chunkWidth, 2);
        handle.Complete();

        return turbulanceMap;
    }

    // Applies Structures to a chunk
    /*
    Depth Values represent how deep below heightmap things will go.
    Range represents if structure always spawn at given Depth, or if it spans below as well
    */
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


    // Generates Plains biome chunk
	public VoxelData GeneratePlainsBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Plains Biomes
        float xhash = 41.21f;
        float zhash = 105.243f;
        byte currentBiome = 0;
        ChunkPos currentChunk = new ChunkPos(chunkX, chunkZ);

        // Checking for Transitions
        byte xBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ));
        byte zBiome = biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1));
        byte xzBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1));
        bool xTransition = false;
        bool zTransition = false;
        bool xzTransition = false;

        if(xBiome != currentBiome)
            xTransition = true;
        if(zBiome != currentBiome)
            zTransition = true;
        if(xzBiome != currentBiome)
            xzTransition = true;

        // Transition Handlers
        NativeArray<ushort> waterLevels = new NativeArray<ushort>(BiomeHandlerData.codeToWater, Allocator.TempJob);

        // Metadata and blockdata
        NativeList<ushort> codes = new NativeList<ushort>(0, Allocator.TempJob);
        NativeHashMap<ushort, ushort> stateDict = new NativeHashMap<ushort, ushort>(0, Allocator.TempJob);
        NativeList<ushort> maps = new NativeList<ushort>(0, Allocator.TempJob);
        
        // HeightMaps
        NativeArray<ushort> map1 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map2 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map3 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map4 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        // The Job Class
        JobHandle job;

        // Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
        GeneratePivotsJob gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            xhash = xhash,
            zhash = zhash,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            selectedCache = map1,
            groundLevel = 20,
            ceilingLevel = 25,
            octave = 0,
        };
        job = gpJob.Schedule();
        job.Complete();

        gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            xhash = xhash*1.712f,
            zhash = zhash*2.511f,
            selectedCache = map2,
            groundLevel = 18,
            ceilingLevel = 30,
            octave = 1,
        };
        job = gpJob.Schedule();
        job.Complete();

        // Combine Pivot Maps
        CombinePivotMapJob cpvJob = new CombinePivotMapJob{
            inMap = map2,
            outMap = map1
        };
        job = cpvJob.Schedule((int)((Chunk.chunkWidth/4)+1), 2);
        job.Complete();        

        // Does different interpolation for normal vs transition chunks
        if(!xTransition && !zTransition && !xzTransition){
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = 4,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }
        else{
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = Chunk.chunkWidth,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }

        // Underground is hold on Cache 2
        AddFromMapJob afmJob = new AddFromMapJob{
            inMap = map1,
            outMap = map2,
            val = -5
        };
        job = afmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();


        // Dirt is hold on Cache 3
        afmJob = new AddFromMapJob{
            inMap = map1,
            outMap = map3,
            val = -1
        };
        job = afmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Adds rest to pipeline
        maps.AddRange(map2);
        codes.Add(3);
        maps.AddRange(map3);
        codes.Add(2);
        maps.AddRange(map1);
        codes.Add(1);

        // Add Water
        if(xTransition){
            if(waterLevels[currentBiome] >= waterLevels[xBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[xBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        if(zTransition){
            if(waterLevels[currentBiome] >= waterLevels[zBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[zBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        // Add Water
        if(xzTransition){
            if(waterLevels[currentBiome] >= waterLevels[xzBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[xzBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        // Add Water
        if(!xTransition && !zTransition && !xzTransition){
            // Water in Cache 4
            GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                ceiling = waterLevels[currentBiome],
                selectedCache = map4
            };
            job = gfmJob.Schedule(Chunk.chunkWidth, 2);
            job.Complete();                

            maps.AddRange(map4);
            codes.Add(6);
        }

        // Sets state dict
        stateDict.Add(6, 0);

        NativeArray<ushort> vox = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpdata = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> statedata = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);

        // ApplyHeightMap
        ApplyMapsJob amJob = new ApplyMapsJob{
            cacheVoxdata = vox,
            cacheMetadataHP = hpdata,
            cacheMetadataState = statedata,
            cacheMaps = maps.AsArray(),
            stateDict = stateDict,
            cacheBlockCodes = codes,
            pregen = pregen
        };
        job = amJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Convert data back
        cacheVoxdata = vox.ToArray();
        cacheMetadataHP = hpdata.ToArray();
        cacheMetadataState = statedata.ToArray();
        cacheHeightMap = map1.ToArray();
        
        // Applies Structs from other chunks
        if(Structure.Exists(currentChunk)){
            Structure.RoughApply(cacheVoxdata, cacheMetadataHP, cacheMetadataState, Structure.GetChunk(currentChunk));
            Structure.RemoveChunk(currentChunk);
        }

        // Pre=Dispose Bin
        vox.Dispose();
        hpdata.Dispose();
        statedata.Dispose();

        // Structures
        GeneratePlainsStructures(currentChunk, xhash, zhash, currentBiome, (xTransition || zTransition || xzTransition));

        // Cave System
        NativeArray<ushort> voxCU = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpCU = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> stateCU = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);
        NativeArray<ushort> cacheCave = GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:20, maskThreshold:0.64f);

        CutUndergroundJob cuJob = new CutUndergroundJob{
            cacheVox = voxCU,
            cacheHP = hpCU,
            cacheState = stateCU,
            cacheCave = cacheCave,
            upper = 20,
            lower = 1,
        };
        job = cuJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Convert Data Back
        cacheVoxdata = voxCU.ToArray();
        cacheMetadataHP = hpCU.ToArray();
        cacheMetadataState = stateCU.ToArray();

        // Dispose Bin
        voxCU.Dispose();
        hpCU.Dispose();
        stateCU.Dispose();
        codes.Dispose();
        stateDict.Dispose();
        maps.Dispose();
        waterLevels.Dispose();
        cacheCave.Dispose();

        map1.Dispose();
        map2.Dispose();
        map3.Dispose();        
        map4.Dispose();

        return new VoxelData(cacheVoxdata);
    }

    // Inserts Structures into Plain Biome
    private void GeneratePlainsStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){        
        foreach(int structCode in BiomeHandler.GetBiomeStructs(biomeCode)){
            if(structCode == 1 || structCode == 2){ // Trees
                if(!transition)
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1, heightlimit:23);
            }
            else if(structCode == 3 || structCode == 4){ // Dirt Piles
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 2, range:true);
            }
            else if(structCode == 5){ // Boulder
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 0);
            }
            else if(structCode >= 9 && structCode <= 11){ // Metal Veins
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 8, range:true);
            }
        }
    }

    // Generates Grassy Highlands biome chunk
    public VoxelData GenerateGrassyHighLandsBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Plains Biomes
        float xhash = 41.21f;
        float zhash = 105.243f;
        byte currentBiome = 1;
        ChunkPos currentChunk = new ChunkPos(chunkX, chunkZ);

        // Checking for Transitions
        byte xBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ));
        byte zBiome = biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1));
        byte xzBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1));
        bool xTransition = false;
        bool zTransition = false;
        bool xzTransition = false;

        if(xBiome != currentBiome)
            xTransition = true;
        if(zBiome != currentBiome)
            zTransition = true;
        if(xzBiome != currentBiome)
            xzTransition = true;

        // Transition Handlers
        NativeArray<ushort> waterLevels = new NativeArray<ushort>(BiomeHandlerData.codeToWater, Allocator.TempJob);

        // Metadata and blockdata
        NativeList<ushort> codes = new NativeList<ushort>(0, Allocator.TempJob);
        NativeHashMap<ushort, ushort> stateDict = new NativeHashMap<ushort, ushort>(0, Allocator.TempJob);
        NativeList<ushort> maps = new NativeList<ushort>(0, Allocator.TempJob);
        
        // HeightMaps
        NativeArray<ushort> map1 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map2 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map3 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map4 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        // The Job Class
        JobHandle job;

        // Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
        GeneratePivotsJob gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            xhash = xhash,
            zhash = zhash,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            selectedCache = map1,
            groundLevel = 30,
            ceilingLevel = 50,
            octave = 0,
        };
        job = gpJob.Schedule();
        job.Complete();

        gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            xhash = xhash*0.712f,
            zhash = zhash*0.2511f,
            selectedCache = map2,
            groundLevel = 30,
            ceilingLevel = 70,
            octave = 1,
        };
        job = gpJob.Schedule();
        job.Complete();

        // Combine Pivot Maps
        CombinePivotMapJob cpvJob = new CombinePivotMapJob{
            inMap = map2,
            outMap = map1
        };
        job = cpvJob.Schedule((int)((Chunk.chunkWidth/4)+1), 2);
        job.Complete();        

        // Does different interpolation for normal vs transition chunks
        if(!xTransition && !zTransition && !xzTransition){
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = 4,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }
        else{
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = Chunk.chunkWidth,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }

        // Underground is hold on Cache 2
        AddFromMapJob afmJob = new AddFromMapJob{
            inMap = map1,
            outMap = map2,
            val = -5
        };
        job = afmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();


        // Dirt is hold on Cache 3
        afmJob = new AddFromMapJob{
            inMap = map1,
            outMap = map3,
            val = -1
        };
        job = afmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Adds rest to pipeline
        maps.AddRange(map2);
        codes.Add(3);
        maps.AddRange(map3);
        codes.Add(2);
        maps.AddRange(map1);
        codes.Add(1);

        // Add Water
        if(xTransition){
            if(waterLevels[currentBiome] >= waterLevels[xBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[xBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        if(zTransition){
            if(waterLevels[currentBiome] >= waterLevels[zBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[zBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        // Add Water
        if(xzTransition){
            if(waterLevels[currentBiome] >= waterLevels[xzBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[xzBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        // Add Water
        
        if(!xTransition && !zTransition && !xzTransition){
            // Water in Cache 4
            GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                ceiling = waterLevels[currentBiome],
                selectedCache = map4
            };
            job = gfmJob.Schedule(Chunk.chunkWidth, 2);
            job.Complete();                

            maps.AddRange(map4);
            codes.Add(6);
        }

        // Sets state dict
        stateDict.Add(6, 0);

        NativeArray<ushort> vox = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpdata = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> statedata = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);

        // ApplyHeightMap
        ApplyMapsJob amJob = new ApplyMapsJob{
            cacheVoxdata = vox,
            cacheMetadataHP = hpdata,
            cacheMetadataState = statedata,
            cacheMaps = maps.AsArray(),
            stateDict = stateDict,
            cacheBlockCodes = codes,
            pregen = pregen
        };
        job = amJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Convert data back
        cacheVoxdata = vox.ToArray();
        cacheMetadataHP = hpdata.ToArray();
        cacheMetadataState = statedata.ToArray();
        cacheHeightMap = map1.ToArray();
        
        // Applies Structs from other chunks
        if(Structure.Exists(currentChunk)){
            Structure.RoughApply(cacheVoxdata, cacheMetadataHP, cacheMetadataState, Structure.GetChunk(currentChunk));
            Structure.RemoveChunk(currentChunk);
        }

        // Pre=Dispose Bin
        vox.Dispose();
        hpdata.Dispose();
        statedata.Dispose();

        // Structures
        GenerateGrassyHighLandsStructures(currentChunk, xhash, zhash, currentBiome, (xTransition || zTransition || xzTransition));

        // Cave System
        NativeArray<ushort> voxCU = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpCU = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> stateCU = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);
        NativeArray<ushort> cacheCave = GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:45, maskThreshold:0.64f);
        
        CutUndergroundJob cuJob = new CutUndergroundJob{
            cacheVox = voxCU,
            cacheHP = hpCU,
            cacheState = stateCU,
            cacheCave = cacheCave,
            upper = 45,
            lower = 1,
        };
        job = cuJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Convert Data Back
        cacheVoxdata = voxCU.ToArray();
        cacheMetadataHP = hpCU.ToArray();
        cacheMetadataState = stateCU.ToArray();

        // Dispose Bin
        voxCU.Dispose();
        hpCU.Dispose();
        stateCU.Dispose();
        codes.Dispose();
        stateDict.Dispose();
        maps.Dispose();
        waterLevels.Dispose();
        cacheCave.Dispose();

        map1.Dispose();
        map2.Dispose();
        map3.Dispose();        
        map4.Dispose();

        return new VoxelData(cacheVoxdata);
    }

    // Inserts Structures into Plain Biome
    private void GenerateGrassyHighLandsStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(biomeCode)){
            if(structCode <= 2){ // Trees
                if(!transition){
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1, heightlimit:43);
                }
            }
            else if(structCode == 3 || structCode == 4){ // Dirt Piles
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 2, range:true);
            }
            else if(structCode == 5){ // Boulder
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 0);
            }
            else if(structCode >= 9 && structCode <= 11){ // Metal Veins
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 8, range:true);
            }
        }
    }

    // Generates Ocean biome chunk
    public VoxelData GenerateOceanBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Plains Biomes
        float xhash = 54.7f;
        float zhash = 69.3f;
        byte currentBiome = 2;
        ChunkPos currentChunk = new ChunkPos(chunkX, chunkZ);

        // Checking for Transitions
        byte xBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ));
        byte zBiome = biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1));
        byte xzBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1));
        bool xTransition = false;
        bool zTransition = false;
        bool xzTransition = false;

        if(xBiome != currentBiome)
            xTransition = true;
        if(zBiome != currentBiome)
            zTransition = true;
        if(xzBiome != currentBiome)
            xzTransition = true;

        // Transition Handlers
        NativeArray<ushort> waterLevels = new NativeArray<ushort>(BiomeHandlerData.codeToWater, Allocator.TempJob);

        // Metadata and blockdata
        NativeList<ushort> codes = new NativeList<ushort>(0, Allocator.TempJob);
        NativeHashMap<ushort, ushort> stateDict = new NativeHashMap<ushort, ushort>(0, Allocator.TempJob);
        NativeList<ushort> maps = new NativeList<ushort>(0, Allocator.TempJob);
        
        // HeightMaps
        NativeArray<ushort> map1 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map2 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map3 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map4 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        // The Job Class
        JobHandle job;

        // Ocean 
        GeneratePivotsJob gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            xhash = xhash,
            zhash = zhash,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            selectedCache = map1,
            groundLevel = 1,
            ceilingLevel = 19,
            octave = 0,
        };
        job = gpJob.Schedule();
        job.Complete();

        gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,
            xhash = xhash*0.112f,
            zhash = zhash*0.31f,

            selectedCache = map2,
            groundLevel = 1,
            ceilingLevel = 19,
            octave = 1,
        };
        job = gpJob.Schedule();
        job.Complete();

        // Combine Pivot Maps
        CombinePivotMapJob cpvJob = new CombinePivotMapJob{
            inMap = map2,
            outMap = map1
        };
        job = cpvJob.Schedule((int)((Chunk.chunkWidth/4)+1), 2);
        job.Complete();        

        // Does different interpolation for normal vs transition chunks
        if(!xTransition && !zTransition && !xzTransition){
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = 4,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }
        else{
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = Chunk.chunkWidth,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }

        // Adds rest to pipeline
        maps.AddRange(map1);
        codes.Add(2);

        // Add Water
        GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
            ceiling = waterLevels[currentBiome],
            selectedCache = map2
        };
        job = gfmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();                

        maps.AddRange(map2);
        codes.Add(6);


        // Sets state dict
        stateDict.Add(6, 0);

        NativeArray<ushort> vox = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpdata = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> statedata = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);

        // ApplyHeightMap
        ApplyMapsJob amJob = new ApplyMapsJob{
            cacheVoxdata = vox,
            cacheMetadataHP = hpdata,
            cacheMetadataState = statedata,
            cacheMaps = maps.AsArray(),
            stateDict = stateDict,
            cacheBlockCodes = codes,
            pregen = pregen
        };
        job = amJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Convert data back
        cacheVoxdata = vox.ToArray();
        cacheMetadataHP = hpdata.ToArray();
        cacheMetadataState = statedata.ToArray();
        cacheHeightMap = map1.ToArray();
        
        // Applies Structs from other chunks
        if(Structure.Exists(currentChunk)){
            Structure.RoughApply(cacheVoxdata, cacheMetadataHP, cacheMetadataState, Structure.GetChunk(currentChunk));
            Structure.RemoveChunk(currentChunk);
        }

        // Pre=Dispose Bin
        vox.Dispose();
        hpdata.Dispose();
        statedata.Dispose();

        // Structures
        GenerateOceanStructures(currentChunk, xhash, zhash, currentBiome, (xTransition || zTransition || xzTransition));

        // Cave System
        NativeArray<ushort> voxCU = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpCU = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> stateCU = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);
        NativeArray<ushort> cacheCave = GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:10, maskThreshold:0.64f);

        CutUndergroundJob cuJob = new CutUndergroundJob{
            cacheVox = voxCU,
            cacheHP = hpCU,
            cacheState = stateCU,
            cacheCave = cacheCave,
            upper = 10,
            lower = 1,
        };
        job = cuJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Convert Data Back
        cacheVoxdata = voxCU.ToArray();
        cacheMetadataHP = hpCU.ToArray();
        cacheMetadataState = stateCU.ToArray();

        // Dispose Bin
        voxCU.Dispose();
        hpCU.Dispose();
        stateCU.Dispose();
        codes.Dispose();
        stateDict.Dispose();
        maps.Dispose();
        waterLevels.Dispose();
        cacheCave.Dispose();

        map1.Dispose();
        map2.Dispose();
        map3.Dispose();        
        map4.Dispose();

        return new VoxelData(cacheVoxdata);
    }

    // Inserts Structures into Ocean Biome
    private void GenerateOceanStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){
        // Nothing Yet
    }

    // Generates Forest biome chunk
    public VoxelData GenerateForestBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Plains Biomes
        float xhash = 72.117f;
        float zhash = 45.483f;
        byte currentBiome = 3;
        ChunkPos currentChunk = new ChunkPos(chunkX, chunkZ);

        // Checking for Transitions
        byte xBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ));
        byte zBiome = biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1));
        byte xzBiome = biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1));
        bool xTransition = false;
        bool zTransition = false;
        bool xzTransition = false;

        if(xBiome != currentBiome)
            xTransition = true;
        if(zBiome != currentBiome)
            zTransition = true;
        if(xzBiome != currentBiome)
            xzTransition = true;

        // Transition Handlers
        NativeArray<ushort> waterLevels = new NativeArray<ushort>(BiomeHandlerData.codeToWater, Allocator.TempJob);

        // Metadata and blockdata
        NativeList<ushort> codes = new NativeList<ushort>(0, Allocator.TempJob);
        NativeHashMap<ushort, ushort> stateDict = new NativeHashMap<ushort, ushort>(0, Allocator.TempJob);
        NativeList<ushort> maps = new NativeList<ushort>(0, Allocator.TempJob);
        
        // HeightMaps
        NativeArray<ushort> map1 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map2 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map3 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<ushort> map4 = new NativeArray<ushort>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        // The Job Class
        JobHandle job;

        // Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
        GeneratePivotsJob gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            xhash = xhash,
            zhash = zhash,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            selectedCache = map1,
            groundLevel = 25,
            ceilingLevel = 32,
            octave = 0,
        };
        job = gpJob.Schedule();
        job.Complete();

        gpJob = new GeneratePivotsJob{
            chunkX = chunkX,
            chunkZ = chunkZ,
            worldSeed = worldSeed,
            generationSeed = generationSeed,
            dispersionSeed = dispersionSeed,
            offsetHash = offsetHash,
            currentBiome = currentBiome,
            xBiome = xBiome,
            zBiome = zBiome,
            xzBiome = xzBiome,

            xhash = xhash*1.712f,
            zhash = zhash*2.511f,
            selectedCache = map2,
            groundLevel = 25,
            ceilingLevel = 45,
            octave = 1,
        };
        job = gpJob.Schedule();
        job.Complete();

        // Combine Pivot Maps
        CombinePivotMapJob cpvJob = new CombinePivotMapJob{
            inMap = map2,
            outMap = map1
        };
        job = cpvJob.Schedule((int)((Chunk.chunkWidth/4)+1), 2);
        job.Complete();        

        // Does different interpolation for normal vs transition chunks
        if(!xTransition && !zTransition && !xzTransition){
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = 4,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }
        else{
            BilinearInterpolateJob biJob = new BilinearInterpolateJob{
                interval = Chunk.chunkWidth,
                heightMap = map1
            };
            job = biJob.Schedule();
            job.Complete();
        }

        // Underground is hold on Cache 2
        AddFromMapJob afmJob = new AddFromMapJob{
            inMap = map1,
            outMap = map2,
            val = -5
        };
        job = afmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();


        // Dirt is hold on Cache 3
        afmJob = new AddFromMapJob{
            inMap = map1,
            outMap = map3,
            val = -1
        };
        job = afmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Adds rest to pipeline
        maps.AddRange(map2);
        codes.Add(3);
        maps.AddRange(map3);
        codes.Add(2);
        maps.AddRange(map1);
        codes.Add(1);

        // Add Water
        
        if(xTransition){
            if(waterLevels[currentBiome] >= waterLevels[xBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[xBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        if(zTransition){
            if(waterLevels[currentBiome] >= waterLevels[zBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[zBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }
        // Add Water
        if(xzTransition){
            if(waterLevels[currentBiome] >= waterLevels[xzBiome]){
                // Water in Cache 4
                GenerateFlatMapJob gfmJob = new GenerateFlatMapJob{
                    ceiling = waterLevels[xzBiome],
                    selectedCache = map4
                };
                job = gfmJob.Schedule(Chunk.chunkWidth, 2);
                job.Complete();                

                maps.AddRange(map4);
                codes.Add(6);
            }
        }



        // Sets state dict
        stateDict.Add(6, 0);

        NativeArray<ushort> vox = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpdata = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> statedata = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);

        // ApplyHeightMap
        ApplyMapsJob amJob = new ApplyMapsJob{
            cacheVoxdata = vox,
            cacheMetadataHP = hpdata,
            cacheMetadataState = statedata,
            cacheMaps = maps.AsArray(),
            stateDict = stateDict,
            cacheBlockCodes = codes,
            pregen = pregen
        };
        job = amJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        // Pre-Convert data back
        cacheVoxdata = vox.ToArray();
        cacheMetadataHP = hpdata.ToArray();
        cacheMetadataState = statedata.ToArray();
        cacheHeightMap = map1.ToArray();

        // Pre=Dispose Bin
        vox.Dispose();
        hpdata.Dispose();
        statedata.Dispose();
        
        // Applies Structs from other chunks
        
        if(Structure.Exists(currentChunk)){
            Structure.RoughApply(cacheVoxdata, cacheMetadataHP, cacheMetadataState, Structure.GetChunk(currentChunk));
            Structure.RemoveChunk(currentChunk);
        }
        

        // Structures
        GenerateForestStructures(currentChunk, xhash, zhash, currentBiome, (xTransition || zTransition || xzTransition));

        // Cave System
        NativeArray<ushort> voxCU = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpCU = new NativeArray<ushort>(cacheMetadataHP, Allocator.TempJob);
        NativeArray<ushort> stateCU = new NativeArray<ushort>(cacheMetadataState, Allocator.TempJob);
        NativeArray<ushort> cacheCave = GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:30, maskThreshold:0.64f);

        
        CutUndergroundJob cuJob = new CutUndergroundJob{
            cacheVox = voxCU,
            cacheHP = hpCU,
            cacheState = stateCU,
            cacheCave = cacheCave,
            upper = 30,
            lower = 1,
        };
        job = cuJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();


        // Convert Data Back
        cacheVoxdata = voxCU.ToArray();
        cacheMetadataHP = hpCU.ToArray();
        cacheMetadataState = stateCU.ToArray();

        // Dispose Bin
        voxCU.Dispose();
        hpCU.Dispose();
        stateCU.Dispose();
        codes.Dispose();
        stateDict.Dispose();
        maps.Dispose();
        waterLevels.Dispose();
        cacheCave.Dispose();

        map1.Dispose();
        map2.Dispose();
        map3.Dispose();        
        map4.Dispose();

        return new VoxelData(cacheVoxdata);
    }

    // Inserts Structures into Forest Biome
    private void GenerateForestStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){

        foreach(int structCode in BiomeHandler.GetBiomeStructs(biomeCode)){
            
            if(structCode == 6){ // Big Tree
                if(!transition){
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1);
                }
            }
            else if(structCode == 1 || structCode == 2 || structCode == 7 || structCode == 8){
                if(!transition){
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1);
                }
            }
            else if(structCode == 3 || structCode == 4){ // Dirt Piles
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 2, range:true);
            }
            else if(structCode >= 9 && structCode <= 11){ // Metal Veins
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 8, range:true);
            }
        }
    }
}


// =====================================================================



/*
MULTITHREADING JOBS
*/
[BurstCompile]
public struct RidgedMultiFractalJob : IJobParallelFor{

    public int chunkX;
    public int chunkZ;
    public float xhash;
    public float yhash;
    public float zhash;
    public float threshold;
    public float generationSeed;
    public int ceiling; 
    public float maskThreshold;

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> turbulenceMap;

    public void Execute(int index){
        int size = Chunk.chunkWidth;
        int j = 0;
        float val;
        float mask;

        int x = chunkX+index;

        j = 0;
        for(int z=chunkZ;z<chunkZ+size;z++){

            mask = Perlin.Noise((x^z)*(xhash*yhash)*1.07f, z*zhash*0.427f);
            for(int y=0;y<Chunk.chunkDepth;y++){
                if(mask < maskThreshold){
                    turbulenceMap[(index*Chunk.chunkDepth*Chunk.chunkWidth)+(y*size)+j] = 0;
                    continue;
                }

                val = Perlin.RidgedMultiFractal(x*xhash*generationSeed, y*yhash*generationSeed, z*zhash*generationSeed);

                if(val >= threshold && y <= ceiling){
                    turbulenceMap[(index*Chunk.chunkDepth*Chunk.chunkWidth)+(y*size)+j] = 1;
                }
                else{
                    turbulenceMap[(index*Chunk.chunkDepth*Chunk.chunkWidth)+(y*size)+j] = 0;
                }
            }
            j++;
        }
    }
}

[BurstCompile]
public struct ApplyMapsJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheVoxdata;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheMetadataHP;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheMetadataState;

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheMaps;
    [ReadOnly]
    public NativeHashMap<ushort, ushort> stateDict;
    [ReadOnly]
    public NativeList<ushort> cacheBlockCodes;
    [ReadOnly]
    public bool pregen;

    public void Execute(int index){
        int size = Chunk.chunkWidth;
        int i=0;

        // Builds the chunk normally
        if(!pregen){
            for(i=0;i<cacheBlockCodes.Length;i++){
                // Heightmap Drawing
                int x = index;
                for(int z=0;z<size;z++){
                    // If it's the first layer to be added
                    if(i == 0){
                        for(int y=0;y<Chunk.chunkDepth;y++){
                            if(y <= cacheMaps[i*(size+1)*(size+1)+x*(size+1)+z]){
                                cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i]; // Adds block code
                                if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                    cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                }
                            }
                            else
                                cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                        }
                    }
                    // If is not the first layer
                    else{
                        for(int y=cacheMaps[(i-1)*(size+1)*(size+1)+x*(size+1)+z]+1;y<=cacheMaps[i*(size+1)*(size+1)+x*(size+1)+z];y++){
                            cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i];

                            if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                            }
                        }               
                    }
                }
                
            }
        }
        // Builds chunk ignoring pregen blocks
        else{
            for(i=0;i<cacheBlockCodes.Length;i++){
                // Heightmap Drawing
                int x = index;
                for(int z=0;z<size;z++){
                    // If it's the first layer to be added
                    if(i == 0){
                        for(int y=0;y<Chunk.chunkDepth;y++){
                            if(y <= cacheMaps[i*(size+1)*(size+1)+x*(size+1)+z]){
                                // Only adds to air blocks
                                if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == 0){
                                    cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i]; // Adds block code
                                    
                                    if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                        cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                    }
                                }
                                // Convertion of pregen air blocks
                                if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == (ushort)(ushort.MaxValue/2))
                                    cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                            }
                            else
                                if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == (ushort)(ushort.MaxValue/2))
                                    cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                        }
                    }
                    // If is not the first layer
                    else{
                        for(int y=cacheMaps[(i-1)*(size+1)*(size+1)+x*(size+1)+z]+1;y<=cacheMaps[i*(size+1)*(size+1)+x*(size+1)+z];y++){
                            if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == 0){
                                cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i]; // Adds block code
                                
                                if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                    cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                }
                            }
                            // Convertion of pregen air blocks
                            if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == (ushort)(ushort.MaxValue/2)){
                                cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                                cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = ushort.MaxValue;
                            }
                        }
                    }
                }
                
            }
        }

    }
}


[BurstCompile]
public struct AddFromMapJob : IJobParallelFor{
    [ReadOnly]
    public NativeArray<ushort> inMap;

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> outMap;

    [ReadOnly]
    public int val;

    // Quick adds all elements in heightMap
    // This makes it easy to get a layer below surface blocks
    // Takes any cache and returns on cacheNumber
    public void Execute(int index){
        int x = index;
        int i;
        int height;

        for(int z=0;z<Chunk.chunkWidth;z++){
            i = x*(Chunk.chunkWidth+1)+z;
            height = inMap[i] + val;

            if(height > Chunk.chunkDepth){
                outMap[i] = (ushort)Chunk.chunkDepth;
            }
            else if(height < 0){
                outMap[i] = 0;
            }
            else{
                outMap[i] = (ushort)(inMap[i] + val);
            }
        }
    }
}


[BurstCompile]
public struct GenerateFlatMapJob : IJobParallelFor{
    [ReadOnly]
    public ushort ceiling;

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> selectedCache;

    // Generates Flat Map of something
    // Consider using this for biomes that are relatively low altitude
    public void Execute(int index){
        if(ceiling >= Chunk.chunkDepth)
            ceiling = (ushort)(Chunk.chunkDepth-1);

        int x = index;
        for(int z=0; z<Chunk.chunkWidth;z++){
            selectedCache[x*(Chunk.chunkWidth+1)+z] = ceiling;
        }
    }
}


[BurstCompile]
public struct BilinearInterpolateJob : IJob{
    [ReadOnly]
    public int interval;

    public NativeArray<ushort> heightMap;

    // Applies bilinear interpolation to a given pivotmap
    // Takes any cache and returns on itself
    public void Execute(){
        int size = Chunk.chunkWidth;
        int interpX = 0;
        int interpZ = 0;
        float step = 1f/interval;
        float scaleX = 0f;
        float scaleZ = 0f;

        // Bilinear Interpolation
        for(int z=0;z<size;z++){
            if(z%interval == 0){
                interpZ+=interval;
                scaleZ = step;
            }
            for(int x=0;x<size;x++){
                // If is a pivot in X axis
                if(x%interval == 0){
                    interpX+=interval;
                    scaleX = step;
                }

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(((heightMap[(interpX-interval)*(Chunk.chunkWidth+1)+(interpZ-interval)])*(1-scaleX)*(1-scaleZ)) + (heightMap[interpX*(Chunk.chunkWidth+1)+(interpZ-interval)]*scaleX*(1-scaleZ)) + (heightMap[(interpX-interval)*(Chunk.chunkWidth+1)+interpZ]*scaleZ*(1-scaleX)) + (heightMap[interpX*(Chunk.chunkWidth+1)+interpZ]*scaleX*scaleZ)));
                scaleX += step;

            }
            interpX = 0;
            scaleX = 0;
            scaleZ += step;
        }   
    }
}


[BurstCompile]
public struct CombinePivotMapJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> outMap;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> inMap;

    // Applies Octaves to Pivot Map
    public void Execute(int index){
        int x = index*4;
        int i;
        
        for(int z=0;z<=Chunk.chunkWidth;z+=4){
            i = x*(Chunk.chunkWidth+1)+z;
            outMap[i] = (ushort)(Mathf.FloorToInt((outMap[i] + inMap[i])/2));
        }  
    }
}


[BurstCompile]
public struct CutUndergroundJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheVox;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheHP;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheState;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> cacheCave;

    [ReadOnly]
    public int lower, upper;

    // Ignores water!
    public void Execute(int index){
        int x = index;

        for(int y=lower;y<upper;y++){
            for(int z=0;z<Chunk.chunkWidth;z++){
                if(cacheVox[(x*Chunk.chunkWidth*Chunk.chunkDepth)+(y*Chunk.chunkWidth)+z] >= 1 && cacheVox[(x*Chunk.chunkWidth*Chunk.chunkDepth)+(y*Chunk.chunkWidth)+z] != 6 && cacheCave[(x*Chunk.chunkWidth*Chunk.chunkDepth)+(y*Chunk.chunkWidth)+z] == 1){
                    cacheVox[(x*Chunk.chunkWidth*Chunk.chunkDepth)+(y*Chunk.chunkWidth)+z] = 0;
                    cacheHP[(x*Chunk.chunkWidth*Chunk.chunkDepth)+(y*Chunk.chunkWidth)+z] = ushort.MaxValue;
                    cacheState[(x*Chunk.chunkWidth*Chunk.chunkDepth)+(y*Chunk.chunkWidth)+z] = ushort.MaxValue;
                }
            }
        } 
    }        
}


[BurstCompile]
public struct GeneratePivotsJob : IJob{
    [ReadOnly]
    public int chunkX, chunkZ;
    [ReadOnly]
    public int octave, groundLevel, ceilingLevel;
    [ReadOnly]
    public float xhash, zhash, worldSeed, generationSeed, offsetHash, dispersionSeed;
    [ReadOnly]
    public byte currentBiome;
    [ReadOnly]
    public byte xBiome, zBiome, xzBiome;

    public NativeArray<ushort> selectedCache;


    // Generates Pivot heightmaps
    public void Execute(){
        int size = Chunk.chunkWidth;
        int chunkXmult = chunkX * size;
        int chunkZmult = chunkZ * size;
        int i = 0;
        int j = 0;

        // Heightmap Sampling
        for(int x=chunkXmult;x<chunkXmult+size;x+=4){
            j = 0;
            for(int z=chunkZmult;z<chunkZmult+size;z+=4){
                selectedCache[i*(Chunk.chunkWidth+1)+j] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x*generationSeed/xhash+offsetHash, z*generationSeed/zhash+offsetHash))), 0, ceilingLevel));
                j+=4;
            }
            i+=4;
        }

        // Look Ahead to X+
        switch(xBiome){
            case 0:
                MixPlainsBorderPivots(false, octave);
                break;
            case 1:
                MixGrassyHighlandsBorderPivots(false, octave);
                break;
            case 2:
                MixOceanBorderPivots(false, octave, currentBiome:currentBiome);
                break;
            case 3:
                MixForestBorderPivots(false, octave); 
                break;               
            default:
                MixPlainsBorderPivots(false, octave);
                break;
        }

        // Look Ahead to Z+
        switch(zBiome){
            case 0:
                MixPlainsBorderPivots(true, octave);
                break;
            case 1:
                MixGrassyHighlandsBorderPivots(true, octave);
                break;
            case 2:
                MixOceanBorderPivots(true, octave, currentBiome:currentBiome);
                break;
            case 3:
                MixForestBorderPivots(true, octave);
                break;
            default:
                break;
        }

        // Look ahead into XZ+
        switch(xzBiome){
            case 0:
                MixPlainsBorderPivots(true, octave, corner:true);
                break;
            case 1:
                MixGrassyHighlandsBorderPivots(true, octave, corner:true);
                break;
            case 2:
                MixOceanBorderPivots(true, octave, corner:true, currentBiome:currentBiome);
                break;
            case 3:
                MixForestBorderPivots(true, octave, corner:true);
                break;
            default:
                break;
        }   
    }

    // Builds Lookahead pivots for biome border smoothing
    private void GenerateLookAheadPivots(float xhashs, float zhashs, bool isSide, int groundLevel=0, int ceilingLevel=99){
        int size = Chunk.chunkWidth;
        int xSize = chunkX * size;
        int zSize = chunkZ * size;
        int i = 0;

        // If is looking ahead into X+ Chunk
        if(isSide){
            for(int x=xSize; x<=xSize+size; x+=4){
                selectedCache[i*(Chunk.chunkWidth+1)+size] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x*generationSeed/xhashs+offsetHash, (zSize+size)*generationSeed/zhashs+offsetHash))), 0, ceilingLevel));
                i+=4;
            }
        }
        // If is looking ahead into Z+ Chunk
        else{
            for(int z=zSize; z<zSize+size; z+=4){ // Don't generate corner twice
                selectedCache[size*(Chunk.chunkWidth+1)+i] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise((xSize+size)*generationSeed/xhashs+offsetHash, z*generationSeed/zhashs+offsetHash))), 0, ceilingLevel));
                i+=4;
            }
        }
    }

    // Builds Lookahead pivot of corner chunk for biome border smoothing
    private void GenerateLookAheadCorner(float xhashs, float zhashs, int groundLevel=0, int ceilingLevel=99){
        selectedCache[selectedCache.Length-1] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(((chunkX+1)*Chunk.chunkWidth*generationSeed)/xhashs+offsetHash, ((chunkZ+1)*Chunk.chunkWidth*generationSeed)/zhashs+offsetHash))), 0, ceilingLevel));
    }

    /*
    BIOME SPECIFIC MIX FUNCTIONS FOR BURST COMPILER
    */

    // Inserts Plains biome border pivots on another selectedHeightMap
    private void MixPlainsBorderPivots(bool isSide, int octave, bool corner=false){
        float xhashs = 41.21f;
        float zhashs = 105.243f;
        float xmod = 1.712f;
        float zmod = 2.511f;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(xhashs, zhashs, isSide, groundLevel:20, ceilingLevel:25);
            else
                GenerateLookAheadPivots(xhashs*xmod, zhashs*zmod, isSide, groundLevel:18, ceilingLevel:30);
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(xhashs, zhashs, groundLevel:20, ceilingLevel:25);
            else
                GenerateLookAheadCorner(xhashs*xmod, zhashs*zmod, groundLevel:18, ceilingLevel:30);
        }
    }

    // Inserts Grassy Highlands biome border pivots on another selectedHeightMap
    private void MixGrassyHighlandsBorderPivots(bool isSide, int octave, bool corner=false){
        float xhashs = 41.21f;
        float zhashs = 105.243f;
        float xmod = 0.712f;
        float zmod = 0.2511f;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(xhashs, zhashs, isSide, groundLevel:30, ceilingLevel:50);
            else
                GenerateLookAheadPivots(xhashs*xmod, zhashs*zmod, isSide, groundLevel:30, ceilingLevel:70);            
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(xhashs, zhashs, groundLevel:30, ceilingLevel:50);
            else
                GenerateLookAheadCorner(xhashs*xmod, zhashs*zmod, groundLevel:30, ceilingLevel:70);

        }
    }

    // Inserts Ocean border pivots on another selectedHeightMap
    private void MixOceanBorderPivots(bool isSide, int octave, bool corner=false, byte currentBiome=255){
        float xhashs = 54.7f;
        float zhashs = 69.3f;
        float xmod = 0.112f;
        float zmod = 0.31f;

        if(currentBiome != 2){
            if(!corner){
                if(octave == 0){
                    GenerateLookAheadPivots(xhashs, zhashs, isSide, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadPivots(xhashs*xmod, zhashs*zmod, isSide, groundLevel:1, ceilingLevel:20);            
                }
            }
            else{
                if(octave == 0){
                    GenerateLookAheadCorner(xhashs, zhashs, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadCorner(xhashs*xmod, zhashs*zmod, groundLevel:1, ceilingLevel:20);
                }

            }
        }
        else{
            if(!corner){
                if(octave == 0){
                    GenerateLookAheadPivots(xhashs, zhashs, isSide, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadPivots(xhashs*xmod, zhashs*zmod, isSide, groundLevel:1, ceilingLevel:20);          
                }
            }
            else{
                if(octave == 0){
                    GenerateLookAheadCorner(xhashs, zhashs, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadCorner(xhashs*xmod, zhashs*zmod, groundLevel:1, ceilingLevel:20);
                }

            }            
        }
    }

    // Inserts Forest biome border pivots on another selectedHeightMap
    private void MixForestBorderPivots(bool isSide, int octave, bool corner=false){
        float xhashs = 72.117f;
        float zhashs = 45.483f;
        float xmod = 1.712f;
        float zmod = 2.511f;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(xhashs, zhashs, isSide, groundLevel:25, ceilingLevel:32);
            else
                GenerateLookAheadPivots(xhashs*xmod, zhashs*zmod, isSide, groundLevel:25, ceilingLevel:45);
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(xhashs, zhashs, groundLevel:25, ceilingLevel:32);
            else
                GenerateLookAheadCorner(xhashs*xmod, zhashs*zmod, groundLevel:25, ceilingLevel:45);
        }
    }

}
