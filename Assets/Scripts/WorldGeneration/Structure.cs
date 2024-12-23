using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Random = System.Random;

[Serializable]
public class Structure
{
    public string name;
    public bool considerAir;
    public bool needsBase;
    public bool randomStates;
    public int sizeX, sizeY, sizeZ;
    public int offsetX, offsetZ;

    /*
    0: OverwriteAll
    1: FreeSpace
    2: SpecificOverwrite
    */
    public int type;


    public ushort[] blockdata_raw;
    public ushort[] statedata_raw;
    public ushort[] hpdata_raw;
    public string[] overwriteBlocks;
    public string[] acceptableBaseBlocks;

    // Post-Serialization
    private ushort[] blockdata;
    private VoxelMetadata meta;
    private FillType fillType;
    private HashSet<ushort> overwriteBlocksInternal;
    private HashSet<ushort> acceptableBaseBlocksInternal;

    private static int cacheX;
    private static int cacheY;
    private static int cacheZ;
    public static List<Chunk> reloadChunks = new List<Chunk>();

    private static Random rng;


    public void SetupAfterSerialize(){
        this.fillType = (FillType)this.type;

        this.blockdata = Compression.DecompressStructureBlocks(blockdata_raw);
        this.meta = new VoxelMetadata(this.sizeX, this.sizeY, this.sizeZ, Compression.DecompressStructureMetadata(hpdata_raw), Compression.DecompressStructureMetadata(statedata_raw));

        if(this.fillType == FillType.SpecificOverwrite){
            this.overwriteBlocksInternal = new HashSet<ushort>();

            foreach(string block in this.overwriteBlocks){
                this.overwriteBlocksInternal.Add(VoxelLoader.GetBlockID(block));
            }

            this.overwriteBlocks = null;
        }

        if(this.needsBase){
            this.acceptableBaseBlocksInternal = new HashSet<ushort>();

            foreach(string block in this.acceptableBaseBlocks){
                this.acceptableBaseBlocksInternal.Add(VoxelLoader.GetBlockID(block));
            }

            this.acceptableBaseBlocks = null;
        }        

        this.blockdata_raw = null;
        this.hpdata_raw = null;
        this.statedata_raw = null;
    }

    public bool AcceptBaseBlock(ushort baseBlock){
        if(!this.needsBase)
            return true;

        if(this.acceptableBaseBlocksInternal.Contains(baseBlock))
            return true;

        return false;
    }


    // Applies this structure to a cachedUshort array and a VoxelMetadata
    public virtual bool Apply(ChunkLoader_Server cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation){
        if(this.offsetX == 0 && this.offsetZ == 0)
            return ApplyAnchored(cl, pos, VD, VMHP, VMState, x, y, z, rotation);
        else
            return ApplyPivot(cl, pos, VD, VMHP, VMState, x, y, z, rotation);
    }

    /*
    Applies structure generation using pivot points as the middle of the structure
    */
    private bool ApplyPivot(ChunkLoader_Server cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation){
        bool retStatus;
        int minXChunk;
        int maxXChunk;
        int minZChunk;
        int maxZChunk;
        int minYChunk;
        int maxYChunk;
        int xRemainder;
        int zRemainder;
        int yRemainder;
        int initStructX, initStructZ, initStructY;
        int actualOffsetX, actualOffsetZ, actualSizeX, actualSizeZ;

        if(rotation == 0 || rotation == 2){
            actualOffsetX = this.offsetX;
            actualOffsetZ = this.offsetZ;
            actualSizeX = this.sizeX;
            actualSizeZ = this.sizeZ;
        }
        else{
            actualOffsetX = this.offsetZ;
            actualOffsetZ = this.offsetX;
            actualSizeX = this.sizeZ;
            actualSizeZ = this.sizeX;
        }


        int actualInitX = FindCoordPosition(x - actualOffsetX);
        int actualInitZ = FindCoordPosition(z - actualOffsetZ);
        int actualInitY = y;

        int mainChunkInitX = FindMainCoordPosition(x, actualOffsetX);
        int mainChunkInitZ = FindMainCoordPosition(z, actualOffsetZ);
        int mainChunkInitY = y;

        // Chunk Limits
        if(x - actualOffsetX < 0)
            minXChunk = Mathf.FloorToInt((x - actualOffsetX)/Chunk.chunkWidth)-1;
        else
            minXChunk = 0;

        if(z - actualOffsetZ < 0)
            minZChunk = Mathf.FloorToInt((z - actualOffsetZ)/Chunk.chunkWidth)-1;
        else
            minZChunk = 0;

        minYChunk = 0;

        maxXChunk = Mathf.FloorToInt((x + ((actualSizeX-1) - actualOffsetX))/Chunk.chunkWidth);
        maxZChunk = Mathf.FloorToInt((z + ((actualSizeZ-1) - actualOffsetZ))/Chunk.chunkWidth);
        maxYChunk = Mathf.FloorToInt((y + (this.sizeY-1))/Chunk.chunkDepth);

        // Calculates Remainder
        if(minXChunk == maxXChunk)
            xRemainder = actualSizeX;
        else if(maxXChunk == 0){
            xRemainder = (actualSizeX - ((Chunk.chunkWidth - actualInitX) + (-minXChunk-1)*Chunk.chunkWidth));
        }
        else
            xRemainder = Chunk.chunkWidth - mainChunkInitX;

        if(minZChunk == maxZChunk)
            zRemainder = actualSizeZ;
        else if(maxZChunk == 0){
            zRemainder = (actualSizeZ - ((Chunk.chunkWidth - actualInitZ) + (-minZChunk-1)*Chunk.chunkWidth));
        }
        else
            zRemainder = Chunk.chunkWidth - mainChunkInitZ;

        if(minYChunk == maxYChunk)
            yRemainder = this.sizeY;
        else
            yRemainder = Chunk.chunkDepth - mainChunkInitY;

        // Calculates initial StructX and StructZ
        if(minXChunk < 0)
            initStructX = actualOffsetX - x;
        else
            initStructX = 0;

        if(minZChunk < 0)
            initStructZ = actualOffsetZ - z;
        else
            initStructZ = 0;

        initStructY = 0;

        retStatus = ApplyToChunk(pos, true, true, true, cl, VD, VMHP, VMState, mainChunkInitX, y, mainChunkInitZ, xRemainder, zRemainder, yRemainder, initStructX, initStructZ, initStructY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation, isPivoted:true);

        if(!retStatus)
            return false;

        // Run loop for multi-chunk structures
        ChunkPos newPos; 
        int posX = 0;
        int posZ = 0;
        int posY = 0;
        int sPosX=0;
        int sPosZ=0;
        int sPosY=0;

        int numberOfXChunks = (maxXChunk - minXChunk) + 1;
        int numberOfZChunks = (maxZChunk - minZChunk) + 1;
        int numberOfYChunks = (maxYChunk - minYChunk) + 1;
        int currentXChunk = 0;
        int currentZChunk = 0;

        for(int yCount = minYChunk; yCount <= maxYChunk; yCount++){
            for(int zCount = minZChunk; zCount <= maxZChunk; zCount++){
                for(int xCount = minXChunk; xCount <= maxXChunk; xCount++){
                    if(zCount == 0 && xCount == 0 && yCount == 0){
                        currentXChunk++;
                        continue;
                    }

                    if(pos.y+yCount > Chunk.chunkMaxY)
                        continue;

                    newPos = new ChunkPos(pos.x+xCount, pos.z+zCount, pos.y+yCount);

                    // Calculates Positions
                    posX = 0;
                    posZ = 0;
                    posY = 0;

                    if(xCount == minXChunk){
                        posX = actualInitX;
                    }
                    if(zCount == minZChunk){
                        posZ = actualInitZ;
                    }
                    if(xCount != minXChunk && zCount != minZChunk){
                        posX = 0;
                        posZ = 0;
                    }
                    if(xCount == 0 && zCount == 0){
                        posX = x;
                        posZ = z;
                    }

                    if(yCount == 0)
                        posY = y;
                    else
                        posY = 0;

                    // Calculate Remainders
                    if(minXChunk == maxXChunk)
                        xRemainder = actualSizeX;
                    else if(xCount == maxXChunk){
                        xRemainder = (actualSizeX - ((Chunk.chunkWidth - actualInitX) + (currentXChunk-1)*Chunk.chunkWidth));
                    }
                    else
                        xRemainder = Chunk.chunkWidth - posX;


                    if(minZChunk == maxZChunk){
                        zRemainder = actualSizeZ;
                    }
                    else if(zCount == maxZChunk){
                        zRemainder = (actualSizeZ - ((Chunk.chunkWidth - actualInitZ) + (currentZChunk-1)*Chunk.chunkWidth));
                    }
                    else{
                        zRemainder = Chunk.chunkWidth - posZ;
                    }


                    if(minYChunk == maxYChunk)
                        yRemainder = this.sizeY;
                    else
                        yRemainder = Chunk.chunkDepth - posY;


                    // Struct Position
                    if(xCount == minXChunk){
                        sPosX = 0;
                    }
                    else if(xCount < maxXChunk)
                        sPosX = initStructX + ((currentXChunk-1) * Chunk.chunkWidth);
                    else if(xCount == maxXChunk){
                        sPosX = actualSizeX - xRemainder;
                    }

                    if(zCount == minZChunk)
                        sPosZ = 0;
                    else if(zCount < maxZChunk)
                        sPosZ = initStructZ + ((currentZChunk-1) * Chunk.chunkWidth);
                    else if(zCount == maxZChunk)
                        sPosZ = actualSizeZ - zRemainder;

                    if(yCount == minYChunk)
                        sPosY = 0;
                    else
                        sPosY = this.sizeY - yRemainder;
                    
                    
                    //Debug.Log("pos: (" + pos.x + ", " + pos.z + ")" + "\tActualInitXZ: " + actualInitX + ", " + actualInitZ + "\n" +
                    //    "SSizes: " + this.sizeX + ", " + this.sizeY + ", " + this.sizeZ + "\tRemainders: " + xRemainder + ", " + zRemainder + "\tsPos: " + sPosX + ", " + sPosZ + "\tChunksUsedX: " + 
                    //    minXChunk + "/" + maxXChunk + "\tChunksUsedZ: " + minZChunk + "/" + maxZChunk + "\tCurrentChunk: " + xCount + ", " + zCount
                    //    + "\tLogicalChunkCode: " + currentXChunk + ", " + currentZChunk + "\tRotation: " + rotation + "\tPos: " + posX + ", " + y + ", " + posZ);
                    

                    // ACTUAL APPLY FUNCTIONS
                    // Checks if it's a loaded chunk
                    if(cl.Contains(newPos)){
                        ApplyToChunk(newPos, false, true, true, cl, cl.GetChunk(newPos).data.GetData(), cl.GetChunk(newPos).metadata.GetHPData(), cl.GetChunk(newPos).metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation, isPivoted:true);
                        AddChunk(cl.GetChunk(newPos));
                        currentXChunk++;
                        continue;
                    }

                    // CASE WHERE REGIONFILES NEED TO BE LOOKED UPON
                    Chunk c;
                    cl.regionHandler.GetCorrectRegion(newPos);

                    // Check if it's an existing chunk
                    if(cl.regionHandler.IsIndexed(newPos)){
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation, isPivoted:true);
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            cl.regionHandler.LoadChunk(c);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation, isPivoted:true);                
                            AddChunk(c);
                        }
                    }
                    // Check if it's an ungenerated chunk
                    else{
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation, isPivoted:true);                        
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            c.biomeName = "Plains";
                            c.needsGeneration = 1;
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation, isPivoted:true);              
                            AddChunk(c);
                        }
                    }

                    currentXChunk++;
                }
                currentXChunk = 0;
                currentZChunk++;
            }
        }

        return true;
    }

    /*
    Applies legacy structure generation technique that disconsiders the usage of Pivot points for structures
    Application is anchored to bottom left of the structure
    */
    // Applies this structure to a cachedUshort array and a VoxelMetadata
    private bool ApplyAnchored(ChunkLoader_Server cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation){
        bool retStatus;

        int actualOffsetX, actualOffsetZ, actualSizeX, actualSizeZ;

        if(rotation == 0 || rotation == 2){
            actualOffsetX = this.offsetX;
            actualOffsetZ = this.offsetZ;
            actualSizeX = this.sizeX;
            actualSizeZ = this.sizeZ;
        }
        else{
            actualOffsetX = this.offsetZ;
            actualOffsetZ = this.offsetX;
            actualSizeX = this.sizeZ;
            actualSizeZ = this.sizeX;
        }

        int xChunks = Mathf.FloorToInt((x + actualSizeX - 1)/Chunk.chunkWidth);
        int zChunks = Mathf.FloorToInt((z + actualSizeZ - 1)/Chunk.chunkWidth);
        int yChunks = Mathf.FloorToInt((y + this.sizeY - 1)/Chunk.chunkDepth);

        int xRemainder, zRemainder, yRemainder;

        // Calculates Remainder
        if(xChunks > 0)
            xRemainder = Chunk.chunkWidth - x;
        else
            xRemainder = actualSizeX;

        if(zChunks > 0)
            zRemainder = Chunk.chunkWidth - z;
        else
            zRemainder = actualSizeZ;

        if(yChunks > 0)
            yRemainder = Chunk.chunkDepth - y;
        else
            yRemainder = this.sizeY;

        // Applies Structure to origin chunk
        retStatus = ApplyToChunk(pos, true, true, true, cl, VD, VMHP, VMState, x, y, z, xRemainder, zRemainder, yRemainder, 0, 0, 0, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation);

        // Possible failed return if in FreeSpace mode
        if(!retStatus){
            return false;
        }

        // Run loop for multi-chunk structures
        ChunkPos newPos;
        int posX = 0;
        int posZ = 0;
        int posY = 0;
        int sPosX=0;
        int sPosZ=0;
        int sPosY=0;

        for(int yCount=0; yCount <= yChunks; yCount++){
            for(int zCount=0; zCount <= zChunks; zCount++){
                for(int xCount=0; xCount <= xChunks; xCount++){

                    // Skips the origin chunk
                    if(zCount == 0 && xCount == 0 && yCount == 0){
                        continue;
                    }

                    // Skips chunks above limit
                    if(pos.y + yCount > Chunk.chunkMaxY)
                        continue;

                    newPos = new ChunkPos(pos.x+xCount, pos.z+zCount, pos.y+yCount);

                    // Calculates Positions
                    if(xCount == 0){
                        posX = x;
                        posZ = 0;
                    }
                    if(zCount == 0){
                        posX = 0;
                        posZ = z;
                    }
                    if(xCount != 0 && zCount != 0){
                        posX = 0;
                        posZ = 0;
                    }
                    if(xCount == 0 && zCount == 0){
                        posX = x;
                        posZ = z;
                    }

                    if(yCount == 0)
                        posY = y;
                    else
                        posY = 0;

                    // Calculate Remainders
                    if(xChunks == 0){
                        xRemainder = actualSizeX;
                    }
                    else if(xCount == xChunks){
                        xRemainder = (actualSizeX - ((Chunk.chunkWidth - x) + (xCount-1)*Chunk.chunkWidth));
                    }
                    else{
                        xRemainder = (Chunk.chunkWidth - posX);
                    }

                    if(zChunks == 0){
                        zRemainder = actualSizeZ;
                    }
                    else if(zCount == zChunks){
                        zRemainder = (actualSizeZ - ((Chunk.chunkWidth - z) + (zCount-1)*Chunk.chunkWidth));
                    }
                    else{
                        zRemainder = (Chunk.chunkWidth - posZ);
                    }

                    if(yChunks == 0){
                        yRemainder = this.sizeY;
                    }
                    else if(yCount == yChunks){
                        yRemainder = (this.sizeY - ((Chunk.chunkDepth - y) + (yCount-1)*Chunk.chunkDepth));
                    }
                    else{
                        yRemainder = (Chunk.chunkDepth - posY);
                    }

                    // Struct Position
                    if(xCount == 0)
                        sPosX = 0;
                    else if(xCount < xChunks)
                        sPosX = (Chunk.chunkWidth - x) + ((xCount-1) * Chunk.chunkWidth);
                    else if(xCount == xChunks)
                        sPosX = actualSizeX - xRemainder;

                    if(zCount == 0)
                        sPosZ = 0;
                    else if(zCount < zChunks)
                        sPosZ = (Chunk.chunkWidth - z) + ((zCount-1) * Chunk.chunkWidth);
                    else if(zCount == zChunks)
                        sPosZ = actualSizeZ - zRemainder;

                    if(yCount == 0)
                        sPosY = 0;
                    else if(yCount < yChunks)
                        sPosY = (Chunk.chunkDepth - y) + ((yCount-1) * Chunk.chunkDepth);
                    else if(yCount == yChunks)
                        sPosY = this.sizeY - yRemainder;


                    // ACTUAL APPLY FUNCTIONS
                    // Checks if it's a loaded chunk
                    if(cl.Contains(newPos)){
                        ApplyToChunk(newPos, false, true, true, cl, cl.GetChunk(newPos).data.GetData(), cl.GetChunk(newPos).metadata.GetHPData(), cl.GetChunk(newPos).metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation);
                        AddChunk(cl.GetChunk(newPos));
                        continue;
                    }

                    // CASE WHERE REGIONFILES NEED TO BE LOOKED UPON
                    Chunk c;
                    cl.regionHandler.GetCorrectRegion(newPos);

                    // Check if it's an existing chunk
                    if(cl.regionHandler.IsIndexed(newPos)){
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation);
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            cl.regionHandler.LoadChunk(c);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation);                
                            AddChunk(c);
                        }
                    }
                    // Check if it's an ungenerated chunk
                    else{
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation);                        
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            c.biomeName = "Plains";
                            c.needsGeneration = 1;
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, actualSizeX, actualSizeZ, actualOffsetX, actualOffsetZ, rotation);              
                            AddChunk(c);
                        }
                    }
                }
            }
        }
        return true;
    }


    // Applies this structure to a chunk
    // Receives a Chunk reference that will be changed in this function
    private bool ApplyToChunk(ChunkPos pos, bool initialchunk, bool exist, bool loaded, ChunkLoader_Server cl, ushort[] VD, ushort[] VMHP, ushort[] VMState, int posX, int posY, int posZ, int remainderX, int remainderZ, int remainderY, int structinitX, int structinitZ, int structinitY, int actualSizeX, int actualSizeZ, int actualOffsetX, int actualOffsetZ, int rotation, bool isPivoted=false){
        bool exists = exist;

        int structX = structinitX;
        int structZ = structinitZ;
        int structY = structinitY;

        // Applies Free Space building rules to existing chunk
        if(this.fillType == FillType.FreeSpace){
            if(initialchunk){
                if(!this.considerAir){
                    if(CheckFreeSpace(VD, posX, posY, posZ, rotation, remainderX, remainderZ, remainderY, actualSizeX, actualSizeZ, isPivoted:isPivoted)){
                        for(int y=posY; y < posY + remainderY; y++){
                            structX = structinitX;
                            for(int x=posX; x < posX + remainderX; x++){
                                structZ = structinitZ;
                                for(int z=posZ; z < posZ + remainderZ; z++){

                                    RotateData(structX, structY, structZ, rotation);


                                    if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                        structZ++;
                                        continue;
                                    }

                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, cacheX, cacheY, cacheZ, this.meta.GetState(cacheX, cacheY, cacheZ));

                                    structZ++;
                                }
                                structX++;
                            }
                            structY++;
                        }
                        return true;
                    }
                    else{
                        return false;
                    }
                }
                else{
                    if(CheckFreeSpace(VD, posX, posY, posZ, rotation, remainderX, remainderZ, remainderY, actualSizeX, actualSizeZ, isPivoted:isPivoted)){
                        for(int y=posY; y < posY + remainderY; y++){
                            structX = structinitX;
                            for(int x=posX; x < posX + remainderX; x++){
                                structZ = structinitZ;
                                for(int z=posZ; z < posZ + remainderZ; z++){
                                    RotateData(structX, structY, structZ, rotation);
                                    if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0)
                                        VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                                    else
                                        VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, cacheX, cacheY, cacheZ, this.meta.GetState(cacheX, cacheY, cacheZ));
                                    structZ++;
                                }
                                structX++;
                            }
                            structY++;
                        }
                        return true;
                    }
                    else{
                        return false;
                    }
                }
            }
            else{
                if(!this.considerAir){
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){

                                RotateData(structX, structY, structZ, rotation);

                                if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                    structZ++;
                                    continue;
                                }

                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, cacheX, cacheY, cacheZ, this.meta.GetState(cacheX, cacheY, cacheZ));

                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    }
                    return true;
                }
                else if(this.considerAir && exists){
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                RotateData(structX, structY, structZ, rotation);

                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, cacheX, cacheY, cacheZ, this.meta.GetState(cacheX, cacheY, cacheZ));
                                
                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    }
                    return true;
                }
                else if(this.considerAir && !exists){
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                RotateData(structX, structY, structZ, rotation);
                                if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, cacheX, cacheY, cacheZ, this.meta.GetState(cacheX, cacheY, cacheZ));
                                }
                                else{
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, cacheX, cacheY, cacheZ, this.meta.GetState(cacheX, cacheY, cacheZ));
                                }
                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    } 
                    return true;
                }            
            }
        }

        // Applies in SpecificOverwrite rule to existing chunk
        else if(this.fillType == FillType.SpecificOverwrite){
            bool shouldDrawNeighbors = false;

            if(exists){
                if(!this.considerAir){
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                if(this.overwriteBlocksInternal.Contains(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){
                                    RotateData(structX, structY, structZ, rotation);
                                    if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                        continue;
                                    }

                                    shouldDrawNeighbors = true;
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                                }
                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    }
                    return shouldDrawNeighbors;
                }
                else{
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                if(this.overwriteBlocksInternal.Contains(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){
                                    RotateData(structX, structY, structZ, rotation);
                                    if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0)
                                        VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                                    else
                                        VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                    shouldDrawNeighbors = true;
                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                                }
                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    }
                    return shouldDrawNeighbors;                
                }
            }
            else{
                if(!this.considerAir){
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                RotateData(structX, structY, structZ, rotation);

                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));

                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    } 
                    return true;
                }
                else if(this.considerAir){
                    for(int y=posY; y < posY + remainderY; y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                RotateData(structX, structY, structZ, rotation);
                                if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                                }
                                else{
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                    VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                    VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                                }
                                structZ++;
                            }
                            structX++;
                        }
                        structY++;
                    } 
                    return true;
                }
            }
        }

        // Applies in OverwriteAll state
        else if(this.fillType == FillType.OverwriteAll){
            // Handling if air is taken into account in generated chunks
            if(this.considerAir && exists){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);

                            VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                            VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                            VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                            
                            structZ++;
                        }
                        structX++;
                    }
                    structY++;
                }
            }
            // Handling if air is taken into account in blank chunks
            else if(this.considerAir && !exists){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
                            if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                            }
                            else{
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                            }
                            structZ++;
                        }
                        structX++;
                    }
                    structY++;
                }                
            }
            // Handles if air is not taken into account in new chunks
            else if(!this.considerAir){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
                            if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] != 0){
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = GetState(pos, x, y, z, this.meta.GetState(cacheX, cacheY, cacheZ));
                            }
                            structZ++;
                        }
                        structX++;
                    }
                    structY++;
                }
            }

            return true;
        }

        return false;
    } 

    // Returns the state of the structure block considering the randomState mechanics
    private ushort GetState(ChunkPos pos, int x, int y, int z, ushort state){
        if(this.randomStates){
            Structure.rng = new Random(Mathf.FloorToInt((pos.x*Chunk.chunkWidth+x)*GenerationSeed.patchRandomStep + (pos.y*Chunk.chunkDepth+y)*GenerationSeed.patchRandomStep + (pos.z*Chunk.chunkWidth+z)*GenerationSeed.patchRandomStep2));
            return (ushort)Structure.rng.Next(0, state);
        }
        return state; 
    }

    // Checks for valid space in FreeSpace mode
    private bool CheckFreeSpace(ushort[] data, int x, int y, int z, int rotation, int remainderX, int remainderZ, int remainderY, int actualSizeX, int actualSizeZ, bool isPivoted=false){
        int xRemainder, zRemainder, yRemainder;

        if(!isPivoted){
            xRemainder = Mathf.Min(Chunk.chunkWidth - x, actualSizeX);
            zRemainder = Mathf.Min(Chunk.chunkWidth - z, actualSizeZ);
            yRemainder = Mathf.Min(Chunk.chunkDepth - y, this.sizeY);
        }
        else{
            xRemainder = Mathf.Min(Chunk.chunkWidth - x, remainderX);
            zRemainder = Mathf.Min(Chunk.chunkWidth - z, remainderZ);
            yRemainder = Mathf.Min(Chunk.chunkDepth - y, remainderY);
        }

        // Case Struct considers it's air as a needed block
        if(this.considerAir){
            for(int yCount = 0; yCount < yRemainder; yCount++){
                for(int xCount = 0; xCount < xRemainder; xCount++){
                    for(int zCount = 0; zCount < zRemainder; zCount++){
                        if(data[(x + xCount)*Chunk.chunkWidth*Chunk.chunkDepth+(y + yCount)*Chunk.chunkWidth+(z + zCount)] != 0)
                            return false;
                    }
                }
            }
            return true;
        }
        // Case Struct doesn't consider air as a needed block
        else{
            for(int yCount = 0; yCount < yRemainder; yCount++){
                for(int xCount = 0; xCount < xRemainder; xCount++){
                    for(int zCount = 0; zCount < zRemainder; zCount++){
                        RotateData(xCount, yCount, zCount, rotation);
                        if(data[(x + xCount)*Chunk.chunkWidth*Chunk.chunkDepth+(y + yCount)*Chunk.chunkWidth+(z + zCount)] != 0 && this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] != 0)
                            return false;
                    }
                }
            }
            return true;          
        }

    }

    // Checks if a chunk pos exists in reloadChunks
    public static bool Exists(ChunkPos pos){
        foreach(Chunk c in Structure.reloadChunks){
            if(c.pos == pos){
                return true;
            }
        }
        return false;
    }

    // Gets the chunk given it's pos
    public static Chunk GetChunk(ChunkPos pos){
        foreach(Chunk c in Structure.reloadChunks){
            if(c.pos == pos){
                return c;
            }
        }
        return new Chunk(pos);
    }

    // Removes a chunk from reload static list
    public static bool RemoveChunk(ChunkPos pos){
        foreach(Chunk c in Structure.reloadChunks){
            if(c.pos == pos){
                Structure.reloadChunks.Remove(c);
                return true;
            }
        }
        return false;
    }
    

    // Does a Rough apply on synchonization problems when loading a Chunk before applying
    //  Structure to it
    public static void RoughApply(Chunk c, Chunk st){
        RoughApply(c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), st);
    }

    // Does a Rough apply on synchonization problems when loading a Chunk before applying
    //  Structure to it
    public static void RoughApply(ushort[] cacheVoxdata, ushort[] cacheHP, ushort[] cacheState, Chunk st){
        NativeArray<ushort> blockIn = new NativeArray<ushort>(st.data.GetData(), Allocator.TempJob);
        NativeArray<ushort> hpIn = new NativeArray<ushort>(st.metadata.GetHPData(), Allocator.TempJob);
        NativeArray<ushort> stateIn = new NativeArray<ushort>(st.metadata.GetStateData(), Allocator.TempJob);
        NativeArray<ushort> blockOut = new NativeArray<ushort>(cacheVoxdata, Allocator.TempJob);
        NativeArray<ushort> hpOut = new NativeArray<ushort>(cacheHP, Allocator.TempJob);
        NativeArray<ushort> stateOut = new NativeArray<ushort>(cacheState, Allocator.TempJob);

        RoughApplyJob raJob = new RoughApplyJob{
            blockIn = blockIn,
            hpIn = hpIn,
            stateIn = stateIn,
            blockOut = blockOut,
            hpOut = hpOut,
            stateOut = stateOut
        };

        JobHandle job = raJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        blockOut.CopyTo(cacheVoxdata);
        hpOut.CopyTo(cacheHP);
        stateOut.CopyTo(cacheState);

        // Dispose Bin
        blockIn.Dispose(); 
        hpIn.Dispose();
        stateIn.Dispose();
        blockOut.Dispose();
        hpOut.Dispose();
        stateOut.Dispose();
    }

    // Changes the index of rotation to rotate Structures at Apply-Time
    /*
    Rotation Types:
    0: No Rotation
    1: 90º
    2: 180º
    3: 270º
    */
    private void RotateData(int sPosX, int sPosY, int sPosZ, int rotation){
        cacheY = sPosY;


        if(rotation == 0){
            cacheX = sPosX;
            cacheZ = sPosZ;
        }
        else if(rotation == 1){
            cacheX = sPosZ;
            cacheZ = sPosX;
        }
        else if(rotation == 2){
            cacheX = (this.sizeX - sPosX) - 1;
            cacheZ = (this.sizeZ - sPosZ) - 1;
        }
        else if(rotation == 3){
            cacheX = (this.sizeX - sPosZ) - 1;
            cacheZ = (this.sizeZ - sPosX) - 1;
        }
        else{
            cacheX = sPosX;
            cacheZ = sPosZ;            
        }
    }

    // Adds chunk to reload static list
    private void AddChunk(Chunk c){
        if(!Structure.reloadChunks.Contains(c)){
            Structure.reloadChunks.Add(c);
        }
    }

    // Does circular assignment of negative into in-chunk coord space
    private int FindCoordPosition(int pos){
        if(pos > 0 && pos < Chunk.chunkWidth)
            return pos;

        if(pos < 0){
            return ((pos%Chunk.chunkWidth)+Chunk.chunkWidth)%Chunk.chunkWidth;
        }

        return pos%Chunk.chunkWidth;
    }

    // Finds the position in main chunk that a structure starts
    private int FindMainCoordPosition(int pos, int structSize){
        pos = pos - structSize;

        if(pos < 0)
            return 0;
        else
            return pos;
    }

}

public enum FillType{
    OverwriteAll, // Will erase any blocks in selected region
    FreeSpace, // Will need free space to generate, if considerAir is off, disconsiders self air colision
    SpecificOverwrite, // Will generate structure blocks only on specific blocks
}


[BurstCompile]
public struct RoughApplyJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockIn;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> stateIn;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> hpIn;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> blockOut;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> stateOut;
    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> hpOut;

    public void Execute(int index){
        ushort block;

        int x = index;

        for(int y=0; y < Chunk.chunkDepth; y++){
            for(int z=0; z < Chunk.chunkWidth; z++){
                block = blockIn[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

                // Ignores all air
                if(block == 0)
                    continue;

                blockOut[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = block;

                hpOut[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = hpIn[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
                stateOut[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = stateIn[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
            }
        }
    }
}
