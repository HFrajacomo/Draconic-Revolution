using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/*
Writes the geometry built by BuildChunkJob and PrepareAssetsJob directly into writable Mesh data, avoiding managed copies.
Mesh 0: Render | Mesh 1: Collider | Mesh 2: Decals | Mesh 3: Raycast hitboxes
*/
[BurstCompile]
public struct WriteChunkMeshJob : IJob{
	public const int RENDER_MESH = 0;
	public const int COLLIDER_MESH = 1;
	public const int DECAL_MESH = 2;
	public const int RAYCAST_MESH = 3;
	public const int MESH_COUNT = 4;

	public Mesh.MeshDataArray meshes;
	[ReadOnly]
	public MeshUpdateFlags flags;

	// Layouts
	[ReadOnly]
	public NativeArray<VertexAttributeDescriptor> renderLayout;
	[ReadOnly]
	public NativeArray<VertexAttributeDescriptor> colliderLayout;
	[ReadOnly]
	public NativeArray<VertexAttributeDescriptor> decalLayout;
	[ReadOnly]
	public NativeArray<VertexAttributeDescriptor> raycastLayout;

	// Render Mesh
	[ReadOnly]
	public NativeList<Vector3> verts;
	[ReadOnly]
	public NativeList<Vector3> UVs;
	[ReadOnly]
	public NativeList<Vector3> lightUV;
	[ReadOnly]
	public NativeList<Vector3> normals;
	[ReadOnly]
	public NativeList<Vector4> tangents;
	[ReadOnly]
	public NativeList<int> normalTris;
	[ReadOnly]
	public NativeList<int> specularTris;
	[ReadOnly]
	public NativeList<int> liquidTris;
	[ReadOnly]
	public NativeList<int> assetTris;
	[ReadOnly]
	public NativeList<int> assetSolidTris;
	[ReadOnly]
	public NativeList<int> leavesTris;
	[ReadOnly]
	public NativeList<int> iceTris;
	[ReadOnly]
	public NativeList<int> lavaTris;

	// Collider Mesh (built by GreedyColliderJob)
	[ReadOnly]
	public NativeList<Vector3> colliderVerts;
	[ReadOnly]
	public NativeList<int> colliderTris;

	// Decal Mesh
	[ReadOnly]
	public NativeList<Vector3> decalVerts;
	[ReadOnly]
	public NativeList<Vector3> decalNormals;
	[ReadOnly]
	public NativeList<Vector2> decalUVs;
	[ReadOnly]
	public NativeList<int> decalTris;

	// Raycast Mesh
	[ReadOnly]
	public NativeList<Vector3> hitboxVerts;
	[ReadOnly]
	public NativeList<Vector3> hitboxNormals;
	[ReadOnly]
	public NativeList<int> hitboxTris;

	// Output
	public NativeArray<Bounds> bounds;
	public NativeArray<int> indexCounts;

	public void Execute(){
		WriteRenderMesh(meshes[RENDER_MESH], CalculateBounds(verts));
		WriteColliderMesh(meshes[COLLIDER_MESH]);
		WriteDecalMesh(meshes[DECAL_MESH]);
		WriteRaycastMesh(meshes[RAYCAST_MESH]);
	}

	// Submeshes follow the material order of the ChunkRenderer
	private void WriteRenderMesh(Mesh.MeshData mesh, Bounds meshBounds){
		int vertexCount = verts.Length;
		int indexCount = normalTris.Length + specularTris.Length + liquidTris.Length + assetTris.Length + assetSolidTris.Length + leavesTris.Length + iceTris.Length + lavaTris.Length;
		IndexFormat format = GetIndexFormat(vertexCount);
		int start = 0;

		mesh.SetVertexBufferParams(vertexCount, renderLayout);
		mesh.GetVertexData<Vector3>(0).CopyFrom(verts.AsArray());

		NativeArray<ChunkRenderVertex> attributes = mesh.GetVertexData<ChunkRenderVertex>(1);

		for(int i=0; i < vertexCount; i++){
			attributes[i] = new ChunkRenderVertex(normals[i], tangents[i], UVs[i], lightUV[i]);
		}

		mesh.SetIndexBufferParams(indexCount, format);
		mesh.subMeshCount = 8;

		start = WriteSubMesh(mesh, 0, normalTris, start, format, vertexCount, meshBounds);
		start = WriteSubMesh(mesh, 1, specularTris, start, format, vertexCount, meshBounds);
		start = WriteSubMesh(mesh, 2, liquidTris, start, format, vertexCount, meshBounds);
		start = WriteSubMesh(mesh, 3, assetTris, start, format, vertexCount, meshBounds);
		start = WriteSubMesh(mesh, 4, assetSolidTris, start, format, vertexCount, meshBounds);
		start = WriteSubMesh(mesh, 5, leavesTris, start, format, vertexCount, meshBounds);
		start = WriteSubMesh(mesh, 6, iceTris, start, format, vertexCount, meshBounds);
		WriteSubMesh(mesh, 7, lavaTris, start, format, vertexCount, meshBounds);

		bounds[RENDER_MESH] = meshBounds;
		indexCounts[RENDER_MESH] = indexCount;
	}

	// Merged faces of solid blocks and ice, plus solid objects
	private void WriteColliderMesh(Mesh.MeshData mesh){
		int vertexCount = colliderVerts.Length;
		IndexFormat format = GetIndexFormat(vertexCount);
		Bounds meshBounds = CalculateBounds(colliderVerts);

		mesh.SetVertexBufferParams(vertexCount, colliderLayout);
		mesh.GetVertexData<Vector3>(0).CopyFrom(colliderVerts.AsArray());

		mesh.SetIndexBufferParams(colliderTris.Length, format);
		mesh.subMeshCount = 1;
		WriteSubMesh(mesh, 0, colliderTris, 0, format, vertexCount, meshBounds);

		bounds[COLLIDER_MESH] = meshBounds;
		indexCounts[COLLIDER_MESH] = colliderTris.Length;
	}

	private void WriteDecalMesh(Mesh.MeshData mesh){
		int vertexCount = decalVerts.Length;
		IndexFormat format = GetIndexFormat(vertexCount);
		Bounds meshBounds = CalculateBounds(decalVerts);

		mesh.SetVertexBufferParams(vertexCount, decalLayout);

		NativeArray<ChunkDecalVertex> vertices = mesh.GetVertexData<ChunkDecalVertex>(0);

		for(int i=0; i < vertexCount; i++){
			vertices[i] = new ChunkDecalVertex(decalVerts[i], decalNormals[i], decalUVs[i]);
		}

		mesh.SetIndexBufferParams(decalTris.Length, format);
		mesh.subMeshCount = 1;
		WriteSubMesh(mesh, 0, decalTris, 0, format, vertexCount, meshBounds);

		bounds[DECAL_MESH] = meshBounds;
		indexCounts[DECAL_MESH] = decalTris.Length;
	}

	private void WriteRaycastMesh(Mesh.MeshData mesh){
		int vertexCount = hitboxVerts.Length;
		IndexFormat format = GetIndexFormat(vertexCount);
		Bounds meshBounds = CalculateBounds(hitboxVerts);

		mesh.SetVertexBufferParams(vertexCount, raycastLayout);

		NativeArray<ChunkRaycastVertex> vertices = mesh.GetVertexData<ChunkRaycastVertex>(0);

		for(int i=0; i < vertexCount; i++){
			vertices[i] = new ChunkRaycastVertex(hitboxVerts[i], hitboxNormals[i]);
		}

		mesh.SetIndexBufferParams(hitboxTris.Length, format);
		mesh.subMeshCount = 1;
		WriteSubMesh(mesh, 0, hitboxTris, 0, format, vertexCount, meshBounds);

		bounds[RAYCAST_MESH] = meshBounds;
		indexCounts[RAYCAST_MESH] = hitboxTris.Length;
	}

	// Writes the indices of a submesh starting at a given position of the index buffer. Returns the position after it
	private int WriteSubMesh(Mesh.MeshData mesh, int subMesh, NativeList<int> triangles, int start, IndexFormat format, int vertexCount, Bounds meshBounds){
		if(format == IndexFormat.UInt32){
			NativeArray<int> indices = mesh.GetIndexData<int>();

			for(int i=0; i < triangles.Length; i++)
				indices[start+i] = triangles[i];
		}
		else{
			NativeArray<ushort> indices = mesh.GetIndexData<ushort>();

			for(int i=0; i < triangles.Length; i++)
				indices[start+i] = (ushort)triangles[i];
		}

		SubMeshDescriptor descriptor = new SubMeshDescriptor(start, triangles.Length, MeshTopology.Triangles);
		descriptor.firstVertex = 0;
		descriptor.vertexCount = vertexCount;
		descriptor.bounds = meshBounds;

		mesh.SetSubMesh(subMesh, descriptor, flags);

		return start + triangles.Length;
	}

	private IndexFormat GetIndexFormat(int vertexCount){
		if(vertexCount >= ushort.MaxValue)
			return IndexFormat.UInt32;
		return IndexFormat.UInt16;
	}

	private Bounds CalculateBounds(NativeList<Vector3> vertices){
		if(vertices.Length == 0)
			return new Bounds(Vector3.zero, Vector3.zero);

		Vector3 min = vertices[0];
		Vector3 max = vertices[0];

		for(int i=1; i < vertices.Length; i++){
			min = Vector3.Min(min, vertices[i]);
			max = Vector3.Max(max, vertices[i]);
		}

		return new Bounds((min + max) * 0.5f, max - min);
	}
}
