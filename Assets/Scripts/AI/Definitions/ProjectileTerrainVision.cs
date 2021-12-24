using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ProjectileTerrainVision : TerrainVision
{
    public ProjectileTerrainVision(ChunkLoader_Server cl){
        this.viewDistance = new int2(1, 1);
        this.Start(cl);
    }

    // Also considers Liquids as ground
    public override bool GroundCollision(Vector3 entityPos){
        ushort blockCode = this.GetBlockBelow();

        if(cl.blockBook.CheckSolid(blockCode) || cl.blockBook.CheckLiquid(blockCode)){
            blockCode = this.GetBlockContained();

            if(cl.blockBook.CheckSolid(blockCode) || cl.blockBook.CheckLiquid(blockCode)){
                return true;
            }
        }

        return false;
    }
}
