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

    // RNG elements
    private Random rng;
    private int iteration = 0;

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

    // Decoration Blocks
    private NativeArray<ushort> decorationSurfaceBlock;
    private NativeArray<ushort> decorationUndergroundBlock;
    private NativeArray<ushort> decorationHellBlock;

    // Other Native Objects
    private NativeParallelHashSet<ushort> caveFreeBlocks;
    private ushort[] caveFreeBlocksArray = new ushort[]{VoxelLoader.GetBlockID("BASE_Water"), VoxelLoader.GetBlockID("BASE_Lava")};

    // Biome Blending Map
    private NativeArray<ushort> biomeBlendingBlock;


    public WorldGenerator(int worldSeed, BiomeHandler biomeReference, ChunkLoader_Server reference){
    	this.worldSeed = worldSeed;
    	this.biomeHandler = biomeReference;
    	this.cl = reference;

        baseMap = new NativeArray<byte>(GenerationSeed.baseNoise, Allocator.Persistent);
        erosionMap = new NativeArray<byte>(GenerationSeed.erosionNoise, Allocator.Persistent);
        peakMap = new NativeArray<byte>(GenerationSeed.peakNoise, Allocator.Persistent);
        temperatureMap = new NativeArray<byte>(GenerationSeed.temperatureNoise, Allocator.Persistent);
        humidityMap = new NativeArray<byte>(GenerationSeed.humidityNoise, Allocator.Persistent);
        patchMap = new NativeArray<byte>(GenerationSeed.patchNoise, Allocator.Persistent);
        caveMap = new NativeArray<byte>(GenerationSeed.caveNoise, Allocator.Persistent);
        maskMap = new NativeArray<byte>(GenerationSeed.cavemaskNoise, Allocator.Persistent);

        SetupDecorationBlocks();

        caveFreeBlocks = new NativeParallelHashSet<ushort>(0, Allocator.Persistent);
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

        SetupDecorationBlocks();

        caveFreeBlocks = new NativeParallelHashSet<ushort>(0, Allocator.Persistent);
        FillCaveFreeBlocks();
    }

    private void SetupDecorationBlocks(){
        this.decorationSurfaceBlock = new NativeArray<ushort>(new ushort[]{VoxelLoader.GetBlockID("BASE_Water"), VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt"),
            VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Sand"), VoxelLoader.GetBlockID("BASE_Sandstone"), VoxelLoader.GetBlockID("BASE_Ice"),
            VoxelLoader.GetBlockID("BASE_Snow"), VoxelLoader.GetBlockID("BASE_Clay")}, Allocator.Persistent);

        this.decorationHellBlock = new NativeArray<ushort>(new ushort[]{VoxelLoader.GetBlockID("BASE_Basalt"), VoxelLoader.GetBlockID("BASE_Hell_Marble"), VoxelLoader.GetBlockID("BASE_Lava")}, Allocator.Persistent);

        this.decorationUndergroundBlock = new NativeArray<ushort>(new ushort[]{VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Basalt"), VoxelLoader.GetBlockID("BASE_Water"),
            VoxelLoader.GetBlockID("BASE_Ice"), VoxelLoader.GetBlockID("BASE_Snow")}, Allocator.Persistent);
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
        this.decorationHellBlock.Dispose();
        this.decorationSurfaceBlock.Dispose();
        this.decorationUndergroundBlock.Dispose();

        this.caveFreeBlocks.Dispose();
    }


    // Applies Structures to a chunk
    /*
    Depth Values represent how deep below heightmap things will go.
    Range represents if structure always spawn at given Depth, or if it spans below as well
    HardSetDepth will be a fixed depth value for the structure to generate in case it is non-negative
    */
    public void GenerateStructures(ChunkPos pos, BiomeCode biome, string structureName, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        // Gets index of amount and percentage
        int index = BiomeHandler.GetBiomeStructs(biome).IndexOf(structureName);
        int amount = BiomeHandler.GetBiomeAmounts(biome)[index];
        int depth = BiomeHandler.GetBiomeDepth(biome)[index];
        int hardSetDepth = BiomeHandler.GetBiomeHSDepth(biome)[index];
        bool range = BiomeHandler.GetBiomeRange(biome)[index];
        float percentage = BiomeHandler.GetBiomePercentages(biome)[index];
        int minRelativeHeight = BiomeHandler.GetBiomeMinHeight(biome)[index];

        int x,y,z;
        int rotation = 0;
        float chance;

        this.rng = new Random((int)(int.MaxValue * NoiseMaker.NormalizedPatchNoise1D((pos.z^(pos.x-1))*pos.y*Chunk.chunkWidth*GenerationSeed.patchNoiseStep3 + (this.iteration * GenerationSeed.patchMultStep))));
        this.iteration++;

        // If structure is static at given heightmap depth
        if(!range){
            for(int i=1; i <= amount; i++){
                chance = (float)this.rng.NextDouble();

                if(chance > percentage)
                    continue;

                rotation = this.rng.Next(0, 4);

                x = this.rng.Next(0, Chunk.chunkWidth);
                z = this.rng.Next(0, Chunk.chunkWidth);

                if(hardSetDepth < 0)
                    y = (int)cacheHeightMap[x*(Chunk.chunkWidth+1)+z] - depth;
                else
                    y = hardSetDepth;


                // Ignores structure on hard limit
                if(y <= minRelativeHeight || y <= 0)
                    continue;

                Structure structure = StructureLoader.GetStructure(structureName);

                if(structure.AcceptBaseBlock(blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z])){
                    structure.Apply(this.cl, pos, blockdata, hpdata, statedata, x, y, z, rotation:rotation); 
                }
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

                if(minRelativeHeight != 0 && hardSetDepth < 0){
                    float yMult = (float)this.rng.NextDouble(); 
                    y = (int)(Mathf.Lerp(minRelativeHeight + cacheHeightMap[x*(Chunk.chunkWidth+1)+z], cacheHeightMap[x*(Chunk.chunkWidth+1)+z], yMult));
                }
                else if(minRelativeHeight != 0 && hardSetDepth >= 0){
                    y = this.rng.Next(hardSetDepth + minRelativeHeight, hardSetDepth);
                }
                else if(hardSetDepth < 0){
                    float yMult = (float)this.rng.NextDouble(); 
                    y = (int)(yMult*(cacheHeightMap[x*(Chunk.chunkWidth+1)+z] - depth));
                }
                else{
                    y = this.rng.Next(1, hardSetDepth);
                }

                // Ignores structure on hard limit
                if(y <= minRelativeHeight || y <= 0)
                    continue;

                Structure structure = StructureLoader.GetStructure(structureName);

                if(structure.AcceptBaseBlock(blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z])){
                    structure.Apply(this.cl, pos, blockdata, hpdata, statedata, x, y, z, rotation:rotation);
                }
            }
        }
    }

    // Handles the generation of chunks
    public void GenerateChunk(ChunkPos pos, bool isPregen=false){
        switch((ChunkDepthID)pos.y){
            case ChunkDepthID.SURFACE:
                GenerateSurfaceChunk(pos, isPregen:isPregen);
                return;
            case ChunkDepthID.UNDERGROUND:
                GenerateUndergroundChunk(pos, isPregen:isPregen);
                return;
            case ChunkDepthID.HELL:
                GenerateHellChunk(pos, isPregen:isPregen);
                return;
            case ChunkDepthID.CORE:
                GenerateCoreChunk(pos, isPregen:isPregen);
                return;
            default:
                return;
        }
    }

    // Generates a Surface Chunk
    public void GenerateSurfaceChunk(ChunkPos pos, bool isPregen=false){
        NativeArray<ushort> voxelData = NativeTools.CopyToNative(cacheVoxdata);
        NativeArray<ushort> stateData = NativeTools.CopyToNative(cacheMetadataState);
        NativeArray<ushort> hpData = NativeTools.CopyToNative(cacheMetadataHP);
        NativeArray<float> heightMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        GenerateSurfaceChunkJob gcj = new GenerateSurfaceChunkJob{
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
        float[] chunkInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        this.cacheBiome = this.biomeHandler.AssignBiome(chunkInfo, ChunkDepthID.SURFACE);

        // X+ Chunk
        float[] xPlusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        xpBiome = this.biomeHandler.AssignBiome(xPlusInfo, ChunkDepthID.SURFACE);

        // Z+ Chunk
        float[] zPlusInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        zpBiome = this.biomeHandler.AssignBiome(zPlusInfo, ChunkDepthID.SURFACE);

        // Z- Chunk
        float[] zMinusInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        zmBiome = this.biomeHandler.AssignBiome(zMinusInfo, ChunkDepthID.SURFACE);

        // XZ+ Chunk
        float[] xzPlusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        xzpBiome = this.biomeHandler.AssignBiome(xzPlusInfo, ChunkDepthID.SURFACE);

        // X+Z- Chunk
        float[] xpzmMinusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        xpzmBiome = this.biomeHandler.AssignBiome(xpzmMinusInfo, ChunkDepthID.SURFACE);

        // X-Z+ Chunk
        float[] xmzpMinusInfo = this.BiomeNoise((pos.x-1)*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth, ChunkDepthID.SURFACE);
        xmzpBiome = this.biomeHandler.AssignBiome(xmzpMinusInfo, ChunkDepthID.SURFACE);

        PopulateSurfaceChunkJob pcj = new PopulateSurfaceChunkJob{
            heightMap = heightMap,
            blockData = voxelData,
            patchNoise = patchMap,
            pos = pos,
            blendingBlock = biomeBlendingBlock,
            biome = this.cacheBiome,
            decorationBlock = this.decorationSurfaceBlock,
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
            caveFreeBlocks = caveFreeBlocks,
            waterBlockID = VoxelLoader.GetBlockID("BASE_Water"),
            cid = ChunkDepthID.SURFACE
        };
        job = gcavej.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        cacheVoxdata = NativeTools.CopyToManaged(voxelData);
        cacheMetadataState = NativeTools.CopyToManaged(stateData);
        cacheMetadataHP = NativeTools.CopyToManaged(hpData);
        cacheHeightMap = NativeTools.CopyToManaged(heightMap);

        this.iteration = 0;
        GenerateBiomeStructures(cl, pos, (BiomeCode)this.cacheBiome, cacheVoxdata, cacheMetadataState, cacheMetadataHP);

        voxelData.Dispose();
        stateData.Dispose();
        hpData.Dispose();
        heightMap.Dispose();
    }

    private void GenerateBiomeStructures(ChunkLoader_Server cl, ChunkPos pos, BiomeCode biomeCode, ushort[] blockdata, ushort[] state, ushort[] hps){
        foreach(string structName in BiomeHandler.GetBiomeStructs(biomeCode)){
            GenerateStructures(pos, biomeCode, structName, blockdata, state, hps);
        }
    }

    // Generates chunks for the Underground Layer
    private void GenerateUndergroundChunk(ChunkPos pos, bool isPregen=false){
        NativeArray<ushort> voxelData = NativeTools.CopyToNative(cacheVoxdata);
        NativeArray<ushort> stateData = NativeTools.CopyToNative(cacheMetadataState);
        NativeArray<ushort> hpData = NativeTools.CopyToNative(cacheMetadataHP);
        NativeArray<float> heightMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        GenerateUndergroundChunkJob gucj = new GenerateUndergroundChunkJob{
            pos = pos,
            blockData = voxelData,
            stateData = stateData,
            hpData = hpData,
            heightMap = heightMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            peakNoise = peakMap,
            waterBlockID = VoxelLoader.GetBlockID("BASE_Water"),
            stoneBlockID = VoxelLoader.GetBlockID("BASE_Stone"),
            pregen = isPregen
        };
        JobHandle job = gucj.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        float[] chunkInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.UNDERGROUND);
        this.cacheBiome = this.biomeHandler.AssignBiome(chunkInfo, ChunkDepthID.UNDERGROUND);

        PopulateUndergroundChunkJob pucj = new PopulateUndergroundChunkJob{
            pos = pos,
            blockData = voxelData,
            heightMap = heightMap,
            patchNoise = patchMap,
            decorationBlock = this.decorationUndergroundBlock,
            biome = this.cacheBiome
        };
        job = pucj.Schedule(Chunk.chunkWidth, 4);
        job.Complete();

        cacheVoxdata = NativeTools.CopyToManaged(voxelData);
        cacheMetadataState = NativeTools.CopyToManaged(stateData);
        cacheMetadataHP = NativeTools.CopyToManaged(hpData);
        cacheHeightMap = NativeTools.CopyToManaged(heightMap);

        this.iteration = 0;
        GenerateBiomeStructures(cl, pos, (BiomeCode)this.cacheBiome, cacheVoxdata, cacheMetadataState, cacheMetadataHP);

        voxelData.Dispose();
        stateData.Dispose();
        hpData.Dispose();
        heightMap.Dispose();
    }

    // Generates chunks for the Hell Layer
    private void GenerateHellChunk(ChunkPos pos, bool isPregen=false){
        NativeArray<ushort> voxelData = NativeTools.CopyToNative(cacheVoxdata);
        NativeArray<ushort> stateData = NativeTools.CopyToNative(cacheMetadataState);
        NativeArray<ushort> hpData = NativeTools.CopyToNative(cacheMetadataHP);
        NativeArray<float> heightMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<float> ceilingMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        GenerateHellChunkJob ghcj = new GenerateHellChunkJob{
            pos = pos,
            blockData = voxelData,
            stateData = stateData,
            hpData = hpData,
            heightMap = heightMap,
            ceilingMap = ceilingMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap,
            peakNoise = peakMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            temperatureNoise = temperatureMap,
            hellMarbleBlockID = VoxelLoader.GetBlockID("BASE_Hell_Marble"),
            acasterBlockID = VoxelLoader.GetBlockID("BASE_Acaster"),
            pregen = isPregen
        };
        JobHandle job = ghcj.Schedule();
        job.Complete();


        byte xmBiome, xpBiome, zpBiome, zmBiome, xpzpBiome, xpzmBiome, xmzpBiome, xmzmBiome;

        // This Chunk
        float[] chunkInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.HELL);
        this.cacheBiome = this.biomeHandler.AssignBiome(chunkInfo, ChunkDepthID.HELL);

        // X- Chunk
        float[] xMinusInfo = this.BiomeNoise((pos.x-1)*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.HELL);
        xmBiome = this.biomeHandler.AssignBiome(xMinusInfo, ChunkDepthID.HELL);

        // X+ Chunk
        float[] xPlusInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.HELL);
        xpBiome = this.biomeHandler.AssignBiome(xPlusInfo, ChunkDepthID.HELL);

        // Z+ Chunk
        float[] zPlusInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth, ChunkDepthID.HELL);
        zpBiome = this.biomeHandler.AssignBiome(zPlusInfo, ChunkDepthID.HELL);

        // Z- Chunk
        float[] zMinusInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth, ChunkDepthID.HELL);
        zmBiome = this.biomeHandler.AssignBiome(zMinusInfo, ChunkDepthID.HELL);

        // XZ+ Chunk
        float[] xpzpInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth, ChunkDepthID.HELL);
        xpzpBiome = this.biomeHandler.AssignBiome(xpzpInfo, ChunkDepthID.HELL);

        // X+Z- Chunk
        float[] xpzmInfo = this.BiomeNoise((pos.x+1)*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth, ChunkDepthID.HELL);
        xpzmBiome = this.biomeHandler.AssignBiome(xpzmInfo, ChunkDepthID.HELL);

        // X-Z+ Chunk
        float[] xmzpInfo = this.BiomeNoise((pos.x-1)*Chunk.chunkWidth, (pos.z+1)*Chunk.chunkWidth, ChunkDepthID.HELL);
        xmzpBiome = this.biomeHandler.AssignBiome(xmzpInfo, ChunkDepthID.HELL);

        // X-Z- Chunk
        float[] xmzmInfo = this.BiomeNoise((pos.x-1)*Chunk.chunkWidth, (pos.z-1)*Chunk.chunkWidth, ChunkDepthID.HELL);
        xmzmBiome = this.biomeHandler.AssignBiome(xmzmInfo, ChunkDepthID.HELL);


        PopulateHellChunkJob phcj = new PopulateHellChunkJob{
            pos = pos,
            blockData = voxelData,
            heightMap = heightMap,
            ceilingMap = ceilingMap,
            patchNoise = patchMap,
            biome = this.cacheBiome,
            blendingBlock = biomeBlendingBlock,
            xmBiome = xmBiome,
            xpBiome = xpBiome,
            zmBiome = zmBiome,
            zpBiome = zpBiome,
            xmzmBiome = xmzmBiome,
            xmzpBiome = xmzpBiome,
            xpzmBiome = xpzmBiome,
            xpzpBiome = xpzpBiome,
            decorationBlock = this.decorationHellBlock
        };
        job = phcj.Schedule(Chunk.chunkWidth, 4);
        job.Complete();

        GenerateCaveJob gcavej = new GenerateCaveJob{
            pos = pos,
            blockData = voxelData,
            stateData = stateData,
            heightMap = heightMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            caveFreeBlocks = caveFreeBlocks,
            cid = ChunkDepthID.HELL
        };
        job = gcavej.Schedule(Chunk.chunkWidth, 2);
        job.Complete();


        cacheVoxdata = NativeTools.CopyToManaged(voxelData);
        cacheMetadataState = NativeTools.CopyToManaged(stateData);
        cacheMetadataHP = NativeTools.CopyToManaged(hpData);
        cacheHeightMap = NativeTools.CopyToManaged(heightMap);

        this.iteration = 0;
        GenerateBiomeStructures(cl, pos, (BiomeCode)this.cacheBiome, cacheVoxdata, cacheMetadataState, cacheMetadataHP);

        voxelData.Dispose();
        stateData.Dispose();
        hpData.Dispose();
        heightMap.Dispose();
        ceilingMap.Dispose();
    }

    // Generates chunks for the Core Layer
    private void GenerateCoreChunk(ChunkPos pos, bool isPregen=false){
        NativeArray<ushort> voxelData = NativeTools.CopyToNative(cacheVoxdata);
        NativeArray<ushort> stateData = NativeTools.CopyToNative(cacheMetadataState);
        NativeArray<ushort> hpData = NativeTools.CopyToNative(cacheMetadataHP);
        NativeArray<float> heightMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);
        NativeArray<float> bottomMap = new NativeArray<float>((Chunk.chunkWidth+1)*(Chunk.chunkWidth+1), Allocator.TempJob);

        GenerateCoreChunkJob gccj = new GenerateCoreChunkJob{
            pos = pos,
            blockData = voxelData,
            stateData = stateData,
            hpData = hpData,
            heightMap = heightMap,
            bottomMap = bottomMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap,
            peakNoise = peakMap,
            moonstoneBlockID = VoxelLoader.GetBlockID("BASE_Moonstone"),
            acasterBlockID = VoxelLoader.GetBlockID("BASE_Acaster"),
            pregen = isPregen
        };
        JobHandle job = gccj.Schedule();
        job.Complete();

        // This Chunk
        float[] chunkInfo = this.BiomeNoise(pos.x*Chunk.chunkWidth, pos.z*Chunk.chunkWidth, ChunkDepthID.CORE);
        this.cacheBiome = this.biomeHandler.AssignBiome(chunkInfo, ChunkDepthID.CORE);

        cacheVoxdata = NativeTools.CopyToManaged(voxelData);
        cacheMetadataState = NativeTools.CopyToManaged(stateData);
        cacheMetadataHP = NativeTools.CopyToManaged(hpData);
        cacheHeightMap = NativeTools.CopyToManaged(heightMap);

        this.iteration = 0;
        GenerateBiomeStructures(cl, pos, (BiomeCode)this.cacheBiome, cacheVoxdata, cacheMetadataState, cacheMetadataHP);

        voxelData.Dispose();
        stateData.Dispose();
        hpData.Dispose();
        heightMap.Dispose();
        bottomMap.Dispose();
    }

    /*
    Biome Generation
    */

    public float[] BiomeNoise(float x, float y, ChunkDepthID cid)
    {
        x += 8;
        y += 8;
        float initialX = x;
        float initialY = y;
        float u,v,x2,y2,u2,v2;
        int A,B,A2,B2,X,Y,X2,Y2;


        float[] biomeVector = new float[5];


        // Temperature Noise
        if(cid != ChunkDepthID.HELL){
            x = initialX*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetX[0];
            x -= Mathf.Floor(x);
            y = initialY*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetY[0];
            y -= Mathf.Floor(y);
            u = Fade(x);
            v = Fade(y);

            x2 = initialX*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetX[0];
            x2 -= Mathf.Floor(x2);
            y2 = initialY*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetY[0];
            y2 -= Mathf.Floor(y2);
            
            u2 = Fade(x2);
            v2 = Fade(y2);

            X = Mathf.FloorToInt(initialX*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetX[0]) & 0xff;
            Y = Mathf.FloorToInt(initialY*GenerationSeed.temperatureNoiseStep1 + GenerationSeed.temperatureOffsetY[0]) & 0xff;
            X2 = Mathf.FloorToInt(initialX*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetX[0]) & 0xff;
            Y2 = Mathf.FloorToInt(initialY*GenerationSeed.temperatureNoiseStep2 + GenerationSeed.temperatureOffsetY[0]) & 0xff;
            A = (GenerationSeed.temperatureNoise[X  ] + Y) & 0xff;
            B = (GenerationSeed.temperatureNoise[X+1] + Y) & 0xff;
            A2 = (GenerationSeed.temperatureNoise[X2  ] + Y2) & 0xff;
            B2 = (GenerationSeed.temperatureNoise[X2+1] + Y2) & 0xff;
            biomeVector[0] = TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.temperatureNoise[A  ], x, y  ), Grad(GenerationSeed.temperatureNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(GenerationSeed.temperatureNoise[A+1], x, y-1), Grad(GenerationSeed.temperatureNoise[B+1], x-1, y-1))), 
                            Lerp(v2, Lerp(u2, Grad(GenerationSeed.temperatureNoise[A2  ], x2, y2  ), Grad(GenerationSeed.temperatureNoise[B2  ], x2-1, y2  )),
                           Lerp(u2, Grad(GenerationSeed.temperatureNoise[A2+1], x2, y2-1), Grad(GenerationSeed.temperatureNoise[B2+1], x2-1, y2-1))));
        }
        else{
            x = initialX*GenerationSeed.temperatureNoiseStep3 + GenerationSeed.temperatureOffsetX[0];
            x -= Mathf.Floor(x);
            y = initialY*GenerationSeed.temperatureNoiseStep3 + GenerationSeed.temperatureOffsetY[0];
            y -= Mathf.Floor(y);
            u = Fade(x);
            v = Fade(y);

            x2 = initialX*GenerationSeed.temperatureNoiseStep4 + GenerationSeed.temperatureOffsetX[0];
            x2 -= Mathf.Floor(x2);
            y2 = initialY*GenerationSeed.temperatureNoiseStep4 + GenerationSeed.temperatureOffsetY[0];
            y2 -= Mathf.Floor(y2);
            
            u2 = Fade(x2);
            v2 = Fade(y2);

            X = Mathf.FloorToInt(initialX*GenerationSeed.temperatureNoiseStep3 + GenerationSeed.temperatureOffsetX[0]) & 0xff;
            Y = Mathf.FloorToInt(initialY*GenerationSeed.temperatureNoiseStep3 + GenerationSeed.temperatureOffsetY[0]) & 0xff;
            X2 = Mathf.FloorToInt(initialX*GenerationSeed.temperatureNoiseStep4 + GenerationSeed.temperatureOffsetX[0]) & 0xff;
            Y2 = Mathf.FloorToInt(initialY*GenerationSeed.temperatureNoiseStep4 + GenerationSeed.temperatureOffsetY[0]) & 0xff;
            A = (GenerationSeed.temperatureNoise[X  ] + Y) & 0xff;
            B = (GenerationSeed.temperatureNoise[X+1] + Y) & 0xff;
            A2 = (GenerationSeed.temperatureNoise[X2  ] + Y2) & 0xff;
            B2 = (GenerationSeed.temperatureNoise[X2+1] + Y2) & 0xff;
            biomeVector[0] = TransformOctaves(Lerp(v, Lerp(u, Grad(GenerationSeed.temperatureNoise[A  ], x, y  ), Grad(GenerationSeed.temperatureNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(GenerationSeed.temperatureNoise[A+1], x, y-1), Grad(GenerationSeed.temperatureNoise[B+1], x-1, y-1))), 
                            Lerp(v2, Lerp(u2, Grad(GenerationSeed.temperatureNoise[A2  ], x2, y2  ), Grad(GenerationSeed.temperatureNoise[B2  ], x2-1, y2  )),
                           Lerp(u2, Grad(GenerationSeed.temperatureNoise[A2+1], x2, y2-1), Grad(GenerationSeed.temperatureNoise[B2+1], x2-1, y2-1))));            
        }
        
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