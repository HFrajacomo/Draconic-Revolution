using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

/*
Appends the meshes of every Object in the chunk to the chunk mesh and to the raycast hitbox mesh.
Object meshes are read from ObjectMeshTables.
Animated Objects are drawn by the Animated Objects mesh of the chunk (see ChunkAnimatedObjects), so only their hitbox,
their collision (solid ones) and their light are written here
*/
[BurstCompile]
public struct PrepareAssetsJob : IJob{
	[ReadOnly]
	public ChunkPos pos;
	[ReadOnly]
	public bool canRain;

	// Output
	public NativeList<Vector3> meshVerts;
	public NativeList<Vector3> meshUVs;
	public NativeList<int> meshTris;
	public NativeList<int> meshSolidTris;
	public NativeList<Vector3> meshNormals;
	public NativeList<Vector4> meshTangents;
	public NativeList<Vector3> meshLightUV;

	// Amount of block vertices, which come before the Object vertices in meshVerts
	public NativeArray<int> blockVertexCount;

	// Hitbox
	public NativeList<Vector3> hitboxVerts;
	public NativeList<Vector3> hitboxNormals;
	public NativeList<int> hitboxTriangles;

	// Animated Objects
	public NativeList<Vector3> solidHitboxVerts; // Hitboxes of solid Animated Objects, added to the collider by GreedyColliderJob
	public NativeList<int> solidHitboxTris;
	public NativeList<int2> animatedLight; // x = voxel index, y = light packed by AnimatedSlot.PackLight

	// Input
	[ReadOnly]
	public NativeList<int3> coords;
	[ReadOnly]
	public NativeArray<ushort> blocks; // Padded snapshot
	[ReadOnly]
	public NativeArray<byte> light; // Padded snapshot
	[ReadOnly]
	public NativeArray<byte> heights; // Padded snapshot
	[ReadOnly]
	public NativeArray<ushort> state;

	// Object Encyclopedia
	[ReadOnly]
	public NativeArray<Vector3> scaling;
	[ReadOnly]
	public NativeArray<Vector3> hitboxScaling;
	[ReadOnly]
	public NativeArray<bool> needRotation;
	[ReadOnly]
	public NativeArray<ShaderIndex> material;
	[ReadOnly]
	public NativeArray<bool> solid;

	// Object Mesh Tables
	[ReadOnly]
	public NativeArray<Vector3> loadedVerts;
	[ReadOnly]
	public NativeArray<Vector3> loadedUV;
	[ReadOnly]
	public NativeArray<Vector3> loadedNormals;
	[ReadOnly]
	public NativeArray<Vector4> loadedTangents;
	[ReadOnly]
	public NativeArray<int> loadedTris;
	[ReadOnly]
	public NativeArray<int> vertsOffset;
	[ReadOnly]
	public NativeArray<int> trisOffset;
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
	public NativeArray<int> stateOffset;
	[ReadOnly]
	public NativeArray<Vector3> stateTranslation;
	[ReadOnly]
	public NativeArray<int2> stateRotation;

	public void Execute(){
		int currentVertAmount = meshVerts.Length;

		blockVertexCount[0] = currentVertAmount;
		int hitboxVertAmount = hitboxVerts.Length;
		int3 coord;
		ushort code;
		int obj;
		Vector3 vertPos;
		Vector3 lightValue;
		Vector3 localOffset;
		int2 rotation;

		for(int j=0; j < coords.Length; j++){
			coord = coords[j];
			code = blocks[PaddedChunk.Index(coord.x, coord.y, coord.z)];

			if(code <= ushort.MaxValue/2)
				continue;

			obj = ushort.MaxValue - code;
			vertPos = new Vector3(coord.x, coord.y+(this.pos.y*Chunk.chunkDepth), coord.z);
			lightValue = new Vector3(GetLight(coord.x, coord.y, coord.z), GetLight(coord.x, coord.y, coord.z, isNatural:false), IsDamp(coord));

			if(material[obj] == ShaderIndex.ANIMATED_OBJECTS){
				hitboxVertAmount = AddAnimatedObject(coord, obj, vertPos, lightValue, hitboxVertAmount);
				continue;
			}

			// If has special offset or rotation
			if(needRotation[obj]){
				GetStateTransform(obj, state[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z], out localOffset, out rotation);

				// Normal Vertices
				for(int vertIndex=vertsOffset[obj]; vertIndex < vertsOffset[obj+1]; vertIndex++){
					meshVerts.Add(Vector3MultOffsetRotate(loadedVerts[vertIndex], scaling[obj], vertPos, localOffset, rotation));
					meshNormals.Add(GetNormalRotation(loadedNormals[vertIndex], rotation));
					meshTangents.Add(GetNormalRotation(loadedTangents[vertIndex], rotation));
					meshLightUV.Add(lightValue);
				}

				// Hitbox Vertices
				for(int vertIndex=hitboxVertsOffset[obj]; vertIndex < hitboxVertsOffset[obj+1]; vertIndex++){
					hitboxVerts.Add(Vector3MultOffsetRotate(loadedHitboxVerts[vertIndex], hitboxScaling[obj], vertPos, localOffset, rotation));
					hitboxNormals.Add(GetNormalRotation(loadedHitboxNormals[vertIndex], rotation));
				}
			}
			// If doesn't have special rotation
			else{
				// Normal Vertices
				for(int vertIndex=vertsOffset[obj]; vertIndex < vertsOffset[obj+1]; vertIndex++){
					meshVerts.Add(Vector3Mult(loadedVerts[vertIndex], scaling[obj], vertPos));
					meshNormals.Add(loadedNormals[vertIndex]);
					meshTangents.Add(loadedTangents[vertIndex]);
					meshLightUV.Add(lightValue);
				}

				// Hitbox Vertices
				for(int vertIndex=hitboxVertsOffset[obj]; vertIndex < hitboxVertsOffset[obj+1]; vertIndex++){
					hitboxVerts.Add(Vector3Mult(loadedHitboxVerts[vertIndex], hitboxScaling[obj], vertPos));
					hitboxNormals.Add(loadedHitboxNormals[vertIndex]);
				}
			}

			// UVs
			for(int UVIndex=vertsOffset[obj]; UVIndex < vertsOffset[obj+1]; UVIndex++){
				meshUVs.Add(loadedUV[UVIndex]);
			}

			// Triangles
			if(material[obj] == ShaderIndex.ASSETS){
				for(int triIndex=trisOffset[obj]; triIndex < trisOffset[obj+1]; triIndex++){
					meshTris.Add(loadedTris[triIndex] + currentVertAmount);
				}	
			}
			else{
				for(int triIndex=trisOffset[obj]; triIndex < trisOffset[obj+1]; triIndex++){
					meshSolidTris.Add(loadedTris[triIndex] + currentVertAmount);
				}			
			}
			currentVertAmount += (vertsOffset[obj+1] - vertsOffset[obj]);

			// Hitbox Triangles
			for(int triIndex=hitboxTrisOffset[obj]; triIndex < hitboxTrisOffset[obj+1]; triIndex++){
				hitboxTriangles.Add(loadedHitboxTriangles[triIndex] + hitboxVertAmount);
			}	
			hitboxVertAmount += (hitboxVertsOffset[obj+1] - hitboxVertsOffset[obj]);	
		}
	}

	// Writes the hitbox and light of an Animated Object. Returns the amount of hitbox vertices after it
	private int AddAnimatedObject(int3 coord, int obj, Vector3 vertPos, Vector3 lightValue, int hitboxVertAmount){
		int voxel = coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z;
		int solidVertAmount = solidHitboxVerts.Length;
		bool isSolid = solid[obj];
		Vector3 localOffset = Vector3.zero;
		int2 rotation = new int2(0, 0);
		Vector3 vert;

		if(needRotation[obj])
			GetStateTransform(obj, state[voxel], out localOffset, out rotation);

		for(int vertIndex=hitboxVertsOffset[obj]; vertIndex < hitboxVertsOffset[obj+1]; vertIndex++){
			vert = Vector3MultOffsetRotate(loadedHitboxVerts[vertIndex], hitboxScaling[obj], vertPos, localOffset, rotation);

			hitboxVerts.Add(vert);
			hitboxNormals.Add(ObjectTransformMath.Rotate(loadedHitboxNormals[vertIndex], rotation));

			if(isSolid)
				solidHitboxVerts.Add(vert);
		}

		for(int triIndex=hitboxTrisOffset[obj]; triIndex < hitboxTrisOffset[obj+1]; triIndex++){
			hitboxTriangles.Add(loadedHitboxTriangles[triIndex] + hitboxVertAmount);

			if(isSolid)
				solidHitboxTris.Add(loadedHitboxTriangles[triIndex] + solidVertAmount);
		}

		animatedLight.Add(new int2(voxel, AnimatedSlot.PackLight((int)lightValue.x, (int)lightValue.y, (int)lightValue.z)));

		return hitboxVertAmount + (hitboxVertsOffset[obj+1] - hitboxVertsOffset[obj]);
	}

	// Gets the offset and rotation of an object in a given state. Unknown states have no offset nor rotation
	private void GetStateTransform(int obj, int objState, out Vector3 localOffset, out int2 rotation){
		int start = stateOffset[obj];
		int count = stateOffset[obj+1] - start;

		if(objState < count){
			localOffset = stateTranslation[start+objState];
			rotation = stateRotation[start+objState];
		}
		else{
			localOffset = Vector3.zero;
			rotation = new int2(0, 0);
		}
	}

	private byte IsDamp(int3 coord){
		if(!this.canRain)
			return 0;

		if(coord.y > heights[PaddedChunk.MapIndex(coord.x, coord.z)])
			return 1;
		return 0;
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
			a = ObjectTransformMath.RotateX(a, rotationDegree.x);
		if(rotationDegree.y != 0)
			a = ObjectTransformMath.RotateY(a, rotationDegree.y);

		a = a + worldOffset;

		return a + localOffset;
	}

	private Vector3 GetNormalRotation(Vector3 normal, int2 rotation){
		return ObjectTransformMath.Rotate(normal, rotation);
	}

	private Vector4 GetNormalRotation(Vector4 normal, int2 rotation){
		return ObjectTransformMath.Rotate(normal, rotation);
	}


	// Gets the light level of a voxel of the chunk
	private int GetLight(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return light[PaddedChunk.Index(x, y, z)] & 0x0F;
		else
			return light[PaddedChunk.Index(x, y, z)] >> 4;
	}
}
