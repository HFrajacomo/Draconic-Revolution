using System;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData{
	private readonly List<Vector3> vertices;
	private List<Vector2> UVs;
	private readonly int[] triangles;
	private readonly List<Vector4> tangents;
	private readonly List<Vector3> normals;

	private readonly List<Vector3> hitboxVertices;
	private readonly int[] hitboxTriangles;

	// For VoxelLoader
	public MeshData(Mesh mesh, Mesh hitboxMesh){
		this.vertices = new List<Vector3>();
		this.UVs = new List<Vector2>();

		this.tangents = new List<Vector4>();
		this.normals = new List<Vector3>();
		this.hitboxVertices = new List<Vector3>();

		mesh.GetVertices(this.vertices);
		this.triangles = mesh.GetTriangles(0);
		mesh.GetUVs(0, this.UVs);
		mesh.GetNormals(this.normals);
		mesh.GetTangents(this.tangents);

		hitboxMesh.GetVertices(this.hitboxVertices);
		this.hitboxTriangles = hitboxMesh.GetTriangles(0);
	}

	public int GetUVs(List<Vector2> outputList){
		outputList.AddRange(this.UVs);
		return this.UVs.Count;
	}

	public int GetVertices(List<Vector3> outputList){
		outputList.AddRange(this.vertices);
		return this.vertices.Count;
	}

	public int GetTangents(List<Vector4> outputList){
		outputList.AddRange(this.tangents);
		return this.tangents.Count;
	}

	public int GetNormals(List<Vector3> outputList){
		outputList.AddRange(this.normals);
		return this.normals.Count;
	}

	public int[] GetTriangles(){return this.triangles;}
	
	public int GetHitboxVertices(List<Vector3> outputList){
		outputList.AddRange(this.hitboxVertices);
		return this.hitboxVertices.Count;
	}

	public int[] GetHitboxTriangles(){return this.hitboxTriangles;}

	public MeshData SetUVs(List<Vector2> UVs){
		this.UVs = UVs;
		UVs = null;

		return this;
	}

	public void DebugCreate(){
		GameObject obj = new GameObject("TestMeshData");
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

		Mesh mesh = new Mesh();
		mesh.SetVertices(this.vertices);
		mesh.SetUVs(0, this.UVs);
		mesh.SetTriangles(this.triangles, 0);
		mesh.SetTangents(this.tangents);
		mesh.SetNormals(this.normals);
		meshFilter.mesh = mesh;
	}
}