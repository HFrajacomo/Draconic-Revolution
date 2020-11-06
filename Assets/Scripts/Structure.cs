using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Structure
{
    public int code;

    public ushort[,,] blockdata;
    public VoxelMetadata meta;
    public ushort?[,,] hpdata;
    public ushort?[,,] statedata;

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
                    this.blockdata[x,y,z] = data[i];

                    if(hp[i] == null & state[i] == null){
                        i++;
                        continue;
                    }

                    if(hp[i] != null)
                        this.meta.GetMetadata(x,y,z).hp = hp[i];
                    if(state[i] != null)
                        this.meta.GetMetadata(x,y,z).state = state[i];

                    i++;
                }
            }
        }
    }


    // Applies this structure to a cachedUshort array and a VoxelMetadata
    public virtual bool Apply(ChunkLoader cl, ChunkPos pos, ushort[,,] VD, VoxelMetadata VM, int x, int y, int z)
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
        retStatus = ApplyToChunk(pos, true, true, true, cl, VD, VM, x, y, z, xRemainder, zRemainder, 0, 0);

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
                    xRemainder = (this.sizeX - (Chunk.chunkWidth - x))%Chunk.chunkWidth;
                }
                else{
                    xRemainder = (Chunk.chunkWidth - posX);
                }

                if(zChunks == 0){
                    zRemainder = this.sizeZ;
                }
                else if(zCount == zChunks){
                    zRemainder = (this.sizeZ - (Chunk.chunkWidth - z))%Chunk.chunkWidth;
                }
                else{
                    zRemainder = (Chunk.chunkWidth - posZ);
                }


                // Struct Position
                if(xCount == 0)
                    sPosX = 0;
                else if(xCount < xChunks)
                    sPosX = (Chunk.chunkWidth - x) + (xCount * Chunk.chunkWidth);
                else if(xCount == xChunks)
                    sPosX = this.sizeX - xRemainder;

                if(zCount == 0)
                    sPosZ = 0;
                else if(zCount < zChunks)
                    sPosZ = (Chunk.chunkWidth - z) + (zCount * Chunk.chunkWidth);
                else if(zCount == zChunks)
                    sPosZ = this.sizeZ - zRemainder;


                // ACTUAL APPLY FUNCTIONS
                // Checks if it's a loaded chunk
                if(cl.chunks.ContainsKey(newPos)){
                    ApplyToChunk(newPos, false, true, true, cl, cl.chunks[newPos].data.GetData(), cl.chunks[newPos].metadata, posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ);
                    cl.budscheduler.ScheduleReload(newPos, 1);
                    continue;
                }

                // CASE WHERE REGIONFILES NEED TO BE LOOKED UPON
                Chunk c;
                cl.regionHandler.GetCorrectRegion(newPos);

                // Check if it's an existing chunk
                if(cl.regionHandler.GetFile().IsIndexed(newPos)){
                    c = new Chunk(newPos, cl.rend, cl.blockBook, cl, fromMemory:true);
                    cl.regionHandler.LoadChunk(c);
                    ApplyToChunk(newPos, false, true, false, cl, c.data.GetData(), c.metadata, posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ);                
                    cl.regionHandler.SaveChunk(c);
                }
                // Check if it's an ungenerated chunk
                else{
                    c = new Chunk(newPos);
                    ApplyToChunk(newPos, false, false, false, cl, c.data.GetData(), c.metadata, posX, y, posZ, xRemainder, zRemainder, sPosX, sPosZ);              
                    cl.regionHandler.SaveChunk(c);
                }
            }
        }
        return true;
    }

    // Applies this structure to a chunk
    // Receives a Chunk reference that will be changed in this function
    private bool ApplyToChunk(ChunkPos pos, bool initialchunk, bool exist, bool loaded, ChunkLoader cl, ushort[,,] VD, VoxelMetadata VM, int posX, int posY, int posZ, int remainderX, int remainderZ, int structinitX, int structinitZ){
        bool exists = exist;

        int structX = structinitX;
        int structZ = structinitZ;
        int structY = 0;

        // Applies Free Space building rules to existing chunk
        if(this.type == FillType.FreeSpace && exists && initialchunk){
            if(!this.considerAir){
                if(CheckFreeSpace(VD, posX, posY, posZ)){
                    for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                VD[x,y,z] = this.blockdata[structX, structY, structZ];

                                if(VM.metadata[x,y,z] != null)
                                    VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
                if(CheckFreeSpace(VD, posX, posY, posZ)){
                    for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                        structX = structinitX;
                        for(int x=posX; x < posX + remainderX; x++){
                            structZ = structinitZ;
                            for(int z=posZ; z < posZ + remainderZ; z++){
                                if(this.blockdata[structX, structY, structZ] == 0)
                                    VD[x,y,z] = (ushort)(ushort.MaxValue/2);
                                else
                                    VD[x,y,z] = this.blockdata[structX, structY, structZ];

                                if(VM.metadata[x,y,z] != null)
                                    VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
                for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            if(this.overwriteBlocks.Contains(VD[x,y,z])){
                                VD[x,y,z] = this.blockdata[structX, structY, structZ];

                                if(VM.metadata[x,y,z] != null)
                                    VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
                for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            if(this.overwriteBlocks.Contains(VD[x,y,z])){
                                if(this.blockdata[structX, structY, structZ] == 0)
                                    VD[x,y,z] = (ushort)(ushort.MaxValue/2);
                                else
                                    VD[x,y,z] = this.blockdata[structX, structY, structZ];

                                if(VM.metadata[x,y,z] != null)
                                    VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
                for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            // If air add pregen block
                            if(this.blockdata[structX, structY, structZ] == 0)
                                VD[x,y,z] = (ushort)(ushort.MaxValue/2);
                            else
                                VD[x,y,z] = this.blockdata[structX, structY, structZ];

                            // Draws Object
                            if(VD[x,y,z] > ushort.MaxValue/2 && loaded && !initialchunk)
                                if(VM.metadata[x,y,z] != null)
                                    cl.chunks[pos].assetGrid.AddDraw(x,y,z, VD[x,y,z], VM.metadata[x,y,z].state, cl);
                                else
                                    cl.chunks[pos].assetGrid.AddDraw(x,y,z, VD[x,y,z], null, cl);

                            if(VM.metadata[x,y,z] != null)
                                VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
                            structZ++;
                        }
                        structX++;
                    }
                    structY++;
                }
            }
            // Handling if air is taken into account in blank chunks
            else if(this.considerAir && !exists){
                for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            if(this.blockdata[structX, structY, structZ] == 0){
                                VD[x,y,z] = (ushort)(ushort.MaxValue/2);
                            }
                            else{
                                VD[x,y,z] = this.blockdata[structX, structY, structZ];

                                if(VM.metadata[x,y,z] != null)
                                    VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
                for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            if(this.blockdata[structX, structY, structZ] != 0){
                                VD[x,y,z] = this.blockdata[structX, structY, structZ];

                                // Draws Object
                                if(VD[x,y,z] > ushort.MaxValue/2 && loaded && !initialchunk)
                                    if(VM.metadata[x,y,z] != null)
                                        cl.chunks[pos].assetGrid.AddDraw(x,y,z, VD[x,y,z], VM.metadata[x,y,z].state, cl);
                                    else
                                        cl.chunks[pos].assetGrid.AddDraw(x,y,z, VD[x,y,z], null, cl);

                                if(VM.metadata[x,y,z] != null)
                                    VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
                for(int y=posY; y < posY + this.blockdata.GetLength(1); y++){
                    structX = structinitX;
                    for(int x=posX; x < posX + remainderX; x++){
                        structZ = structinitZ;
                        for(int z=posZ; z < posZ + remainderZ; z++){
                            VD[x,y,z] = this.blockdata[structX, structY, structZ];

                            if(VM.metadata[x,y,z] != null)
                                VM.metadata[x, y, z] = this.meta.metadata[structX, structY, structZ];
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
    private bool CheckFreeSpace(ushort[,,] data, int x, int y, int z){
        int xRemainder = Mathf.Min(Chunk.chunkWidth - x, this.sizeX);
        int zRemainder = Mathf.Min(Chunk.chunkWidth - z, this.sizeZ);

        // Case Struct considers it's air as a needed block
        if(this.considerAir){
            for(int yCount = 0; yCount < this.blockdata.GetLength(1); yCount++){
                for(int xCount = 0; xCount < xRemainder; xCount++){
                    for(int zCount = 0; zCount < zRemainder; zCount++){
                        if(data[x + xCount, y + yCount, z + zCount] != 0)
                            return false;
                    }
                }
            }
            return true;
        }
        // Case Struct doesn't consider air as a needed block
        else{
            for(int yCount = 0; yCount < this.blockdata.GetLength(1); yCount++){
                for(int xCount = 0; xCount < xRemainder; xCount++){
                    for(int zCount = 0; zCount < zRemainder; zCount++){
                        if(data[x + xCount, y + yCount, z + zCount] != 0 && this.blockdata[xCount, yCount, zCount] != 0)
                            return false;
                    }
                }
            }
            return true;          
        }
    }

}


public enum FillType{
    OverwriteAll, // Will erase any blocks in selected region
    FreeSpace, // Will need free space to generate, if considerAir is off, disconsiders self air colission
    SpecificOverwrite, // Will generate structure blocks only on specific blocks
}