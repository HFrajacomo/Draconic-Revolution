using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Random = System.Random;
using System.IO;
using System.Text;


public class WorldGenerator
{
	// World Gen Settings
	public int worldSeed;
	public BiomeHandler biomeHandler;
	public ChunkLoader_Server cl;
    private Random rng;

    // Prefab System
    public StructureHandler structHandler;
    public StructureGenerator structureGenerator;

	// Cached Data
	private	ushort[] cacheVoxdata = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataHP = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataState = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private float[] cacheHeightMap = new float[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
    private byte cacheBiome;

    // Native Noise Maps
    private NativeArray<byte> baseMap;
    private NativeArray<byte> erosionMap;
    private NativeArray<byte> peakMap;
    private NativeArray<byte> temperatureMap;
    private NativeArray<byte> humidityMap;
    private NativeArray<byte> patchMap;
    private NativeArray<byte> caveMap;
    private NativeArray<byte> maskMap;

    // Other Native Objects
    private NativeHashSet<ushort> caveFreeBlocks;
    private ushort[] caveFreeBlocksArray = new ushort[]{6};

    // Biome Blending Map
    private NativeArray<ushort> biomeBlendingBlock;


    public WorldGenerator(int worldSeed, BiomeHandler biomeReference, StructureHandler structHandler, ChunkLoader_Server reference){
    	this.worldSeed = worldSeed;
    	this.biomeHandler = biomeReference;
    	this.cl = reference;
    	this.structHandler = structHandler;
        this.structureGenerator = new StructureGenerator(this);

        baseMap = new NativeArray<byte>(GenerationSeed.baseNoise, Allocator.Persistent);
        erosionMap = new NativeArray<byte>(GenerationSeed.erosionNoise, Allocator.Persistent);
        peakMap = new NativeArray<byte>(GenerationSeed.peakNoise, Allocator.Persistent);
        temperatureMap = new NativeArray<byte>(GenerationSeed.temperatureNoise, Allocator.Persistent);
        humidityMap = new NativeArray<byte>(GenerationSeed.humidityNoise, Allocator.Persistent);
        patchMap = new NativeArray<byte>(GenerationSeed.patchNoise, Allocator.Persistent);
        caveMap = new NativeArray<byte>(GenerationSeed.caveNoise, Allocator.Persistent);
        maskMap = new NativeArray<byte>(GenerationSeed.cavemaskNoise, Allocator.Persistent);

        caveFreeBlocks = new NativeHashSet<ushort>(0, Allocator.Persistent);
        FillCaveFreeBlocks();
    }

    public WorldGenerator(int seed){
        this.worldSeed = seed;

        baseMap = new NativeArray<byte>(GenerationSeed.baseNoise, Allocator.Persistent);
        erosionMap = new NativeArray<byte>(GenerationSeed.erosionNoise, Allocator.Persistent);
        peakMap = new NativeArray<byte>(GenerationSeed.peakNoise, Allocator.Persistent);
        temperatureMap = new NativeArray<byte>(GenerationSeed.temperatureNoise, Allocator.Persistent);
        humidityMap = new NativeArray<byte>(GenerationSeed.humidityNoise, Allocator.Persistent);
        patchMap = new NativeArray<byte>(GenerationSeed.patchNoise, Allocator.Persistent);
        caveMap = new NativeArray<byte>(GenerationSeed.caveNoise, Allocator.Persistent);
        maskMap = new NativeArray<byte>(GenerationSeed.cavemaskNoise, Allocator.Persistent);

        caveFreeBlocks = new NativeHashSet<ushort>(0, Allocator.Persistent);
        FillCaveFreeBlocks();
    }

    public void SetBiomeBlending(ushort[] blendingArray){
        this.biomeBlendingBlock = new NativeArray<ushort>(blendingArray, Allocator.Persistent);
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
    public string GetCacheBiome(){
        return BiomeHandler.ByteToBiome(this.cacheBiome);
    }

    public void FillCaveFreeBlocks(){
        foreach(ushort block in this.caveFreeBlocksArray){
            this.caveFreeBlocks.Add(block);
        }
    }

    public void ClearCaches(){
        Array.Clear(this.cacheVoxdata, 0, this.cacheVoxdata.Length);
        Array.Clear(this.cacheMetadataHP, 0, this.cacheMetadataHP.Length);
        Array.Clear(this.cacheMetadataState, 0, this.cacheMetadataState.Length);
    }

    public void DestroyNativeMemory(){
        this.baseMap.Dispose();
        this.erosionMap.Dispose();
        this.peakMap.Dispose();
        this.temperatureMap.Dispose();
        this.humidityMap.Dispose();
        this.patchMap.Dispose();
        this.caveMap.Dispose();
        this.maskMap.Dispose();
        this.biomeBlendingBlock.Dispose();

        this.caveFreeBlocks.Dispose();
    }


    // Applies Structures to a chunk
    /*
    Depth Values represent how deep below heightmap things will go.
    Range represents if structure always spawn at given Depth, or if it spans below as well
    */
    public void GenerateStructures(ChunkPos pos, BiomeCode biome, int structureCode, int depth, ushort[] blockdata, ushort[] statedata, ushort[] hpdata, int heightlimit=0, bool range=false){
        // Gets index of amount and percentage
        int index = BiomeHandler.GetBiomeStructs(biome).IndexOf(structureCode);
        int amount = BiomeHandler.GetBiomeAmounts(biome)[index];

        float percentage = BiomeHandler.GetBiomePercentages(biome)[index];

        int x,y,z;
        int rotation = 0;
        float chance;
        this.rng = new Random((int)(int.MaxValue * PatchNoise((pos.z^(pos.x * pos.x))*Chunk.chunkWidth*GenerationSeed.patchNoiseStep3)));


        // If structure is static at given heightmap depth
        if(!range){
            for(int i=1; i <= amount; i++){
                chance = (float)this.rng.NextDouble();

                if(chance > percentage)
                    continue;

                rotation = this.rng.Next(0, 4);

                x = this.rng.Next(0, Chunk.chunkWidth);
                z = this.rng.Next(0, Chunk.chunkWidth);
                y = (int)cacheHeightMap[x*(Chunk.chunkWidth+1)+z] - depth;


                // Ignores structure on hard limit
                if(y <= heightlimit)
                    continue;

                Structure structure = this.structHandler.LoadStructure(structureCode);

                if(structure.AcceptBaseBlock(blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z]))
                    structure.Apply(this.cl, pos, blockdata, hpdata, statedata, x, y, z, rotation:rotation); 
            }
        }
        // If can be placed in a range
        else{
            for(int i=1; i <= amount; i++){
                chance = (float)this.rng.NextDouble();

                if(chance > percentage)
                    continue;

                rotation = this.rng.Next(0, 4);

                x = this.rng.Next(0, Chunk.chunkWidth);
                z = this.rng.Next(0, Chunk.chunkWidth);
                float yMult = (float)this.rng.NextDouble(); 

                y = (int)(heightlimit + yMult*(cacheHeightMap[x*(Chunk.chunkWidth+1)+z] - depth));

                // Ignores structure on hard limit
                if(y <= heightlimit || y == 0)
                    continue;

                Structure structure = this.structHandler.LoadStructure(structureCode);

                if(structure.AcceptBaseBlock(blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z]))
                    structure.Apply(this.cl, pos, blockdata, hpdata, statedata, x, y, z, rotation:rotation);
            }            
        }
    }

    // Generates a Chunk
    public void GenerateChunk(ChunkPos pos, bool isPregen=false){
        NativeArray<ushort> voxelData = NativeTools.CopyToNative(cacheVoxdata);
        NativeArray<ushort> stateData = NativeTools.CopyToNative(cacheMetadataState);
        NativeArray<ushort> hpData = NativeTools.CopyToNative(cacheMetadataHP);
        NativeArray<float> heightMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        GenerateChunkJob gcj = new GenerateChunkJob{
            chunkX = pos.x,
            chunkZ = pos.z,
            blockData = voxelData,
            stateData = stateData,
            hpData = hpData,
            heightMap = heightMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap,
            peakNoise = peakMap,
            pregen = isPregen
        };
        JobHandle job = gcj.Schedule();
        job.Complete();

        /*
        Figuring out the neighboring biomes
        */

        byte xpBiome, zpBiome, zmBiome, xzpBiome, xpzmBiome, xmzpBiome;

        // This Chunk
        float[] chunkInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, pos.z*Chunk.chunkWidth);
        this.cacheBiome = this.biomeHandler.AssignBiome(chunkInfo);

        // X+ Chunk
        float[] xPlusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, pos.z*Chunk.chunkWidth);
        xpBiome = this.biomeHandler.AssignBiome(xPlusInfo);

        // Z+ Chunk
        float[] zPlusInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth);
        zpBiome = this.biomeHandler.AssignBiome(zPlusInfo);

        // Z- Chunk
        float[] zMinusInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth);
        zmBiome = this.biomeHandler.AssignBiome(zMinusInfo);

        // XZ+ Chunk
        float[] xzPlusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth);
        xzpBiome = this.biomeHandler.AssignBiome(xzPlusInfo);

        // X+Z- Chunk
        float[] xpzmMinusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth);
        xpzmBiome = this.biomeHandler.AssignBiome(xpzmMinusInfo);

        // X-Z+ Chunk
        float[] xmzpMinusInfo = this.BiomeNoise((pos.x-1)*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth);
        xmzpBiome = this.biomeHandler.AssignBiome(xmzpMinusInfo);

        PopulateChunkJob pcj = new PopulateChunkJob{
            heightMap = heightMap,
            blockData = voxelData,
            patchNoise = patchMap,
            pos = pos,
            blendingBlock = biomeBlendingBlock,
            biome = this.cacheBiome,
            xpBiome = xpBiome,
            zpBiome = zpBiome,
            zmBiome = zmBiome,
            xzpBiome = xzpBiome,
            xpzmBiome = xpzmBiome,
            xmzpBiome = xmzpBiome
        };
        job = pcj.Schedule();
        job.Complete();

        GenerateCaveJob gcavej = new GenerateCaveJob{
            pos = pos,
            blockData = voxelData,
            stateData = stateData,
            heightMap = heightMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            caveFreeBlocks = caveFreeBlocks
        };
        job = gcavej.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        cacheVoxdata = NativeTools.CopyToManaged(voxelData);
        cacheMetadataState = NativeTools.CopyToManaged(stateData);
        cacheMetadataHP = NativeTools.CopyToManaged(hpData);
        cacheHeightMap = NativeTools.CopyToManaged(heightMap);

        structureGenerator.GenerateBiomeStructures(cl, pos, (BiomeCode)this.cacheBiome, cacheVoxdata, cacheMetadataState, cacheMetadataHP);

        voxelData.Dispose();
        stateData.Dispose();
        hpData.Dispose();
        heightMap.Dispose();
    }


    public float PatchNoise(float x)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        return Normalize(Lerp(u, Grad(GenerationSeed.patchNoise[X], x), Grad(GenerationSeed.patchNoise[X+1], x-1)));
    }

    public float PatchNoise(float x, float y){
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = (GenerationSeed.patchNoise[X  ] + Y) & 0xff;
        int B = (GenerationSeed.patchNoise[X+1] + Y) & 0xff;
        return Normalize(Lerp(v, Lerp(u, Grad(GenerationSeed.patchNoise[A  ], x, y  ), Grad(GenerationSeed.patchNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.patchNoise[A+1], x, y-1), Grad(GenerationSeed.patchNoise[B+1], x-1, y-1))));
    }

    private float Normalize(float x){
        return (1 + x)/2;
    }

    /*
    Biome Generation
    */

    public float[] BiomeNoise(float x, float y)
    {
        x += 8;
        y += 8;
        float initialX = x;
        float initialY = y;


        float[] biomeVector = new float[5];


        // Temperature Noise
        x = initialX*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetX[0];
        x -= Mathf.Floor(x);
        y = initialY*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetY[0];
        y -= Mathf.Floor(y);
        float u = Fade(x);
        float v = Fade(y);

        float x2 = initialX*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetX[0];
        x2 -= Mathf.Floor(x2);
        float y2 = initialY*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetY[0];
        y2 -= Mathf.Floor(y2);
        
        float u2 = Fade(x2);
        float v2 = Fade(y2);

        int X = Mathf.FloorToInt(initialX*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetX[0]) & 0xff;
        int Y = Mathf.FloorToInt(initialY*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetY[0]) & 0xff;
        int X2 = Mathf.FloorToInt(initialX*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetX[0]) & 0xff;
        int Y2 = Mathf.FloorToInt(initialY*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetY[0]) & 0xff;
        int A = (GenerationSeed.temperatureNoise[X  ] + Y) & 0xff;
        int B = (GenerationSeed.temperatureNoise[X+1] + Y) & 0xff;
        int A2 = (GenerationSeed.temperatureNoise[X2  ] + Y2) & 0xff;
        int B2 = (GenerationSeed.temperatureNoise[X2+1] + Y2) & 0xff;
        biomeVector[0] = TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.temperatureNoise[A  ], x, y  ), Grad(GenerationSeed.temperatureNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.temperatureNoise[A+1], x, y-1), Grad(GenerationSeed.temperatureNoise[B+1], x-1, y-1))), 
                        Lerp(v2, Lerp(u2, Grad(GenerationSeed.temperatureNoise[A2  ], x2, y2  ), Grad(GenerationSeed.temperatureNoise[B2  ], x2-1, y2  )),
                       Lerp(u2, Grad(GenerationSeed.temperatureNoise[A2+1], x2, y2-1), Grad(GenerationSeed.temperatureNoise[B2+1], x2-1, y2-1))));
        
        // Humidity Noise
        x = initialX*GenerationSeed.humidityNoiseStep1 + GenerationSeed.humidityOffsetX[0];
        x -= Mathf.Floor(x);
        y = initialY*GenerationSeed.humidityNoiseStep1 + GenerationSeed.humidityOffsetY[0];
        y -= Mathf.Floor(y);
        u = Fade(x);
        v = Fade(y);

        x2 = initialX*GenerationSeed.humidityNoiseStep2 + GenerationSeed.humidityOffsetX[0];
        x2 -= Mathf.Floor(x2);
        y2 = initialY*GenerationSeed.humidityNoiseStep2 + GenerationSeed.humidityOffsetY[0];
        y2 -= Mathf.Floor(y2);
        
        u2 = Fade(x2);
        v2 = Fade(y2);

        X = Mathf.FloorToInt(initialX*GenerationSeed.humidityNoiseStep1 + GenerationSeed.humidityOffsetX[0]) & 0xff;
        Y = Mathf.FloorToInt(initialY*GenerationSeed.humidityNoiseStep1 + GenerationSeed.humidityOffsetY[0]) & 0xff;
        X2 = Mathf.FloorToInt(initialX*GenerationSeed.humidityNoiseStep2 + GenerationSeed.humidityOffsetX[0]) & 0xff;
        Y2 = Mathf.FloorToInt(initialY*GenerationSeed.humidityNoiseStep2 + GenerationSeed.humidityOffsetY[0]) & 0xff;
        A = (GenerationSeed.humidityNoise[X  ] + Y) & 0xff;
        B = (GenerationSeed.humidityNoise[X+1] + Y) & 0xff;
        A2 = (GenerationSeed.humidityNoise[X2  ] + Y2) & 0xff;
        B2 = (GenerationSeed.humidityNoise[X2+1] + Y2) & 0xff;
        biomeVector[1] = TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.humidityNoise[A  ], x, y  ), Grad(GenerationSeed.humidityNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.humidityNoise[A+1], x, y-1), Grad(GenerationSeed.humidityNoise[B+1], x-1, y-1))), 
                        Lerp(v2, Lerp(u, Grad(GenerationSeed.humidityNoise[A2  ], x2, y2  ), Grad(GenerationSeed.humidityNoise[B2  ], x2-1, y2  )),
                       Lerp(u2, Grad(GenerationSeed.humidityNoise[A2+1], x2, y2-1), Grad(GenerationSeed.humidityNoise[B2+1], x2-1, y2-1))));

        // Base Noise
        x = initialX*GenerationSeed.baseNoiseStep1;
        x -= Mathf.Floor(x);
        y = initialY*GenerationSeed.baseNoiseStep1;
        y -= Mathf.Floor(y);
        u = Fade(x);
        v = Fade(y);

        x2 = initialX*GenerationSeed.baseNoiseStep2;
        x2 -= Mathf.Floor(x2);
        y2 = initialY*GenerationSeed.baseNoiseStep2;
        y2 -= Mathf.Floor(y2);

        u2 = Fade(x2);
        v2 = Fade(y2);

        X = Mathf.FloorToInt(initialX*GenerationSeed.baseNoiseStep1) & 0xff;
        Y = Mathf.FloorToInt(initialY*GenerationSeed.baseNoiseStep1) & 0xff;
        X2 = Mathf.FloorToInt(initialX*GenerationSeed.baseNoiseStep2) & 0xff;
        Y2 = Mathf.FloorToInt(initialY*GenerationSeed.baseNoiseStep2) & 0xff;
        A = (GenerationSeed.baseNoise[X  ] + Y) & 0xff;
        B = (GenerationSeed.baseNoise[X+1] + Y) & 0xff;
        A2 = (GenerationSeed.baseNoise[X2  ] + Y2) & 0xff;
        B2 = (GenerationSeed.baseNoise[X2+1] + Y2) & 0xff;

        biomeVector[2] = TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.baseNoise[A  ], x, y  ), Grad(GenerationSeed.baseNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.baseNoise[A+1], x, y-1), Grad(GenerationSeed.baseNoise[B+1], x-1, y-1))), 
                        Lerp(v2, Lerp(u2, Grad(GenerationSeed.baseNoise[A2  ], x2, y2  ), Grad(GenerationSeed.baseNoise[B2  ], x2-1, y2  )),
                       Lerp(u2, Grad(GenerationSeed.baseNoise[A2+1], x2, y2-1), Grad(GenerationSeed.baseNoise[B2+1], x2-1, y2-1))));

        // Erosion Noise
        x = initialX*GenerationSeed.erosionNoiseStep1;
        x -= Mathf.Floor(x);
        y = initialY*GenerationSeed.erosionNoiseStep1;
        y -= Mathf.Floor(y);
        u = Fade(x);
        v = Fade(y);

        x2 = initialX*GenerationSeed.erosionNoiseStep2;
        x2 -= Mathf.Floor(x2);
        y2 = initialY*GenerationSeed.erosionNoiseStep2;
        y2 -= Mathf.Floor(y2);
        
        u2 = Fade(x2);
        v2 = Fade(y2);

        X = Mathf.FloorToInt(initialX*GenerationSeed.erosionNoiseStep1) & 0xff;
        Y = Mathf.FloorToInt(initialY*GenerationSeed.erosionNoiseStep1) & 0xff;
        X2 = Mathf.FloorToInt(initialX*GenerationSeed.erosionNoiseStep2) & 0xff;
        Y2 = Mathf.FloorToInt(initialY*GenerationSeed.erosionNoiseStep2) & 0xff;
        A = (GenerationSeed.erosionNoise[X  ] + Y) & 0xff;
        B = (GenerationSeed.erosionNoise[X+1] + Y) & 0xff;
        A2 = (GenerationSeed.erosionNoise[X2  ] + Y2) & 0xff;
        B2 = (GenerationSeed.erosionNoise[X2+1] + Y2) & 0xff;

        biomeVector[3] = TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.erosionNoise[A  ], x, y  ), Grad(GenerationSeed.erosionNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.erosionNoise[A+1], x, y-1), Grad(GenerationSeed.erosionNoise[B+1], x-1, y-1))), 
                        Lerp(v2, Lerp(u2, Grad(GenerationSeed.erosionNoise[A2  ], x2, y2  ), Grad(GenerationSeed.erosionNoise[B2  ], x2-1, y2  )),
                       Lerp(u2, Grad(GenerationSeed.erosionNoise[A2+1], x2, y2-1), Grad(GenerationSeed.erosionNoise[B2+1], x2-1, y2-1))));

        // Peak Noise
        x = initialX*GenerationSeed.peakNoiseStep1;
        x -= Mathf.Floor(x);
        y = initialY*GenerationSeed.peakNoiseStep1;
        y -= Mathf.Floor(y);
        u = Fade(x);
        v = Fade(y);

        x2 = initialX*GenerationSeed.peakNoiseStep2;
        x2 -= Mathf.Floor(x2);
        y2 = initialY*GenerationSeed.peakNoiseStep2;
        y2 -= Mathf.Floor(y2);
        
        u2 = Fade(x2);
        v2 = Fade(y2);

        X = Mathf.FloorToInt(initialX*GenerationSeed.peakNoiseStep1) & 0xff;
        Y = Mathf.FloorToInt(initialY*GenerationSeed.peakNoiseStep1) & 0xff;
        X2 = Mathf.FloorToInt(initialX*GenerationSeed.peakNoiseStep2) & 0xff;
        Y2 = Mathf.FloorToInt(initialY*GenerationSeed.peakNoiseStep2) & 0xff;
        A = (GenerationSeed.peakNoise[X  ] + Y) & 0xff;
        B = (GenerationSeed.peakNoise[X+1] + Y) & 0xff;
        A2 = (GenerationSeed.peakNoise[X2  ] + Y2) & 0xff;
        B2 = (GenerationSeed.peakNoise[X2+1] + Y2) & 0xff;
        biomeVector[4] =  TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.peakNoise[A  ], x, y  ), Grad(GenerationSeed.peakNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.peakNoise[A+1], x, y-1), Grad(GenerationSeed.peakNoise[B+1], x-1, y-1))), 
                        Lerp(v2, Lerp(u2, Grad(GenerationSeed.peakNoise[A2  ], x2, y2  ), Grad(GenerationSeed.peakNoise[B2  ], x2-1, y2  )),
                       Lerp(u2, Grad(GenerationSeed.peakNoise[A2+1], x2, y2-1), Grad(GenerationSeed.peakNoise[B2+1], x2-1, y2-1))));

        return biomeVector;
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
    // Calculates the cumulative distribution function of a Normal Distribution
    private float TransformOctaves(float a, float b){
        float c = (a+b)/2f;

        return (2f/(1f + Mathf.Exp(-c*4.1f)))-1;
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
    public bool pregen;

    // Noises
    [ReadOnly]
    public NativeArray<byte> baseNoise;
    [ReadOnly]
    public NativeArray<byte> erosionNoise;
    [ReadOnly]
    public NativeArray<byte> peakNoise;

    public void Execute(){
        int waterLevel = Constants.WORLD_WATER_LEVEL;
        GeneratePivots();
        BilinearIntepolateMap();
        ApplyMap(waterLevel);
    }

    public void GeneratePivots(){
        float height;
        float erosionMultiplier;
        float peakAdd;

        for(int x=0; x <= Chunk.chunkWidth; x+=4){
            for(int z=0; z <= Chunk.chunkWidth; z+=4){
                height = FindSplineHeight(TransformOctaves(Noise((chunkX*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep1, (chunkZ*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep1, NoiseMap.BASE), (Noise((chunkX*Chunk.chunkWidth+x)*GenerationSeed.baseNoiseStep2, (chunkZ*Chunk.chunkWidth+z)*GenerationSeed.baseNoiseStep2, NoiseMap.BASE))), NoiseMap.BASE);
                erosionMultiplier = FindSplineHeight(TransformOctaves(Noise((chunkX*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep1, (chunkZ*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep1, NoiseMap.EROSION), Noise((chunkX*Chunk.chunkWidth+x)*GenerationSeed.erosionNoiseStep2, (chunkZ*Chunk.chunkWidth+z)*GenerationSeed.erosionNoiseStep2, NoiseMap.EROSION)), NoiseMap.EROSION);
                peakAdd = FindSplineHeight(TransformOctaves(Noise((chunkX*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep1, (chunkZ*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep1, NoiseMap.PEAK), (Noise((chunkX*Chunk.chunkWidth+x)*GenerationSeed.peakNoiseStep2, (chunkZ*Chunk.chunkWidth+z)*GenerationSeed.peakNoiseStep2, NoiseMap.PEAK))), NoiseMap.PEAK);

                heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.CeilToInt((height + PeakErosion(peakAdd, erosionMultiplier)) * erosionMultiplier));
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
    
    // Calculates the cumulative distribution function of a Normal Distribution
    private float TransformOctaves(float a, float b){
        float c = (a+b)/2f;

        return (2f/(1f + Mathf.Exp(-c*4.1f)))-1;
    }

    private float FindSplineHeight(float noiseValue, NoiseMap type){
        int index = GenerationSeed.baseNoiseSplineX.Length-2;

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
        else if(type == NoiseMap.PEAK){
            for(int i=1; i < GenerationSeed.peakNoiseSplineX.Length; i++){
                if(GenerationSeed.peakNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - GenerationSeed.peakNoiseSplineX[index])/(GenerationSeed.peakNoiseSplineX[index+1] - GenerationSeed.peakNoiseSplineX[index]) ;

            if(GenerationSeed.peakNoiseSplineY[index] > GenerationSeed.peakNoiseSplineY[index+1])
                return Mathf.Lerp(GenerationSeed.peakNoiseSplineY[index], GenerationSeed.peakNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
            else
                return Mathf.Lerp(GenerationSeed.peakNoiseSplineY[index], GenerationSeed.peakNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));
        }
        else{
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
        else if(type == NoiseMap.PEAK)
            return Lerp(u, Grad(GenerationSeed.peakNoise[X], x), Grad(GenerationSeed.peakNoise[X+1], x-1)) * 2;
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
        else if(type == NoiseMap.PEAK){
            int A = (peakNoise[X  ] + Y) & 0xff;
            int B = (peakNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(peakNoise[A  ], x, y  ), Grad(peakNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(peakNoise[A+1], x, y-1), Grad(peakNoise[B+1], x-1, y-1)));
        }
        else{
            int A = (baseNoise[X  ] + Y) & 0xff;
            int B = (baseNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(baseNoise[A  ], x, y  ), Grad(baseNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(baseNoise[A+1], x, y-1), Grad(baseNoise[B+1], x-1, y-1)));
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
    private float PeakErosion(float peak, float erosion){
        if(peak > -0.4f)
            return peak*(erosion*erosion);
        return peak;
    }
}

[BurstCompile]
public struct PopulateChunkJob : IJob{
    public NativeArray<ushort> blockData;
    [ReadOnly]
    public NativeArray<float> heightMap;
    [ReadOnly]
    public NativeArray<byte> patchNoise;
    [ReadOnly]
    public NativeArray<ushort> blendingBlock;

    [ReadOnly]
    public ChunkPos pos;
    [ReadOnly]
    public byte biome;
    [ReadOnly]
    public byte xpBiome, zpBiome, zmBiome;
    [ReadOnly]
    public byte xzpBiome, xpzmBiome, xmzpBiome;

    public void Execute(){
        ApplySurfaceDecoration(biome);
        ApplyBiomeBlending();
        ApplyWaterBodyFloor();
    }

    public void ApplySurfaceDecoration(byte biome){
        BiomeCode code = (BiomeCode)biome;
        ushort blockCode;
        int depth = 0;

        if(code == BiomeCode.PLAINS){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else if(depth == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.GRASS;
                            depth++;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.DIRT;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }
        }
        else if(code == BiomeCode.GRASSY_HIGHLANDS){
            float stoneThreshold = 0.1f;
            bool isStoneFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    isStoneFloor = false;
                    depth = 0;

                    if(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) >= stoneThreshold)
                        isStoneFloor = true;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else if(isStoneFloor && depth < 5){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.STONE;
                            depth++;
                        } 
                        else if(depth == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.GRASS;
                            depth++;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.DIRT;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }       
        }
        else if(code == BiomeCode.OCEAN){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SAND;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }             
        }
        else if(code == BiomeCode.FOREST){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else if(depth == 0){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.GRASS;
                            depth++;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.DIRT;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }         
        }
        else if(code == BiomeCode.DESERT){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER)
                            depth++;
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SAND;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }          
        }
        else if(code == BiomeCode.SNOWY_PLAINS){
            float iceThreshold = -0.2f;
            bool isIceFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    isIceFloor = false;

                    if(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) >= iceThreshold){
                        isIceFloor = true;
                    }

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }              
        }
        else if(code == BiomeCode.SNOWY_HIGHLANDS){
            float stoneThreshold = 0.1f;
            bool isStoneFloor = false;
            float iceThreshold = -0.2f;
            bool isIceFloor = false;
            float patchNoise;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    isStoneFloor = false;
                    isIceFloor = false;
                    depth = 0;

                    patchNoise = Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2);
                    isStoneFloor = patchNoise >= stoneThreshold;
                    isIceFloor = patchNoise >= iceThreshold;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else if(isStoneFloor && depth < 5){
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.STONE;
                            depth++;
                        } 
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }
        }
        else if(code == BiomeCode.ICE_OCEAN){
            float iceThreshold = 0f;
            bool isIceFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    isIceFloor = false;

                    if(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) >= iceThreshold)
                        isIceFloor = true;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++; 
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }        
        }
        else if(code == BiomeCode.SNOWY_FOREST){
            float iceThreshold = -0.2f;
            bool isIceFloor = false;

            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    depth = 0;
                    isIceFloor = false;

                    if(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) >= iceThreshold)
                        isIceFloor = true;

                    for(int y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode == (ushort)BlockID.WATER){
                            depth++;
                        }
                        else if(depth == 0 && y < Constants.WORLD_WATER_LEVEL){
                            if(isIceFloor)
                                blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+Constants.WORLD_WATER_LEVEL*Chunk.chunkWidth+z] = (ushort)BlockID.ICE;
                            break;
                        }
                        else{
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SNOW;
                            depth++;
                        }

                        if(depth == 5){
                            break;
                        }
                    }
                }
            }  
        }
    }

    public void ApplyWaterBodyFloor(){
        int depth = 0;
        int finalDepth = 2;
        ushort blockCode;
        int height;

        for(int x=0; x < Chunk.chunkWidth; x++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                depth = 0;
                height = (int)heightMap[x*(Chunk.chunkWidth+1)+z];

                blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+height*Chunk.chunkWidth+z];

                if(blockCode != (ushort)BlockID.WATER)
                    continue;
                else{
                    for(int y=height-1; y > 0; y--){
                        blockCode = blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                        if(blockCode != (ushort)BlockID.WATER){
                            depth++;
                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.SAND;
                        }

                        if(depth == finalDepth)
                            break;
                    }
                }
            }
        }          
    }

    public void ApplyBiomeBlending(){
        int blendingAmount = Chunk.chunkWidth - 3;
        int exageratedBlendingAmount = blendingAmount - (Chunk.chunkWidth - blendingAmount);
        float blendingSafety = 0f;

        if(biome == xpBiome && biome == zpBiome)
            return;
        
        int y;

        // X+ Side
        if(blendingBlock[biome] != blendingBlock[xpBiome]){
            // If needs rounded borders at the top
            if(biome != xpBiome && xpBiome != xzpBiome){
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(x-z > 0 && Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }                
            }
            // If needs rounded borders at the bottom
            else if(biome != xpBiome && xpBiome != xpzmBiome){
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(x+z >= Chunk.chunkWidth && Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }
            }
            // Both edges are rounded
            else if(biome != xpBiome && xpBiome != xzpBiome && xpBiome != xpzmBiome){
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(x+z <= blendingAmount*2 && x-z <= blendingAmount && Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }
            }

            // "Straight" line through border
            else{
                for(int z=Chunk.chunkWidth-1; z >= 0; z--){
                    for(int x=blendingAmount; x < Chunk.chunkWidth; x++){
                        if(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[xpBiome];
                        }
                    }
                }                
            }
        }

        // Z+ Side
        if(blendingBlock[biome] != blendingBlock[zpBiome]){
            // If needs rounded borders at the right
            if(biome != zpBiome && zpBiome != xzpBiome){
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=0; x < Chunk.chunkWidth; x++){
                        if(x-z <= blendingAmount - Chunk.chunkWidth && Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }                
            }
            // If needs rounded borders at the left
            else if(biome != zpBiome && zpBiome != xmzpBiome){
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=0; x < Chunk.chunkWidth; x++){
                        if(x+z >= Chunk.chunkWidth-1 && Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }
            }
            // Both ends are rounded
            else if(biome != zpBiome && zpBiome != xzpBiome && zpBiome != xmzpBiome){
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=0; x < Chunk.chunkWidth; x++){
                        if(x-z <= blendingAmount - Chunk.chunkWidth && x+z >= Chunk.chunkWidth-1 && Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }                
            }
            // "Straight" line through border
            else{
                for(int z=blendingAmount; z < Chunk.chunkWidth; z++){
                    for(int x=Chunk.chunkWidth-1; x >= 0; x--){
                        if(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchNoiseStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchNoiseStep2) < blendingSafety){
                            y = (int)heightMap[x*(Chunk.chunkWidth+1)+z]-1;

                            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blendingBlock[zpBiome];
                        }
                    }
                }               
            }
        }

    }

    private float Noise(float x, float y)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = (patchNoise[X  ] + Y) & 0xff;
        int B = (patchNoise[X+1] + Y) & 0xff;
        return Lerp(v, Lerp(u, Grad(patchNoise[A  ], x, y  ), Grad(patchNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(patchNoise[A+1], x, y-1), Grad(patchNoise[B+1], x-1, y-1)));        
    }

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }
    private float Grad(int hash, float x, float y)
    {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }
}

[BurstCompile]
public struct GenerateCaveJob : IJobParallelFor{ // Chunk.chunkWidth, 2 on Schedule call
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

                if(NoiseMask((pos.x*Chunk.chunkWidth+x)*GenerationSeed.cavemaskNoiseStep1, y*GenerationSeed.cavemaskYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.cavemaskNoiseStep1) < maskThreshold)
                    continue;

                val = TransformOctaves(Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep1, y*GenerationSeed.caveYStep1, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep1), Noise((pos.x*Chunk.chunkWidth+x)*GenerationSeed.caveNoiseStep2, y*GenerationSeed.caveYStep2, (pos.z*Chunk.chunkWidth+z)*GenerationSeed.caveNoiseStep2));
            

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


    // Calculates 3D Noise for Cave System procedural generation
    public float Noise(float x, float y, float z)
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
      
        int A  = (caveNoise[X  ] + Y) & 0xff;
        int B  = (caveNoise[X+1] + Y) & 0xff;
        int AA = (caveNoise[A  ] + Z) & 0xff;
        int BA = (caveNoise[B  ] + Z) & 0xff;
        int AB = (caveNoise[A+1] + Z) & 0xff;
        int BB = (caveNoise[B+1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(caveNoise[AA  ], x, y  , z  ), Grad(caveNoise[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(caveNoise[AB  ], x, y-1, z  ), Grad(caveNoise[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(caveNoise[AA+1], x, y  , z-1), Grad(caveNoise[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(caveNoise[AB+1], x, y-1, z-1), Grad(caveNoise[BB+1], x-1, y-1, z-1))));
    }

    public float NoiseMask(float x, float y, float z)
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
      
        int A  = (cavemaskNoise[X  ] + Y) & 0xff;
        int B  = (cavemaskNoise[X+1] + Y) & 0xff;
        int AA = (cavemaskNoise[A  ] + Z) & 0xff;
        int BA = (cavemaskNoise[B  ] + Z) & 0xff;
        int AB = (cavemaskNoise[A+1] + Z) & 0xff;
        int BB = (cavemaskNoise[B+1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(cavemaskNoise[AA  ], x, y  , z  ), Grad(cavemaskNoise[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(cavemaskNoise[AB  ], x, y-1, z  ), Grad(cavemaskNoise[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(cavemaskNoise[AA+1], x, y  , z-1), Grad(cavemaskNoise[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(cavemaskNoise[AB+1], x, y-1, z-1), Grad(cavemaskNoise[BB+1], x-1, y-1, z-1))));
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

[BurstCompile]
public struct RemoveSpikesJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockData;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> stateData;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> heightMap;

    public void Execute(int index){
        RemoveSpikes(index);
    }

    public void RemoveSpikes(int z){
        int currentNextDiff = 0;
        int currentPrevDiff = 0;
        int maxSmoothDistance = 3;

        for(int x=1; x < Chunk.chunkWidth-1; x++){
            currentNextDiff = (int)heightMap[x*(Chunk.chunkWidth+1)+z] - (int)heightMap[(x+1)*(Chunk.chunkWidth+1)+z];
            currentPrevDiff = (int)heightMap[x*(Chunk.chunkWidth+1)+z] - (int)heightMap[(x-1)*(Chunk.chunkWidth+1)+z];

            // If current is spike
            if(Abs(currentNextDiff) > maxSmoothDistance && Abs(currentPrevDiff) > maxSmoothDistance){
                // If current is spike up
                if(currentNextDiff > 0){
                    BreakPillar(x, z, (int)heightMap[x*(Chunk.chunkWidth+1)+z], (int)heightMap[(x+1)*(Chunk.chunkWidth+1)+z]);
                    heightMap[x*(Chunk.chunkWidth+1)+z] = heightMap[(x+1)*(Chunk.chunkWidth+1)+z] + 1;                    
                }
                // If current is spike down
                else{
                    AddPillar(x, z, (int)heightMap[(x+1)*(Chunk.chunkWidth+1)+z]-1, (int)heightMap[x*(Chunk.chunkWidth+1)+z]);
                    heightMap[x*(Chunk.chunkWidth+1)+z] = heightMap[(x+1)*(Chunk.chunkWidth+1)+z];                    
                }
            }
        }

        for(int x=Chunk.chunkWidth-2; x >= 1 ; x--){
            currentNextDiff = (int)heightMap[x*(Chunk.chunkWidth+1)+z] - (int)heightMap[(x+1)*(Chunk.chunkWidth+1)+z];
            currentPrevDiff = (int)heightMap[x*(Chunk.chunkWidth+1)+z] - (int)heightMap[(x-1)*(Chunk.chunkWidth+1)+z];

            // If current is spike
            if(Abs(currentNextDiff) > maxSmoothDistance && Abs(currentPrevDiff) > maxSmoothDistance){
                // If current is spike up
                if(currentNextDiff > 0){
                    BreakPillar(x, z, (int)heightMap[x*(Chunk.chunkWidth+1)+z], (int)heightMap[(x+1)*(Chunk.chunkWidth+1)+z]);
                    heightMap[x*(Chunk.chunkWidth+1)+z] = heightMap[(x+1)*(Chunk.chunkWidth+1)+z] + 1;                    
                }
                // If current is spike down
                else{
                    AddPillar(x, z, (int)heightMap[(x+1)*(Chunk.chunkWidth+1)+z]-1, (int)heightMap[x*(Chunk.chunkWidth+1)+z]);
                    heightMap[x*(Chunk.chunkWidth+1)+z] = heightMap[(x+1)*(Chunk.chunkWidth+1)+z];                    
                }
            }
        }

        // Smooth process for z=0 and z=15
        SmoothenProcessForBorder(0, z, 1, 2, maxSmoothDistance);
        SmoothenProcessForBorder(Chunk.chunkWidth-1, z, Chunk.chunkWidth-2, Chunk.chunkWidth-3, maxSmoothDistance);
        
    }

    private void SmoothenProcessForBorder(int x, int z, int targetX, int targetX2, int smoothFactor){
        int currentToZDiff = (int)heightMap[x*(Chunk.chunkWidth+1)+z] - (int)heightMap[targetX*(Chunk.chunkWidth+1)+z];
        int currentToZ2Diff = (int)heightMap[x*(Chunk.chunkWidth+1)+z] - (int)heightMap[targetX2*(Chunk.chunkWidth+1)+z];

        if(Abs(currentToZDiff) > smoothFactor && Abs(currentToZ2Diff) > smoothFactor){
            if(currentToZDiff > 0){
                BreakPillar(x, z, (int)heightMap[x*(Chunk.chunkWidth+1)+z], (int)heightMap[targetX*(Chunk.chunkWidth+1)+z]);
                heightMap[x*(Chunk.chunkWidth+1)+z] = heightMap[targetX*(Chunk.chunkWidth+1)+z] + 1;                  
            }
            else{
                AddPillar(x, z, (int)heightMap[targetX*(Chunk.chunkWidth+1)+z]-1, (int)heightMap[x*(Chunk.chunkWidth+1)+z]);
                heightMap[x*(Chunk.chunkWidth+1)+z] = heightMap[targetX*(Chunk.chunkWidth+1)+z];                  
            }
        }
    }

    private int Abs(int x){
        if(x > 0)
            return x;
        else
            return -x;
    }

    private void BreakPillar(int x, int z, int initialY, int endY){
        for(int y=initialY; y > endY; y--){
            if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == (ushort)BlockID.WATER)
                continue;

            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;
            stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;  
        }
    }

    private void AddPillar(int x, int z, int initialY, int endY){
        for(int y=initialY; y >= endY; y--){
            if(blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] == (ushort)BlockID.WATER)
                continue;

            blockData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)BlockID.STONE;
            stateData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = 0;  
        }        
    }
}