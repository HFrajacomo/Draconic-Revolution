using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StructSpawn
{
    public StructureCode code;
    public int quantity;
    public float chance;
    public int depth;
    public int hardSetDepth;
    public bool hasRange;
    public int minHeight;

    public StructSpawn(StructureCode c, int q, float p, int d, int hsD, bool r, int mh){
        this.code = c;
        this.quantity = q;
        this.chance = p;
        this.depth = d;
        this.hardSetDepth = hsD;
        this.hasRange = r;
        this.minHeight = mh;
    }

    public void AddToSpawn(List<int> codeList, List<int> quantityList, List<float> percentageList, List<int> depthList, List<int> hsDepthList, List<bool> rangeList, List<int> minHeightList){
        codeList.Add((int)this.code);
        quantityList.Add(this.quantity);
        percentageList.Add(this.chance);
        depthList.Add(this.depth);
        hsDepthList.Add(this.hardSetDepth);
        rangeList.Add(this.hasRange);
        minHeightList.Add(this.minHeight);
    }
}
