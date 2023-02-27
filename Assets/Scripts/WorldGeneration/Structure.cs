using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public abstract class Structure
{
    public int code;

    public ushort[] blockdata;
    public VoxelMetadata meta;

    private static int cacheX;
    private static int cacheY;
    private static int cacheZ;

    public static List<Chunk> reloadChunks = new List<Chunk>();

    public bool considerAir;
    public bool needsBase;

    public int sizeX, sizeY, sizeZ;
    public int offsetX, offsetZ;

    // Cache
    private ushort[] decompressedBlocks;
    private ushort[] decompressedHP;
    private ushort[] decompressedState;

    /*
    0: OverwriteAll
    1: FreeSpace
    2: SpecificOverwrite
    */
    public FillType type;

    public HashSet<ushort> overwriteBlocks;
    public HashSet<BlockID> acceptableBaseBlocks;


    /*
    Overall Structure List
    ADD TO THIS LIST FOR EVERY STRUCTURE IMPLEMENTED
    */
    public static Structure Generate(int code){
        switch((StructureCode)code){
            case StructureCode.TestStruct:
                return new TestStruct();
            case StructureCode.TreeSmallA:
                return new TreeSmallA();
            case StructureCode.TreeMediumA:
                return new TreeMediumA();
            case StructureCode.DirtPileA:
                return new DirtPileA();
            case StructureCode.DirtPileB:
                return new DirtPileB();
            case StructureCode.BoulderNormalA:
                return new BoulderNormalA();
            case StructureCode.TreeBigA:
                return new TreeBigA();
            case StructureCode.TreeCrookedMediumA:
                return new TreeCrookedMediumA();
            case StructureCode.TreeSmallB:
                return new TreeSmallB();
            case StructureCode.IronVeinA:
                return new IronVeinA();
            case StructureCode.IronVeinB:
                return new IronVeinB();
            case StructureCode.IronVeinC:
                return new IronVeinC();
            case StructureCode.CoalVeinA:
                return new CoalVeinA();
            case StructureCode.CoalVeinB:
                return new CoalVeinB();
            case StructureCode.CoalVeinC:
                return new CoalVeinC();
            case StructureCode.CopperVeinA:
                return new CopperVeinA();
            case StructureCode.CopperVeinB:
                return new CopperVeinB();
            case StructureCode.TinVeinA:
                return new TinVeinA();
            case StructureCode.TinVeinB:
                return new TinVeinB();
            case StructureCode.GoldVeinA:
                return new GoldVeinA();
            case StructureCode.GoldVeinB:
                return new GoldVeinB();
            case StructureCode.AluminiumVeinA:
                return new AluminiumVeinA();
            case StructureCode.AluminiumVeinB:
                return new AluminiumVeinB();
            case StructureCode.EmeriumVeinA:
                return new EmeriumVeinA();
            case StructureCode.EmeriumVeinB:
                return new EmeriumVeinB();
            case StructureCode.UraniumVeinA:
                return new UraniumVeinA();
            case StructureCode.UraniumVeinB:
                return new UraniumVeinB();
            case StructureCode.MagnetiteVeinA:
                return new MagnetiteVeinA();
            case StructureCode.MagnetiteVeinB:
                return new MagnetiteVeinB();
            case StructureCode.EmeraldVeinA:
                return new EmeraldVeinA();
            case StructureCode.EmeraldVeinB:
                return new EmeraldVeinB();
            case StructureCode.RubyVeinA:
                return new RubyVeinA();
            case StructureCode.RubyVeinB:
                return new RubyVeinB();
            case StructureCode.GravelPile:
                return new GravelPile();
            default:
                return new TestStruct();
        }
    }

    public static Structure Generate(StructureCode code){
        return Structure.Generate((int)code);
    }


    // Prepares array
    public virtual void Prepare(ushort[] data, ushort[] hp, ushort[] state){
        int i=0;

        decompressedBlocks = Compression.DecompressStructureBlocks(data);
        decompressedHP = Compression.DecompressStructureMetadata(hp);
        decompressedState = Compression.DecompressStructureMetadata(state);

        for(int y=0; y < this.sizeY; y++){
            for(int x=0; x < this.sizeX; x++){
                for(int z=0; z < this.sizeZ; z++){
                    this.blockdata[x*sizeZ*sizeY+y*sizeZ+z] = decompressedBlocks[i];
                    this.meta.SetHP(x,y,z, decompressedHP[i]);
                    this.meta.SetState(x,y,z, decompressedState[i]);

                    i++;
                }
            }
        }
    }

    public bool AcceptBaseBlock(ushort baseBlock){
        if(!this.needsBase)
            return true;

        if(this.acceptableBaseBlocks.Contains((BlockID)baseBlock))
            return true;

        return false;
    }


    // Applies this structure to a cachedUshort array and a VoxelMetadata
    public virtual bool Apply(ChunkLoader_Server cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation=0){
        if(this.offsetX == 0 && this.offsetZ == 0)
            return ApplyAnchored(cl, pos, VD, VMHP, VMState, x, y, z, rotation:rotation);
        else
            return ApplyPivot(cl, pos, VD, VMHP, VMState, x, y, z, rotation:rotation);
    }

    /*
    Applies structure generation using pivot points as the middle of the structure
    */
    private bool ApplyPivot(ChunkLoader_Server cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation=0){
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

        int actualInitX = FindCoordPosition(x - this.offsetX);
        int actualInitZ = FindCoordPosition(z - this.offsetZ);
        int actualInitY = y;

        int mainChunkInitX = FindMainCoordPosition(x);
        int mainChunkInitZ = FindMainCoordPosition(z, isX:false);
        int mainChunkInitY = y;

        // Chunk Limits
        if(x - this.offsetX < 0)
            minXChunk = Mathf.FloorToInt((x - this.offsetX)/Chunk.chunkWidth)-1;
        else
            minXChunk = 0;

        if(z - this.offsetZ < 0)
            minZChunk = Mathf.FloorToInt((z - this.offsetZ)/Chunk.chunkWidth)-1;
        else
            minZChunk = 0;

        minYChunk = 0;

        maxXChunk = Mathf.FloorToInt((x + ((this.sizeX-1) - this.offsetX))/Chunk.chunkWidth);
        maxZChunk = Mathf.FloorToInt((z + ((this.sizeZ-1) - this.offsetZ))/Chunk.chunkWidth);
        maxYChunk = Mathf.FloorToInt((y + (this.sizeY-1))/Chunk.chunkDepth);

        // Calculates Remainder
        if(minXChunk == maxXChunk)
            xRemainder = this.sizeX;
        else if(maxXChunk == 0){
            xRemainder = (this.sizeX - ((Chunk.chunkWidth - actualInitX) + (-minXChunk-1)*Chunk.chunkWidth));
        }
        else
            xRemainder = Chunk.chunkWidth - mainChunkInitX;

        if(minZChunk == maxZChunk)
            zRemainder = this.sizeZ;
        else if(maxZChunk == 0){
            zRemainder = (this.sizeZ - ((Chunk.chunkWidth - actualInitZ) + (-minZChunk-1)*Chunk.chunkWidth));
        }
        else
            zRemainder = Chunk.chunkWidth - mainChunkInitZ;

        if(minYChunk == maxYChunk)
            yRemainder = this.sizeY;
        else
            yRemainder = Chunk.chunkDepth - mainChunkInitY;

        // Calculates initial StructX and StructZ
        if(minXChunk < 0)
            initStructX = this.offsetX - x;
        else
            initStructX = 0;

        if(minZChunk < 0)
            initStructZ = this.offsetZ - z;
        else
            initStructZ = 0;

        initStructY = 0;

        /*
        Debug.Log("MAIN CHUNK: " + pos + "\tStruct: " + this.code + "\n" + "SSizes: " + this.sizeX + ", " + this.sizeY + ", " + this.sizeZ + "\tRemainders: " + xRemainder + ", " + zRemainder
         + "\tsPos: " + initStructX + ", " + initStructZ + "\tChunksUsedX: " + 
            minXChunk + "/" + maxXChunk + "\tChunksUsedZ: " + minZChunk + "/" + maxZChunk + "\tRotation: " + rotation + "\tPos: " + mainChunkInitX + ", " + y + ", " + mainChunkInitZ
            + "\tPivot: " + x + ", " + y + ", " + z);
        */

        retStatus = ApplyToChunk(pos, true, true, true, cl, VD, VMHP, VMState, mainChunkInitX, y, mainChunkInitZ, xRemainder, zRemainder, yRemainder, initStructX, initStructZ, initStructY, rotation:rotation, isPivoted:true);

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
                        xRemainder = this.sizeX;
                    else if(xCount == maxXChunk){
                        xRemainder = (this.sizeX - ((Chunk.chunkWidth - actualInitX) + (currentXChunk-1)*Chunk.chunkWidth));
                    }
                    else
                        xRemainder = Chunk.chunkWidth - posX;


                    if(minZChunk == maxZChunk){
                        zRemainder = this.sizeZ;
                    }
                    else if(zCount == maxZChunk){
                        zRemainder = (this.sizeZ - ((Chunk.chunkWidth - actualInitZ) + (currentZChunk-1)*Chunk.chunkWidth));
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
                        sPosX = this.sizeX - xRemainder;
                    }

                    if(zCount == minZChunk)
                        sPosZ = 0;
                    else if(zCount < maxZChunk)
                        sPosZ = initStructZ + ((currentZChunk-1) * Chunk.chunkWidth);
                    else if(zCount == maxZChunk)
                        sPosZ = this.sizeZ - zRemainder;

                    if(yCount == minYChunk)
                        sPosY = 0;
                    else
                        sPosY = this.sizeY - yRemainder;
                    
                    /*
                    Debug.Log("SSizes: " + this.sizeX + ", " + this.sizeY + ", " + this.sizeZ + "\tRemainders: " + xRemainder + ", " + zRemainder + "\tsPos: " + sPosX + ", " + sPosZ + "\tChunksUsedX: " + 
                        minXChunk + "/" + maxXChunk + "\tChunksUsedZ: " + minZChunk + "/" + maxZChunk + "\tCurrentChunk: " + xCount + ", " + zCount
                        + "\tLogicalChunkCode: " + currentXChunk + ", " + currentZChunk + "\tRotation: " + rotation + "\tPos: " + posX + ", " + y + ", " + posZ);
                    */

                    // ACTUAL APPLY FUNCTIONS
                    // Checks if it's a loaded chunk
                    if(cl.chunks.ContainsKey(newPos)){
                        ApplyToChunk(newPos, false, true, true, cl, cl.chunks[newPos].data.GetData(), cl.chunks[newPos].metadata.GetHPData(), cl.chunks[newPos].metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation, isPivoted:true);
                        AddChunk(cl.chunks[newPos]);
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
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation, isPivoted:true);
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            cl.regionHandler.LoadChunk(c);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation, isPivoted:true);                
                            AddChunk(c);
                        }
                    }
                    // Check if it's an ungenerated chunk
                    else{
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation, isPivoted:true);                        
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            c.biomeName = "Plains";
                            c.needsGeneration = 1;
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation, isPivoted:true);              
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
    private bool ApplyAnchored(ChunkLoader_Server cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation=0){
        bool retStatus;
        int xChunks = Mathf.FloorToInt((x + this.sizeX - 1)/Chunk.chunkWidth);
        int zChunks = Mathf.FloorToInt((z + this.sizeZ - 1)/Chunk.chunkWidth);
        int yChunks = Mathf.FloorToInt((y + this.sizeY - 1)/Chunk.chunkDepth);

        int xRemainder, zRemainder, yRemainder;

        // Calculates Remainder
        if(xChunks > 0)
            xRemainder = Chunk.chunkWidth - x;
        else
            xRemainder = this.sizeX;

        if(zChunks > 0)
            zRemainder = Chunk.chunkWidth - z;
        else
            zRemainder = this.sizeZ;

        if(yChunks > 0)
            yRemainder = Chunk.chunkDepth - y;
        else
            yRemainder = this.sizeY;

        // Applies Structure to origin chunk
        retStatus = ApplyToChunk(pos, true, true, true, cl, VD, VMHP, VMState, x, y, z, xRemainder, zRemainder, yRemainder, 0, 0, 0);

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
                        xRemainder = this.sizeX;
                    }
                    else if(xCount == xChunks){
                        xRemainder = (this.sizeX - ((Chunk.chunkWidth - x) + (xCount-1)*Chunk.chunkWidth));
                    }
                    else{
                        xRemainder = (Chunk.chunkWidth - posX);
                    }

                    if(zChunks == 0){
                        zRemainder = this.sizeZ;
                    }
                    else if(zCount == zChunks){
                        zRemainder = (this.sizeZ - ((Chunk.chunkWidth - z) + (zCount-1)*Chunk.chunkWidth));
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
                        sPosX = this.sizeX - xRemainder;

                    if(zCount == 0)
                        sPosZ = 0;
                    else if(zCount < zChunks)
                        sPosZ = (Chunk.chunkWidth - z) + ((zCount-1) * Chunk.chunkWidth);
                    else if(zCount == zChunks)
                        sPosZ = this.sizeZ - zRemainder;

                    if(yCount == 0)
                        sPosY = 0;
                    else if(yCount < yChunks)
                        sPosY = (Chunk.chunkDepth - y) + ((yCount-1) * Chunk.chunkDepth);
                    else if(yCount == yChunks)
                        sPosY = this.sizeY - yRemainder;


                    // ACTUAL APPLY FUNCTIONS
                    // Checks if it's a loaded chunk
                    if(cl.chunks.ContainsKey(newPos)){
                        ApplyToChunk(newPos, false, true, true, cl, cl.chunks[newPos].data.GetData(), cl.chunks[newPos].metadata.GetHPData(), cl.chunks[newPos].metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation);
                        AddChunk(cl.chunks[newPos]);
                        continue;
                    }

                    // CASE WHERE REGIONFILES NEED TO BE LOOKED UPON
                    Chunk c;
                    cl.regionHandler.GetCorrectRegion(newPos);

                    // Check if it's an existing chunk
                    if(cl.regionHandler.IsIndexed(newPos)){
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation);
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            cl.regionHandler.LoadChunk(c);
                            ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation);                
                            AddChunk(c);
                        }
                    }
                    // Check if it's an ungenerated chunk
                    else{
                        if(Structure.Exists(newPos)){
                            c = Structure.GetChunk(newPos);
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation);                        
                        }
                        else{
                            c = new Chunk(newPos, server:true);
                            c.biomeName = "Plains";
                            c.needsGeneration = 1;
                            ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, posY, posZ, xRemainder, zRemainder, yRemainder, sPosX, sPosZ, sPosY, rotation:rotation);              
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
    private bool ApplyToChunk(ChunkPos pos, bool initialchunk, bool exist, bool loaded, ChunkLoader_Server cl, ushort[] VD, ushort[] VMHP, ushort[] VMState, int posX, int posY, int posZ, int remainderX, int remainderZ, int remainderY, int structinitX, int structinitZ, int structinitY, int rotation=0, bool isPivoted=false){

        bool exists = exist;

        int structX = structinitX;
        int structZ = structinitZ;
        int structY = structinitY;

        // Applies Free Space building rules to existing chunk
        if(this.type == FillType.FreeSpace && exists && initialchunk){
            if(!this.considerAir){
                if(CheckFreeSpace(VD, posX, posY, posZ, rotation, remainderX, remainderZ, remainderY, isPivoted:isPivoted)){
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
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);

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
                if(CheckFreeSpace(VD, posX, posY, posZ, rotation, remainderX, remainderZ, remainderY, isPivoted:isPivoted)){
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
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
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

        // Applies in SpecificOverwrite rule to existing chunk
        else if(this.type == FillType.SpecificOverwrite && exists && initialchunk){
            bool shouldDrawNeighbors = false;

            if(!this.considerAir){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            if(this.overwriteBlocks.Contains(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){

                                RotateData(structX, structY, structZ, rotation);

                                if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                    continue;
                                }

                                shouldDrawNeighbors = true;
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
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
                            if(this.overwriteBlocks.Contains(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){
                                RotateData(structX, structY, structZ, rotation);
                                if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0)
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                                else
                                    VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                shouldDrawNeighbors = true;
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
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

        // Applies in OverwriteAll state
        else if(this.type == FillType.OverwriteAll || !exists || exists){
            // Handling if air is taken into account in generated chunks
            if(this.considerAir && exists){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
                            // If air add pregen block
                            if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0)
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = (ushort)(ushort.MaxValue/2);
                            else
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                            VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                            VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
                            
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
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
                            }
                            else{
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];
                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
                            }
                            structZ++;
                        }
                        structX++;
                    }
                    structY++;
                }                
            }
            // Handling if air is not taken into account in generated chunks
            else if(!this.considerAir && exists){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
                            if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] != 0){
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
                            }
                            structZ++;
                        }
                        structX++;
                    }
                    structY++;
                }               
            }
            // Handles if air is not taken into account in new chunks
            else if(!this.considerAir && !exists){
                for(int y=posY; y < posY + remainderY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
                            if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] != 0){
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
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

    // Checks for valid space in FreeSpace mode
    private bool CheckFreeSpace(ushort[] data, int x, int y, int z, int rotation, int remainderX, int remainderZ, int remainderY, bool isPivoted=false){
        int xRemainder, zRemainder, yRemainder;

        if(!isPivoted){
            xRemainder = Mathf.Min(Chunk.chunkWidth - x, this.sizeX);
            zRemainder = Mathf.Min(Chunk.chunkWidth - z, this.sizeZ);
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
    private void RotateData(int x, int y, int z, int rotation){
        cacheY = y;

        // No rotation
        if(rotation == 0){
            cacheX = x;
            cacheZ = z;
        }
        else if(rotation == 1){
            cacheX = (this.sizeX - x) - 1;
            cacheZ = z;
        }
        else if(rotation == 2){
            cacheX = (this.sizeX - x) - 1;
            cacheZ = (this.sizeZ - z) - 1;
        }
        else if(rotation == 3){
            cacheX = x;
            cacheZ = (this.sizeZ - z) - 1;
        }
        else{
            cacheX = x;
            cacheZ = z;            
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
    private int FindMainCoordPosition(int pos, bool isX=true){
        if(isX)
            pos = pos - this.offsetX;
        else
            pos = pos - this.offsetZ;

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
