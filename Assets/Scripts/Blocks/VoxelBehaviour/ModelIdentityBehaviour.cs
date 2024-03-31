using System;
using UnityEngine;

[Serializable]
public class ModelIdentityBehaviour : VoxelBehaviour {
	public string modelUnityPath;
	public string textureName;

	private int textureCode;

	public override void PostDeserializationSetup(bool isClient){
		// TODO: Get TextureCode
	}

	public Mesh GetMesh(){return GameObject.Find(this.modelUnityPath).GetComponent<MeshFilter>().sharedMesh;}
	public Mesh GetHitboxMesh(){return GameObject.Find(this.modelUnityPath + "/Hitbox").GetComponent<MeshFilter>().sharedMesh;}
	public int GetTextureCode(){return this.textureCode;}
	public string GetTextureName(){return this.textureName;}
}