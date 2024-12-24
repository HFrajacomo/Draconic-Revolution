using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StructureGroup
{
    public string name;
    public StructSpawn[] structData;

    public void AddStructureGroup(Biome b){
        foreach(StructSpawn s in structData){
            s.AddToSpawn(b.structNames, b.amountStructs, b.percentageStructs, b.depthValues, b.hardSetDepth, b.hasRange, b.minHeight);
        }
    }
}