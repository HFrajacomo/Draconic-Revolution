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
	[ReadOnly]
	public NativeArray<byte> renderMap;
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
		int3 c;
		int ii;
		byte renderMapTop;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int z=0; z < Chunk.chunkWidth; z++){

				if(renderMap[x*Chunk.chunkWidth+z] < byte.MaxValue)
					renderMapTop = (byte)(renderMap[x*Chunk.chunkWidth+z]+1);
				else
					renderMapTop = byte.MaxValue;

				for(int y=renderMapTop; y >= 0; y--){
			    	block = blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

		    		if(CheckTransparentOrInvisible(block)){
				    	for(int i=0; i<6; i++){
				    		// Chunk Border and floor culling here! ----------
				    		c = GetCoords(x, y, z, i);
				    		ii = InvertDir(i);
				    		hp = GetNeighborHP(x, y, z, i);
				    		neighborBlock = GetNeighbor(x, y, z, i);

				    		if(neighborBlock == 0)
				    			continue;

				    		if(neighborBlock <= ushort.MaxValue/2){
				    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[neighborBlock])
				    				continue;
				    		}
				    		else{
				    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[neighborBlock])
				    				continue;			    			
				    		}
				    		
			    			// If Corner
				    		if(c.x >= Chunk.chunkWidth || c.x < 0 || c.z >= Chunk.chunkWidth || c.z < 0)
				    			break;

			    			if((c.x == 0 || c.x == Chunk.chunkWidth-1) && (c.z == 0 || c.z == Chunk.chunkWidth-1) && (i != 4 && i != 5))
			    				continue;
								
				    		if((c.x == 0 && (ii == 3)) || (c.z == 0 && (ii == 2)))
				    			continue;

				    		if((c.x == Chunk.chunkWidth-1 && (ii == 1)) || (c.z == Chunk.chunkWidth-1 && (ii == 0)))
				    			continue;

				    		if(c.y == 0 && ii == 5)
				    			continue;

				    		if(c.y == Chunk.chunkDepth-1 && ii == 4){
				    			continue;
				    		}

			    			decalCode = GetDecalStage(neighborBlock, hp);

			    			if(decalCode >= 0)
			    				BuildDecal(c.x, c.y, c.z, ii, decalCode);

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

    // Gets neighbor hp
	private ushort GetNeighborHP(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		} 

		return hpdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	private int3 GetCoords(int x, int y, int z, int dir){
		return new int3(x, y, z) + VoxelData.offsets[dir];
	}

    private int InvertDir(int i){
    	if(i == 0)
    		return 2;
    	if(i == 1)
    		return 3;
    	if(i == 2)
    		return 0;
    	if(i == 3)
    		return 1;
    	if(i == 4)
    		return 5;
    	if(i == 5)
    		return 4;
    	return 0;
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