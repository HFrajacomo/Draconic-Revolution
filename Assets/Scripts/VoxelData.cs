using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

public class VoxelData
{
	private static ShaderReferences shaderReferences;
	private static ComputeShader shadowMapShader;

	private ushort[] data;
	private byte[] heightMap;
	private byte[] shadowMap;

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
	public void CalculateHeightMap_BURST(){
		if(this.data == null)
			return;

		NativeArray<ushort> nativeData = new NativeArray<ushort>(this.data, Allocator.TempJob);
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

	/*
	Burst compiler function
	*/
	public void CalculateShadowMap_BURST(){		
		if(this.heightMap == null)
			CalculateHeightMap();

		NativeArray<byte> shadowMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> heightMap = new NativeArray<byte>(this.heightMap, Allocator.TempJob);
		NativeArray<ushort> data = new NativeArray<ushort>(this.data, Allocator.TempJob);
		NativeArray<byte> isTransparentBlock = new NativeArray<byte>(BlockEncyclopediaECS.blockTransparent, Allocator.TempJob);
		NativeArray<byte> isTransparentObj = new NativeArray<byte>(BlockEncyclopediaECS.objectTransparent, Allocator.TempJob);

		JobHandle job;

		CalculateLightJob clJob = new CalculateLightJob{
			shadowMap = shadowMap,
			heightMap = heightMap,
			data = data,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = isTransparentObj,
			isTransparentBlock = isTransparentBlock
		};

        job = clJob.Schedule();
        job.Complete();

        this.shadowMap = shadowMap.ToArray();

        shadowMap.Dispose();
        heightMap.Dispose();
        data.Dispose();
        isTransparentBlock.Dispose();
        isTransparentObj.Dispose();
	}

	/*
	ComputeShader Accelerated Function
	*/
	public void CalculateShadowMap_GPU(){
        ComputeBuffer voxelDataBuffer = new ComputeBuffer(data.Length/2, sizeof(int));
        ComputeBuffer heightMapBuffer = new ComputeBuffer(heightMap.Length/4, sizeof(int));
        ComputeBuffer shadowMapBuffer = new ComputeBuffer(data.Length/4, sizeof(int));

        voxelDataBuffer.SetData(data);
        heightMapBuffer.SetData(heightMap);
        shadowMapBuffer.SetData(shadowMap);

        VoxelData.shadowMapShader.SetBuffer(0, "data", voxelDataBuffer);
        VoxelData.shadowMapShader.SetBuffer(0, "heightMap", heightMapBuffer);
        VoxelData.shadowMapShader.SetBuffer(0, "shadowMap", shadowMapBuffer);

        VoxelData.shadowMapShader.Dispatch(0, 25, 1, 1);

        shadowMapBuffer.GetData(this.shadowMap);

        voxelDataBuffer.Dispose();
        heightMapBuffer.Dispose();
        shadowMapBuffer.Dispose();
	}

	public void CalculateHeightMap(){
		if(this.data == null)
			return;
		if(this.heightMap == null)
			this.heightMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];

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

	public void CalculateHeightMap(int x, int z){
		ushort blockCode;

		for(int y=Chunk.chunkDepth-1; y >= 0; y--){
			blockCode = this.data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

			// If is a block
			if(blockCode <= ushort.MaxValue/2){
				if(BlockEncyclopediaECS.blockAffectLight[blockCode]){
					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
					return;
				}
			}
			// If it's an object
			else{
				if(BlockEncyclopediaECS.objectAffectLight[ushort.MaxValue - blockCode]){
					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
					return;
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

	public ushort GetShadow(int3 coord){
		return shadowMap[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z];
	}

	public void SetCell(int x, int y, int z, ushort blockCode){
		data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;
	}

	public byte[] GetShadowMap(){
		return this.shadowMap;
	}

	public ushort[] GetData(){
		return this.data;
	}

	public void SetData(ushort[] data){
		this.data = data;

		if(VoxelData.shaderReferences == null){
			VoxelData.shaderReferences = GameObject.Find("ShaderReferences").GetComponent<ShaderReferences>();
			VoxelData.shadowMapShader = VoxelData.shaderReferences.GetShadowMapShader();
		}

		if(this.heightMap == null)
			CalculateHeightMap();
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

	// DEBUG
	public void PrintShadow(){
		StringBuilder sb = new StringBuilder();
		foreach(byte b in this.shadowMap){
			sb.Append(b.ToString());
			sb.Append(" ");
		}

		Debug.Log(sb.ToString());
	}

	public void PrintHeight(){
		StringBuilder sb = new StringBuilder();
		foreach(byte b in this.heightMap){
			sb.Append(b.ToString());
			sb.Append(" ");
		}

		Debug.Log(sb.ToString());
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

[BurstCompile]
public struct CalculateLightJob : IJob{
	public NativeArray<byte> shadowMap;
	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public NativeArray<byte> isTransparentBlock;
	[ReadOnly]
	public NativeArray<byte> isTransparentObj;

	public void Execute(){
		bool isBlock;
		ushort blockCode;
		int index;

		for(int z=0; z < chunkWidth; z++){
			for(int y=chunkDepth-1; y >= 0; y--){
				for(int x=0; x < chunkWidth; x++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					blockCode = data[index];
					isBlock = blockCode <= ushort.MaxValue/2;

					// If is above heightMap
					if(y > heightMap[x*chunkWidth+z]){
						shadowMap[index] = 2;
						continue;
					}
					// If is transparent
					if(isBlock){
						if(isTransparentBlock[blockCode] == 1){
							shadowMap[index] = 1;
						}
						else{
							shadowMap[index] = 0;
						}
					}
					else{
						if(isTransparentObj[ushort.MaxValue - blockCode] == 1){
							shadowMap[index] = 1;
						}
						else{
							shadowMap[index] = 0;
						}						
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