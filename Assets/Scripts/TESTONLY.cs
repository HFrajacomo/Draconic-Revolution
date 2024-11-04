using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;


public class TESTONLY : MonoBehaviour
{
	private int verticalChunkLoaded = 0;

	void Start(){
		Test(new Vector3(0f, 1023.43f, 0f));
		Test(new Vector3(0f, 1024.43f, 0f));
		Test(new Vector3(0f, 880.43f, 0f));
	}


    private void Test(Vector3 pos){
        CastCoord coord = new CastCoord(pos);

        if(coord.blockY <= Constants.CHUNK_LOADING_VERTICAL_CHUNK_DISTANCE)
            this.verticalChunkLoaded = -1;
        else if(coord.blockY >= Chunk.chunkDepth - Constants.CHUNK_LOADING_VERTICAL_CHUNK_DISTANCE)
            this.verticalChunkLoaded = 1;
        else
            this.verticalChunkLoaded = 0;

        // Fix for being near the upper/lower edge of map
        if(coord.chunkY == Chunk.chunkMaxY && this.verticalChunkLoaded == 1)
            this.verticalChunkLoaded = 0;
        if(coord.chunkY == 0 && this.verticalChunkLoaded == -1)
            this.verticalChunkLoaded = 0;

        // Fix for going above/below map limit
        if(pos.y >= (Chunk.chunkMaxY+1)*Chunk.chunkDepth || pos.y < 0)
            this.verticalChunkLoaded = 0;

        Debug.Log($"{pos} -> {this.verticalChunkLoaded}");
    }
}