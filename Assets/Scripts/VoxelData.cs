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
	private void CalculateShadowMap_BURST(){		
		if(this.heightMap == null)
			CalculateHeightMap();

		NativeArray<byte> shadowMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> heightMap = new NativeArray<byte>(this.heightMap, Allocator.TempJob);
		NativeArray<ushort> data = new NativeArray<ushort>(this.data, Allocator.TempJob);
		NativeArray<byte> isTransparentBlock = new NativeArray<byte>(BlockEncyclopediaECS.blockTransparent, Allocator.TempJob);
		NativeArray<byte> isTransparentObj = new NativeArray<byte>(BlockEncyclopediaECS.objectTransparent, Allocator.TempJob);

		JobHandle job;

		CalculateShadowMapJob csmJob = new CalculateShadowMapJob{
			shadowMap = shadowMap,
			heightMap = heightMap,
			data = data,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = isTransparentObj,
			isTransparentBlock = isTransparentBlock
		};

        job = csmJob.Schedule();
        job.Complete();

        this.lightMap = shadowMap.ToArray();

        shadowMap.Dispose();
        heightMap.Dispose();
        data.Dispose();
        isTransparentBlock.Dispose();
        isTransparentObj.Dispose();
	}

	/*
	ComputeShader Accelerated Function
	*/
	private void CalculateShadowMap_GPU(){
        ComputeBuffer voxelDataBuffer = new ComputeBuffer(data.Length/2, sizeof(int));
        ComputeBuffer heightMapBuffer = new ComputeBuffer(heightMap.Length/4, sizeof(int));
        ComputeBuffer shadowMapBuffer = new ComputeBuffer(data.Length/4, sizeof(int));

        voxelDataBuffer.SetData(data);
        heightMapBuffer.SetData(heightMap);
        shadowMapBuffer.SetData(lightMap);

        VoxelData.shadowMapShader.SetBuffer(0, "data", voxelDataBuffer);
        VoxelData.shadowMapShader.SetBuffer(0, "heightMap", heightMapBuffer);
        VoxelData.shadowMapShader.SetBuffer(0, "shadowMap", shadowMapBuffer);

        VoxelData.shadowMapShader.Dispatch(0, 25, 1, 1);

        shadowMapBuffer.GetData(this.lightMap);

        voxelDataBuffer.Dispose();
        heightMapBuffer.Dispose();
        shadowMapBuffer.Dispose();
	}

	/*
	Burst Compiled
	*/
	public void CalculateLightMap(){
		CalculateShadowMap_BURST();

		NativeArray<byte> lightMap = new NativeArray<byte>(this.lightMap, Allocator.TempJob);
		NativeArray<byte> heightMap = new NativeArray<byte>(this.heightMap, Allocator.TempJob);
		NativeList<int3> bfsq = new NativeList<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visited = new NativeHashSet<int3>(0, Allocator.TempJob);

		JobHandle job;

		CalculateLightMapJob clmJob = new CalculateLightMapJob{
			lightMap = lightMap,
			heightMap = heightMap,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			bfsq = bfsq,
			visited = visited
		};

        job = clmJob.Schedule();
        job.Complete();

        this.lightMap = lightMap.ToArray();

        lightMap.Dispose();
        heightMap.Dispose();
        bfsq.Dispose();
        visited.Dispose();
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

	public ushort GetLight(int3 coord){
		return lightMap[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z];
	}

	public ushort GetLight(int x, int y, int z){
		return lightMap[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
	}

	public void SetCell(int x, int y, int z, ushort blockCode){
		data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;
	}

	public byte[] GetLightMap(){
		return this.lightMap;
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
public struct CalculateShadowMapJob : IJob{
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

/*
Takes the ShadowMap and turns it into a progressive lightmap
*/
[BurstCompile]
public struct CalculateLightMapJob : IJob{
	public NativeArray<byte> lightMap;
	public NativeList<int3> bfsq; // Breadth-first search queue
	public NativeHashSet<int3> visited;

	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;


	public void Execute(){
		int3 current;

		DetectSunlight();
		
		// Iterates through queue
		while(bfsq.Length > 0){
			current = bfsq[0];

			if(visited.Contains(current)){
				bfsq.RemoveAt(0);
				continue;
			}

			ScanSurroudings(current, lightMap[GetIndex(current)]);

			visited.Add(current);
			bfsq.RemoveAt(0);
		}

		CleanUnlit();
	}

	// Checks the surroundings and adds light fallout
	public void ScanSurroudings(int3 c, byte currentLight){
		if(currentLight == 0)
			return;

		int3 aux;
		int index;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(lightMap[index] < currentLight && lightMap[index] != 0){
					lightMap[index] = (byte)(currentLight-1);
					bfsq.Add(aux);
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(lightMap[index] < currentLight && lightMap[index] != 0){
					lightMap[index] = (byte)(currentLight-1);
					bfsq.Add(aux);
				}
			}
		}	

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(lightMap[index] < currentLight && lightMap[index] != 0){
					lightMap[index] = (byte)(currentLight-1);
					bfsq.Add(aux);
				}
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(lightMap[index] < currentLight && lightMap[index] != 0){
					lightMap[index] = (byte)(currentLight-1);
					bfsq.Add(aux);
				}
			}
		}

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(lightMap[index] < currentLight && lightMap[index] != 0){
					lightMap[index] = (byte)(currentLight-1);
					bfsq.Add(aux);
				}
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(lightMap[index] < currentLight && lightMap[index] != 0){
					lightMap[index] = (byte)(currentLight-1);
					bfsq.Add(aux);
				}
			}
		}
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}
	public int GetIndex(int x, int y, int z){
		return x*chunkWidth*chunkDepth+y*chunkWidth+z;
	}

	// Iterates through heightMap and populates the BFS queue
	public void DetectSunlight(){
		int index;
		byte height;
		byte maxLightLevel = 15;

		for(int z=0; z < chunkWidth; z++){
			for(int x=0; x < chunkWidth; x++){

				if(heightMap[x*chunkWidth+z] >= chunkDepth-1){
					continue;
				}

				height = (byte)(heightMap[x*chunkWidth+z]+1);

				index = x*chunkWidth*chunkDepth+height*chunkWidth+z;

				if(lightMap[index] == 2){
					bfsq.Add(new int3(x, height, z));
					lightMap[index] = maxLightLevel;
				}

				// Sets the remaining skylight above to max
				for(int y=height+1; y < chunkDepth; y++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					lightMap[index] = maxLightLevel;

					AnalyzeSunShaft(x, y, z);
				}
			}
		}
	}

	// Finds if a natural light affected block should propagate
	public bool AnalyzeSunShaft(int x, int y, int z){
		bool xp = false;
		bool xm = false;
		bool zp = false;
		bool zm = false;

		// Checks borders
		if(x > 0){
			xm = true;
		}
		if(x < chunkWidth-2){
			xp = true;
		}
		if(z > 0){
			zm = true;
		}
		if(z < chunkWidth-2){
			zp = true;
		}

		if(xm){// && heightMap[(x-1)*chunkWidth+z] >= y){
			if(lightMap[GetIndex(x-1, y, z)] == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(xp){// && heightMap[(x+1)*chunkWidth+z] >= y){
			if(lightMap[GetIndex(x+1, y, z)] == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zm){// && heightMap[x*chunkWidth+(z-1)] >= y){
			if(lightMap[GetIndex(x, y, z-1)] == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zp){// && heightMap[x*chunkWidth+(z+1)] >= y){
			if(lightMap[GetIndex(x, y, z+1)] == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}

		return false;
	}

	// Removes all 1's that were not calculated as light
	public void CleanUnlit(){
		int index;
		int3 pos;

		for(int z=0; z < chunkWidth; z++){
			for(int x=0; x < chunkWidth; x++){
				for(int y=heightMap[x*chunkWidth+z]; y > 0; y--){
					pos = new int3(x, y, z);
					index = GetIndex(pos);

					if(lightMap[index] == 1 && !visited.Contains(pos)){
						lightMap[index] = 0;
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


/*
LIGHT DOCUMENTATION:

Natural Light Level [0-16]

0: Blocked by solid
1: Completely dark
2-14: Light 
15: Completely lit up

Use the first hexadecimal for natural light and he second for light sources
0xFF
*/