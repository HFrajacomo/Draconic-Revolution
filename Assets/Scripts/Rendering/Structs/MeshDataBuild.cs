using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct MeshDataBuild{
	// Mesh Data
	public Vector3[] vertices;
	public Vector2[] UVs;
	public int[] tris;
	public int[] specularTris;
	public int[] liquidTris;
	public int[] assetTris;
	public int[] assetSolidTris;
	public int[] leavesTris;
	public int[] iceTris;
	public int[] lavaTris;
	public Vector3[] lightUVs;
	public Vector4[] tangents;
	public Vector3[] normals;

	// Collider Data
	public Vector3[] colliderVertices;
	public int[] colliderTris;
	public int[] colliderIceTris;
	public int[] colliderAssetSolidTris;

	// Decal Data
	public Vector3[] decalVertices;
	public Vector2[] decalUVs;
	public int[] decalTris;

	// Raycast Data
	public Vector3[] raycastVertices;
	public Vector3[] raycastNormals;
	public int[] raycastTriangles;

	// Load Coord List
	public int3[] loadList;

	public void SetData(Vector3[] vertices, int[] tris, int[] specularTris, int[] liquidTris, int[] assetTris, int[] assetSolidTris, int[] leavesTris, int[] iceTris, int[] lavaTris, Vector2[] UVs, Vector3[] lightUV, Vector3[] normals, Vector4[] tangents){
    	this.vertices = vertices;
		this.UVs = UVs;
		this.tris = tris;
		this.specularTris = specularTris;
		this.liquidTris = liquidTris;
		this.assetTris = assetTris;
		this.assetSolidTris = assetSolidTris;
		this.leavesTris = leavesTris;
		this.iceTris = iceTris;
		this.lavaTris = lavaTris;
		this.lightUVs = lightUV;
		this.tangents = tangents;
		this.normals = normals;
	}

	public void SetColliderData(Vector3[] verts, int[] tris, int[] iceTris, int[] assetSolidTris){
		this.colliderVertices = verts;
		this.colliderTris = tris;
		this.colliderIceTris = iceTris;
		this.colliderAssetSolidTris = assetSolidTris;
	}

	public void SetDecalData(Vector3[] verts, Vector2[] UV, int[] triangles){
		this.decalVertices = verts;
		this.decalUVs = UV;
		this.decalTris = triangles;
	}

	public void SetRaycastData(Vector3[] verts, Vector3[] normals, int[] triangles){
		this.raycastVertices = verts;
		this.raycastNormals = normals;
		this.raycastTriangles = triangles;
	}

	public void SetLoadList(int3[] loadList){
		this.loadList = loadList;
	}

	public void Destroy(){
    	this.vertices = null;
		this.UVs = null;
		this.tris = null;
		this.specularTris = null;
		this.liquidTris = null;
		this.assetTris = null;
		this.assetSolidTris = null;
		this.leavesTris = null;
		this.iceTris = null;
		this.lavaTris = null;
		this.lightUVs = null;
		this.tangents = null;
		this.normals = null;
		this.colliderVertices = null;
		this.colliderTris = null;
		this.colliderIceTris = null;
		this.colliderAssetSolidTris = null;
		this.decalVertices = null;
		this.decalUVs = null;
		this.decalTris = null;
		this.raycastVertices = null;
		this.raycastNormals = null;
		this.raycastTriangles = null;
	}

	public bool VerifyIntegrity(){
    	if(this.vertices == null)
    		return false;
		if(this.UVs == null)
			return false;
		if(this.tris == null)
			return false;
		if(this.specularTris == null)
			return false;
		if(this.liquidTris == null)
			return false;
		if(this.assetTris == null)
			return false;
		if(this.assetSolidTris == null)
			return false;
		if(this.leavesTris == null)
			return false;
		if(this.iceTris == null)
			return false;
		if(this.lavaTris == null)
			return false;
		if(this.lightUVs == null)
			return false;
		if(this.tangents == null)
			return false;
		if(this.normals == null)
			return false;
		if(this.colliderVertices == null)
			return false;
		if(this.colliderTris == null)
			return false;
		if(this.colliderIceTris == null)
			return false;
		if(this.colliderAssetSolidTris == null)
			return false;
		if(this.decalVertices == null)
			return false;
		if(this.decalUVs == null)
			return false;
		if(this.decalTris == null)
			return false;
		if(this.raycastVertices == null)
			return false;
		if(this.raycastNormals == null)
			return false;
		if(this.raycastTriangles == null)
			return false;

		return true;
	}

	// Builds Main Chunk mesh
	public Mesh BuildMesh(){
		Mesh mesh = new Mesh();

		if(this.vertices.Length == 0)
			return mesh;
    	if(this.vertices.Length >= ushort.MaxValue)
    		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    	mesh.subMeshCount = 8;

    	mesh.SetVertices(this.vertices);

		mesh.SetTriangles(this.tris, 0);
		mesh.SetTriangles(this.specularTris, 1);
		mesh.SetTriangles(this.liquidTris, 2);
		mesh.SetTriangles(this.assetTris, 3);
		mesh.SetTriangles(this.assetSolidTris, 4);
		mesh.SetTriangles(this.leavesTris, 5);
		mesh.SetTriangles(this.iceTris, 6);
		mesh.SetTriangles(this.lavaTris, 7);

		mesh.SetUVs(0, this.UVs);
		mesh.SetUVs(3, this.lightUVs);

		mesh.SetNormals(this.normals);
		mesh.SetTangents(this.tangents);

		return mesh;
	}

	// Builds Chunk collider mesh
	public Mesh BuildColliderMesh(){
		Mesh mesh = new Mesh();

		if(this.colliderVertices.Length == 0)
			return mesh;
    	if(this.colliderVertices.Length >= ushort.MaxValue)
    		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    	mesh.subMeshCount = 3;

    	mesh.SetVertices(this.colliderVertices);

    	mesh.SetTriangles(this.colliderTris, 0);
 	   	mesh.SetTriangles(this.colliderAssetSolidTris, 1);
 	   	mesh.SetTriangles(this.colliderIceTris, 2);
 	   	return mesh;
	}

	// Builds Chunk Decal mesh
	public Mesh BuildDecalMesh(){
		Mesh mesh = new Mesh();

		if(this.decalVertices.Length == 0)
			return mesh;
    	if(this.decalVertices.Length >= ushort.MaxValue)
    		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;	

    	mesh.SetVertices(this.decalVertices);
		mesh.SetTriangles(this.decalTris, 0);
		mesh.SetUVs(0, this.decalUVs);
		mesh.RecalculateNormals();

		return mesh;
	}

	// Builds Chunk Raycast collider
	public Mesh BuildRaycastMesh(){
		Mesh mesh = new Mesh();

		if(this.raycastVertices == null)
			return mesh;
		if(this.raycastVertices.Length == 0)
			return mesh;
		if(this.raycastVertices.Length >= ushort.MaxValue)
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		mesh.SetVertices(this.raycastVertices);
		mesh.SetTriangles(this.raycastTriangles, 0);
		mesh.SetNormals(this.raycastNormals);

		return mesh;
	}

	public bool IsLoadNull(){return this.loadList == null;}
	public int3[] GetLoadList(){return this.loadList;}
}