using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ItemEntityHand
{
	// Shared Properties
	public GameObject go;
	public MeshFilter meshFilter;
	public ChunkRenderer renderer;
	private Mesh mesh;
	public ushort id;

	public ItemEntityHand(ushort id, ChunkRenderer renderer){
		this.id = id;
		this.renderer = renderer;
	}
		/*
		this.go = new GameObject();
		this.go.name = "item";
		this.go.AddComponent<MeshFilter>();
		this.go.AddComponent<MeshRenderer>();
		this.meshFilter = this.go.GetComponent<MeshFilter>();
		this.go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		this.renderer = renderer;
		this.go.GetComponent<MeshRenderer>().materials = this.renderer.GetComponent<MeshRenderer>().materials;
		this.mesh = new Mesh();
		this.id = id;
		this.iconID = iconID;

		this.BuildMesh();
	}
	*/

	public ushort GetID(){return this.id;}

	/*
	public void BuildMesh(){
		return;
		this.mesh.subMeshCount = 2;
		this.mesh.SetVertices(ItemMeshData.vertices);
		this.mesh.SetTriangles(ItemMeshData.imageTris, 0);
		UpdateMeshUV(this.iconID);

		this.meshFilter.sharedMesh = this.mesh;
	}

	public void ChangeItem(Item item){
		this.id = item.id;
		this.iconID = item.iconID;
		UpdateMeshUV(this.iconID);
	}

	private void UpdateMeshUV(uint iconID){
		this.mesh.SetUVs(0, Icon.GetItemEntityUV(iconID));
	}

	public void Destroy(){
		GameObject.Destroy(this.go);
	}
	*/
}
