using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ProjectileTerrainVision : TerrainVision
{
    private CastCoord cacheBelow;

    public ProjectileTerrainVision(ChunkLoader_Server cl){
        this.Start(cl);
    }

    public override byte RefreshView(CastCoord coord){
        if(coord.Equals(null)){
            this.REFRESH_VISION = false;
            return 0;
        }

        if(!CastCoord.Eq(this.coord, coord) || this.REFRESH_VISION){
            this.REFRESH_VISION = false;
            this.lastCoord = this.coord;
            this.coord = coord;
            return 1;
        }

        return 0;
    }

    public override ushort GetBlockBelow(){
        if(coord.Equals(null))
            return 0;

        this.cacheBelow = new CastCoord(coord.GetWorldX(), coord.GetWorldY()-1, coord.GetWorldZ());
        return cl.GetBlock(cacheBelow);
    }

    private ushort GetBlockContained(){
        if(coord.Equals(null))
            return 0;

        return cl.GetBlock(this.coord);
    }

    private ushort GetBlockAbove(){
        if(coord.Equals(null))
            return 0;

        this.cacheBelow = new CastCoord(coord.GetWorldX(), coord.GetWorldY()+1, coord.GetWorldZ());
        return cl.GetBlock(cacheBelow);
    }


    // Also considers Liquids as ground
    public override EntityTerrainCollision GroundCollision(){
        ushort blockCode = this.GetBlockBelow();

        if(VoxelLoader.CheckSolid(blockCode))
            return EntityTerrainCollision.SOLID;

        return EntityTerrainCollision.NONE;
    }

    /*
    Return a flag
    1: XP
    2: XM
    4: ZP
    8: ZM
    16: YP
    32: Liquid Top
    64: Liquid Elevator
    */
    public override int CollidedAround(){
        ushort blockCode = this.GetBlockContained();

        if(VoxelLoader.CheckSolid(blockCode)){
            return CastCoord.TestEntityCollision(this.coord, this.lastCoord);
        }
        else if(VoxelLoader.CheckLiquid(blockCode)){
            blockCode = this.GetBlockAbove();

            if(VoxelLoader.CheckLiquid(blockCode)){
                return 64;
            }
            return 32;
        }

        return 0;
    }
}
