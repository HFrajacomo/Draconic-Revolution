using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure
{
    ushort[,,] data;
    Metadata[,,] metadata;
    public int stopX, stopY, stopZ;

    /*
    0: Partial Fit (Fits what it cans)
    1: Best Fit (Only fits if whole space available)
    2: Perfect Fit (Overwrite fit)
    */
    public int fitType;

    public Structure(ushort[,,] data, ushort?[,,] metadata)
    {
        this.data = (ushort[,,])data.Clone();
        this.metadata = (Metadata[,,])metadata.Clone();
    }

    public void SetCell(int x, int y, int z, ushort blockCode)
    {
        this.data[x, y, z] = blockCode;
    }

    public ushort GetCell(int x, int y, int z)
    {
        return this.data[x, y, z];
    }

    public Metadata GetMetaCell(int x, int y, int z)
    {
        return this.metadata[x, y, z];
    }

    public ushort[,,] GetData()
    {
        return this.data;
    }

    public int GetWidth()
    {
        return this.data.GetLength(0);
    }

    public int GetHeight()
    {
        return this.data.GetLength(1);
    }

    public int GetDepth()
    {
        return this.data.GetLength(2);
    }

    // Applies this structure to a VoxelData
    /* Application Types
    0: Partial Fit (Only adds structure to air blocks, even if it gets cut)
    1: Best Fit (Only adds structure if whole space is available)
    2: Perfect Fit (Overwrites all blocks in the way if any)
    */
    public bool Apply(VoxelData VD, VoxelMetadata VM, int x, int y, int z)
    {
        // maximum length of area to be filled
        int lengthFillX = Chunk.chunkWidth - x;
        int lengthFillY = Chunk.chunkDepth - y;
        int lengthFillZ = Chunk.chunkWidth - z;

        // calculates where to stop on structure data
        this.stopX = GetWidth() > lengthFillX ? lengthFillX : GetWidth();
        this.stopY = GetHeight() > lengthFillY ? lengthFillY : GetHeight();
        this.stopZ = GetDepth() > lengthFillZ ? lengthFillZ : GetDepth();

        for (int i = 0; i < stopX; i++)
        {
            for (int j = 0; j < stopY; j++)
            {
                for (int k = 0; k < stopZ; k++)
                {
                    if (VD.GetCell(i + x, j + y, k + z) == 0)
                    {
                        ushort blockCode = this.GetCell(i, j, k);
                        VD.SetCell(x + i, y + j, z + k, blockCode);

                        Metadata blockMeta = this.GetMetaCell(i, j, k);
                        VM.metadata[x + i, y + j, z + k] = blockMeta;
                    }
                }
            }
        }

        return stopX == GetWidth() && stopY == GetHeight() && stopZ == GetDepth();
    }
}
