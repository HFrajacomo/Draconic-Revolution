using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StructSpawn
{
    public string structName;
    private ushort structCode;
    public int quantity;
    public float chance;
    public int depth;
    public int hardSetDepth;
    public bool hasRange;
    public int minHeight;

    public void RunPostDeserializationRoutine(){
        this.structCode = StructureLoader.GetStructureID(this.structName);
    }

    public void AddToSpawn(List<string> nameList, List<int> quantityList, List<float> percentageList, List<int> depthList, List<int> hsDepthList, List<bool> rangeList, List<int> minHeightList){
        nameList.Add(this.structName);
        quantityList.Add(this.quantity);
        percentageList.Add(this.chance);
        depthList.Add(this.depth);
        hsDepthList.Add(this.hardSetDepth);
        rangeList.Add(this.hasRange);
        minHeightList.Add(this.minHeight);
    }
}
