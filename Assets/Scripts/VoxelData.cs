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
	private bool[] ceilingMap;
	private ChunkPos pos;

	private static ChunkLoader cl; 

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

	public VoxelData(ChunkPos pos){
		this.data = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
		this.pos = pos;
	}

	public VoxelData(ushort[] data, ChunkPos pos){
		this.data = (ushort[])data.Clone();
		this.heightMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];
		this.pos = pos;
		CalculateHeightMap();
	}

	public void Destroy(){
		this.data = null;
		this.heightMap = null;
		this.shadowMap = null;
		this.lightMap = null;
		this.ceilingMap = null;
		this.renderMap = null;
	}

	public static void SetChunkLoader(ChunkLoader reference){
		VoxelData.cl = reference;
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
	public static ushort PropagateLight(VoxelData a, VoxelMetadata aMetadata, VoxelData b, VoxelMetadata bMetadata, byte borderCode){
		NativeArray<byte> lightMap1 = NativeTools.CopyToNative(a.GetLightMap(aMetadata));
		NativeArray<byte> lightMap2 = NativeTools.CopyToNative(b.GetLightMap(bMetadata));
		NativeArray<byte> shadowMap1 = NativeTools.CopyToNative(a.GetShadowMap());
		NativeArray<byte> shadowMap2 = NativeTools.CopyToNative(b.GetShadowMap());

		NativeList<int3> bfsq1 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsq2 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsqe1 = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> bfsqe2 = new NativeList<int3>(0, Allocator.TempJob);
		NativeParallelHashSet<int3> visited1 = new NativeParallelHashSet<int3>(0, Allocator.TempJob);
		NativeParallelHashSet<int3> visited2 = new NativeParallelHashSet<int3>(0, Allocator.TempJob);
		NativeParallelHashSet<int3> visitede1 = new NativeParallelHashSet<int3>(0, Allocator.TempJob);
		NativeParallelHashSet<int3> visitede2 = new NativeParallelHashSet<int3>(0, Allocator.TempJob);
		NativeList<int4> aux = new NativeList<int4>(0, Allocator.TempJob);
		NativeParallelHashSet<int4> hashAux = new NativeParallelHashSet<int4>(0, Allocator.TempJob);
		NativeArray<byte> changed = new NativeArray<byte>(new byte[]{0, 0, 0, 0}, Allocator.TempJob);
		ushort updateFlag = 0;

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
			cpos = a.pos,
			changed = changed
		};

        job = clpJob.Schedule();
        job.Complete();

        a.SetLightMap(NativeTools.CopyToManaged(lightMap1));
        b.SetLightMap(NativeTools.CopyToManaged(lightMap2));
        a.SetShadowMap(NativeTools.CopyToManaged(shadowMap1));
        b.SetShadowMap(NativeTools.CopyToManaged(shadowMap2));
        
        updateFlag += changed[0];
        updateFlag += (ushort)(changed[1] << 1);
        updateFlag += (ushort)(changed[2] << 2);
        updateFlag += (ushort)(changed[3] << 3);

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

		bool isStandalone = true;
		ChunkPos cachedPos;

		NativeArray<byte> lightMap;
		NativeArray<byte> shadowMap;
		NativeArray<byte> memoryLightMap;
		NativeArray<byte> memoryShadowMap;
		NativeList<int4> lightSources = new NativeList<int4>(0, Allocator.TempJob);
		NativeArray<byte> heightMap = NativeTools.CopyToNative(this.heightMap);
		NativeArray<byte> changed = new NativeArray<byte>(new byte[]{0}, Allocator.TempJob);
		NativeArray<ushort> states = NativeTools.CopyToNative(metadata.GetStateData());
		NativeArray<bool> ceilingMap = NativeTools.CopyToNative(this.ceilingMap);
		NativeArray<bool> neighborCeilingMap;

		if(this.shadowMap == null){
			shadowMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
			memoryShadowMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		}
		else{
			shadowMap = NativeTools.CopyToNative(this.shadowMap);
			memoryShadowMap = NativeTools.CopyToNative(this.shadowMap);
		}
		if(this.lightMap == null){
			lightMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
			memoryLightMap = new NativeArray<byte>(0, Allocator.TempJob);
		}
		else{
			lightMap = NativeTools.CopyToNative(this.lightMap);
			memoryLightMap = NativeTools.CopyToNative(this.lightMap);
		}

		// Check if should be calculated as standalone chunk
		
		if(this.pos.y < Chunk.chunkMaxY){
			cachedPos = new ChunkPos(this.pos.x, this.pos.z, this.pos.y+1);

			if(cl.Contains(cachedPos)){
				if(cl.Get(cachedPos).data.ShadowMapIsSet()){
					isStandalone = false;
					neighborCeilingMap = NativeTools.CopyToNative(cl.Get(cachedPos).data.GetCeilingMap());
				}
				else{
					neighborCeilingMap = new NativeArray<bool>(0, Allocator.TempJob);
				}
			}
			else{
				neighborCeilingMap = new NativeArray<bool>(0, Allocator.TempJob);
			}
		}
		else{
			neighborCeilingMap = new NativeArray<bool>(0, Allocator.TempJob);
		}

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
			isStandalone = isStandalone,
			cpos = this.pos,
			ceilingMap = ceilingMap,
			neighborCeilingMap = neighborCeilingMap
		};

        job = csmJob.Schedule();
        job.Complete();

		NativeList<int3> bfsq = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int4> bfsqExtra = new NativeList<int4>(0, Allocator.TempJob);
		NativeList<byte5> directionalList = new NativeList<byte5>(0, Allocator.TempJob);
		NativeList<byte5> bfsqDir = new NativeList<byte5>(0, Allocator.TempJob);
		NativeParallelHashSet<int3> visited = new NativeParallelHashSet<int3>(0, Allocator.TempJob);
		NativeList<int4> auxLightSources = new NativeList<int4>(0, Allocator.TempJob);
		NativeList<byte5> auxDirectionals = new NativeList<byte5>(0, Allocator.TempJob);

		// LIGHTMAPPING =========================================================
		CalculateLightMapJob clmJob = new CalculateLightMapJob{
			lightMap = lightMap,
			shadowMap = shadowMap,
			memoryLightMap = memoryLightMap,
			memoryShadowMap = memoryShadowMap,
			lightSources = lightSources,
			heightMap = heightMap,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			bfsq = bfsq,
			bfsqExtra = bfsqExtra,
			bfsqDir = bfsqDir,
			visited = visited,
			directionalList = directionalList,
			auxLightSources = auxLightSources,
			auxDirectionals = auxDirectionals,
			changed = changed,
			cpos = this.pos
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
        bfsqDir.Dispose();
        visited.Dispose();
        lightSources.Dispose();
        lightMap.Dispose();
        heightMap.Dispose();
        shadowMap.Dispose();
        changed.Dispose();
        ceilingMap.Dispose();
        neighborCeilingMap.Dispose();
        memoryLightMap.Dispose();
        memoryShadowMap.Dispose();
        directionalList.Dispose();
        auxDirectionals.Dispose();
        auxLightSources.Dispose();
	}

	public void CalculateHeightMap(){
		if(this.data == null)
			return;
		if(this.heightMap == null)
			this.heightMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];
		if(this.renderMap == null)
			this.renderMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];
		if(this.ceilingMap == null)
			this.ceilingMap = new bool[Chunk.chunkWidth*Chunk.chunkWidth];

		NativeArray<ushort> data = NativeTools.CopyToNative(this.data);
		NativeArray<byte> hMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth, Allocator.TempJob);
		NativeArray<byte> rMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth, Allocator.TempJob);
		NativeArray<bool> cMap = new NativeArray<bool>(Chunk.chunkWidth*Chunk.chunkWidth, Allocator.TempJob);
		JobHandle job;

		GenerateHeightMapJob ghmj = new GenerateHeightMapJob{
			data = data,
			heightMap = hMap,
			renderMap = rMap,
			ceilingMap = cMap,
			blockInvisible = BlockEncyclopediaECS.blockInvisible,
			objectInvisible = BlockEncyclopediaECS.objectInvisible,
			blockAffectLight = BlockEncyclopediaECS.blockAffectLight,
			objectAffectLight = BlockEncyclopediaECS.objectAffectLight
		};

		job = ghmj.Schedule();
		job.Complete();

		this.heightMap = NativeTools.CopyToManaged(hMap);
		this.renderMap = NativeTools.CopyToManaged(rMap);
		this.ceilingMap = NativeTools.CopyToManaged(cMap);

		hMap.Dispose();
		rMap.Dispose();
		cMap.Dispose();
		data.Dispose();
	}


	public void CalculateHeightMap(int x, int z){
		ushort blockCode;
		bool found, foundRender;
		byte biggest = 0;
		bool xm = false;
		bool xp = false;
		bool zm = false;
		bool zp = false;

		found = false;
		foundRender = false;
		byte newRenderValue = 0;

		for(int y=Chunk.chunkDepth-1; y >= 0; y--){
			blockCode = this.data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

			// If is a block
			if(blockCode <= ushort.MaxValue/2){
				if(BlockEncyclopediaECS.blockAffectLight[blockCode] && !found){
					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
					this.ceilingMap[x*Chunk.chunkWidth+z] = true;
					found = true;
				}

				if(!BlockEncyclopediaECS.blockInvisible[blockCode] && !foundRender){
					if(y < Chunk.chunkDepth-1)
						newRenderValue = (byte)(y+1);
					else
						newRenderValue = (byte)(Chunk.chunkDepth-1);

					foundRender = true;
				}
			}
			// If it's an object
			else{
				if(BlockEncyclopediaECS.objectAffectLight[ushort.MaxValue - blockCode] && !found){
					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
					this.ceilingMap[x*Chunk.chunkWidth+z] = true;
					found =  true;
				}

				if(!BlockEncyclopediaECS.objectInvisible[ushort.MaxValue - blockCode] && !foundRender){
					if(y < Chunk.chunkDepth-1)
						newRenderValue = (byte)(y+1);
					else
						newRenderValue = (byte)(Chunk.chunkDepth-1);

					foundRender = true;
				}	
			}
		}

		if(!found){
			this.heightMap[x*Chunk.chunkWidth+z] = 0;
			this.ceilingMap[x*Chunk.chunkWidth+z] = false;
		}

		if(foundRender){
			this.renderMap[x*Chunk.chunkWidth+z] = newRenderValue;

			if(x > 0){
				if(this.renderMap[(x-1)*Chunk.chunkWidth+z] > biggest)
					biggest = this.renderMap[(x-1)*Chunk.chunkWidth+z];

				xm = true;
			}
			if(x < Chunk.chunkWidth-1){
				if(this.renderMap[(x+1)*Chunk.chunkWidth+z] > biggest)
					biggest = this.renderMap[(x+1)*Chunk.chunkWidth+z];

				xp = true;
			}
			if(z > 0){
				if(this.renderMap[x*Chunk.chunkWidth+(z-1)] > biggest)
					biggest = this.renderMap[x*Chunk.chunkWidth+(z-1)];

				zm = true;
			}
			if(z < Chunk.chunkWidth-1){
				if(this.renderMap[x*Chunk.chunkWidth+(z+1)] > biggest)
					biggest = this.renderMap[x*Chunk.chunkWidth+(z+1)];

				zp = true;
			}

			// If new value is the highest
			if(this.renderMap[x*Chunk.chunkWidth+z] >= biggest)
				biggest = this.renderMap[x*Chunk.chunkWidth+z];

			this.renderMap[x*Chunk.chunkWidth+z] = biggest;

			if(xm)
				this.renderMap[(x-1)*Chunk.chunkWidth+z] = biggest;
			if(xp)
				this.renderMap[(x+1)*Chunk.chunkWidth+z] = biggest;
			if(zm)
				this.renderMap[x*Chunk.chunkWidth+(z-1)] = biggest;
			if(zp)
				this.renderMap[x*Chunk.chunkWidth+(z+1)] = biggest;
		}
	}

	public ushort GetHeight(byte x, byte z){
		if(x < 0 || z < 0 || x > Chunk.chunkWidth || z > Chunk.chunkWidth)
			return ushort.MaxValue;
		else
			return this.heightMap[x*Chunk.chunkWidth + z];
	}

	public ushort GetRender(byte x, byte z){
		if(x < 0 || z < 0 || x > Chunk.chunkWidth || z > Chunk.chunkWidth)
			return ushort.MaxValue;
		else
			return this.renderMap[x*Chunk.chunkWidth + z];
	}

	public byte GetPropagationFlag(){
		byte flag = this.PROPAGATE_LIGHT_FLAG;
		this.PROPAGATE_LIGHT_FLAG = 0;
		return flag;
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

	public bool[] GetCeilingMap(){
		if(this.ceilingMap == null)
			this.CalculateHeightMap();

		return this.ceilingMap;
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

	public byte[] GetHeightMap(){
		return this.heightMap;
	}

	public ushort[] GetData(){
		return this.data;
	}

	public bool ShadowMapIsSet(){
		return this.shadowMap != null;
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