using System;
using UnityEngine;

[Serializable]
public class ModelIdentityBehaviour : VoxelBehaviour {
	public string modelUnityPath;
	public string textureName;

	private int textureCode;
	private MeshData meshdata;

	public override void PostDeserializationSetup(bool isClient){
		if(isClient){
			this.textureCode = VoxelLoader.GetTextureID(this.textureName);
			this.meshdata = new MeshData(GameObject.Find(this.modelUnityPath).GetComponent<MeshFilter>().mesh, GameObject.Find(this.modelUnityPath + "/Hitbox").GetComponent<MeshFilter>().sharedMesh);
		}
	}

	public MeshData GetMeshData(){return this.meshdata;}

	public int GetTextureCode(){return this.textureCode;}
	public string GetTextureName(){return this.textureName;}
}