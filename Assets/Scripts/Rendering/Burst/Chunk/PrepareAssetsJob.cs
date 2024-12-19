using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;

[BurstCompile]
public struct PrepareAssetsJob : IJob{
	[ReadOnly]
	public ChunkPos pos;

	// Output
	public NativeList<Vector3> meshVerts;
	public NativeList<Vector3> meshUVs;
	public NativeList<int> meshTris;
	public NativeList<int> meshSolidTris;
	public NativeList<Vector3> meshNormals;
	public NativeList<Vector4> meshTangents;
	public NativeList<Vector3> meshLightUV;

	// Hitbox
	public NativeList<Vector3> hitboxVerts;
	public NativeList<Vector3> hitboxNormals;
	public NativeList<int> hitboxTriangles;

	[ReadOnly]
	public int vCount;
	[ReadOnly]
	public bool canRain;

	// Input
	[ReadOnly]
	public NativeArray<ushort> blockdata;
	[ReadOnly]
	public NativeArray<ushort> metadata;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public NativeList<int3> coords;
	[ReadOnly]
	public NativeList<ushort> blockCodes;
	[ReadOnly]
	public NativeList<int> vertsOffset;
	[ReadOnly]
	public NativeList<int> trisOffset;
	[ReadOnly]
	public NativeList<int> UVOffset;
	[ReadOnly]
	public NativeArray<Vector3> scaling;
	[ReadOnly]
	public NativeArray<bool> needRotation;
	[ReadOnly]
	public NativeParallelHashMap<int, Vector3> inplaceOffset;
	[ReadOnly]
	public NativeParallelHashMap<int, int2> inplaceRotation;

	// Loaded Mesh Data
	[ReadOnly]
	public NativeArray<Vector3> loadedVerts;
	[ReadOnly]
	public NativeArray<Vector3> loadedUV;
	[ReadOnly]
	public NativeArray<int> loadedTris;
	[ReadOnly]
	public NativeArray<Vector3> loadedNormals;
	[ReadOnly]
	public NativeArray<Vector4> loadedTangents;

	// Loaded Hitbox Data
	[ReadOnly]
	public NativeArray<Vector3> loadedHitboxVerts;
	[ReadOnly]
	public NativeArray<Vector3> loadedHitboxNormals;
	[ReadOnly]
	public NativeArray<int> loadedHitboxTriangles;
	[ReadOnly]
	public NativeArray<int> hitboxVertsOffset;
	[ReadOnly]
	public NativeArray<int> hitboxTrisOffset;
	[ReadOnly]
	public NativeArray<Vector3> hitboxScaling;
	[ReadOnly]
	public NativeArray<ShaderIndex> material;

	public void Execute(){
		int i;
		int currentVertAmount = vCount;
		int hitboxVertAmount = 0;
		int blockdataIndex;

		for(int j=0; j < coords.Length; j++){
			blockdataIndex = blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];
			i = GetIndex(blockdataIndex);

			if(i == -1)
				continue;

			// If has special offset or rotation
			if(needRotation[ushort.MaxValue - blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z]]){
				int code = blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];
				int state = metadata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];

				// Normal Vertices
				Vector3 vertPos = new Vector3(coords[j].x, coords[j].y+(this.pos.y*Chunk.chunkDepth), coords[j].z);
				for(int vertIndex=vertsOffset[i]; vertIndex < vertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3MultOffsetRotate(loadedVerts[vertIndex], scaling[i], vertPos, inplaceOffset[code*256+state], inplaceRotation[code*256+state]);
					meshVerts.Add(resultVert);
					meshNormals.Add(GetNormalRotation(loadedNormals[vertIndex], inplaceRotation[code*256+state]));
					meshTangents.Add(GetNormalRotation(loadedTangents[vertIndex], inplaceRotation[code*256+state]));
					meshLightUV.Add(new Vector3(GetLight(coords[j].x, coords[j].y, coords[j].z), GetLight(coords[j].x, coords[j].y, coords[j].z, isNatural:false), IsDamp(this.heightMap, coords[j])));
				}

				// Hitbox Vertices
				for(int vertIndex=hitboxVertsOffset[i]; vertIndex < hitboxVertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3MultOffsetRotate(loadedHitboxVerts[vertIndex], hitboxScaling[i], vertPos, inplaceOffset[code*256+state], inplaceRotation[code*256+state]);
					hitboxVerts.Add(resultVert);
					hitboxNormals.Add(GetNormalRotation(loadedHitboxNormals[vertIndex], inplaceRotation[code*256+state]));
				}

			}
			// If doesn't have special rotation
			else{
				// Normal Vertices
				Vector3 vertPos = new Vector3(coords[j].x, coords[j].y+(this.pos.y*Chunk.chunkDepth), coords[j].z);
				for(int vertIndex=vertsOffset[i]; vertIndex < vertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3Mult(loadedVerts[vertIndex], scaling[i], vertPos);
					meshVerts.Add(resultVert);
					meshNormals.Add(loadedNormals[vertIndex]);
					meshTangents.Add(loadedTangents[vertIndex]);
					meshLightUV.Add(new Vector3(GetLight(coords[j].x, coords[j].y, coords[j].z), GetLight(coords[j].x, coords[j].y, coords[j].z, isNatural:false), IsDamp(this.heightMap, coords[j])));
				}

				// Hitbox Vertices
				for(int vertIndex=hitboxVertsOffset[i]; vertIndex < hitboxVertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3Mult(loadedHitboxVerts[vertIndex], hitboxScaling[i], vertPos);
					hitboxVerts.Add(resultVert);
					hitboxNormals.Add(loadedHitboxNormals[vertIndex]);
				}
			}

			// UVs
			for(int UVIndex=UVOffset[i]; UVIndex < UVOffset[i+1]; UVIndex++){
				int code = blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];
				meshUVs.Add(new Vector3(loadedUV[UVIndex].x, loadedUV[UVIndex].y, loadedUV[UVIndex].z));
			}

			// Triangles
			if(material[ushort.MaxValue - blockdataIndex] == ShaderIndex.ASSETS){
				for(int triIndex=trisOffset[i]; triIndex < trisOffset[i+1]; triIndex++){
					meshTris.Add(loadedTris[triIndex] + currentVertAmount);
				}	
			}
			else{
				for(int triIndex=trisOffset[i]; triIndex < trisOffset[i+1]; triIndex++){
					meshSolidTris.Add(loadedTris[triIndex] + currentVertAmount);
				}			
			}
			currentVertAmount += (vertsOffset[i+1] - vertsOffset[i]);

			// Hitbox Triangles
			for(int triIndex=hitboxTrisOffset[i]; triIndex < hitboxTrisOffset[i+1]; triIndex++){
				hitboxTriangles.Add(loadedHitboxTriangles[triIndex] + hitboxVertAmount);
			}	
			hitboxVertAmount += (hitboxVertsOffset[i+1] - hitboxVertsOffset[i]);	
		}
	}

	private byte IsDamp(NativeArray<byte> heightMap, int3 coord){
		if(!this.canRain)
			return 0;

		if(coord.y > heightMap[coord.x*Chunk.chunkWidth+coord.z])
			return 1;
		return 0;
	}

	// Check if a blockCode is contained in blockCodes List
	private int GetIndex(int code){
		for(int i=0; i < blockCodes.Length; i++){
			if(blockCodes[i] == code){
				return i;
			}
		}
		return -1;
	}

	private Vector3 Vector3Mult(Vector3 a, Vector3 b, Vector3 plus){
		return new Vector3(a.x * b.x + plus.x, a.y * b.y + plus.y, a.z * b.z + plus.z);
	}

	private Vector3 Vector3Mult(Vector3 a, Vector3 b){
		return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
	}

	private Vector3 Vector3MultOffsetRotate(Vector3 a, Vector3 worldScaling, Vector3 worldOffset, Vector3 localOffset, int2 rotationDegree){
		a = Vector3Mult(a, worldScaling);

		if(rotationDegree.x != 0)
			a = RotateX(a, rotationDegree.x);
		if(rotationDegree.y != 0)
			a = RotateY(a, rotationDegree.y);

		a = a + worldOffset;

		return a + localOffset;
	}

	private Vector3 GetNormalRotation(Vector3 normal, int2 rotation){
		return RotateY(RotateX(normal, rotation.x), rotation.y);
	}

	private Vector4 GetNormalRotation(Vector4 normal, int2 rotation){
		return RotateY(RotateX(normal, rotation.x), rotation.y);
	}

	private Vector3 RotateX(Vector3 a, int degrees){
		return new Vector3(a.x*Mathf.Cos(degrees *Mathf.Deg2Rad) - a.z*Mathf.Sin(degrees *Mathf.Deg2Rad), a.y, a.x*Mathf.Sin(degrees *Mathf.Deg2Rad) + a.z*Mathf.Cos(degrees *Mathf.Deg2Rad));
	}

	private Vector4 RotateX(Vector4 a, int degrees){
		return new Vector4(a.x*Mathf.Cos(degrees *Mathf.Deg2Rad) - a.z*Mathf.Sin(degrees *Mathf.Deg2Rad), a.y, a.x*Mathf.Sin(degrees *Mathf.Deg2Rad) + a.z*Mathf.Cos(degrees *Mathf.Deg2Rad), a.w);
	}

	private Vector3 RotateY(Vector3 a, int degrees){
		return new Vector3(a.x, a.y * Mathf.Cos(degrees * Mathf.Deg2Rad) - a.z * Mathf.Sin(degrees * Mathf.Deg2Rad), a.y * Mathf.Sin(degrees * Mathf.Deg2Rad) + a.z * Mathf.Cos(degrees * Mathf.Deg2Rad));
	}

	private Vector4 RotateY(Vector4 a, int degrees){
		return new Vector4(a.x, a.y * Mathf.Cos(degrees * Mathf.Deg2Rad) - a.z * Mathf.Sin(degrees * Mathf.Deg2Rad), a.y * Mathf.Sin(degrees * Mathf.Deg2Rad) + a.z * Mathf.Cos(degrees * Mathf.Deg2Rad), a.w);
	}


	// Gets neighbor light level
	private int GetLight(int x, int y, int z, bool isNatural=true){
		int3 coord = new int3(x, y, z);

		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0xF0;
	}
}