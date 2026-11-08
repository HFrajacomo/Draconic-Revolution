using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

/*
Vertex layouts of the meshes written by WriteChunkMeshJob.
The render mesh keeps positions in their own stream, so depth and shadow passes only fetch positions.
Its other attributes are compressed (36 bytes per vertex in total). Shaders still receive them as floats
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
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4, 1),
			new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.SNorm8, 4, 1),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4, 1),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float16, 4, 1)
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

/*
Stream 1 of the render mesh. Field order must follow the VertexAttribute order of ChunkMeshLayout.render
Normal and Tangent are SNorm8x4. UV (u, v, texture layer) and Light UV (natural light, extra light, dampness) are Float16x4,
which keep their integer values exact. The unused 4th components are 1, the value shaders get for missing components
*/
[StructLayout(LayoutKind.Sequential)]
public struct ChunkRenderVertex{
	public uint normal;
	public uint tangent;
	public half4 uv;
	public half4 lightUV;

	public ChunkRenderVertex(Vector3 normal, Vector4 tangent, Vector3 uv, Vector3 lightUV){
		this.normal = PackSNorm8(normal.x, normal.y, normal.z, 0f);
		this.tangent = PackSNorm8(tangent.x, tangent.y, tangent.z, tangent.w);
		this.uv = new half4(new float4(uv.x, uv.y, uv.z, 1f));
		this.lightUV = new half4(new float4(lightUV.x, lightUV.y, lightUV.z, 1f));
	}

	// Packs 4 values in [-1, 1] as signed normalized bytes, x being the lowest byte
	private static uint PackSNorm8(float x, float y, float z, float w){
		return ToSNorm8(x) | (ToSNorm8(y) << 8) | (ToSNorm8(z) << 16) | (ToSNorm8(w) << 24);
	}

	private static uint ToSNorm8(float value){
		int packed = (int)math.round(math.clamp(value, -1f, 1f) * 127f);

		return (uint)(packed & 0xFF);
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
