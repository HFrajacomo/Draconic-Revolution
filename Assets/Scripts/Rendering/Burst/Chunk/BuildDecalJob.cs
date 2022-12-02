using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;

[BurstCompile]
public struct BuildDecalJob : IJob{
	[ReadOnly]
	public ChunkPos pos;

	[ReadOnly]
	public NativeArray<ushort> blockdata;
	public NativeList<Vector3> verts;
	public NativeList<Vector2> UV; 
	public NativeList<int> triangles;
	[ReadOnly]
	public NativeArray<ushort> hpdata;
	[ReadOnly]
	public NativeArray<ushort> blockHP;
	[ReadOnly]
	public NativeArray<ushort> objectHP;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<byte> blockTransparent;
	[ReadOnly]
	public NativeArray<byte> objectTransparent;
	public NativeArray<Vector3> cacheCubeVerts;

	public void Execute(){
		ushort block;
		ushort hp;
		int decalCode;
		ushort neighborBlock;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int z=0; z < Chunk.chunkWidth; z++){

			    	for(int i=0; i<6; i++){
			    		block = blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
			    		neighborBlock = GetNeighbor(x, y, z, i);
			    		hp = hpdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
			    		
			    		// Chunk Border and floor culling here! ----------
			    		
			    		if((x == 0 && 3 == i) || (z == 0 && 2 == i)){
			    			continue;
			    		}
			    		if((x == Chunk.chunkWidth-1 && 1 == i) || (z == Chunk.chunkWidth-1 && 0 == i)){
			    			continue;
			    		}
			    		if(y == 0 && 5 == i){
			    			continue;
			    		}

			    		if(block <= ushort.MaxValue/2){
			    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[block])
			    				continue;
			    		}
			    		else{
			    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[block])
			    				continue;			    			
			    		}

			    		if(CheckTransparentOrInvisible(neighborBlock)){
			    			decalCode = GetDecalStage(block, hp);

			    			if(decalCode >= 0)
			    				BuildDecal(x, y, z, i, decalCode);
			    		}

			    	}
				}
			}
		}
	}

	public void BuildDecal(int x, int y, int z, int dir, int decal){
		FaceVertices(cacheCubeVerts, dir, 0.5f, GetDecalPosition(x, y+(this.pos.y*Chunk.chunkDepth), z, dir));
		verts.AddRange(cacheCubeVerts);
		int vCount = verts.Length;

		FillUV(decal);
		
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +1);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4 +3); 
	}

	public void FillUV(int decal){
		float xSize = 1 / (float)Constants.DECAL_STAGE_SIZE;
		float xMin = (float)decal * xSize;

		UV.Add(new Vector2(xMin, 0));
		UV.Add(new Vector2(xMin, 1));
		UV.Add(new Vector2(xMin + xSize, 1));
		UV.Add(new Vector2(xMin + xSize, 0));
	}

	// Cube Mesh Data get verts
	public void FaceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	public bool CheckTransparentOrInvisible(ushort block){
		if(block <= ushort.MaxValue/2)
			return Boolean(blockTransparent[block]) || blockInvisible[block];
		else
			return objectInvisible[ushort.MaxValue - block] || Boolean(objectTransparent[ushort.MaxValue - block]);
	}

	public int GetDecalStage(ushort block, ushort hp){
		float hpPercentage;

		if(block <= ushort.MaxValue/2)
			hpPercentage = (float)hp / (float)blockHP[block];
		else
			hpPercentage = (float)hp / (float)objectHP[ushort.MaxValue - block];

	    for(int i=0; i < Constants.DECAL_STAGE_SIZE; i++){
			if(hpPercentage <= Constants.DECAL_STAGE_PERCENTAGE[i])
				return (Constants.DECAL_STAGE_SIZE - 1) - i;
		}

		return -1;
	}

	// Gets neighbor element
	private ushort GetNeighbor(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		}

		return blockdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	private bool Boolean(byte b){
		if(b == 0)
			return false;
		return true;
	}

	public Vector3 GetDecalPosition(float x, float y, float z, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, Constants.DECAL_OFFSET);
		else if(dir == 1)
			normal = new Vector3(Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -Constants.DECAL_OFFSET);
		else if(dir == 3)
			normal = new Vector3(-Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, Constants.DECAL_OFFSET, 0);
		else
			normal = new Vector3(0, -Constants.DECAL_OFFSET, 0);

		return new Vector3(x + normal.x, y + normal.y, z + normal.z);
	}
}