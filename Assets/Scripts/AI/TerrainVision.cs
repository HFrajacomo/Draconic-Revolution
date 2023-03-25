using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class TerrainVision
{
    protected ChunkLoader_Server cl;
    protected CastCoord coord;
    protected CastCoord lastCoord;
    protected int2 viewDistance;
    protected ushort[] viewFieldBlocks;
    protected ushort[] viewFieldStates;
    protected EntityHitbox hitbox;
    protected bool REFRESH_VISION;

    /*
    Function to gather the view area containing blocks and states that the mob will have the knowledge of
    */
    public virtual byte RefreshView(CastCoord coord){
        if(this.viewFieldBlocks == null)
            return 0;

        if(coord.Equals(null)){
            this.REFRESH_VISION = false;
            this.cl.GetField(coord, viewDistance, ref viewFieldBlocks, ref viewFieldStates);
            return 1;
        }

        if(!CastCoord.Eq(this.coord, coord) || this.REFRESH_VISION){
            this.REFRESH_VISION = false;
            this.cl.GetField(coord, viewDistance, ref viewFieldBlocks, ref viewFieldStates);
            this.lastCoord = this.coord;
            this.coord = coord;
            return 2;
        }

        return 0;
    }

    public void Start(ChunkLoader_Server cl){
        this.SetChunkloader(cl);
        this.REFRESH_VISION = true;

        this.viewFieldBlocks = new ushort[(this.viewDistance.x+2)*(this.viewDistance.x+2)*(this.viewDistance.y+2)];
        this.viewFieldStates = new ushort[(this.viewDistance.x+2)*(this.viewDistance.x+2)*(this.viewDistance.y+2)];
    }

    private void SetChunkloader(ChunkLoader_Server cl){
        this.cl = cl;
    }

    public void SetHitbox(EntityHitbox box){
        this.hitbox = box;
    }

    public void SetRefresh(){
        this.REFRESH_VISION = true;
    }

    // Gets the blockCode that is directly below
    public virtual ushort GetBlockBelow(){
        if(coord.Equals(null))
            return 0;

        return this.viewFieldBlocks[this.viewDistance.x*(this.viewDistance.y*2+1)*(this.viewDistance.x*2+1) + ((this.viewDistance.y - ((int)(this.hitbox.GetDiameter().y/2)+1)))*(this.viewDistance.x*2+1) + this.viewDistance.x];
    }

    // Gets the blockCode that is in the middle of the hitbox
    public virtual ushort GetBlockCenter(){
        if(coord.Equals(null))
            return 0;

        return this.viewFieldBlocks[this.viewDistance.x*(this.viewDistance.y*2+1)*(this.viewDistance.x*2+1) + this.viewDistance.y*(this.viewDistance.x*2+1) + this.viewDistance.x];
    }

    public virtual int CollidedAround(){
        // NOT IMPLEMENTED
        return 0;
    }

    // Is in the ground
    public virtual EntityTerrainCollision GroundCollision(){
        if(cl.blockBook.CheckSolid(this.GetBlockBelow()))
            return EntityTerrainCollision.SOLID;
        if(cl.blockBook.CheckLiquid(this.GetBlockBelow()))
            return EntityTerrainCollision.LIQUID;
        return EntityTerrainCollision.NONE;
    }
}
