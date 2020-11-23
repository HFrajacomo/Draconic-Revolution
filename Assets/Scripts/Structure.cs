using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public int sizeX, sizeY, sizeZ;
    public int offsetX, offsetZ;

    /*
    0: OverwriteAll
    1: FreeSpace
    2: SpecificOverwrite
    */
    public FillType type;

    public List<ushort> overwriteBlocks;


    /*
    Overall Structure List
    ADD TO THIS LIST FOR EVERY STRUCTURE IMPLEMENTED
    */
    public static Structure Generate(int code){
        switch(code){
            case 0:
                return new TestStruct();
            case 1:
                return new TreeSmallA();
            case 2:
                return new TreeMediumA();
            case 3:
                return new DirtPileA();
            case 4:
                return new DirtPileB();
            case 5:
                return new BoulderNormalA();
            case 6:
                return new TreeBigA();
            case 7:
                return new TreeCrookedMediumA();
            case 8:
                return new TreeSmallB();
            case 9:
                return new MetalVeinA();
            case 10:
                return new MetalVeinB();
            case 11:
                return new MetalVeinC();
            default:
                return new TestStruct();
        }
    }

    public static Structure Generate(StructureCode code){
        return Structure.Generate((int)code);
    }


    // Prepares array
    public virtual void Prepare(ushort[] data, ushort?[] hp, ushort?[] state){
        int i=0;

        for(int y=0; y < this.sizeY; y++){
            for(int x=0; x < this.sizeX; x++){
                for(int z=0; z < this.sizeZ; z++){
                    this.blockdata[x*sizeZ*sizeY+y*sizeZ+z] = data[i];

                    if(hp[i] != null)
                        this.meta.SetHP(x,y,z, (ushort)hp[i]);
                    else
                        this.meta.SetHP(x,y,z, ushort.MaxValue);

                    if(state[i] != null)
                        this.meta.SetState(x,y,z, (ushort)state[i]);
                    else
                        this.meta.SetState(x,y,z, ushort.MaxValue);

                    i++;
                }
            }
        }
    }


    // Applies this structure to a cachedUshort array and a VoxelMetadata
    public virtual bool Apply(ChunkLoader cl, ChunkPos pos, ushort[] VD, ushort[] VMHP, ushort[] VMState, int x, int y, int z, int rotation=0)
    {
        bool retStatus;
        int xChunks = Mathf.FloorToInt((x + this.sizeX - 1)/Chunk.chunkWidth);
        int zChunks = Mathf.FloorToInt((z + this.sizeZ - 1)/Chunk.chunkWidth);

        int xRemainder, zRemainder;

        // Calculates Remainder
        if(xChunks > 0)
            xRemainder = Chunk.chunkWidth - x;
        else
            xRemainder = this.sizeX;

        if(zChunks > 0)
            zRemainder = Chunk.chunkWidth - z;
        else
            zRemainder = this.sizeZ;

        // Applies Structure to origin chunk
        retStatus = ApplyToChunk(pos, true, true, true, cl, VD, VMHP, VMState, x, y, z, xRemainder, zRemainder, 0, 0);


        // Possible failed return if in FreeSpace mode
        if(!retStatus){
            return false;
        }

        // Run loop for multi-chunk structures
        ChunkPos newPos; 
        int posX = 0;
        int posZ = 0;
        int sPosX=0;
        int sPosZ=0;

        for(int zCount=0; zCount <= zChunks; zCount++){
            for(int xCount=0; xCount <= xChunks; xCount++){

                // Skips the origin chunk
                if(zCount == 0 && xCount == 0){
                    continue;
                }

                newPos = new ChunkPos(pos.x+xCount, pos.z+zCount);

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


                // ACTUAL APPLY FUNCTIONS
                // Checks if it's a loaded chunk
                if(cl.chunks.ContainsKey(newPos)){
                    ApplyToChunk(newPos, false, true, true, cl, cl.chunks[newPos].data.GetData(), cl.chunks[newPos].metadata.GetHPData(), cl.chunks[newPos].metadata.GetStateData(), posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ, rotation:rotation);
                    AddChunk(cl.chunks[newPos]);
                    continue;
                }

                // CASE WHERE REGIONFILES NEED TO BE LOOKED UPON
                Chunk c;
                cl.regionHandler.GetCorrectRegion(newPos);

                // Check if it's an existing chunk
                if(cl.regionHandler.GetFile().IsIndexed(newPos)){
                    if(Structure.Exists(newPos)){

                        c = Structure.GetChunk(newPos);
                        ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ, rotation:rotation);
                    }
                    else{

                        c = new Chunk(newPos, cl.rend, cl.blockBook, cl, fromMemory:true);
                        cl.regionHandler.LoadChunk(c);
                        ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ, rotation:rotation);                
                        AddChunk(c); 
                    }
                }
                // Check if it's an ungenerated chunk
                else{

                    if(Structure.Exists(newPos)){

                        c = Structure.GetChunk(newPos);
                        ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ, rotation:rotation);                        
                    }
                    else{

                        c = new Chunk(newPos, cl.rend, cl.blockBook, cl, fromMemory:true);
                        c.biomeName = "Plains";
                        c.needsGeneration = 1;
                        ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata.GetHPData(), c.metadata.GetStateData(), posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ, rotation:rotation);              
                        AddChunk(c);
                    }
                }
            }
        }
        return true;
    }

    // Applies this structure to a chunk
    // Receives a Chunk reference that will be changed in this function
    private bool ApplyToChunk(ChunkPos pos, bool initialchunk, bool exist, bool loaded, ChunkLoader cl, ushort[] VD, ushort[] VMHP, ushort[] VMState, int posX, int posY, int posZ, int remainderX, int remainderZ, int structinitX, int structinitZ, int rotation=0){
        bool exists = exist;

        int structX = structinitX;
        int structZ = structinitZ;
        int structY = 0;

        // Applies Free Space building rules to existing chunk
        if(this.type == FillType.FreeSpace && exists && initialchunk){
            if(!this.considerAir){
                if(CheckFreeSpace(VD, posX, posY, posZ, rotation)){
                    for(int y=posY; y < posY + this.sizeY; y++){
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
                if(CheckFreeSpace(VD, posX, posY, posZ, rotation)){
                    for(int y=posY; y < posY + this.sizeY; y++){
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
            if(!this.considerAir){
                for(int y=posY; y < posY + this.sizeY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            if(this.overwriteBlocks.Contains(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){

                                RotateData(structX, structY, structZ, rotation);

                                if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] == 0){
                                    continue;
                                }

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
                return true;
            }
            else{
                for(int y=posY; y < posY + this.sizeY; y++){
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

                                VMHP[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetHP(cacheX, cacheY, cacheZ);
                                VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.meta.GetState(cacheX, cacheY, cacheZ);
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

        // Applies in OverwriteAll state
        else if(this.type == FillType.OverwriteAll || !exists || exists){
            // Handling if air is taken into account in generated chunks
            if(this.considerAir && exists){
                for(int y=posY; y < posY + this.sizeY; y++){
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

                            // Draws Object
                            if(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] > ushort.MaxValue/2 && loaded && !initialchunk)
                                cl.chunks[pos].assetGrid.AddDraw(x,y,z, VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], cl);

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
                for(int y=posY; y < posY + this.sizeY; y++){
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
                for(int y=posY; y < posY + this.sizeY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
                            if(this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ] != 0){
                                VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = this.blockdata[cacheX*sizeZ*sizeY+cacheY*sizeZ+cacheZ];

                                // Draws Object
                                if(VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] > ushort.MaxValue/2 && loaded && !initialchunk)
                                    cl.chunks[pos].assetGrid.AddDraw(x,y,z, VD[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], VMState[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], cl);

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
                for(int y=posY; y < posY + this.sizeY; y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            RotateData(structX, structY, structZ, rotation);
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

            return true;
        }

        return false;
    } 

    // Checks for valid space in FreeSpace mode
    private bool CheckFreeSpace(ushort[] data, int x, int y, int z, int rotation){
        int xRemainder = Mathf.Min(Chunk.chunkWidth - x, this.sizeX);
        int zRemainder = Mathf.Min(Chunk.chunkWidth - z, this.sizeZ);

        // Case Struct considers it's air as a needed block
        if(this.considerAir){
            for(int yCount = 0; yCount < this.sizeY; yCount++){
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
            for(int yCount = 0; yCount < this.sizeY; yCount++){
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
        ushort block;

        for(int y=0; y < Chunk.chunkDepth; y++){
            for(int x=0; x < Chunk.chunkWidth; x++){
                for(int z=0; z < Chunk.chunkWidth; z++){
                    block = st.data.GetCell(x,y,z);

                    // Ignores all air
                    if(block == 0)
                        continue;

                    c.data.SetCell(x,y,z,block);

                    c.metadata.SetHP(x,y,z, st.metadata.GetHP(x,y,z));
                    c.metadata.SetState(x,y,z, st.metadata.GetState(x,y,z));


                }
            }
        }
    }

    // Changes the index of rotation torotate Structures at Apply-Time
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


}


public enum FillType{
    OverwriteAll, // Will erase any blocks in selected region
    FreeSpace, // Will need free space to generate, if considerAir is off, disconsiders self air colission
    SpecificOverwrite, // Will generate structure blocks only on specific blocks
}