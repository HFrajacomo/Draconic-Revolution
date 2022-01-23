using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

public class VoxelData
{
	private static bool CONFIGURED_SHADER;
	private ushort[] data;
	private byte[] heightMap;
	private byte[] lightMap;

	public static readonly int3[] offsets = new int3[]{
		new int3(0,0,1),
		new int3(1,0,0),
		new int3(0,0,-1),
		new int3(-1,0,0),
		new int3(0,1,0),
		new int3(0,-1,0)
	};

	public VoxelData(){
		this.data = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
	}

	public VoxelData(ushort[] data){
		this.data = (ushort[])data.Clone();
		this.heightMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];
		CalculateHeightMap();
	}

	/*
	Currently unused because unbursted option is faster
	*/
	/*
	public void CalculateHeightMap_BURST(){
		if(this.data == null)
			return;

		NativeArray<ushort> nativeData = new NativeArray<ushort>(data, Allocator.TempJob);
		NativeArray<byte> nativeHeightMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth, Allocator.TempJob);
		NativeArray<bool> blockAffectLightECS = new NativeArray<bool>(BlockEncyclopediaECS.blockAffectLight, Allocator.TempJob);
		NativeArray<bool> objectAffectLightECS = new NativeArray<bool>(BlockEncyclopediaECS.objectAffectLight, Allocator.TempJob);

		JobHandle job;

		GetHeightMapJob hmJob = new GetHeightMapJob{
			heightMap = nativeHeightMap,
			data = nativeData,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			blockAffectLight = blockAffectLightECS,
			objectAffectLight = objectAffectLightECS
		};

        job = hmJob.Schedule(Chunk.chunkWidth, 2);
        job.Complete();

        this.data = nativeData.ToArray();
        this.heightMap = nativeHeightMap.ToArray();

        nativeData.Dispose();
        nativeHeightMap.Dispose();
        blockAffectLightECS.Dispose();
        objectAffectLightECS.Dispose();
	}
	*/

	public void CalculateHeightMap(){
		if(this.data == null)
			return;

		ushort blockCode;
		bool found;

		for(int x=0; x < Chunk.chunkWidth; x++){
	    	for(int z=0; z < Chunk.chunkWidth; z++){
	    		found = false;
	    		for(int y=Chunk.chunkDepth-1; y >= 0; y--){
	    			blockCode = this.data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

	    			// If is a block
	    			if(blockCode <= ushort.MaxValue/2){
	    				if(BlockEncyclopediaECS.blockAffectLight[blockCode]){
	    					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
	    					found = true;
	    					break;
	    				}
	    			}
	    			// If it's an object
	    			else{
	    				if(BlockEncyclopediaECS.objectAffectLight[ushort.MaxValue - blockCode]){
	    					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
	    					found = true;
	    					break;
	    				}		
	    			}
	    		}

	    		if(!found){
	    			this.heightMap[x*Chunk.chunkWidth+z] = 0;
	    		}
	    	}
		}
	}

	public ushort GetHeight(byte x, byte z){
		if(x < 0 || z < 0 || x > Chunk.chunkWidth || z > Chunk.chunkWidth)
			return ushort.MaxValue;
		else
			return this.heightMap[x*Chunk.chunkWidth + z];
	}

	public ushort GetCell(int x, int y, int z){
		return data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
	}

	public ushort GetCell(int i){
		return data[i];
	}

	public ushort GetCell(int3 coord){
		return data[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z];
	}

	public void SetCell(int x, int y, int z, ushort blockCode){
		data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;
	}

	public ushort[] GetData(){
		return this.data;
	}

	public void SetData(ushort[] data){
		this.data = data;
	}

	public override string ToString(){
		string str = "";
		foreach(var item in data){
			str += item.ToString();
		}

		return base.ToString() + " -> " + str;
	}

	public ushort GetNeighbor(int x, int y, int z, Direction dir){
		int3 neighborCoord = new int3(x, y, z) + offsets[(int)dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		} 

		return GetCell(neighborCoord.x, neighborCoord.y, neighborCoord.z);
	}
}

[BurstCompile]
public struct GetHeightMapJob : IJobParallelFor{
    [NativeDisableParallelForRestriction]
    public NativeArray<byte> heightMap;
    [ReadOnly]
    public NativeArray<ushort> data;
    [ReadOnly]
    public int chunkWidth;
    [ReadOnly]
    public int chunkDepth;
    [ReadOnly]
    public NativeArray<bool> blockAffectLight;
    [ReadOnly]
    public NativeArray<bool> objectAffectLight;

    public void Execute(int index){
    	ushort blockCode;

    	for(int z=0; z < chunkWidth; z++){
    		for(int y=chunkDepth-1; y >= 0; y--){
    			blockCode = data[index*chunkWidth*chunkDepth+y*chunkWidth+z];

    			// If is a block
    			if(blockCode <= ushort.MaxValue/2){
    				if(blockAffectLight[blockCode]){
    					this.heightMap[index*chunkWidth+z] = (byte)y;
    					break;
    				}
    			}
    			// If it's an object
    			else{
    				if(objectAffectLight[ushort.MaxValue - blockCode]){
    					this.heightMap[index*chunkWidth+z] = (byte)y;
    					break;
    				}		
    			}
    		}
    	}
    }
}

public enum Direction{
	North,
	East,
	South,
	West,
	Up,
	Down
}