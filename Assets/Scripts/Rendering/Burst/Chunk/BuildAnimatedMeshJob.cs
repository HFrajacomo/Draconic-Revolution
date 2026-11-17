using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/*
Writes the Animated Objects mesh of a chunk (see ChunkAnimatedObjects) into writable Mesh data.
Every Object is copied in model space from ObjectMeshTables, with the part and slot of each vertex, so the mesh doesn't depend on
the position, state, animation or light of the Objects, which AnimatedObjects.hlsl reads from the slots
*/
[BurstCompile]
public struct BuildAnimatedMeshJob : IJob{
	public Mesh.MeshDataArray meshes;
	[ReadOnly]
	public MeshUpdateFlags flags;
	[ReadOnly]
	public NativeArray<VertexAttributeDescriptor> layout;
	[ReadOnly]
	public Bounds meshBounds;

	// x = slot, y = object index
	[ReadOnly]
	public NativeList<int2> objects;

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
	public NativeArray<float> loadedParts;
	[ReadOnly]
	public NativeArray<int> loadedTris;
	[ReadOnly]
	public NativeArray<int> vertsOffset;
	[ReadOnly]
	public NativeArray<int> trisOffset;

	public void Execute(){
		int vertexCount = 0;
		int indexCount = 0;
		int obj, slot, baseVertex;
		int v = 0;
		int t = 0;

		for(int i=0; i < objects.Length; i++){
			obj = objects[i].y;
			vertexCount += vertsOffset[obj+1] - vertsOffset[obj];
			indexCount += trisOffset[obj+1] - trisOffset[obj];
		}

		IndexFormat format = (vertexCount >= ushort.MaxValue) ? IndexFormat.UInt32 : IndexFormat.UInt16;
		Mesh.MeshData mesh = meshes[0];

		mesh.SetVertexBufferParams(vertexCount, layout);
		mesh.SetIndexBufferParams(indexCount, format);

		NativeArray<Vector3> positions = mesh.GetVertexData<Vector3>(0);
		NativeArray<AnimatedObjectVertex> attributes = mesh.GetVertexData<AnimatedObjectVertex>(1);

		for(int i=0; i < objects.Length; i++){
			slot = objects[i].x;
			obj = objects[i].y;
			baseVertex = v;

			for(int k=vertsOffset[obj]; k < vertsOffset[obj+1]; k++){
				positions[v] = loadedVerts[k];
				attributes[v] = new AnimatedObjectVertex(loadedNormals[k], loadedTangents[k], loadedUV[k], loadedParts[k], slot);
				v++;
			}

			if(format == IndexFormat.UInt32){
				NativeArray<int> indices = mesh.GetIndexData<int>();

				for(int k=trisOffset[obj]; k < trisOffset[obj+1]; k++){
					indices[t] = loadedTris[k] + baseVertex;
					t++;
				}
			}
			else{
				NativeArray<ushort> indices = mesh.GetIndexData<ushort>();

				for(int k=trisOffset[obj]; k < trisOffset[obj+1]; k++){
					indices[t] = (ushort)(loadedTris[k] + baseVertex);
					t++;
				}
			}
		}

		SubMeshDescriptor descriptor = new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles);
		descriptor.firstVertex = 0;
		descriptor.vertexCount = vertexCount;
		descriptor.bounds = meshBounds;

		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, descriptor, flags);
	}
}
