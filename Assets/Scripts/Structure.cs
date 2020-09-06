using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure
{
    int[,,] data;
    public int stopX, stopY, stopZ;

    public Structure(int[,,] data)
    {
        this.data = (int[,,])data.Clone();
    }

    public void SetCell(int x, int y, int z, int blockCode)
    {
        this.data[x, y, z] = blockCode;
    }

    public int GetCell(int x, int y, int z)
    {
        return this.data[x, y, z];
    }

    public int[,,] GetData()
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

    public bool Apply(VoxelData VD, int x, int y, int z)
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
                    if (VD.GetCell(x + i, y + j, z + k) == 0)
                    {
                        int blockCode = this.GetCell(i, j, k);
                        VD.SetCell(x + i, y + j, z + k, blockCode);
                    }
                }
            }
        }

        return stopX == GetWidth() && stopY == GetHeight() && stopZ == GetDepth();
    }
}
