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
	/*
	private static ShaderReferences shaderReferences;
	private static ComputeShader shadowMapShader;
	*/

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
	ComputeShader Accelerated Function
	*/
	/*
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
	*/

	/*
	Burst Compiled
	*/
	public void CalculateLightMap(bool withExtraLight=false){
		if(this.heightMap == null)
			CalculateHeightMap();

		NativeArray<byte> lightMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeList<int4> lightSources = new NativeList<int4>(0, Allocator.TempJob);
		//NativeArray<byte> heightMap = NativeTools.CopyToNative(this.heightMap);
		NativeArray<byte> heightMap = new NativeArray<byte>(this.heightMap, Allocator.TempJob);

		JobHandle job;

		//NativeArray<ushort> blockData = NativeTools.CopyToNative(this.data);
		NativeArray<ushort> blockData = new NativeArray<ushort>(this.data, Allocator.TempJob);
		//NativeArray<byte> isTransparentBlock = NativeTools.CopyToNative(BlockEncyclopediaECS.blockTransparent);
		NativeArray<byte> isTransparentBlock = new NativeArray<byte>(BlockEncyclopediaECS.blockTransparent, Allocator.TempJob);
		//NativeArray<byte> isTransparentObj = NativeTools.CopyToNative(BlockEncyclopediaECS.objectTransparent);
		NativeArray<byte> isTransparentObj = new NativeArray<byte>(BlockEncyclopediaECS.objectTransparent, Allocator.TempJob);
		//NativeArray<byte> blockLuminosity = NativeTools.CopyToNative(BlockEncyclopediaECS.blockLuminosity);
		NativeArray<byte> blockLuminosity = new NativeArray<byte>(BlockEncyclopediaECS.blockLuminosity, Allocator.TempJob);
		//NativeArray<byte> objectLuminosity = NativeTools.CopyToNative(BlockEncyclopediaECS.objectLuminosity);
		NativeArray<byte> objectLuminosity = new NativeArray<byte>(BlockEncyclopediaECS.objectLuminosity, Allocator.TempJob);

		CalculateShadowMapJob csmJob = new CalculateShadowMapJob{
			shadowMap = lightMap,
			lightSources = lightSources,
			heightMap = heightMap,
			data = blockData,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = isTransparentObj,
			isTransparentBlock = isTransparentBlock,
			blockLuminosity = blockLuminosity,
			objectLuminosity = objectLuminosity,
			withExtraLight = withExtraLight
		};

        job = csmJob.Schedule();
        job.Complete();


		NativeList<int3> bfsq = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int4> bfsqExtra = new NativeList<int4>(0, Allocator.TempJob);
		NativeHashSet<int3> visited = new NativeHashSet<int3>(0, Allocator.TempJob);


		CalculateLightMapJob clmJob = new CalculateLightMapJob{
			lightMap = lightMap,
			lightSources = lightSources,
			heightMap = heightMap,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			bfsq = bfsq,
			bfsqExtra = bfsqExtra,
			visited = visited,
			withExtraLight = withExtraLight
		};

        job = clmJob.Schedule();
        job.Complete();


        this.lightMap = lightMap.ToArray(); //NativeTools.CopyToManaged(lightMap);


        blockData.Dispose();
        isTransparentBlock.Dispose();
        isTransparentObj.Dispose();
        blockLuminosity.Dispose();
        objectLuminosity.Dispose();

        bfsq.Dispose();
        bfsqExtra.Dispose();
        visited.Dispose();
        lightSources.Dispose();
        lightMap.Dispose();
        heightMap.Dispose();
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

	public ushort GetLight(int3 coord, bool isNatural=true){
		if(isNatural)
			return (ushort)(lightMap[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F);
		else
			return (ushort)(lightMap[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4);
	}

	public ushort GetLight(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return (ushort)(lightMap[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F);
		else
			return (ushort)(lightMap[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4);
	}

	public void SetCell(int x, int y, int z, ushort blockCode){
		data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;
	}

	public byte[] GetLightMap(){
		if(this.lightMap == null){
			this.CalculateLightMap();
		}

		return this.lightMap;
	}

	public ushort[] GetData(){
		return this.data;
	}

	public void SetData(ushort[] data){
		this.data = data;

		/*
		if(VoxelData.shaderReferences == null){
			VoxelData.shaderReferences = GameObject.Find("ShaderReferences").GetComponent<ShaderReferences>();
			VoxelData.shadowMapShader = VoxelData.shaderReferences.GetShadowMapShader();
		}
		*/

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
	public NativeList<int4> lightSources;
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
	[ReadOnly]
	public NativeArray<byte> blockLuminosity;
	[ReadOnly]
	public NativeArray<byte> objectLuminosity;
	[ReadOnly]
	public bool withExtraLight;

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
						if(withExtraLight)
							shadowMap[index] = 18;
						else
							shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 2);

						// Gets lightsource
						if(withExtraLight){
							if(isBlock){
								if(blockLuminosity[blockCode] > 0)
									lightSources.Add(new int4(x, y, z, blockLuminosity[blockCode]));
							}
							else{
								if(objectLuminosity[ushort.MaxValue - blockCode] > 0)
									lightSources.Add(new int4(x, y, z, objectLuminosity[ushort.MaxValue - blockCode]));							
							}
						}
						continue;
					}
					// If is transparent
					if(isBlock){
						if(isTransparentBlock[blockCode] == 1){
							if(withExtraLight)
								shadowMap[index] = 17;
							else
								shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 1);
						}
						else{
							if(!withExtraLight)
								shadowMap[index] = (byte)(shadowMap[index] & 0xF0);
						}
						if(blockLuminosity[blockCode] > 0 && withExtraLight)
							lightSources.Add(new int4(x, y, z, blockLuminosity[blockCode]));
					}
					else{
						if(isTransparentObj[ushort.MaxValue - blockCode] == 1){
							if(withExtraLight)
								shadowMap[index] = 17;
							else
								shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 1);
						}
						else{
							if(!withExtraLight)
								shadowMap[index] = (byte)(shadowMap[index] & 0xF0);
						}
						if(objectLuminosity[ushort.MaxValue - blockCode] > 0  && withExtraLight)
							lightSources.Add(new int4(x, y, z, objectLuminosity[ushort.MaxValue - blockCode]));			
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
	public NativeArray<int4> lightSources;
	public NativeList<int3> bfsq; // Breadth-first search queue
	public NativeList<int4> bfsqExtra;
	public NativeHashSet<int3> visited;

	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public bool withExtraLight;


	public void Execute(){
		int3 current;
		int4 currentExtra;

		/***************************************
		Natural Light
		***************************************/
		DetectSunlight();
		
		// Iterates through queue
		while(bfsq.Length > 0){
			current = bfsq[0];

			if(visited.Contains(current)){
				bfsq.RemoveAt(0);
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap[GetIndex(current)] & 0x0F), true);

			visited.Add(current);
			bfsq.RemoveAt(0);
		}

		CleanUnlit();
		visited.Clear();

		/***************************************
		Extra Lights
		***************************************/
		if(withExtraLight){

			QuickSort();
			
			int currentLevel = 15;
			bool searchedCurrentLevel = false;
			bool initiateExtraLightSearch = false;
			int lastIndex = lightSources.Length - 1;
			int index = 0;

			if(lightSources.Length == 0) 
				return;

			while(bfsqExtra.Length > 0 || !initiateExtraLightSearch || lastIndex >= 0){
				initiateExtraLightSearch = true;

				if(bfsqExtra.Length > 0 && currentLevel == -1){
					searchedCurrentLevel = true;
					currentLevel = bfsqExtra[0].w;
				}
				else if(bfsqExtra.Length == 0 && lastIndex >= 0){
					searchedCurrentLevel = false;
					currentLevel = lightSources[lastIndex].w;
				}
				else if(bfsqExtra.Length == 0 && lastIndex == -1)
					break;

				if(!searchedCurrentLevel){
					for(int i=lastIndex; i >= -1; i--){
						if(i == -1){
							searchedCurrentLevel = true;
							currentLevel = -1;
							lastIndex = -1;
							break;
						}

						if(lightSources[i].w == currentLevel){
							index = GetIndex(lightSources[i].xyz);

							if(lightSources[i].w > lightMap[index] >> 4){
								bfsqExtra.Add(lightSources[i]);
								lightMap[index] = (byte)((lightMap[index] & 0x0F) + (lightSources[i].w << 4));
							}
						}
						else{
							searchedCurrentLevel = true;
							currentLevel = lightSources[i].w;
							lastIndex = i;
							break;
						}
					}
				}

				currentExtra = bfsqExtra[0];
				bfsqExtra.RemoveAt(0);

				if(currentExtra.w == currentLevel && lastIndex >= 0)
					searchedCurrentLevel = false;

				ScanSurroundings(currentExtra.xyz, (byte)currentExtra.w, false);
			} 
		}
	}

	// Checks the surroundings and adds light fallout
	public void ScanSurroundings(int3 c, byte currentLight, bool isNatural){
		if(currentLight == 1)
			return;
		if(isNatural && currentLight == 2)
			return;

		int3 aux;
		int index;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (lightMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (lightMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (lightMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (lightMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}	

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (lightMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (lightMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (lightMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (lightMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (lightMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (lightMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (lightMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (lightMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}
	}

	private void QuickSort(){
		int init = 0;
		int end = lightSources.Length -1;

		if(lightSources.Length == 0)
			return;

		QuickSort(init, end);
	}

	private void QuickSort(int init, int end){
		if(init < end){
			int4 val = lightSources[init];
			int i = init +1;
			int e = end;

			while(i <= e){
				if(lightSources[i].w <= val.w)
					i++;
				else if(val.w < lightSources[e].w)
					e--;
				else{
					int4 swap = lightSources[i];
					lightSources[i] = lightSources[e];
					lightSources[e] = swap;
					i++;
					e--;
				}
			}

			lightSources[init] = lightSources[e];
			lightSources[e] = val;

			QuickSort(init, e - 1);
			QuickSort(e + 1, end);
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

				if((lightMap[index] & maxLightLevel) == 2){
					bfsq.Add(new int3(x, height, z));
					lightMap[index] = (byte)((lightMap[index] & 0xF0) | maxLightLevel);
				}

				// Sets the remaining skylight above to max
				for(int y=height+1; y < chunkDepth; y++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					lightMap[index] = (byte)((lightMap[index] & 0xF0) | maxLightLevel);

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

		if(xm){
			if((lightMap[GetIndex(x-1, y, z)] & 0x0F) == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(xp){
			if((lightMap[GetIndex(x+1, y, z)] & 0x0F) == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zm){
			if((lightMap[GetIndex(x, y, z-1)] & 0x0F) == 1){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zp){
			if((lightMap[GetIndex(x, y, z+1)] & 0x0F) == 1){
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

					if((lightMap[index] & 0x0F) == 1 && !visited.Contains(pos)){
						lightMap[index] = (byte)(lightMap[index] & 0xF0);
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

0: Blocked by solid/Dark
1-14: Light 
15: Completely lit up


Extra Light Level [0-16 << 4]

0: Blocked by Solid
1: Dark
2-14: Light
15: Completely lit up (which is slightly less than Natural Light)




Use the first hexadecimal for natural light and he second for light sources
0xFF
*/