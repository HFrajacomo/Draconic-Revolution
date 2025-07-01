using System;
using System.Collections.Generic;
using UnityEngine;

public struct ShapeKeyDeltaData {
	public string shapeKeyName;
	public int shapeKeyIndex;
	public int startIndex;
	public float weight;
	public Vector3[] deltaVertices;
	public Vector3[] deltaNormals;
	public Vector3[] deltaTangents;

	public ShapeKeyDeltaData(string name, int ski, float w, Mesh mesh, int startIndex){
		this.shapeKeyName = name;
		this.shapeKeyIndex = ski;
		this.weight = w;
		this.startIndex = startIndex;
		this.deltaVertices = new Vector3[mesh.vertexCount];
		this.deltaNormals = new Vector3[mesh.vertexCount];
		this.deltaTangents = new Vector3[mesh.vertexCount];

		mesh.GetBlendShapeFrameVertices(ski, 0, this.deltaVertices, this.deltaNormals, this.deltaTangents);
	}

	public int Length(){return this.deltaVertices.Length;}

	public static void CopyBlendShapes(List<ShapeKeyDeltaData> shapeKeys, Mesh mesh){
		if(shapeKeys.Count == 0)
			return;

		int finalZeroStart;
		ShapeKeyDeltaData aux;
		int startIndex = 0;
		Vector3[] finalVerts;
		Vector3[] finalNormals;
		Vector3[] finalTangents;

		for(int i = 0; i < shapeKeys.Count; i++){
			aux = shapeKeys[i];
			startIndex = aux.startIndex;
			finalZeroStart = startIndex + aux.Length();

			// Recreates final arrays
			finalVerts = new Vector3[mesh.vertexCount];
			finalNormals = new Vector3[mesh.vertexCount];
			finalTangents = new Vector3[mesh.vertexCount];

			// Fills initial verts (before SK)
			Array.Fill(finalVerts, Vector3.zero, 0, startIndex);
			Array.Fill(finalNormals, Vector3.zero, 0, startIndex);
			Array.Fill(finalTangents, Vector3.zero, 0, startIndex);

			// Fills SK part of array
			Array.Copy(aux.deltaVertices, 0, finalVerts, startIndex, aux.Length());
			Array.Copy(aux.deltaNormals, 0, finalNormals, startIndex, aux.Length());
			Array.Copy(aux.deltaTangents, 0, finalTangents, startIndex, aux.Length());

			// Fills the last verts (after SK)
			Array.Fill(finalVerts, Vector3.zero, finalZeroStart, mesh.vertexCount - finalZeroStart);
			Array.Fill(finalNormals, Vector3.zero, finalZeroStart, mesh.vertexCount - finalZeroStart);
			Array.Fill(finalTangents, Vector3.zero, finalZeroStart, mesh.vertexCount - finalZeroStart);

			mesh.AddBlendShapeFrame(aux.shapeKeyName, aux.weight, finalVerts, finalNormals, finalTangents);
		}
	}
}