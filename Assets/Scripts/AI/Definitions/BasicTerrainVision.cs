using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BasicTerrainVision : TerrainVision
{
    public BasicTerrainVision(ChunkLoader_Server cl){
        this.viewDistance = new int2(8, 8);
        this.Start(cl);
    }
}
