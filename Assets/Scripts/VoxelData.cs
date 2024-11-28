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

	public void SetCell(int x, int y, int z, ushort blockCode){
		data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;
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

	public byte[] GetHeightMap(){
		return this.heightMap;
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