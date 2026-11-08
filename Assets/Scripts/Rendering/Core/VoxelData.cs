using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

public class VoxelData {
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

	private byte PROPAGATE_LIGHT_FLAG = 0; // 0 = no, 1 = xm, 2 = xp, 4 = zm, 8 = zp, 16 = ym, 32 = yp

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

	private VoxelData(ChunkPos pos, ushort[] ownedData){
		this.data = ownedData;
		this.heightMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth];
		this.pos = pos;
		CalculateHeightMap();
	}

	// Creates a VoxelData that uses the given array instead of a copy of it
	public static VoxelData FromOwnedArray(ushort[] data, ChunkPos pos){
		return new VoxelData(pos, data);
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
		// Light Maps must exist before the scratch buffers are prepared, since calculating them also uses scratch buffers
		byte[] managedLight1 = a.GetLightMap(aMetadata);
		byte[] managedLight2 = b.GetLightMap(bMetadata);

		LightingScratch.PreparePropagation();

		PinnedArray<byte> lightMap1 = PinnedArray<byte>.Pin(managedLight1);
		PinnedArray<byte> lightMap2 = PinnedArray<byte>.Pin(managedLight2);
		PinnedArray<byte> shadowMap1 = PinnedArray<byte>.Pin(a.GetShadowMap());
		PinnedArray<byte> shadowMap2 = PinnedArray<byte>.Pin(b.GetShadowMap());
		NativeArray<byte> changed = LightingScratch.changed;
		ushort updateFlag = 0;

		CalculateLightPropagationJob clpJob = new CalculateLightPropagationJob{
			lightMap1 = lightMap1.array,
			lightMap2 = lightMap2.array,
			shadowMap1 = shadowMap1.array,
			shadowMap2 = shadowMap2.array,
			bfsq1 = LightingScratch.bfsq1,
			bfsq2 = LightingScratch.bfsq2,
			visited1 = LightingScratch.visited1,
			visited2 = LightingScratch.visited2,
			visitede1 = LightingScratch.visitede1,
			visitede2 = LightingScratch.visitede2,
			bfsqe1 = LightingScratch.bfsqe1,
			bfsqe2 = LightingScratch.bfsqe2,
			aux = LightingScratch.aux,
			hashAux = LightingScratch.hashAux,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			borderCode = borderCode,
			cpos = a.pos,
			changed = changed
		};

		clpJob.Run();

		updateFlag += changed[0];
		updateFlag += (ushort)(changed[1] << 1);
		updateFlag += (ushort)(changed[2] << 2);
		updateFlag += (ushort)(changed[3] << 3);

		lightMap1.Release();
		lightMap2.Release();
		shadowMap1.Release();
		shadowMap2.Release();

		return updateFlag;
	}

	/*
	Burst Compiled
	*/
	public void CalculateLightMap(VoxelMetadata metadata){
		if(this.heightMap == null || this.ceilingMap == null)
			CalculateHeightMap();

		bool isStandalone = true;
		bool[] neighborCeiling = null;
		ChunkPos cachedPos;

		// Check if should be calculated as standalone chunk
		if(this.pos.y < Chunk.chunkMaxY){
			cachedPos = new ChunkPos(this.pos.x, this.pos.z, this.pos.y+1);

			if(cl.Contains(cachedPos)){
				if(cl.Get(cachedPos).data.ShadowMapIsSet()){
					isStandalone = false;
					neighborCeiling = cl.Get(cachedPos).data.GetCeilingMap();
				}
			}
		}

		// Light and Shadow maps are calculated in-place
		if(this.lightMap == null)
			this.lightMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth];
		if(this.shadowMap == null)
			this.shadowMap = new byte[Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth];

		LightingScratch.PrepareLightMap();
		metadata.CopyStateTo(LightingScratch.states);

		PinnedArray<byte> lightMap = PinnedArray<byte>.Pin(this.lightMap);
		PinnedArray<byte> shadowMap = PinnedArray<byte>.Pin(this.shadowMap);
		PinnedArray<byte> heightMap = PinnedArray<byte>.Pin(this.heightMap);
		PinnedArray<bool> ceilingMap = PinnedArray<bool>.Pin(this.ceilingMap);
		PinnedArray<ushort> blockData = PinnedArray<ushort>.Pin(this.data);
		PinnedArray<bool> neighborCeilingMap = new PinnedArray<bool>();

		if(!isStandalone)
			neighborCeilingMap = PinnedArray<bool>.Pin(neighborCeiling);

		// SHADOWMAPPING =========================================================

		CalculateShadowMapJob csmJob = new CalculateShadowMapJob{
			shadowMap = shadowMap.array,
			lightMap = lightMap.array,
			lightSources = LightingScratch.lightSources,
			heightMap = heightMap.array,
			data = blockData.array,
			states = LightingScratch.states,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = BlockEncyclopediaECS.objectTransparent,
			isTransparentBlock = BlockEncyclopediaECS.blockTransparent,
			blockLuminosity = BlockEncyclopediaECS.blockLuminosity,
			objectLuminosity = BlockEncyclopediaECS.objectLuminosity,
			isStandalone = isStandalone,
			cpos = this.pos,
			ceilingMap = ceilingMap.array,
			neighborCeilingMap = isStandalone ? LightingScratch.emptyBool : neighborCeilingMap.array
		};

		csmJob.Run();

		// LIGHTMAPPING =========================================================
		CalculateLightMapJob clmJob = new CalculateLightMapJob{
			lightMap = lightMap.array,
			shadowMap = shadowMap.array,
			lightSources = LightingScratch.lightSources,
			heightMap = heightMap.array,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			bfsq = LightingScratch.bfsq,
			bfsqExtra = LightingScratch.bfsqExtra,
			bfsqDir = LightingScratch.bfsqDir,
			visited = LightingScratch.visited,
			directionalList = LightingScratch.directionalList,
			auxLightSources = LightingScratch.auxLightSources,
			auxDirectionals = LightingScratch.auxDirectionals,
			cpos = this.pos
		};

		clmJob.Run();

		lightMap.Release();
		shadowMap.Release();
		heightMap.Release();
		ceilingMap.Release();
		blockData.Release();
		neighborCeilingMap.Release();
	}

	public void VerifyPropagation(ChunkLoader cl){
		VoxelData xm = cl.Get(new ChunkPos(this.pos.x-1, this.pos.z, this.pos.y)).data;
		VoxelData xp = cl.Get(new ChunkPos(this.pos.x+1, this.pos.z, this.pos.y)).data;
		VoxelData zm = cl.Get(new ChunkPos(this.pos.x, this.pos.z-1, this.pos.y)).data;
		VoxelData zp = cl.Get(new ChunkPos(this.pos.x, this.pos.z+1, this.pos.y)).data;
		VoxelData vp = null;
		VoxelData vm = null;
		ChunkPos verticalPos;

		verticalPos = new ChunkPos(this.pos.x, this.pos.z, this.pos.y+1);

		if(cl.Contains(verticalPos)){
			vp = cl.Get(verticalPos).data;
		}
		else{
			verticalPos = new ChunkPos(this.pos.x, this.pos.z, this.pos.y-1);

			if(cl.Contains(verticalPos))
				vm = cl.Get(verticalPos).data;
		}

		LightingScratch.PrepareVerification();

		PinnedArray<byte> lightdata = PinnedArray<byte>.Pin(this.lightMap);
		PinnedArray<byte> shadowdata = PinnedArray<byte>.Pin(this.shadowMap);
		PinnedArray<byte> xmlight = PinnedArray<byte>.Pin(xm.GetLightMap());
		PinnedArray<byte> xmshadow = PinnedArray<byte>.Pin(xm.GetShadowMap());
		PinnedArray<byte> xplight = PinnedArray<byte>.Pin(xp.GetLightMap());
		PinnedArray<byte> xpshadow = PinnedArray<byte>.Pin(xp.GetShadowMap());
		PinnedArray<byte> zmlight = PinnedArray<byte>.Pin(zm.GetLightMap());
		PinnedArray<byte> zmshadow = PinnedArray<byte>.Pin(zm.GetShadowMap());
		PinnedArray<byte> zplight = PinnedArray<byte>.Pin(zp.GetLightMap());
		PinnedArray<byte> zpshadow = PinnedArray<byte>.Pin(zp.GetShadowMap());
		PinnedArray<byte> vplight = new PinnedArray<byte>();
		PinnedArray<byte> vpshadow = new PinnedArray<byte>();
		PinnedArray<byte> vmlight = new PinnedArray<byte>();
		PinnedArray<byte> vmshadow = new PinnedArray<byte>();

		if(vp != null){
			vplight = PinnedArray<byte>.Pin(vp.GetLightMap());
			vpshadow = PinnedArray<byte>.Pin(vp.GetShadowMap());
		}
		else if(vm != null){
			vmlight = PinnedArray<byte>.Pin(vm.GetLightMap());
			vmshadow = PinnedArray<byte>.Pin(vm.GetShadowMap());
		}

		VerifyPropagationJob vpJob = new VerifyPropagationJob{
			lightdata = lightdata.array,
			shadowdata = shadowdata.array,
			xmlight = xmlight.array,
			xplight = xplight.array,
			xmshadow = xmshadow.array,
			xpshadow = xpshadow.array,
			zmlight = zmlight.array,
			zplight = zplight.array,
			zmshadow = zmshadow.array,
			zpshadow = zpshadow.array,
			vmlight = vm != null ? vmlight.array : LightingScratch.emptyByte,
			vplight = vp != null ? vplight.array : LightingScratch.emptyByte,
			vmshadow = vm != null ? vmshadow.array : LightingScratch.emptyByte,
			vpshadow = vp != null ? vpshadow.array : LightingScratch.emptyByte,
			changed = LightingScratch.changed
		};

		vpJob.Run();

		this.PROPAGATE_LIGHT_FLAG = LightingScratch.changed[0];

		lightdata.Release();
		shadowdata.Release();
		xmlight.Release();
		xmshadow.Release();
		xplight.Release();
		xpshadow.Release();
		zmlight.Release();
		zmshadow.Release();
		zplight.Release();
		zpshadow.Release();
		vplight.Release();
		vpshadow.Release();
		vmlight.Release();
		vmshadow.Release();
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

		// Every entry of the maps is written by the job, so they are calculated in-place
		PinnedArray<ushort> data = PinnedArray<ushort>.Pin(this.data);
		PinnedArray<byte> hMap = PinnedArray<byte>.Pin(this.heightMap);
		PinnedArray<byte> rMap = PinnedArray<byte>.Pin(this.renderMap);
		PinnedArray<bool> cMap = PinnedArray<bool>.Pin(this.ceilingMap);

		GenerateHeightMapJob ghmj = new GenerateHeightMapJob{
			data = data.array,
			heightMap = hMap.array,
			renderMap = rMap.array,
			ceilingMap = cMap.array,
			blockInvisible = BlockEncyclopediaECS.blockInvisible,
			objectInvisible = BlockEncyclopediaECS.objectInvisible,
			blockAffectLight = BlockEncyclopediaECS.blockAffectLight,
			objectAffectLight = BlockEncyclopediaECS.objectAffectLight
		};

		ghmj.Run();

		hMap.Release();
		rMap.Release();
		cMap.Release();
		data.Release();
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

	public byte[] GetLightMap(){
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
		foreach(ushort item in data){
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