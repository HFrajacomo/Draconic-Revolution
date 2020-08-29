using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlocklikeObject
{
	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool liquid;
	public static int objectCount = 1;

	// Texture
	public string prefabName;
	public Mesh mesh;
	public Vector3 centeringOffset;
	public Vector3 scaling;

	public static BlocklikeObject Create(int blockID){
		if(blockID == -1){
			Torch_Object aux = new Torch_Object();
			return aux;
		}
		else{
			return new Torch_Object();
		}
	}

	// Moves all vertices from local to world space
	public Vector3[] ToWorldSpace(Vector3 pos){
		if(this.mesh == null){
			return null;
		}

		Vector3[] newVert = new Vector3[this.mesh.vertices.Length];


		for(int i=0;i<this.mesh.vertices.Length;i++){
			newVert[i] = this.mesh.vertices[i] + pos;
		}

		return newVert;
	}

	public void LoadMesh(){
		GameObject go;
		Vector3[] newVerts;
		Vector3 cachedVector;

		go = GameObject.Find(this.prefabName);
		this.mesh = go.GetComponent<MeshFilter>().mesh;
		newVerts = new Vector3[this.mesh.vertices.Length];

		for(int i=0; i<this.mesh.vertices.Length; i++){
			cachedVector = new Vector3(this.mesh.vertices[i].x, this.mesh.vertices[i].y, this.mesh.vertices[i].z);
			
			// Rotation
			cachedVector = Quaternion.AngleAxis(-90, Vector3.right) * cachedVector;
			
			// Whole Scaling
			cachedVector *= 10f;

			// Scaling
			cachedVector.x *= this.scaling.x;
			cachedVector.y *= this.scaling.y;
			cachedVector.z *= this.scaling.z;

			// Shifting
			newVerts[i] = cachedVector + this.centeringOffset;
		}
		this.mesh.vertices = newVerts;
	}

	/*
	VIRTUAL METHODS
	*/

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ){return 0;}
	
}
