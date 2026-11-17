using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/*
Builds the chunk collider by merging the collidable block faces of each layer into the largest possible rectangles (greedy meshing).
Solid Object geometry is appended afterwards with its indices remapped.
Rectangles keep the corner order and winding of CubeMeshData faces
*/
[BurstCompile]
public struct GreedyColliderJob : IJob{
	[ReadOnly]
	public ChunkPos pos;

	// Collidable faces, indexed by direction*VOXEL_COUNT + voxel index. Consumed by this job
	public NativeBitArray colliderFaces;

	// Render geometry, used for Solid Objects
	[ReadOnly]
	public NativeList<Vector3> verts;
	[ReadOnly]
	public NativeList<int> assetSolidTris;
	[ReadOnly]
	public NativeArray<int> blockVertexCount;

	// Output
	public NativeList<Vector3> colliderVerts;
	public NativeList<int> colliderTris;

	public void Execute(){
		// Face direction, then the axis of the layer and the two axes of the face (0 = x, 1 = y, 2 = z)
		// Rectangles grow along the last axis first
		Greedy(0, 2, 0, 1); // North (z+)
		Greedy(1, 0, 2, 1); // East (x+)
		Greedy(2, 2, 0, 1); // South (z-)
		Greedy(3, 0, 2, 1); // West (x-)
		Greedy(4, 1, 0, 2); // Up (y+)
		Greedy(5, 1, 0, 2); // Down (y-)

		AppendSolidObjects();
	}

	private void Greedy(int dir, int layerAxis, int uAxis, int vAxis){
		int layerCount = AxisSize(layerAxis);
		int uCount = AxisSize(uAxis);
		int vCount = AxisSize(vAxis);
		int width, height;
		bool canGrow;

		for(int layer=0; layer < layerCount; layer++){
			for(int u=0; u < uCount; u++){
				for(int v=0; v < vCount; v++){
					if(!IsFace(dir, layerAxis, uAxis, vAxis, layer, u, v))
						continue;

					// Grows along v
					height = 1;
					while(v+height < vCount && IsFace(dir, layerAxis, uAxis, vAxis, layer, u, v+height))
						height++;

					// Grows along u while the whole column is set
					width = 1;
					while(u+width < uCount){
						canGrow = true;

						for(int i=0; i < height; i++){
							if(!IsFace(dir, layerAxis, uAxis, vAxis, layer, u+width, v+i)){
								canGrow = false;
								break;
							}
						}

						if(!canGrow)
							break;

						width++;
					}

					for(int du=0; du < width; du++){
						for(int dv=0; dv < height; dv++){
							colliderFaces.Set(GetFaceIndex(dir, ToCoord(layerAxis, uAxis, vAxis, layer, u+du, v+dv)), false);
						}
					}

					EmitQuad(dir, ToCoord(layerAxis, uAxis, vAxis, layer, u, v), ToCoord(layerAxis, uAxis, vAxis, layer, u+width-1, v+height-1));

					v += height-1;
				}
			}
		}
	}

	// Writes the face of a rectangle of voxels, from voxel min to voxel max (inclusive)
	private void EmitQuad(int dir, int3 min, int3 max){
		int baseIndex = colliderVerts.Length;
		float3 sign;
		float yOffset = this.pos.y*Chunk.chunkDepth;

		for(int i=0; i < 4; i++){
			sign = CubeMeshData.floatverts[CubeMeshData.faceTriangles[dir*4+i]];

			colliderVerts.Add(new Vector3(
				sign.x > 0 ? max.x+0.5f : min.x-0.5f,
				(sign.y > 0 ? max.y+0.5f : min.y-0.5f) + yOffset,
				sign.z > 0 ? max.z+0.5f : min.z-0.5f
			));
		}

		colliderTris.Add(baseIndex);
		colliderTris.Add(baseIndex+1);
		colliderTris.Add(baseIndex+2);
		colliderTris.Add(baseIndex);
		colliderTris.Add(baseIndex+2);
		colliderTris.Add(baseIndex+3);
	}

	// Copies the Object vertices and the triangles of Solid Objects
	private void AppendSolidObjects(){
		int firstObjectVertex = blockVertexCount[0];
		int offset = colliderVerts.Length - firstObjectVertex;

		if(assetSolidTris.Length == 0)
			return;

		for(int i=firstObjectVertex; i < verts.Length; i++)
			colliderVerts.Add(verts[i]);

		for(int i=0; i < assetSolidTris.Length; i++)
			colliderTris.Add(assetSolidTris[i] + offset);
	}

	private bool IsFace(int dir, int layerAxis, int uAxis, int vAxis, int layer, int u, int v){
		return colliderFaces.IsSet(GetFaceIndex(dir, ToCoord(layerAxis, uAxis, vAxis, layer, u, v)));
	}

	private int GetFaceIndex(int dir, int3 c){
		return dir*Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth + c.x*Chunk.chunkWidth*Chunk.chunkDepth + c.y*Chunk.chunkWidth + c.z;
	}

	private int3 ToCoord(int layerAxis, int uAxis, int vAxis, int layer, int u, int v){
		int3 c = new int3(0, 0, 0);

		c[layerAxis] = layer;
		c[uAxis] = u;
		c[vAxis] = v;

		return c;
	}

	private int AxisSize(int axis){
		if(axis == 1)
			return Chunk.chunkDepth;
		return Chunk.chunkWidth;
	}
}
