using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/*
Turns the MeshRecords written by BuildChunkJob into mesh data.
Slices are read in order, so the mesh is the same as building the chunk in a single pass
*/
[BurstCompile]
public struct MergeChunkMeshJob : IJob{
	// How far liquid faces touching a non-liquid transparent block are pulled into the liquid
	private const float LIQUID_INSET = 0.004f;

	[ReadOnly]
	public ChunkPos pos;
	[ReadOnly]
	public NativeStream.Reader records;
	[ReadOnly]
	public NativeArray<ushort> state;
	[ReadOnly]
	public NativeArray<int3> blockTiles;

	// Objects
	public NativeList<int3> loadAssetList;
	public NativeBitArray assetCoordinates; // Indexed by voxel index

	// Collidable faces, indexed by direction*VOXEL_COUNT + voxel index. Merged into the collider by GreedyColliderJob
	public NativeBitArray colliderFaces;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector3> UVs;
	public NativeList<Vector3> lightUV;
	public NativeList<Vector3> normals;
	public NativeList<Vector4> tangents;

	// Decals
	public NativeList<Vector3> decalVerts;
	public NativeList<Vector3> decalNormals;
	public NativeList<Vector2> decalUVs;
	public NativeList<int> decalTris;

	// Render Thread Triangles
	public NativeList<int> normalTris;
	public NativeList<int> specularTris;
	public NativeList<int> liquidTris;
	public NativeList<int> leavesTris;
	public NativeList<int> iceTris;
	public NativeList<int> lavaTris;

	// Cache
	public NativeArray<Vector3> cacheCubeVert;
	public NativeArray<Vector3> cacheCubeUV;
	public NativeArray<Vector3> extraUV;
	public NativeArray<Vector3> cacheCubeNormal;
	public NativeArray<Vector4> cacheCubeTangent;


	public void Execute(){
		int count;
		MeshRecord record;

		Reserve(records.Count());

		for(int i=0; i < records.ForEachCount; i++){
			count = records.BeginForEachIndex(i);

			for(int j=0; j < count; j++){
				record = records.Read<MeshRecord>();

				// Objects are only added once, the first time one of their faces is visible
				if(record.isObject){
					int index = record.x*Chunk.chunkWidth*Chunk.chunkDepth+record.y*Chunk.chunkWidth+record.z;

					if(assetCoordinates.IsSet(index))
						continue;

					assetCoordinates.Set(index, true);
				}

				if(record.kind == MeshRecord.FACE)
					WriteFace(record);
				else if(record.kind == MeshRecord.DECAL)
					BuildDecal(record.x, record.y, record.z, record.dir, record.decal);
				else
					loadAssetList.Add(new int3(record.x, record.y, record.z));
			}

			records.EndForEachIndex();
		}
	}

	// Makes sure the vertex lists can take every record without growing
	private void Reserve(int recordCount){
		int needed = verts.Length + recordCount*4;

		if(verts.Capacity < needed){
			verts.SetCapacity(needed);
			UVs.SetCapacity(needed);
			lightUV.SetCapacity(needed);
			normals.SetCapacity(needed);
			tangents.SetCapacity(needed);
		}
	}

	private void WriteFace(MeshRecord record){
		int x = record.x;
		int y = record.y;
		int z = record.z;
		int dir = record.dir;
		ShaderIndex renderThread = (ShaderIndex)record.material;
		Vector3 position = new Vector3(x, y+(pos.y*Chunk.chunkDepth), z);

		if(renderThread == ShaderIndex.WATER || renderThread == ShaderIndex.LAVA){
			BuildChunkJob.VertsByState(cacheCubeVert, dir, state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], position);
			LiquidTexture(cacheCubeUV, x, z);
		}
		else{
			BuildChunkJob.faceVertices(cacheCubeVert, dir, 0.5f, position);
			AddTexture(cacheCubeUV, dir, record.block);
		}

		if(renderThread == ShaderIndex.OPAQUE || renderThread == ShaderIndex.ICE)
			colliderFaces.Set(dir*Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth + x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z, true);

		CalculateNormal(cacheCubeNormal, dir);

		// Pulled against its normal, into the liquid, so it doesn't z-fight with the face of the transparent block it touches
		if(record.inset){
			for(int i=0; i < 4; i++)
				cacheCubeVert[i] = cacheCubeVert[i] - cacheCubeNormal[i]*LIQUID_INSET;
		}

		verts.AddRange(cacheCubeVert);
		int vCount = verts.Length;

		UVs.AddRange(cacheCubeUV);

		for(int i=0; i < 4; i++)
			extraUV[i] = new Vector3(MeshRecord.Unpack(record.natural, i), MeshRecord.Unpack(record.extra, i), record.dampness);
		lightUV.AddRange(extraUV);

		normals.AddRange(cacheCubeNormal);

		CalculateTangent(cacheCubeTangent, dir);
		tangents.AddRange(cacheCubeTangent);

		switch(renderThread){
			case ShaderIndex.OPAQUE:
				AddTriangles(normalTris, vCount);
				break;
			case ShaderIndex.SPECULAR:
				AddTriangles(specularTris, vCount);
				break;
			case ShaderIndex.WATER:
				AddTriangles(liquidTris, vCount);
				break;
			case ShaderIndex.LEAVES:
				AddTriangles(leavesTris, vCount);
				break;
			case ShaderIndex.ICE:
				AddTriangles(iceTris, vCount);
				break;
			default:
				AddTriangles(lavaTris, vCount);
				break;
		}
	}

	private void AddTriangles(NativeList<int> tris, int vCount){
    	tris.Add(vCount -4);
    	tris.Add(vCount -4 +1);
    	tris.Add(vCount -4 +2);
    	tris.Add(vCount -4);
    	tris.Add(vCount -4 +2);
    	tris.Add(vCount -4 +3);
	}

	private Vector3 GetDecalPosition(float x, float y, float z, int dir){
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

	private void BuildDecal(int x, int y, int z, int dir, int decal){
		BuildChunkJob.faceVertices(cacheCubeVert, dir, 0.5f, GetDecalPosition(x, y+(this.pos.y*Chunk.chunkDepth), z, dir));
		decalVerts.AddRange(cacheCubeVert);
		int vCount = decalVerts.Length;

		CalculateNormal(cacheCubeNormal, dir);
		decalNormals.AddRange(cacheCubeNormal);

		FillUV(decal);

		AddTriangles(decalTris, vCount);
	}

	private void FillUV(int decal){
		float xSize = 1 / (float)Constants.DECAL_STAGE_SIZE;
		float xMin = (float)decal * xSize;

		decalUVs.Add(new Vector2(xMin, 0));
		decalUVs.Add(new Vector2(xMin, 1));
		decalUVs.Add(new Vector2(xMin + xSize, 1));
		decalUVs.Add(new Vector2(xMin + xSize, 0));
	}

	// Sets UV mapping for a direction
	private void AddTexture(NativeArray<Vector3> array, int dir, ushort blockCode){
		int textureID;

		if(dir == 4)
			textureID = blockTiles[blockCode].x;
		else if(dir == 5)
			textureID = blockTiles[blockCode].y;
		else
			textureID = blockTiles[blockCode].z;

		array[0] = new Vector3(0, 1, textureID);
		array[1] = new Vector3(1, 1, textureID);
		array[2] = new Vector3(1, 0, textureID);
		array[3] = new Vector3(0, 0, textureID);
	}

	// Gets UV Map for Liquid blocks
	private void LiquidTexture(NativeArray<Vector3> array, int x, int z){
		int size = Chunk.chunkWidth;
		float tileSize = 1f/size;

		array[0] = new Vector3(x*tileSize,z*tileSize, 0);
		array[1] = new Vector3(x*tileSize,(z+1)*tileSize, 0);
		array[2] = new Vector3((x+1)*tileSize,(z+1)*tileSize, 0);
		array[3] = new Vector3((x+1)*tileSize,z*tileSize, 0);
	}

	private void CalculateNormal(NativeArray<Vector3> normals, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, 1);
		else if(dir == 1)
			normal = new Vector3(1, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -1);
		else if(dir == 3)
			normal = new Vector3(-1, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, 1, 0);
		else
			normal = new Vector3(0, -1, 0);

		normals[0] = normal;
		normals[1] = normal;
		normals[2] = normal;
		normals[3] = normal;
	}

	private void CalculateTangent(NativeArray<Vector4> tangents, int dir){
		Vector4 tangent;

		if(dir == 0)
			tangent = new Vector4(-1, 0, 0, -1);
		else if(dir == 1)
			tangent = new Vector4(0, 0, 1, -1);
		else if(dir == 2)
			tangent = new Vector4(1, 0, 0, -1);
		else if(dir == 3)
			tangent = new Vector4(0, 0, -1, -1);
		else if(dir == 4)
			tangent = new Vector4(-1, 0, 0, -1);
		else
			tangent = new Vector4(-1, 0, 0, -1);

		tangents[0] = tangent;
		tangents[1] = tangent;
		tangents[2] = tangent;
		tangents[3] = tangent;
	}
}
