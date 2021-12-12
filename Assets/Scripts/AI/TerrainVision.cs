using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class TerrainVision
{
    protected ChunkLoader_Server cl;
    protected int2 viewDistance;
    protected ushort[] viewFieldBlocks;
    protected ushort[] viewFieldStates;

    /*
    Function to gather the view area containing blocks and states that the mob will have the knowledge of
    */
    public void RefreshView(CastCoord coord){
        if(this.viewFieldBlocks == null)
            return;

        if(coord.Equals(null))
            this.cl.GetField(coord, viewDistance, ref viewFieldBlocks, ref viewFieldStates);
    }

    public void Start(ChunkLoader_Server cl){
        this.SetChunkloader(cl);

        this.viewFieldBlocks = new ushort[this.viewDistance.x*this.viewDistance.x*this.viewDistance.y];
        this.viewFieldStates = new ushort[this.viewDistance.x*this.viewDistance.x*this.viewDistance.y];
    }

    private void SetChunkloader(ChunkLoader_Server cl){
        this.cl = cl;
    }
}
