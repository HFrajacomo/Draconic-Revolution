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
}
