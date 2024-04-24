using System;
using UnityEngine;

[Serializable]
public class ModelIdentityBehaviour : VoxelBehaviour {
	public string modelUnityPath;
	public string textureName;

	private int textureCode;
	private Mesh mesh;
	private Mesh hitboxMesh;

	public override void PostDeserializationSetup(bool isClient){
		if(isClient){
			this.textureCode = VoxelLoader.GetTextureID(this.textureName);
		}
	}

	public Mesh GetMesh(){
		if(this.mesh == null){
			this.mesh = GameObject.Find(this.modelUnityPath).GetComponent<MeshFilter>().mesh;
		}
		return this.mesh;
	}

	public Mesh GetHitboxMesh(){
		if(this.hitboxMesh == null)
			this.hitboxMesh = GameObject.Find(this.modelUnityPath + "/Hitbox").GetComponent<MeshFilter>().sharedMesh;
		return this.hitboxMesh;
	}

	public int GetTextureCode(){return this.textureCode;}
	public string GetTextureName(){return this.textureName;}
}