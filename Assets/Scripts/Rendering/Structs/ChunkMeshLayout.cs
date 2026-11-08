using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

/*
Vertex layouts of the meshes written by WriteChunkMeshJob.
The render mesh keeps positions in their own stream, so depth and shadow passes only fetch positions
*/
public static class ChunkMeshLayout{
	private static bool isCreated = false;

	public static NativeArray<VertexAttributeDescriptor> render;
	public static NativeArray<VertexAttributeDescriptor> collider;
	public static NativeArray<VertexAttributeDescriptor> decal;
	public static NativeArray<VertexAttributeDescriptor> raycast;

	public static void Init(){
		if(isCreated)
			return;

		render = new NativeArray<VertexAttributeDescriptor>(new VertexAttributeDescriptor[]{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1),
			new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 1),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3, 1),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 3, 1)
		}, Allocator.Persistent);

		collider = new NativeArray<VertexAttributeDescriptor>(new VertexAttributeDescriptor[]{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0)
		}, Allocator.Persistent);

		decal = new NativeArray<VertexAttributeDescriptor>(new VertexAttributeDescriptor[]{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0)
		}, Allocator.Persistent);

		raycast = new NativeArray<VertexAttributeDescriptor>(new VertexAttributeDescriptor[]{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0)
		}, Allocator.Persistent);

		isCreated = true;
	}

	public static void Destroy(){
		if(!isCreated)
			return;

		render.Dispose();
		collider.Dispose();
		decal.Dispose();
		raycast.Dispose();

		isCreated = false;
	}
}

// Stream 1 of the render mesh. Field order must follow the VertexAttribute order of ChunkMeshLayout.render
[StructLayout(LayoutKind.Sequential)]
public struct ChunkRenderVertex{
	public Vector3 normal;
	public Vector4 tangent;
	public Vector3 uv;
	public Vector3 lightUV;

	public ChunkRenderVertex(Vector3 normal, Vector4 tangent, Vector3 uv, Vector3 lightUV){
		this.normal = normal;
		this.tangent = tangent;
		this.uv = uv;
		this.lightUV = lightUV;
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct ChunkDecalVertex{
	public Vector3 position;
	public Vector3 normal;
	public Vector2 uv;

	public ChunkDecalVertex(Vector3 position, Vector3 normal, Vector2 uv){
		this.position = position;
		this.normal = normal;
		this.uv = uv;
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct ChunkRaycastVertex{
	public Vector3 position;
	public Vector3 normal;

	public ChunkRaycastVertex(Vector3 position, Vector3 normal){
		this.position = position;
		this.normal = normal;
	}
}
