using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEntity
{
	public GameObject go;
	public MeshFilter meshFilter;
	public ChunkRenderer renderer;
	private Mesh mesh;
	public uint iconID;
	public ItemID id;

	public ItemEntity(ItemID id, uint iconID, ChunkRenderer renderer, bool playerHand=true){
		this.go = new GameObject();
		this.go.name = "item";
		this.go.AddComponent<MeshFilter>();
		this.go.AddComponent<MeshRenderer>();
		this.meshFilter = this.go.GetComponent<MeshFilter>();
		this.renderer = renderer;
		this.go.GetComponent<MeshRenderer>().materials = this.renderer.GetComponent<MeshRenderer>().materials;
		this.mesh = new Mesh();
		this.id = id;
		this.iconID = iconID;

		this.BuildMesh();
	}

	public void BuildMesh(){
		this.mesh.subMeshCount = 2;
		this.mesh.SetVertices(ItemMeshData.vertices);
		this.mesh.SetTriangles(ItemMeshData.imageTris, 0);
		this.mesh.SetTriangles(ItemMeshData.materialTris, 1);
		UpdateMeshUV(this.iconID);

		this.meshFilter.sharedMesh = this.mesh;
	}

	public void ChangeItem(Item item){
		this.id = item.id;
		this.iconID = item.iconID;
		UpdateMeshUV(this.iconID);
	}

	private void UpdateMeshUV(uint iconID){
		this.mesh.SetUVs(0, Icon.GetItemEntityUV(this.iconID));
	}

	public void Destroy(){
		GameObject.Destroy(this.go);
	}
}
