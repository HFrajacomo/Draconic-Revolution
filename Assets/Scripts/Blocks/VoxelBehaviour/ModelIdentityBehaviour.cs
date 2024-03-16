using System;
using UnityEngine;

[Serializable]
public class ModelIdentityBehaviour : VoxelBehaviour {
	public string modelUnityPath;

	public Mesh GetMesh(){return GameObject.Find(this.modelUnityPath).GetComponent<MeshFilter>().sharedMesh;}
	public Mesh GetHitboxMesh(){return GameObject.Find(this.modelUnityPath + "/Hitbox").GetComponent<MeshFilter>().sharedMesh;}
}