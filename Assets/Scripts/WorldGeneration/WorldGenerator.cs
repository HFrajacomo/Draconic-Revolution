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
    private float[] cacheHeightMap = new float[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
    private float[] cacheBiomeVector = new float[5];

    // Generation Slots. Each one generates a chunk, so chunks can be generated in parallel
    private GenerationSlot[] slots;

    // Biome of every chunk position that was already evaluated, keyed by (x, z, layer)
    private Dictionary<int3, byte> biomeCache = new Dictionary<int3, byte>();
    private static readonly int BIOME_CACHE_LIMIT = 16384;

    // Block IDs used by generation jobs
    private ushort waterBlockID;
    private ushort stoneBlockID;
    private ushort hellMarbleBlockID;
    private ushort acasterBlockID;
    private ushort moonstoneBlockID;

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
        SetupGeneration();
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
        SetupGeneration();
    }

    private void SetupGeneration(){
        this.waterBlockID = VoxelLoader.GetBlockID("BASE_Water");
        this.stoneBlockID = VoxelLoader.GetBlockID("BASE_Stone");
        this.hellMarbleBlockID = VoxelLoader.GetBlockID("BASE_Hell_Marble");
        this.acasterBlockID = VoxelLoader.GetBlockID("BASE_Acaster");
        this.moonstoneBlockID = VoxelLoader.GetBlockID("BASE_Moonstone");

        this.slots = new GenerationSlot[Constants.SERVER_GENERATION_BATCH];

        for(int i=0; i < this.slots.Length; i++)
            this.slots[i] = new GenerationSlot();
    }

    public int GetSlotCount(){return this.slots.Length;}
    public GenerationSlot GetSlot(int index){return this.slots[index];}

    private void SetupDecorationBlocks(){
        this.decorationSurfaceBlock = new NativeArray<ushort>(new ushort[]{VoxelLoader.GetBlockID("BASE_Water"), VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt"),
            VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Sand"), VoxelLoader.GetBlockID("BASE_Sandstone"), VoxelLoader.GetBlockID("BASE_Ice"),
            VoxelLoader.GetBlockID("BASE_Snow"), VoxelLoader.GetBlockID("BASE_Clay")}, Allocator.Persistent);

        this.decorationHellBlock = new NativeArray<ushort>(new ushort[]{VoxelLoader.GetBlockID("BASE_Basalt"), VoxelLoader.GetBlockID("BASE_Hell_Marble"), VoxelLoader.GetBlockID("BASE_Lava")}, Allocator.Persistent);

        this.decorationUndergroundBlock = new NativeArray<ushort>(new ushort[]{VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Basalt"), VoxelLoader.GetBlockID("BASE_Water"),
            VoxelLoader.GetBlockID("BASE_Ice"), VoxelLoader.GetBlockID("BASE_Snow")}, Allocator.Persistent);
    }

    public void SetBiomeBlending(ushort[] blendingArray){
        if(this.biomeBlendingBlock.IsCreated)
            this.biomeBlendingBlock.Dispose();

        this.biomeBlendingBlock = new NativeArray<ushort>(blendingArray, Allocator.Persistent);
    }

    public void FillCaveFreeBlocks(){
        foreach(ushort block in this.caveFreeBlocksArray){
            this.caveFreeBlocks.Add(block);
        }
    }

    public void DestroyNativeMemory(){
        if(this.slots != null){
            for(int i=0; i < this.slots.Length; i++)
                this.slots[i].Dispose();

            this.slots = null;
        }

        this.baseMap.Dispose();
        this.erosionMap.Dispose();
        this.peakMap.Dispose();
        this.temperatureMap.Dispose();
        this.humidityMap.Dispose();
        this.patchMap.Dispose();
        this.caveMap.Dispose();
        this.maskMap.Dispose();

        if(this.biomeBlendingBlock.IsCreated)
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
    public void GenerateStructures(ChunkPos pos, BiomeCode biome, int index, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        string structureName = BiomeHandler.GetBiomeStructs(biome)[index];
        Structure structure = null;
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

                if(structure == null)
                    structure = StructureLoader.GetStructure(structureName);

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

                if(structure == null)
                    structure = StructureLoader.GetStructure(structureName);

                if(structure.AcceptBaseBlock(blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z])){
                    structure.Apply(this.cl, pos, blockdata, hpdata, statedata, x, y, z, rotation:rotation);
                }
            }
        }
    }

    /*
    Schedules the generation jobs of a chunk into a slot. Pre-generated chunks must have their data loaded.
    Biomes are assigned before scheduling, since they don't depend on the generated voxels
    */
    public void ScheduleGeneration(GenerationSlot slot, Chunk chunk, bool isPregen=false){
        slot.Begin(chunk, isPregen);

        switch((ChunkDepthID)chunk.pos.y){
            case ChunkDepthID.SURFACE:
                ScheduleSurfaceChunk(slot, chunk.pos, isPregen);
                break;
            case ChunkDepthID.UNDERGROUND:
                ScheduleUndergroundChunk(slot, chunk.pos, isPregen);
                break;
            case ChunkDepthID.HELL:
                ScheduleHellChunk(slot, chunk.pos, isPregen);
                break;
            case ChunkDepthID.CORE:
                ScheduleCoreChunk(slot, chunk.pos, isPregen);
                break;
            default:
                slot.handle = default(JobHandle);
                break;
        }
    }

    /*
    Waits for the generation of a slot, writes it into its chunk and applies the chunk structures.
    Structures may touch neighboring chunks, so slots must be finished one at a time, in the order chunks were requested
    */
    public void FinishGeneration(GenerationSlot slot){
        Chunk chunk = slot.chunk;
        ushort[] blocks = chunk.data.GetData();
        ushort[] states = chunk.metadata.GetStateData();
        ushort[] hps = chunk.metadata.GetHPData();

        slot.CompleteInto(blocks, states, hps, this.cacheHeightMap);

        chunk.biome = slot.biome;

        this.iteration = 0;
        GenerateBiomeStructures(chunk.pos, (BiomeCode)slot.biome, blocks, states, hps);
        RunGenerationEvents(chunk.pos, blocks, states, hps);

        // The chunk keeps its arrays, only its height maps are calculated
        chunk.BuildOnVoxelData(VoxelData.FromOwnedArray(blocks, chunk.pos));

        slot.Release();
    }

    // Runs the onGenerate event of every voxel that has one. Must run after all structures, since they may change a voxel's neighbors
    private void RunGenerationEvents(ChunkPos pos, ushort[] blocks, ushort[] states, ushort[] hps){
        ushort code;
        int x, y, z;

        for(int i=0; i < blocks.Length; i++){
            code = blocks[i];

            if(!VoxelLoader.HasGenerateEvent(code))
                continue;

            x = i / (Chunk.chunkWidth*Chunk.chunkDepth);
            y = (i / Chunk.chunkWidth) % Chunk.chunkDepth;
            z = i % Chunk.chunkWidth;

            if(code <= ushort.MaxValue/2)
                VoxelLoader.GetBlock(code).OnGenerate(pos, x, y, z, blocks, states, hps, this.worldSeed);
            else
                VoxelLoader.GetObject(code).OnGenerate(pos, x, y, z, blocks, states, hps, this.worldSeed);
        }
    }

    private void GenerateBiomeStructures(ChunkPos pos, BiomeCode biomeCode, ushort[] blockdata, ushort[] state, ushort[] hps){
        int amount = BiomeHandler.GetBiomeStructs(biomeCode).Count;

        for(int i=0; i < amount; i++){
            GenerateStructures(pos, biomeCode, i, blockdata, state, hps);
        }
    }

    // Returns the biome of a chunk position, evaluating it only once
    private byte GetBiome(int chunkX, int chunkZ, ChunkDepthID layer){
        int3 key = new int3(chunkX, chunkZ, (int)layer);
        byte biome;

        if(this.biomeCache.TryGetValue(key, out biome))
            return biome;

        if(this.biomeCache.Count >= BIOME_CACHE_LIMIT)
            this.biomeCache.Clear();

        biome = this.biomeHandler.AssignBiome(this.BiomeNoise(chunkX*Chunk.chunkWidth, chunkZ*Chunk.chunkWidth, layer), layer);
        this.biomeCache.Add(key, biome);

        return biome;
    }

    // Generates a Surface Chunk
    private void ScheduleSurfaceChunk(GenerationSlot slot, ChunkPos pos, bool isPregen){
        slot.biome = GetBiome(pos.x, pos.z, ChunkDepthID.SURFACE);

        GenerateSurfaceChunkJob gcj = new GenerateSurfaceChunkJob{
            chunkX = pos.x,
            chunkZ = pos.z,
            blockData = slot.voxels,
            stateData = slot.state,
            hpData = slot.hp,
            heightMap = slot.heightMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap,
            peakNoise = peakMap,
            pregen = isPregen
        };
        JobHandle job = gcj.Schedule();

        PopulateSurfaceChunkJob pcj = new PopulateSurfaceChunkJob{
            heightMap = slot.heightMap,
            blockData = slot.voxels,
            patchNoise = patchMap,
            pos = pos,
            blendingBlock = biomeBlendingBlock,
            biome = slot.biome,
            decorationBlock = this.decorationSurfaceBlock,
            xpBiome = GetBiome(pos.x+1, pos.z, ChunkDepthID.SURFACE),
            zpBiome = GetBiome(pos.x, pos.z+1, ChunkDepthID.SURFACE),
            zmBiome = GetBiome(pos.x, pos.z-1, ChunkDepthID.SURFACE),
            xzpBiome = GetBiome(pos.x+1, pos.z+1, ChunkDepthID.SURFACE),
            xpzmBiome = GetBiome(pos.x+1, pos.z-1, ChunkDepthID.SURFACE),
            xmzpBiome = GetBiome(pos.x-1, pos.z+1, ChunkDepthID.SURFACE)
        };
        job = pcj.Schedule(job);

        GenerateCaveJob gcavej = new GenerateCaveJob{
            pos = pos,
            blockData = slot.voxels,
            stateData = slot.state,
            heightMap = slot.heightMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            caveFreeBlocks = caveFreeBlocks,
            waterBlockID = this.waterBlockID,
            cid = ChunkDepthID.SURFACE
        };
        slot.handle = gcavej.Schedule(Chunk.chunkWidth, 2, job);
    }

    // Generates chunks for the Underground Layer
    private void ScheduleUndergroundChunk(GenerationSlot slot, ChunkPos pos, bool isPregen){
        slot.biome = GetBiome(pos.x, pos.z, ChunkDepthID.UNDERGROUND);

        GenerateUndergroundChunkJob gucj = new GenerateUndergroundChunkJob{
            pos = pos,
            blockData = slot.voxels,
            stateData = slot.state,
            hpData = slot.hp,
            heightMap = slot.heightMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            peakNoise = peakMap,
            waterBlockID = this.waterBlockID,
            stoneBlockID = this.stoneBlockID,
            pregen = isPregen
        };
        JobHandle job = gucj.Schedule(Chunk.chunkWidth, 2);

        PopulateUndergroundChunkJob pucj = new PopulateUndergroundChunkJob{
            pos = pos,
            blockData = slot.voxels,
            heightMap = slot.heightMap,
            patchNoise = patchMap,
            decorationBlock = this.decorationUndergroundBlock,
            biome = slot.biome
        };
        slot.handle = pucj.Schedule(Chunk.chunkWidth, 4, job);
    }

    // Generates chunks for the Hell Layer
    private void ScheduleHellChunk(GenerationSlot slot, ChunkPos pos, bool isPregen){
        slot.biome = GetBiome(pos.x, pos.z, ChunkDepthID.HELL);

        GenerateHellChunkJob ghcj = new GenerateHellChunkJob{
            pos = pos,
            blockData = slot.voxels,
            stateData = slot.state,
            hpData = slot.hp,
            heightMap = slot.heightMap,
            ceilingMap = slot.auxMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap,
            peakNoise = peakMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            temperatureNoise = temperatureMap,
            hellMarbleBlockID = this.hellMarbleBlockID,
            acasterBlockID = this.acasterBlockID,
            pregen = isPregen
        };
        JobHandle job = ghcj.Schedule();

        PopulateHellChunkJob phcj = new PopulateHellChunkJob{
            pos = pos,
            blockData = slot.voxels,
            heightMap = slot.heightMap,
            ceilingMap = slot.auxMap,
            patchNoise = patchMap,
            biome = slot.biome,
            blendingBlock = biomeBlendingBlock,
            xmBiome = GetBiome(pos.x-1, pos.z, ChunkDepthID.HELL),
            xpBiome = GetBiome(pos.x+1, pos.z, ChunkDepthID.HELL),
            zmBiome = GetBiome(pos.x, pos.z-1, ChunkDepthID.HELL),
            zpBiome = GetBiome(pos.x, pos.z+1, ChunkDepthID.HELL),
            xmzmBiome = GetBiome(pos.x-1, pos.z-1, ChunkDepthID.HELL),
            xmzpBiome = GetBiome(pos.x-1, pos.z+1, ChunkDepthID.HELL),
            xpzmBiome = GetBiome(pos.x+1, pos.z-1, ChunkDepthID.HELL),
            xpzpBiome = GetBiome(pos.x+1, pos.z+1, ChunkDepthID.HELL),
            decorationBlock = this.decorationHellBlock
        };
        job = phcj.Schedule(Chunk.chunkWidth, 4, job);

        GenerateCaveJob gcavej = new GenerateCaveJob{
            pos = pos,
            blockData = slot.voxels,
            stateData = slot.state,
            heightMap = slot.heightMap,
            caveNoise = caveMap,
            cavemaskNoise = maskMap,
            caveFreeBlocks = caveFreeBlocks,
            cid = ChunkDepthID.HELL
        };
        slot.handle = gcavej.Schedule(Chunk.chunkWidth, 2, job);
    }

    // Generates chunks for the Core Layer
    private void ScheduleCoreChunk(GenerationSlot slot, ChunkPos pos, bool isPregen){
        slot.biome = GetBiome(pos.x, pos.z, ChunkDepthID.CORE);

        GenerateCoreChunkJob gccj = new GenerateCoreChunkJob{
            pos = pos,
            blockData = slot.voxels,
            stateData = slot.state,
            hpData = slot.hp,
            heightMap = slot.heightMap,
            bottomMap = slot.auxMap,
            baseNoise = baseMap,
            erosionNoise = erosionMap,
            peakNoise = peakMap,
            moonstoneBlockID = this.moonstoneBlockID,
            acasterBlockID = this.acasterBlockID,
            pregen = isPregen
        };
        slot.handle = gccj.Schedule();
    }

    /*
    Biome Generation
    */

    // The returned array is reused by every call, so it must be consumed before calling this again
    public float[] BiomeNoise(float x, float y, ChunkDepthID cid)
    {
        x += 8;
        y += 8;
        float initialX = x;
        float initialY = y;
        float u,v,x2,y2,u2,v2;
        int A,B,A2,B2,X,Y,X2,Y2;


        float[] biomeVector = this.cacheBiomeVector;
        Array.Clear(biomeVector, 0, biomeVector.Length);


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