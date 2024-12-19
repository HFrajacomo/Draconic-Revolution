using System;
using UnityEngine;

[Serializable]
public class ModelIdentityBehaviour : VoxelBehaviour {
	public string modelUnityPath;
	public string textureName;
	private MeshData meshdata;

	public override void PostDeserializationSetup(bool isClient){
		if(isClient){
			this.meshdata = new MeshData(GameObject.Find(this.modelUnityPath).GetComponent<MeshFilter>().mesh, GameObject.Find(this.modelUnityPath + "/Hitbox").GetComponent<MeshFilter>().sharedMesh, VoxelLoader.GetTextureID(this.textureName));
		}
	}

	public string GetTextureName(){return this.textureName;}
	public MeshData GetMeshData(){return this.meshdata;}
	public void SetMeshData(MeshData meshData){this.meshdata = meshData;}
}