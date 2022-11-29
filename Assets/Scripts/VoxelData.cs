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
	private byte[] renderMap;
	private byte[] shadowMap;
	private byte[] lightMap;

	private byte PROPAGATE_LIGHT_FLAG = 0; // 0 = no, 1 = xm, 2 = xp, 4 = zm, 8 = zp

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

	public void Destroy(){
		this.data = null;
		this.heightMap = null;
		this.shadowMap = null;
		this.lightMap = null;
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
	public static byte PropagateLight(VoxelData a, VoxelMetadata aMetadata, VoxelData b, VoxelMetadata bMetadata,  byte borderCode){
		NativeArray<byte> lightMap1 = NativeTools.CopyToNative(a.GetLightMap(aMetadata));
		NativeArray<byte> lightMap2 = NativeTools.CopyToNative(b.GetLightMap(bMetadata));
		NativeArray<byte> shadowMap1 = NativeTools.CopyToNative(a.GetShadowMap());
		NativeArray<byte> shadowMap2 = NativeTools.CopyToNative(b.GetShadowMap());

		NativeList<int3> bfsq1 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsq2 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsqe1 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsqe2 = new NativeList<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visited1 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visited2 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visitede1 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> visitede2 = new NativeHashSet<int3>(0, Allocator.TempJob);
		NativeList<int4> aux = new NativeList<int4>(0, Allocator.TempJob);
		NativeHashSet<int4> hashAux = new NativeHashSet<int4>(0, Allocator.TempJob);
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
			visited1 = visited1,
			visited2 = visited2,
			visitede1 = visitede1,
			visitede2 = visitede2,
			bfsqe1 = bfsqe1,
			bfsqe2 = bfsqe2,
			aux = aux,
			hashAux = hashAux,
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
        bfsqe1.Dispose();
        bfsqe2.Dispose();
        visited1.Dispose();
        visited2.Dispose();
        visitede1.Dispose();
        visitede2.Dispose();
        aux.Dispose();
        hashAux.Dispose();
        changed.Dispose();

        return updateFlag;
    }

	/*
	Burst Compiled
	*/
	public void CalculateLightMap(VoxelMetadata metadata){
		if(this.heightMap == null)
			CalculateHeightMap();

		NativeArray<byte> lightMap;
		NativeArray<byte> shadowMap;
		NativeList<int4> lightSources = new NativeList<int4>(0, Allocator.TempJob);
		NativeArray<byte> heightMap = NativeTools.CopyToNative(this.heightMap);
		NativeArray<byte> changed = new NativeArray<byte>(new byte[]{0}, Allocator.TempJob);
		NativeArray<ushort> states = NativeTools.CopyToNative(metadata.GetStateData());

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

		// SHADOWMAPPING =========================================================

		CalculateShadowMapJob csmJob = new CalculateShadowMapJob{
			shadowMap = shadowMap,
			lightMap = lightMap,
			lightSources = lightSources,
			heightMap = heightMap,
			data = blockData,
			states = states,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = BlockEncyclopediaECS.objectTransparent,
			isTransparentBlock = BlockEncyclopediaECS.blockTransparent,
			blockLuminosity = BlockEncyclopediaECS.blockLuminosity,
			objectLuminosity = BlockEncyclopediaECS.objectLuminosity,
			changed = changed
		};

        job = csmJob.Schedule();
        job.Complete();

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
			changed = changed
		};

        job = clmJob.Schedule();
        job.Complete();

        this.lightMap = NativeTools.CopyToManaged(lightMap);
        this.shadowMap = NativeTools.CopyToManaged(shadowMap);
        this.PROPAGATE_LIGHT_FLAG = changed[0];

        blockData.Dispose();
        states.Dispose();

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
		if(this.renderMap == null)
			this.renderMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];

		ushort blockCode;
		bool found, foundRender;

		for(int x=0; x < Chunk.chunkWidth; x++){
	    	for(int z=0; z < Chunk.chunkWidth; z++){
	    		found = false;
	    		foundRender = false;
	    		for(int y=Chunk.chunkDepth-1; y >= 0; y--){
	    			blockCode = this.data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

	    			// If is a block
	    			if(blockCode <= ushort.MaxValue/2){
	    				if(!BlockEncyclopediaECS.blockInvisible[blockCode] && !foundRender){
	    					if(y < Chunk.chunkDepth-1)
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(y+1);
	    					else
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(Chunk.chunkDepth-1);
	    					foundRender = true;
	    				}

	    				if(BlockEncyclopediaECS.blockAffectLight[blockCode]){
	    					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
	    					found = true;
	    					break;
	    				}
	    			}
	    			// If it's an object
	    			else{
	    				if(!BlockEncyclopediaECS.blockInvisible[ushort.MaxValue - blockCode] && !foundRender){
	    					if(y < Chunk.chunkDepth-1)
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(y+1);
	    					else
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(Chunk.chunkDepth-1);
	    					foundRender = true;
	    				}

	    				if(BlockEncyclopediaECS.objectAffectLight[ushort.MaxValue - blockCode]){
	    					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
	    					found = true;
	    					break;
	    				}		
	    			}
	    		}

	    		if(!foundRender){
	    			this.renderMap[x*Chunk.chunkWidth+z] = 0;
	    		}
	    		if(!found){
	    			this.heightMap[x*Chunk.chunkWidth+z] = 0;
	    		}
	    	}
		}

		FixRenderMap();
	}

	// Remaps renderMap correctly
	private void FixRenderMap(){
		byte biggest;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int z=0; z < Chunk.chunkWidth; z++){
				biggest = 0;

				if(x > 0)
					if(this.renderMap[(x-1)*Chunk.chunkWidth+z] > biggest)
						biggest = this.renderMap[(x-1)*Chunk.chunkWidth+z];
				if(x < Chunk.chunkWidth-1)
					if(this.renderMap[(x+1)*Chunk.chunkWidth+z] > biggest)
						biggest = this.renderMap[(x+1)*Chunk.chunkWidth+z];
				if(z > 0)
					if(this.renderMap[x*Chunk.chunkWidth+(z-1)] > biggest)
						biggest = this.renderMap[x*Chunk.chunkWidth+(z-1)];
				if(z < Chunk.chunkWidth-1)
					if(this.renderMap[x*Chunk.chunkWidth+(z+1)] > biggest)
						biggest = this.renderMap[x*Chunk.chunkWidth+(z+1)];

				if(this.renderMap[x*Chunk.chunkWidth+z] > biggest)
					biggest = this.renderMap[x*Chunk.chunkWidth+z];

				this.renderMap[x*Chunk.chunkWidth+z] = biggest;
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

	public byte[] GetLightMap(VoxelMetadata metadata){
		if(this.lightMap == null){
			this.CalculateLightMap(metadata);
		}

		return this.lightMap;
	}

	public byte[] GetRenderMap(){
		if(this.renderMap == null)
			this.CalculateHeightMap();

		return this.renderMap;
	}

	public void SetLightMap(byte[] lm){
		this.lightMap = lm;
	}

	public void SetShadowMap(byte[] sm){
		this.shadowMap = sm;
	}

	public byte[] GetShadowMap(){
		return this.shadowMap;
	}

	public ushort[] GetData(){
		return this.data;
	}

	public void SetData(ushort[] data, bool isServer){
		this.data = data;

		/*
		if(VoxelData.shaderReferences == null){
			VoxelData.shaderReferences = GameObject.Find("ShaderReferences").GetComponent<ShaderReferences>();
			VoxelData.shadowMapShader = VoxelData.shaderReferences.GetShadowMapShader();
		}
		*/

		if(this.heightMap == null && !isServer)
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
	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<ushort> states;
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
	public NativeArray<byte> changed;

	public void Execute(){
		bool isBlock;
		ushort blockCode;
		int index;
		byte border;

		for(int z=0; z < chunkWidth; z++){
			for(int y=chunkDepth-1; y >= 0; y--){
				for(int x=0; x < chunkWidth; x++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					blockCode = data[index];
					isBlock = blockCode <= ushort.MaxValue/2;

					// If is above heightMap
					if(y > heightMap[x*chunkWidth+z]){
						shadowMap[index] = 18;

						// Gets lightsource
						if(isBlock){
							if((blockLuminosity[blockCode] & 0x0F) > 0 && states[index] <= (blockLuminosity[blockCode] >> 4))
								lightSources.Add(new int4(x, y, z, (blockLuminosity[blockCode] & 0x0F)));
						}
						else{
							if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) > 0 && states[index] <= (objectLuminosity[ushort.MaxValue - blockCode] >> 4))
								lightSources.Add(new int4(x, y, z, objectLuminosity[ushort.MaxValue - blockCode] & 0x0F));							
						}

						lightMap[index] = 0;
						continue;
					}

					// If is transparent
					if(isBlock){
						if(isTransparentBlock[blockCode] == 1){
							if((shadowMap[index] >> 4) >= 7){
								if((shadowMap[index] & 0x0F) < 7){
									// Set darken flag
									border = CheckBorder(x, y, z);
									if((shadowMap[index] & 0x0F) == 3 && border > 0)
										changed[0] = (byte)(changed[0] | border);

									shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
								}
							}
							else{
								if((shadowMap[index] & 0x0F) >= 7)
									shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
								else{
									// Set darken flag
									border = CheckBorder(x, y, z);
									if((shadowMap[index] & 0x0F) == 3 && border > 0)
										changed[0] = (byte)(changed[0] | border);

									shadowMap[index] = 17;
								}
							}
						}
						else
							shadowMap[index] = 0;

						if((blockLuminosity[blockCode] & 0x0F) > 0 && states[index] <= (blockLuminosity[blockCode] >> 4))
							lightSources.Add(new int4(x, y, z, (blockLuminosity[blockCode] & 0x0F)));
					}
					else{
						if(isTransparentObj[ushort.MaxValue - blockCode] == 1){
							if((shadowMap[index] >> 4) >= 7){
								if((shadowMap[index] & 0x0F) < 7){
									// Set darken flag
									border = CheckBorder(x, y, z);
									if((shadowMap[index] & 0x0F) == 3 && border > 0)
										changed[0] = (byte)(changed[0] | border);

									shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
								}
							}
							else{
								if((shadowMap[index] & 0x0F) >= 7)
									shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
								else{
									// Set darken flag
									border = CheckBorder(x, y, z);
									if((shadowMap[index] & 0x0F) == 3 && border > 0)
										changed[0] = (byte)(changed[0] | border);

									shadowMap[index] = 17;
								}
							}
						}
						else
							shadowMap[index] = 0;

						if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) > 0 && states[index] <= (objectLuminosity[ushort.MaxValue - blockCode] >> 4))
							lightSources.Add(new int4(x, y, z, (objectLuminosity[ushort.MaxValue - blockCode] & 0x0F)));			
					}

					
					if((shadowMap[index] >> 4) >= 7 && (shadowMap[index] & 0x0F) < 7)
						lightMap[index] = (byte)(lightMap[index] & 0xF0);
					else if((shadowMap[index] >> 4) < 7 && (shadowMap[index] & 0x0F) < 7)
						lightMap[index] = 0;
					if((shadowMap[index] & 0x0F) >= 7)
						lightMap[index] = (byte)(lightMap[index] & 0x0F);

					if((shadowMap[index] >> 4) >= 7){
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
						lightMap[index] = (byte)(lightMap[index] & 0x0F);
					}
				}
			}
		}
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}

	public byte CheckBorder(int x, int y, int z){
		if(x == 0)
			return 1;
		else if(x == chunkWidth-1)
			return 2;
		else if(z == 0)
			return 4;
		else if(z == chunkWidth-1)
			return 8;
		else
			return 0;
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
	public NativeArray<byte> changed;


	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;


	public void Execute(){
		int3 current;
		int4 currentExtra;
		int bfsqSize;

		/***************************************
		Natural Light
		***************************************/
		DetectSunlight();
		bfsqSize = bfsq.Length;	
		
		// Iterates through queue
		while(bfsqSize > 0){
			current = bfsq[0];

			if(visited.Contains(current)){
				bfsq.RemoveAt(0);
				bfsqSize = bfsq.Length;	
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap[GetIndex(current)] & 0x0F), true);

			visited.Add(current);
			bfsq.RemoveAt(0);
			bfsqSize = bfsq.Length;	
		}

		DetectDirectionals();
		bfsqSize = bfsq.Length;	

		// Iterates through queue
		while(bfsqSize > 0){
			current = bfsq[0];

			if(visited.Contains(current)){
				bfsq.RemoveAt(0);
				bfsqSize = bfsq.Length;	
				continue;
			}

			ScanDirectionals(current, (byte)(lightMap[GetIndex(current)] & 0x0F), true, shadowMap[GetIndex(current)]);

			visited.Add(current);
			bfsq.RemoveAt(0);
			bfsqSize = bfsq.Length;	
		}
		

		visited.Clear();

		/***************************************
		Extra Lights
		***************************************/

		QuickSort();
		
		int currentLevel = 15;
		bool searchedCurrentLevel = false;
		bool initiateExtraLightSearch = false;
		int lastIndex = lightSources.Length - 1;
		int index = 0;
		bfsqSize = 0;

		visited.Clear();

		if(lightSources.Length > 0){
			while(bfsqSize > 0 || !initiateExtraLightSearch || lastIndex >= 0){
				initiateExtraLightSearch = true;
 
				if(bfsqExtra.Length > 0 && currentLevel == -1){
					searchedCurrentLevel = true;
					currentLevel = bfsqExtra[0].w;
				}
				else if(bfsqExtra.Length == 0 && lastIndex >= 0){
					searchedCurrentLevel = false;
					currentLevel = lightSources[lastIndex].w;
				}
				else if(bfsqExtra.Length == 0 && lastIndex == -1){
					break;
				}

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

							if(lightSources[i].w > (lightMap[index] >> 4)){
								bfsqExtra.Add(lightSources[i]);
								lightMap[index] = (byte)((lightMap[index] & 0x0F) + (lightSources[i].w << 4));
								shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
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

				if(bfsqExtra.Length == 0){
					continue;
				}

				currentExtra = bfsqExtra[0];
				bfsqExtra.RemoveAt(0);

				if(currentExtra.w == currentLevel && lastIndex >= 0)
					searchedCurrentLevel = false;

				ScanSurroundings(currentExtra.xyz, (byte)currentExtra.w, false);

				bfsqSize = bfsqExtra.Length;
			}
		}

		DetectDirectionals(extraLight:true);
		bfsqSize = bfsqExtra.Length;
		visited.Clear();

		// Fills propagations from outside chunks
		while(bfsqSize > 0){
			current = bfsqExtra[0].xyz;
			index = GetIndex(current);

			if(visited.Contains(current)){
				bfsqExtra.RemoveAt(0);
				bfsqSize = bfsqExtra.Length;	
				continue;
			}

			ScanDirectionals(current, (byte)(lightMap[index] >> 4), false, (byte)(shadowMap[index] >> 4));

			visited.Add(current);
			bfsqExtra.RemoveAt(0);	
			bfsqSize = bfsqExtra.Length;			
		}

		CheckBorders();
	}

	// Checks if chunk has empty space in neighborhood
	public void CheckBorders(){
		int index;

		for(int x=0; x < chunkWidth; x++){
			for(int y=0; y < chunkDepth; y++){
				for(int z=0; z < chunkWidth; z++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;

					if(x == 0 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 1) == 1))
						changed[0] = (byte)(changed[0] | 1);
					else if(x == chunkWidth-1 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 2) == 2))
						changed[0] = (byte)(changed[0] | 2);
					if(z == 0 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 4) == 4))
						changed[0] = (byte)(changed[0] | 4);
					else if(z == chunkWidth-1 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 8) == 8))
						changed[0] = (byte)(changed[0] | 8);

					if(x == 0 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 1);
					else if(x == chunkWidth-1 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 2);
					if(z == 0 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 4);
					else if(z == chunkWidth-1 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 8);
						
				}
			}
		}
	}

	public void DetectDirectionals(bool extraLight=false){
		int index;
		// xm
		for(int z=0; z < chunkWidth; z++){
			for(int y=heightMap[z]; y > 0; y--){
				index = y*chunkWidth+z;

				if(!extraLight)
					if((shadowMap[index] & 0x0F) >= 7)
						bfsq.Add(new int3(0, y, z));
				else
					if((shadowMap[index] >> 4) >= 7)
						bfsqExtra.Add(new int4(0, y, z, -1));					
			}
		}

		// xp
		for(int z=0; z < chunkWidth; z++){
			for(int y=heightMap[(chunkWidth-1)*chunkWidth+z]; y > 0; y--){
				index = (chunkWidth-1)*chunkWidth*chunkDepth+y*chunkWidth+z;

				if(!extraLight)
					if((shadowMap[index] & 0x0F) >= 7)
						bfsq.Add(new int3((chunkWidth-1), y, z));
				else
					if((shadowMap[index] >> 4) >= 7)
						bfsqExtra.Add(new int4((chunkWidth-1), y, z, -1));	
			}
		}

		// zm
		for(int x=0; x < chunkWidth; x++){
			for(int y=heightMap[x*chunkWidth]; y > 0; y--){
				index = x*chunkWidth*chunkDepth+y*chunkWidth;

				if(!extraLight)
					if((shadowMap[index] & 0x0F) >= 7)
						bfsq.Add(new int3(x, y, 0));
				else
					if((shadowMap[index] >> 4) >= 7)
						bfsqExtra.Add(new int4(x, y, 0, -1));	
			}
		}

		// zp
		for(int x=0; x < chunkWidth; x++){
			for(int y=heightMap[x*chunkWidth+(chunkWidth-1)]; y > 0; y--){
				index = x*chunkWidth*chunkDepth+y*chunkWidth+(chunkWidth-1);

				if(!extraLight)
					if((shadowMap[index] & 0x0F) >= 7)
						bfsq.Add(new int3(x, y, (chunkWidth-1)));
				else
					if((shadowMap[index] >> 4) >= 7)
						bfsqExtra.Add(new int4(x, y, (chunkWidth-1), -1));	
			}
		}
	}

	// Checks the surroundings and adds light fallout
	public void ScanSurroundings(int3 c, byte currentLight, bool isNatural){
		if(currentLight == 1)
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
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
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
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
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
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
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
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
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
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
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
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}
	}

	// Checks the surroundings and adds light fallout
	public void ScanDirectionals(int3 c, byte currentLight, bool isNatural, byte newShadow){
		if(currentLight == 0)
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
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
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
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
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
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
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
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
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
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
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
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
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
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(xp){
			shadow = (byte)(shadowMap[GetIndex(x+1, y, z)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zm){
			shadow = (byte)(shadowMap[GetIndex(x, y, z-1)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zp){
			shadow = (byte)(shadowMap[GetIndex(x, y, z+1)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}

		return false;
	}
}


[BurstCompile]
public struct CalculateLightPropagationJob : IJob{
	public NativeArray<byte> lightMap1;
	public NativeArray<byte> lightMap2;
	public NativeArray<byte> shadowMap1;
	public NativeArray<byte> shadowMap2;

	public NativeList<int3> bfsq1; // Breadth-first search queue
	public NativeList<int3> bfsq2;
	public NativeHashSet<int3> visited1;
	public NativeHashSet<int3> visited2;
	public NativeList<int3> bfsqe1;
	public NativeList<int3> bfsqe2;
	public NativeHashSet<int3> visitede1;
	public NativeHashSet<int3> visitede2;
	public NativeList<int4> aux;
	public NativeHashSet<int4> hashAux;

	public NativeArray<byte> changed; // [0] = Update current Chunk after the neighbor, [1] = Update neighbor Chunk, [2] = Update neighbor with lights, [3] = Xm,Xp,Zm,Zp flags of chunk of neighbor to calculate borders

	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public byte borderCode; // 0 = xm, 1 = xp, 2 = zm, 3 = zp


	public void Execute(){
		int index1, index2;

		// Processing Shadows
		// xm
		if(borderCode == 0){
			for(int y=0; y < chunkDepth; y++){
				for(int z=0; z < chunkWidth; z++){
					index1 = y*chunkWidth+z;
					index2 = (chunkWidth-1)*chunkDepth*chunkWidth+y*chunkWidth+z;

					ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
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
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
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
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
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
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
				}
			}			
		}

		int3 current;

		// CURRENT CHUNK LIGHT PROPAG =====================================
		int bfsq1Size = bfsq1.Length;

		while(bfsq1Size > 0){
			current = bfsq1[0];

			if(visited1.Contains(current)){
				bfsq1.RemoveAt(0);
				bfsq1Size = bfsq1.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap1[GetIndex(current)] & 0x0F), true, 1, lightMap1, lightMap2, shadowMap1, shadowMap2, bfsq1, bfsq2, visited1, visited2, borderCode);
			WriteLightUpdateFlag(current, borderCode);

			visited1.Add(current);
			bfsq1.RemoveAt(0);
			bfsq1Size = bfsq1.Length;
		}

		// NEIGHBOR CHUNK LIGHT PROPAG ====================================
		int bfsq2Size = bfsq2.Length;

		while(bfsq2Size > 0){
			changed[1] = 1;

			current = bfsq2[0];

			if(visited2.Contains(current)){
				bfsq2.RemoveAt(0);
				bfsq2Size = bfsq2.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap2[GetIndex(current)] & 0x0F), false, 2, lightMap2, lightMap1, shadowMap2, shadowMap1, bfsq2, bfsq1, visited2, visited1, borderCode);
			WriteLightUpdateFlag(current, borderCode);

			visited2.Add(current);
			bfsq2.RemoveAt(0);
			bfsq2Size = bfsq2.Length;
		}

		// CURRENT CHUNK EXTRA LIGHT PROPAG =====================================
		int bfsqe1Size = bfsqe1.Length;

		while(bfsqe1Size > 0){
			current = bfsqe1[0];

			if(visitede1.Contains(current)){
				bfsqe1.RemoveAt(0);
				bfsqe1Size = bfsqe1.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap1[GetIndex(current)] >> 4), true, 1, lightMap1, lightMap2, shadowMap1, shadowMap2, bfsqe1, bfsqe2, visitede1, visitede2, borderCode, extraLight:true);
			WriteLightUpdateFlag(current, borderCode);

			visitede1.Add(current);
			bfsqe1.RemoveAt(0);
			bfsqe1Size = bfsqe1.Length;
		}

		// NEIGHBOR CHUNK EXTRA LIGHT PROPAG ====================================
		int bfsqe2Size = bfsqe2.Length;

		while(bfsqe2Size > 0){

			current = bfsqe2[0];

			if(visitede2.Contains(current)){
				bfsqe2.RemoveAt(0);
				bfsqe2Size = bfsqe2.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap2[GetIndex(current)] >> 4), false, 2, lightMap2, lightMap1, shadowMap2, shadowMap1, bfsqe2, bfsqe1, visitede2, visitede1, borderCode, extraLight:true);
			WriteLightUpdateFlag(current, borderCode);

			visitede2.Add(current);
			bfsqe2.RemoveAt(0);
			bfsqe2Size = bfsqe2.Length;
		}
	}

	/*
	BFS into removing neighbor's directional shadow
	*/
	public void RemoveDirectionFromChunk(NativeArray<byte> selectedLightMap, NativeArray<byte> selectedShadowMap, byte currentShadow, int3 pos, byte borderCode, bool extraLight=false){
		int index = GetIndex(pos);
		int4 current;
		byte thisShadow, thisLight;
		bool firstIteration = true;
		int numIterations = 0;

		if(currentShadow < 7)
			return;

		if(!extraLight)
			aux.Add(new int4(pos, (selectedLightMap[index] & 0x0F)+1));
		else
			aux.Add(new int4(pos, (selectedLightMap[index] >> 4)+1));

		int auxSize = aux.Length;

		while(auxSize > 0){
			current = aux[0];
			index = GetIndex(current.xyz);

			if(hashAux.Contains(current)){
				aux.RemoveAt(0);
				auxSize = aux.Length;
				continue;
			}

			if(!extraLight){
				thisLight = (byte)(selectedLightMap[index] & 0x0F);
				thisShadow = (byte)(selectedShadowMap[index] & 0x0F);
			}
			else{
				thisLight = (byte)(selectedLightMap[index] >> 4);
				thisShadow = (byte)(selectedShadowMap[index] >> 4);
			}

			if(thisShadow == currentShadow){
				if(thisLight == current.w-1 && thisLight > 0){
					numIterations++;

					if(!extraLight){
						selectedLightMap[index] = (byte)(selectedLightMap[index] & 0xF0);
						selectedShadowMap[index] = (byte)((selectedShadowMap[index] & 0xF0) | 1);
					}
					else{
						selectedLightMap[index] = (byte)(selectedLightMap[index] & 0x0F);
						selectedShadowMap[index] = (byte)((selectedShadowMap[index] & 0x0F) | 16);
					}

					if(firstIteration){
						if(current.x == 0 && borderCode != 1)
							changed[3] = (byte)(changed[3] | 1);
						if(current.x == chunkWidth-1 && borderCode != 0)
							changed[3] = (byte)(changed[3] | 2);
						if(current.z == 0 && borderCode != 3)
							changed[3] = (byte)(changed[3] | 4);
						if(current.z == chunkWidth-1 && borderCode != 2)
							changed[3] = (byte)(changed[3] | 8);
					}
					else{
						if(current.x == 0)
							changed[3] = (byte)(changed[3] | 1);
						if(current.x == chunkWidth-1)
							changed[3] = (byte)(changed[3] | 2);
						if(current.z == 0)
							changed[3] = (byte)(changed[3] | 4);
						if(current.z == chunkWidth-1)
							changed[3] = (byte)(changed[3] | 8);						
					}

					if(current.x > 0)
						aux.Add(new int4(current.x-1, current.y, current.z, current.w-1));
					if(current.x < chunkWidth-1)
						aux.Add(new int4(current.x+1, current.y, current.z, current.w-1));
					if(current.z > 0)
						aux.Add(new int4(current.x, current.y, current.z-1, current.w-1));
					if(current.z < chunkWidth-1)
						aux.Add(new int4(current.x, current.y, current.z+1, current.w-1));
					if(current.y > 0)
						aux.Add(new int4(current.x, current.y-1, current.z, current.w-1));
					if(current.y < chunkDepth-1)
						aux.Add(new int4(current.x, current.y+1, current.z, current.w-1));

					hashAux.Add(current);

				}
			}

			aux.RemoveAt(0);
			auxSize = aux.Length;
			firstIteration = false;
		}

		if(numIterations > 0){
			changed[2] = 1;
			changed[0] = 1;
		}

		hashAux.Clear();
		aux.Clear();
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
	public void ProcessShadowCode(int a, int b, int index1, int index2, byte borderCode, bool extraLight=false){
		// Checking if was explored already
		int3 coord1, coord2;

		coord1 = GetCoord(index1);
		coord2 = GetCoord(index2);

		if(extraLight)
			if(visitede1.Contains(coord1) && this.visitede2.Contains(coord2))
				return;
		else
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

		// 0-2, 0-3
		if((shadowCode == 2 || shadowCode == 3) && a == 0)
			ApplyShadowWork(1, order, index1, index2, borderCode, extraLight:extraLight);

		// 0-7, 0-8, 0-9, 0-10
		else if(shadowCode >= 7 && a == 0)
			ApplyShadowWork(1, order, index1, index2, borderCode, extraLight:extraLight);

		// 1-2, 1-3
		else if(a == 1 && (b == 2 || b == 3))
			ApplyShadowWork(2, order, index1, index2, borderCode, extraLight:extraLight);

		// 3-3
		else if(shadowCode == 6 && a == 3)
			ApplyShadowWork(3, order, index1, index2, borderCode, extraLight:extraLight);
		
		// 1-7, 1-8, 1-9, 1-10
		else if(b >= 7 && a == 1)
			ApplyShadowWork(4, order, index1, index2, borderCode, extraLight:extraLight);

		// All directionals linked to a 2 or 3 (e.g. 2-7, 3-7, 2-8, 3-8, etc.)
		else if(b >= 7 && (a == 2 || a == 3))
			ApplyShadowWork(5, order, index1, index2, borderCode, extraLight:extraLight);

		// Almost any combination of directionals
		else if(shadowCode >= 15)
			ApplyShadowWork(5, order, index1, index2, borderCode, extraLight:extraLight);

		// 2-3
		else if(a == 2 && b == 3)
			ApplyShadowWork(6, order, index1, index2, borderCode, extraLight:extraLight);
	}

	// Applies propagation of light 
	public void ApplyShadowWork(int workCode, bool normalOrder, int index1, int index2, byte borderCode, bool extraLight=false){
		// Update border UVs only
		if(workCode == 1){
			if(!normalOrder)
				changed[1] = 1;
		}

		// Propagate normally and sets the correct shadow direction
		else if(workCode == 2){
			if(normalOrder){
				if(!extraLight){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq1.Add(GetCoord(index1));
					visited2.Add(GetCoord(index2));
				}
				else{
					lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe1.Add(GetCoord(index1));
					visitede2.Add(GetCoord(index2));					
				}
			}
			else{
				if(!extraLight){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq2.Add(GetCoord(index2));
					visited1.Add(GetCoord(index1));
				}
				else{
					lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe2.Add(GetCoord(index2));
					visitede1.Add(GetCoord(index1));
				}
			}
		}

		// Find which side to propagate
		else if(workCode == 3){
			if(!extraLight){
				if((lightMap2[index2] & 0x0F) < ((lightMap1[index1] & 0x0F) -1)){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq2.Add(GetCoord(index2));
					visited1.Add(GetCoord(index1));
				}
				else if((lightMap1[index1] & 0x0F) < ((lightMap2[index2] & 0x0F) -1)){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq1.Add(GetCoord(index1));
					visited2.Add(GetCoord(index2));
				}
			}
			else{
				if((lightMap2[index2] & 0xF0) < ((lightMap1[index1] & 0xF0) - 16)){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe2.Add(GetCoord(index2));
					visitede1.Add(GetCoord(index1));
				}
				else if((lightMap1[index1] & 0xF0) < ((lightMap2[index2] & 0xF0) - 16)){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe1.Add(GetCoord(index1));
					visitede2.Add(GetCoord(index2));
				}
			}
		}

		// Propagate to a third chunk or dies because of lack of transmitter
		else if(workCode == 4){
			if(normalOrder){
				if(!extraLight){
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == (shadowMap2[index2] & 0x0F)){
						RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] & 0x0F), GetCoord(index2), borderCode, extraLight:extraLight);
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
					if(GetShadowDirection(borderCode, !normalOrder) == ((shadowMap2[index2] & 0xF0) >> 4)){
						RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)((shadowMap2[index2] & 0xF0) >> 4), GetCoord(index2), borderCode, extraLight:extraLight);
					}
					// If not same direction, propagate
					else{
						// If actually can propagate some light
						if((((lightMap2[index2] & 0xF0) - 16) >> 4) > 0){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe1.Add(GetCoord(index1));
							visitede2.Add(GetCoord(index2));
						}
					}
				}
			}
			else{
				if(!extraLight){
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == (shadowMap1[index1] & 0x0F)){
						RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] & 0x0F), GetCoord(index1), borderCode, extraLight:extraLight);
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
				else{
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == ((shadowMap1[index1] & 0xF0) >> 4)){
						RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)((shadowMap1[index1] & 0xF0) >> 4), GetCoord(index1), borderCode, extraLight:extraLight);
					}
					// If not same direction, propagate
					else{
						// If actually can propagate some light
						if(((lightMap1[index1] - 16) >> 4) > 0){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe2.Add(GetCoord(index2));
							visitede1.Add(GetCoord(index1));
						}
					}
				}
			}
		}

		// If Directionals hit transmitters or directionals hit directionals
		else if(workCode == 5){
			if(normalOrder){
				if(!extraLight){
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap2[index2] & 0x0F)){
						if((lightMap2[index2] & 0x0F) < (lightMap1[index1] & 0x0F) - 1){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
							bfsq2.Add(GetCoord(index2));
						}
					}
					else{
						if((lightMap2[index2] & 0x0F) < (lightMap1[index1] & 0x0F) - 1){
							RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] & 0x0F), GetCoord(index2), borderCode, extraLight:extraLight);
							lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						}
					}
				}
				else{
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap2[index2] >> 4)){
						if(((lightMap2[index2] >> 4)) < (lightMap1[index1] >> 4) - 1){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe2.Add(GetCoord(index2));
						}
					}
					else{
						if((lightMap2[index2] >> 4) < (lightMap1[index1] >> 4) - 1){
							RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] >> 4), GetCoord(index2), borderCode, extraLight:extraLight);
							lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						}
						// If transmitter hits directional in border of extra light, try expand directionals
						else{
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						} 
					}
				}
			}
			else{
				if(!extraLight){
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap1[index1] & 0x0F)){
						if((lightMap1[index1] & 0x0F) < (lightMap2[index2] & 0x0F) - 1){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
							bfsq1.Add(GetCoord(index1));
						}
					}
					else{
						if((lightMap1[index1] >> 4) < (lightMap2[index2] >> 4) - 1){
							RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] & 0x0F), GetCoord(index1), borderCode, extraLight:extraLight);
							lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						}
					}
				}
				else{
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap1[index1] >> 4)){
						if((lightMap1[index1] & 0xF0) < (lightMap2[index2] & 0xF0) - 16){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe1.Add(GetCoord(index1));
						}
					}
					else{
						if((lightMap1[index1] >> 4) < (lightMap2[index2] >> 4) - 1){
							RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] >> 4), GetCoord(index1), borderCode, extraLight:extraLight);
							lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						}
						// If transmitter hits directional in border of extra light, try expand directionals
						else{
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						} 
					}			
				}
			}
		}

		// If Sunlight hits local propagation in neighbor
		else if(workCode == 6){
			if(normalOrder){
				if(!extraLight){
					if((lightMap1[index1] & 0x0F) < ((lightMap2[index2] & 0x0F) -1)){
						lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
						shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq1.Add(GetCoord(index1));
					}	
				}
				else{
					if((lightMap1[index1] >> 4) < ((lightMap2[index2] >> 4) - 1)){
						lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
						shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
						bfsqe1.Add(GetCoord(index1));
					}	
				}
			}
			else{
				if(!extraLight){
					if((lightMap2[index2] & 0x0F) < ((lightMap1[index1] & 0x0F) -1)){
						lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
						shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq2.Add(GetCoord(index2));
					}	
				}
				else{
					if((lightMap2[index2] >> 4) < ((lightMap1[index1] >> 4) - 1)){
						lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
						shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
						bfsqe2.Add(GetCoord(index2));
					}	
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
	public void ScanSurroundings(int3 c, byte currentLight, bool normalOrder, int side, NativeArray<byte> selectedMap, NativeArray<byte> otherMap, NativeArray<byte> selectedShadow, NativeArray<byte> otherShadow, NativeList<int3> bfsq, NativeList<int3> otherBfsq, NativeHashSet<int3> visited, NativeHashSet<int3> otherVisited, byte borderCode, bool extraLight=false){
		if(currentLight == 1)
			return;

		int3 aux;
		int index;
		byte sideShadow;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 1 && side == 1) || (borderCode == 0 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 0 && side == 1) || (borderCode == 1 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}
			}
		}

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 3 && side == 1) || (borderCode == 4 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 4 && side == 1) || (borderCode == 3 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}			
			}
		}	

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}
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