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
	private byte[] shadowMap;
	private byte[] lightMap;

	private byte PROPAGATE_LIGHT_FLAG = 0; // 0 = no; 1 = xm; 2 = xp, 4 = zm, 8 = zp

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

	Return byte = {
	0: Shouldn't update any chunk
	1: Update the current chunk
	2: Update the neighbor chunk
	3: Update both chunks
	4: Update Lights in neighbor chunk
	}
	*/
	public static byte PropagateLight(VoxelData a, VoxelData b, byte borderCode){
		NativeArray<byte> lightMap1 = NativeTools.CopyToNative(a.GetLightMap());
		NativeArray<byte> lightMap2 = NativeTools.CopyToNative(b.GetLightMap());
		NativeArray<byte> shadowMap1 = NativeTools.CopyToNative(a.GetShadowMap());
		NativeArray<byte> shadowMap2 = NativeTools.CopyToNative(b.GetShadowMap());

		NativeList<int3> bfsq1 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsq2 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsqr1 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsqr2 = new NativeList<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visited1 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visited2 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visitedr1 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visitedr2 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeArray<byte> changed = new NativeArray<byte>(new byte[]{0, 0, 0, 0}, Allocator.TempJob);
		byte updateFlag = 0;

		JobHandle job;

		CalculateLightPropagationJob clpJob = new CalculateLightPropagationJob{
			lightMap1 = lightMap1,
			lightMap2 = lightMap2,
			shadowMap1 = shadowMap1,
			shadowMap2 = shadowMap2,
			bfsq1 = bfsq1,
			bfsq2 = bfsq2,
			bfsqr1 = bfsqr1,
			bfsqr2 = bfsqr2,
			visited1 = visited1,
			visited2 = visited2,
			visitedr1 = visitedr1,
			visitedr2 = visitedr2,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			borderCode = borderCode,
			changed = changed
		};

        job = clpJob.Schedule();
        job.Complete();

        a.SetLightMap(NativeTools.CopyToManaged(lightMap1));
        b.SetLightMap(NativeTools.CopyToManaged(lightMap2));
        a.SetShadowMap(NativeTools.CopyToManaged(shadowMap1));
        b.SetShadowMap(NativeTools.CopyToManaged(shadowMap2));
        
        updateFlag += changed[0];
        updateFlag += (byte)(changed[1] << 1);
        updateFlag += (byte)(changed[2] << 2);
        updateFlag += (byte)(changed[3] << 3);

        lightMap1.Dispose();
        lightMap2.Dispose();
        shadowMap1.Dispose();
        shadowMap2.Dispose();
        bfsq1.Dispose();
        bfsq2.Dispose();
        bfsqr1.Dispose();
        bfsqr2.Dispose();
        visited1.Dispose();
        visited2.Dispose();
        visitedr1.Dispose();
        visitedr2.Dispose();
        changed.Dispose();

        return updateFlag;
    }

	/*
	Burst Compiled
	*/
	public void CalculateLightMap(bool withExtraLight=false){
		if(this.heightMap == null)
			CalculateHeightMap();

		NativeArray<byte> lightMap;
		NativeArray<byte> shadowMap;
		NativeList<int4> lightSources = new NativeList<int4>(0, Allocator.TempJob);
		NativeArray<byte> heightMap = NativeTools.CopyToNative(this.heightMap);
		NativeArray<byte> changed = new NativeArray<byte>(new byte[]{0}, Allocator.TempJob);

		if(this.shadowMap == null)
			shadowMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		else
			shadowMap = NativeTools.CopyToNative(this.shadowMap);
		if(this.lightMap == null)
			lightMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		else
			lightMap = NativeTools.CopyToNative(this.lightMap);

		JobHandle job;

		NativeArray<ushort> blockData = NativeTools.CopyToNative(this.data);
		NativeArray<byte> isTransparentBlock = NativeTools.CopyToNative(BlockEncyclopediaECS.blockTransparent);
		NativeArray<byte> isTransparentObj = NativeTools.CopyToNative(BlockEncyclopediaECS.objectTransparent);
		NativeArray<byte> blockLuminosity = NativeTools.CopyToNative(BlockEncyclopediaECS.blockLuminosity);
		NativeArray<byte> objectLuminosity = NativeTools.CopyToNative(BlockEncyclopediaECS.objectLuminosity);

		// SHADOWMAPPING =========================================================

		CalculateShadowMapJob csmJob = new CalculateShadowMapJob{
			shadowMap = shadowMap,
			lightMap = lightMap,
			lightSources = lightSources,
			heightMap = heightMap,
			data = blockData,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = isTransparentObj,
			isTransparentBlock = isTransparentBlock,
			blockLuminosity = blockLuminosity,
			objectLuminosity = objectLuminosity,
			withExtraLight = withExtraLight,
			changed = changed
		};

        job = csmJob.Schedule();
        job.Complete();

        this.PROPAGATE_LIGHT_FLAG = changed[0];

		NativeList<int3> bfsq = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int4> bfsqExtra = new NativeList<int4>(0, Allocator.TempJob);
		NativeHashSet<int3> visited = new NativeHashSet<int3>(0, Allocator.TempJob);

		// LIGHTMAPPING =========================================================

		CalculateLightMapJob clmJob = new CalculateLightMapJob{
			lightMap = lightMap,
			shadowMap = shadowMap,
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

        this.lightMap = NativeTools.CopyToManaged(lightMap);
        this.shadowMap = NativeTools.CopyToManaged(shadowMap);


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
        shadowMap.Dispose();
        changed.Dispose();
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

	public bool CalculateHeightMap(int x, int z){
		ushort blockCode;

		for(int y=Chunk.chunkDepth-1; y >= 0; y--){
			blockCode = this.data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

			// If is a block
			if(blockCode <= ushort.MaxValue/2){
				if(BlockEncyclopediaECS.blockAffectLight[blockCode]){
					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
					return true;
				}
			}
			// If it's an object
			else{
				if(BlockEncyclopediaECS.objectAffectLight[ushort.MaxValue - blockCode]){
					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
					return true;
				}		
			}
		}

		return false;
	}

	public ushort GetHeight(byte x, byte z){
		if(x < 0 || z < 0 || x > Chunk.chunkWidth || z > Chunk.chunkWidth)
			return ushort.MaxValue;
		else
			return this.heightMap[x*Chunk.chunkWidth + z];
	}

	public byte GetPropagationFlag(){
		return this.PROPAGATE_LIGHT_FLAG;
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

	public ushort GetShadow(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return (ushort)(shadowMap[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F);
		else
			return (ushort)(shadowMap[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4);
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

	public void SetLightMap(byte[] lm){
		this.lightMap = lm;
	}

	public void SetShadowMap(byte[] sm){
		this.shadowMap = sm;
	}

	public byte[] GetShadowMap(){
		if(this.shadowMap == null){
			this.CalculateLightMap();
		}

		return this.shadowMap;
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
public struct CalculateShadowMapJob : IJob{
	public NativeArray<byte> shadowMap;
	public NativeArray<byte> lightMap;
	public NativeList<int4> lightSources;
	public NativeArray<byte> changed;
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
					shadowMap[index] = (byte)(shadowMap[index] & 0xF0);

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

						lightMap[index] = 0;
						continue;
					}

					// If is propagated
					if((shadowMap[index] & 0x0F) >= 7){
						CheckChanged(x, z);
						continue;
					}
					else{
						shadowMap[index] = (byte)(shadowMap[index] & 0xF0);
					}

					// If is transparent
					if(isBlock){
						if(isTransparentBlock[blockCode] == 1){
							CheckChanged(x, z);
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
							CheckChanged(x, z);
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

					lightMap[index] = 0;
				}
			}
		}
	}

	// Checks if chunk has empty space in neighborhood
	public void CheckChanged(int x, int z){
		if(x == 0)
			changed[0] = (byte)(changed[0] | 1);
		else if(x == chunkWidth-1)
			changed[0] = (byte)(changed[0] | 2);

		if(z == 0)
			changed[0] = (byte)(changed[0] | 4);
		else if(z == chunkWidth-1)
			changed[0] = (byte)(changed[0] | 8);
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}
}

/*
Takes the ShadowMap and turns it into a progressive lightmap
*/
[BurstCompile]
public struct CalculateLightMapJob : IJob{
	public NativeArray<byte> lightMap;
	public NativeArray<byte> shadowMap;
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

				ScanSurroundings(currentExtra.xyz, (byte)currentExtra.w, false); // ALTER THAT ZERO LATER
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
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (shadowMap[index] >> 4) > 0){
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
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (shadowMap[index] >> 4) > 0){
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
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (shadowMap[index] >> 4) > 0){
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
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (shadowMap[index] >> 4) > 0){
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
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (shadowMap[index] >> 4) > 0){
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
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight-1 && (shadowMap[index] >> 4) > 0){
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

				if((shadowMap[index] & maxLightLevel) == 2){
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
		byte shadow;

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
			shadow = (byte)(shadowMap[GetIndex(x-1, y, z)] & 0x0F);
			if(shadow == 1 || shadow >= 3){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(xp){
			shadow = (byte)(shadowMap[GetIndex(x+1, y, z)] & 0x0F);
			if(shadow == 1 || shadow >= 3){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zm){
			shadow = (byte)(shadowMap[GetIndex(x, y, z-1)] & 0x0F);
			if(shadow == 1 || shadow >= 3){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zp){
			shadow = (byte)(shadowMap[GetIndex(x, y, z+1)] & 0x0F);
			if(shadow == 1 || shadow >= 3){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}

		return false;
	}
}


//[BurstCompile]
public struct CalculateLightPropagationJob : IJob{
	public NativeArray<byte> lightMap1;
	public NativeArray<byte> lightMap2;
	public NativeArray<byte> shadowMap1;
	public NativeArray<byte> shadowMap2;

	public NativeList<int3> bfsq1; // Breadth-first search queue
	public NativeList<int3> bfsq2;
	public NativeList<int3> bfsqr1;
	public NativeList<int3> bfsqr2;
	public NativeHashSet<int3> visited1;
	public NativeHashSet<int3> visited2;
	public NativeHashSet<int3> visitedr1;
	public NativeHashSet<int3> visitedr2;

	public NativeArray<byte> changed; // [0] = Update current Chunk, [1] = Update neighbor Chunk, [2] = Update neighbor with lights, [3] = Xm,Xp,Zm,Zp flags of chunk of neighbor to calculate borders

	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public byte borderCode; // 0 = xm, 1 = xp, 2 = zm, 3 = zp


	public void Execute(){
		int index1, index2;
		bool start = true;

		while(bfsq1.Length > 0 || bfsq2.Length > 0 || start){
			start = false;


			// Processing Shadows

			// xm
			if(borderCode == 0){
				for(int y=0; y < chunkDepth; y++){
					for(int z=0; z < chunkWidth; z++){
						index1 = y*chunkWidth+z;
						index2 = (chunkWidth-1)*chunkDepth*chunkWidth+y*chunkWidth+z;

						ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					}
				}
			}
			// xp
			else if(borderCode == 1){
				for(int y=0; y < chunkDepth; y++){
					for(int z=0; z < chunkWidth; z++){
						index1 = (chunkWidth-1)*chunkDepth*chunkWidth+y*chunkWidth+z;
						index2 = y*chunkWidth+z;

						ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					}
				}			
			}
			// zm
			else if(borderCode == 2){
				for(int y=0; y < chunkDepth; y++){
					for(int x=0; x < chunkWidth; x++){
						index1 = x*chunkDepth*chunkWidth+y*chunkWidth;
						index2 = x*chunkDepth*chunkWidth+y*chunkWidth+(chunkWidth-1);

						ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					}
				}
			}
			// zp
			else if(borderCode == 3){
				for(int y=0; y < chunkDepth; y++){
					for(int x=0; x < chunkWidth; x++){
						index1 = x*chunkDepth*chunkWidth+y*chunkWidth+(chunkWidth-1);
						index2 = x*chunkDepth*chunkWidth+y*chunkWidth;

						ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					}
				}			
			}

			int3 current;

			// CURRENT CHUNK LIGHT DELETE ====================================
			while(bfsqr1.Length > 0){
				changed[0] = 1;

				current = bfsqr1[0];

				if(visitedr1.Contains(current)){
					bfsqr1.RemoveAt(0);
					continue;
				}

				lightMap1[GetIndex(current)] = (byte)(lightMap1[GetIndex(current)] & 0xF0);
				WriteLightUpdateFlag(current, borderCode);

				ScanDeletes(current, (byte)(lightMap1[GetIndex(current)] & 0x0F), (byte)(shadowMap1[GetIndex(current)] & 0x0F), lightMap1, shadowMap1, bfsqr1, visitedr1, borderCode);

				shadowMap1[GetIndex(current)] = (byte)((shadowMap1[GetIndex(current)] & 0xF0) | 1);

				visitedr1.Add(current);
				bfsqr1.RemoveAt(0);
			}

			// NEIGHBOR CHUNK LIGHT DELETE ====================================
			while(bfsqr2.Length > 0){
				changed[1] = 1;
				changed[2] = 1;

				current = bfsqr2[0];

				if(visitedr2.Contains(current)){
					bfsqr2.RemoveAt(0);
					continue;
				}

				lightMap2[GetIndex(current)] = (byte)(lightMap2[GetIndex(current)] & 0xF0);
				WriteLightUpdateFlag(current, borderCode);

				ScanDeletes(current, (byte)(lightMap2[GetIndex(current)] & 0x0F), (byte)(shadowMap2[GetIndex(current)] & 0x0F), lightMap2, shadowMap2, bfsqr2, visitedr2, borderCode);

				shadowMap2[GetIndex(current)] = (byte)((shadowMap2[GetIndex(current)] & 0xF0) | 1);

				visitedr2.Add(current);
				bfsqr2.RemoveAt(0);
			}

			// CURRENT CHUNK LIGHT PROPAG =====================================
			while(bfsq1.Length > 0){
				changed[0] = 1;

				current = bfsq1[0];

				if(visited1.Contains(current)){
					bfsq1.RemoveAt(0);
					continue;
				}

				// Fix light for reshedings
				if((lightMap1[GetIndex(current)] & 0x0F) == 15)
					lightMap1[GetIndex(current)] = (byte)((lightMap1[GetIndex(current)] & 0xF0) | ((lightMap2[GetIndex(GetNeighborCoord(current, borderCode, 1))] & 0x0F) -1));

				ScanSurroundings(current, (byte)(lightMap1[GetIndex(current)] & 0x0F), true, true, 1, lightMap1, lightMap2, shadowMap1, shadowMap2, bfsq1, bfsq2, visited1, borderCode);
				WriteLightUpdateFlag(current, borderCode);

				visited1.Add(current);
				bfsq1.RemoveAt(0);
			}

			// NEIGHBOR CHUNK LIGHT PROPAG ====================================

			while(bfsq2.Length > 0){
				changed[1] = 1;

				current = bfsq2[0];

				if(visited2.Contains(current)){
					bfsq2.RemoveAt(0);
					continue;
				}

				// Fix light for reshedings
				if((lightMap2[GetIndex(current)] & 0x0F) == 15)
					lightMap2[GetIndex(current)] = (byte)((lightMap2[GetIndex(current)] & 0xF0) | ((lightMap1[GetIndex(GetNeighborCoord(current, borderCode, 2))] & 0x0F) -1));

				ScanSurroundings(current, (byte)(lightMap2[GetIndex(current)] & 0x0F), true, false, 2, lightMap2, lightMap1, shadowMap2, shadowMap1, bfsq2, bfsq1, visited2, borderCode);
				WriteLightUpdateFlag(current, borderCode);

				visited2.Add(current);
				bfsq2.RemoveAt(0);
			}

			// Restart?
			if((changed[3] & 128) != 0){
				start = true;
				changed[3] = (byte)(changed[3] & 127);
			}
		}
	}

	// Checks if current block is bordering the neighbor chunk
	public bool CheckBorder(int3 c, byte borderCode, int side){
		if(side == 1){
			if(borderCode == 0 && c.x == 0)
				return true;
			if(borderCode == 1 && c.x == chunkWidth-1)
				return true;
			if(borderCode == 2 && c.z == 0)
				return true;
			if(borderCode == 3 && c.z == chunkWidth-1)
				return true;
		}
		else{
			if(borderCode == 0 && c.x == chunkWidth-1)
				return true;
			if(borderCode == 1 && c.x == 0)
				return true;
			if(borderCode == 2 && c.z == chunkWidth-1)
				return true;
			if(borderCode == 3 && c.z == 0)
				return true;
		}

		return false;
	}

	// Checks whether the neighbor border block to the given one was visited before
	public bool CheckVisited(int3 c, byte borderCode, int side){
		int3 originalNeighbor;

		if(side == 1){
			if(borderCode == 0)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));
			else
				originalNeighbor = new int3(c.x, c.y, 0);
		}
		else{
			if(borderCode == 0)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, 0);
			else
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));			
		}

		if(side == 1)
			return visited2.Contains(originalNeighbor);
		else
			return visited1.Contains(originalNeighbor);
	}

	public int3 GetNeighborCoord(int3 c, byte borderCode, int side){
		int3 originalNeighbor;

		if(side == 1){
			if(borderCode == 0)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));
			else
				originalNeighbor = new int3(c.x, c.y, 0);
		}
		else{
			if(borderCode == 0)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, 0);
			else
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));
		}

		return originalNeighbor;
	}	

	// Finds out which light processing the given shadow border must go through
	public void ProcessShadowCode(int a, int b, int index1, int index2, byte borderCode){
		// Checking if was explored already
		int3 coord1, coord2;

		coord1 = GetCoord(index1);
		coord2 = GetCoord(index2);
		if(visited1.Contains(coord1) && visited2.Contains(coord2))
			return;

		// Finding the order
		int shadowCode = a+b;

		if(shadowCode == 0)
			return;

		int aux;
		bool order = a <= b;

		if(b < a){
			aux = b;
			b = a;
			a = aux;
		}

		// 0-1, 0-2, 0-3, 
		if((shadowCode == 1 || shadowCode == 2 || shadowCode == 3) && a == 0)
			ApplyShadowWork(1, order, index1, index2, borderCode);

		// 0-7, 0-8, 0-9, 0-10
		else if(shadowCode >= 7 && a == 0)
			ApplyShadowWork(1, order, index1, index2, borderCode);

		// 1-2, 1-3
		else if(a == 1 && (b == 2 || b == 3))
			ApplyShadowWork(2, order, index1, index2, borderCode);

		// 3-3
		else if(shadowCode == 6 && a == 3)
			ApplyShadowWork(3, order, index1, index2, borderCode);
		
		// 1-7, 1-8, 1-9, 1-10
		else if(b >= 7 && a == 1)
			ApplyShadowWork(4, order, index1, index2, borderCode);

		// All directionals linked to a 2 or 3 (e.g. 2-7, 3-7, 2-8, 3-8, etc.)
		else if(b >= 7 && (a == 2 || a == 3))
			ApplyShadowWork(5, order, index1, index2, borderCode);

		// 2-3
		else if(a == 2 && b == 3)
			ApplyShadowWork(6, order, index1, index2, borderCode);
	}

	// Applies propagation of light 
	public void ApplyShadowWork(int workCode, bool normalOrder, int index1, int index2, byte borderCode){
		// Update border UVs only
		if(workCode == 1){
			if(normalOrder)
				changed[0] = 1;
			else
				changed[1] = 1;
		}

		// Propagate normally and sets the correct shadow direction
		else if(workCode == 2){
			if(normalOrder){
				lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
				shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				bfsq1.Add(GetCoord(index1));
				visited2.Add(GetCoord(index2));
			}
			else{
				lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
				shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				bfsq2.Add(GetCoord(index2));
				visited1.Add(GetCoord(index1));
			}
		}

		// Find which side to propagate
		else if(workCode == 3){
			if((lightMap2[index2] & 0x0F) < ((lightMap1[index1] & 0x0F) -1)){
				lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
				shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				bfsq2.Add(GetCoord(index2));			
			}
			else if((lightMap1[index1] & 0x0F) < ((lightMap2[index2] & 0x0F) -1)){
				lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
				shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				bfsq1.Add(GetCoord(index1));
			}
		}

		// Propagate to a third chunk or dies because of lack of transmitter
		else if(workCode == 4){
			if(normalOrder){
				// If is the same direction, delete
				if(GetShadowDirection(borderCode, !normalOrder) == (shadowMap2[index2] & 0x0F) && (lightMap1[index1] & 0x0F) != 15){
					bfsqr2.Add(GetCoord(index2));
				}
				// If not same direction, propagate
				else{
					// If actually can propagate some light
					if(((lightMap2[index2] & 0x0F) - 1) > 0){
						lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
						shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq1.Add(GetCoord(index1));
						visited2.Add(GetCoord(index2));
					}
				}
			}
			else{
				// If is the same direction, delete
				if(GetShadowDirection(borderCode, !normalOrder) == (shadowMap1[index1] & 0x0F) && ((lightMap2[index2] & 0x0F) != 15)){
					bfsqr1.Add(GetCoord(index1));
				}
				// If not same direction, propagate
				else{
					// If actually can propagate some light
					if(((lightMap1[index1] & 0x0F) - 1) > 0){
						lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
						shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq2.Add(GetCoord(index2));
						visited1.Add(GetCoord(index1));
					}
				}
			}
		}

		// If Directionals hit transmitters
		else if(workCode == 5){
			if(normalOrder){
				// If not the same direction, try to expand
				if(GetShadowDirection(borderCode, normalOrder) != (shadowMap2[index2] & 0x0F)){
					if((lightMap2[index2] & 0x0F) < (lightMap1[index1] & 0x0F) - 1){
						lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
						shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq2.Add(GetCoord(index1));
					}
				}
			}
			else{
				// If not the same direction, try to expand
				if(GetShadowDirection(borderCode, normalOrder) != (shadowMap1[index1] & 0x0F)){
					if((lightMap1[index1] & 0x0F) < (lightMap1[index2] & 0x0F) - 1){
						lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
						shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq1.Add(GetCoord(index1));
					}
				}				
			}
		}

		// If Sunlight hits local propagation in neighbor
		else if(workCode == 6){
			if(normalOrder){
				if((lightMap1[index1] & 0x0F) < ((lightMap2[index2] & 0x0F) -1)){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq1.Add(GetCoord(index1));
				}	
			}
			else{
				if((lightMap2[index2] & 0x0F) < ((lightMap1[index1] & 0x0F) -1)){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq2.Add(GetCoord(index2));
				}			
			}
		}
	}

	public byte GetShadowDirection(byte borderCode, bool normalOrder){
		if(normalOrder){
			if(borderCode == 0)
				return 8;
			if(borderCode == 1)
				return 7;
			if(borderCode == 2)
				return 10;
			else
				return 9;
		}
		else
			return (byte)(borderCode + 7);
	}

	public int3 GetCoord(int index){
		int x = index / (chunkWidth*chunkDepth);
		int y = (index/chunkWidth)%chunkDepth;
		int z = index%chunkWidth;

		return new int3(x, y, z);
	}


	// Checks the surroundings and adds light fallout
	public void ScanSurroundings(int3 c, byte currentLight, bool isNatural, bool normalOrder, int side, NativeArray<byte> selectedMap, NativeArray<byte> otherMap, NativeArray<byte> selectedShadow, NativeArray<byte> otherShadow, NativeList<int3> bfsq, NativeList<int3> otherBfsq, NativeHashSet<int3> visited, byte borderCode){
		if(currentLight == 1)
			return;

		int3 aux;
		int index;
		byte sideShadow;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			index = GetIndex(aux);

			if(isNatural){
				if((selectedMap[index] & 0x0F) < currentLight){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight-1){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					//bfsqExtra.Add(new int4(aux, (currentLight-1)));
				}					
			}
		}
		else{
			if((borderCode == 1 && side == 1) || (borderCode == 0 && side == 2)){
				if(isNatural){
					int n = GetIndex(GetNeighborCoord(c, borderCode, side));
					sideShadow = (byte)(otherShadow[n] & 0x0F);
					if(sideShadow == 1){
						changed[3] = (byte)(changed[3] | 128);
						otherMap[n] = (byte)((otherMap[n] & 0xF0) | 15);
						otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
						otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
					}
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			index = GetIndex(aux);

			if(isNatural){
				if((selectedMap[index] & 0x0F) < currentLight){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight-1){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					//bfsqExtra.Add(new int4(aux, (currentLight-1)));
				}					
			}
		}
		else{
			if((borderCode == 0 && side == 1) || (borderCode == 1 && side == 2)){
				if(isNatural){
					int n = GetIndex(GetNeighborCoord(c, borderCode, side));
					sideShadow = (byte)(otherShadow[n] & 0x0F);
					if(sideShadow == 1){
						changed[3] = (byte)(changed[3] | 128);
						otherMap[n] = (byte)((otherMap[n] & 0xF0) | 15);
						otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
						otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
					}
				}
			}
		}

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			index = GetIndex(aux);

			if(isNatural){
				if((selectedMap[index] & 0x0F) < currentLight){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight-1){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					//bfsqExtra.Add(new int4(aux, (currentLight-1)));
				}					
			}
		}
		else{
			if((borderCode == 3 && side == 1) || (borderCode == 4 && side == 2)){
				if(isNatural){
					int n = GetIndex(GetNeighborCoord(c, borderCode, side));
					sideShadow = (byte)(otherShadow[n] & 0x0F);
					if(sideShadow == 1){
						changed[3] = (byte)(changed[3] | 128);
						otherMap[n] = (byte)((otherMap[n] & 0xF0) | 15);
						otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
						otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
					}
				}	
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			index = GetIndex(aux);

			if(isNatural){
				if((selectedMap[index] & 0x0F) < currentLight){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight-1){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					//bfsqExtra.Add(new int4(aux, (currentLight-1)));
				}					
			}
		}
		else{
			if((borderCode == 4 && side == 1) || (borderCode == 3 && side == 2)){
				if(isNatural){
					int n = GetIndex(GetNeighborCoord(c, borderCode, side));
					sideShadow = (byte)(otherShadow[n] & 0x0F);
					if(sideShadow == 1){
						changed[3] = (byte)(changed[3] | 128);
						otherMap[n] = (byte)((otherMap[n] & 0xF0) | 15);
						otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
						otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
					}
				}				
			}
		}	

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			index = GetIndex(aux);

			if(isNatural){
				if((selectedMap[index] & 0x0F) < currentLight){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight-1){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					//bfsqExtra.Add(new int4(aux, (currentLight-1)));
				}					
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			index = GetIndex(aux);

			if(isNatural){
				if((selectedMap[index] & 0x0F) < currentLight){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight-1){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) | ((currentLight-1) << 4)));
					//bfsqExtra.Add(new int4(aux, (currentLight-1)));
				}
			}
		}
	}

	public void	ScanDeletes(int3 c, byte currentLight, byte currentShadow, NativeArray<byte> selectedMap, NativeArray<byte> selectedShadow, NativeList<int3> bfsqr, NativeHashSet<int3> visitedr, byte borderCode){
		if(currentLight == 1)
			return;

		int3 aux;
		int index;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			index = GetIndex(aux);

			if((selectedMap[index] & 0x0F) == currentLight - 1 && currentShadow == (selectedShadow[index] & 0x0F)){
				bfsqr.Add(aux);
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			index = GetIndex(aux);

			if((selectedMap[index] & 0x0F) == currentLight - 1 && currentShadow == (selectedShadow[index] & 0x0F)){
				bfsqr.Add(aux);
			}
		}

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth-1){
			index = GetIndex(aux);

			if((selectedMap[index] & 0x0F) == currentLight - 1 && currentShadow == (selectedShadow[index] & 0x0F)){
				bfsqr.Add(aux);
			}
		}

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			index = GetIndex(aux);

			if((selectedMap[index] & 0x0F) == currentLight - 1 && currentShadow == (selectedShadow[index] & 0x0F)){
				bfsqr.Add(aux);
			}
		}

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth-1){
			index = GetIndex(aux);

			if((selectedMap[index] & 0x0F) == currentLight - 1 && currentShadow == (selectedShadow[index] & 0x0F)){
				bfsqr.Add(aux);
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			index = GetIndex(aux);

			if((selectedMap[index] & 0x0F) == currentLight - 1 && currentShadow == (selectedShadow[index] & 0x0F)){
				bfsqr.Add(aux);
			}
		}
	}


	// Adds to further light update flag if playing with directionals
	public void WriteLightUpdateFlag(int3 aux, byte borderCode){
		// zp
		if(aux.z == chunkWidth-1 && borderCode != 3)
			changed[3] = (byte)(changed[3] | (1 << 3));
		// zm
		else if(aux.z == 0 && borderCode != 4)
			changed[3] = (byte)(changed[3] | (1 << 2));

		// xp
		if(aux.x == chunkWidth-1 && borderCode != 0)
			changed[3] = (byte)(changed[3] | (1 << 1));
		// xm
		else if(aux.x == 0 && borderCode != 1)
			changed[3] = (byte)(changed[3] | (1));
	}

	public byte InvertShadowDirection(byte shadow){
		if(shadow == 7)
			return 8;
		if(shadow == 8)
			return 7;
		if(shadow == 9)
			return 10;
		if(shadow == 10)
			return 9;
		return 0;
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}
	public int GetIndex(int x, int y, int z){
		return x*chunkWidth*chunkDepth+y*chunkWidth+z;
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

/*
SHADOW DOCUMENTATION

0: Solid
1: Empty
2: Direct Sunlight
3: Transmitted Sunlight
4-6: [UNUSED]
7: Received from neighbor chunk in a XM propagation
8: Received from neighbor chunk in a XP propagation
9: Received from neighbor chunk in a ZM propagation
10: Received from neighbor chunk in a ZP propagation
*/