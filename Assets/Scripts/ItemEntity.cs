using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using Random = System.Random;


public class ItemEntity : MonoBehaviour
{
	private static readonly string NAME = "DroppedItem";
	private static readonly Random rng = new Random();

	public MeshFilter meshFilter;
	private Mesh mesh;
	private ItemStack its;
	public GameObject go;
    public GameObject droppedItemHierarchy;
	private Vector3 initialForce = new Vector3(0f, 0f, 0f);
	private Animator animator;
	private bool isRotation = false;

	private static readonly string ANIMATION_MEGASPIN = "DroppedItemMegaSpin";
	private static readonly string ANIMATION_ROTATION = "DroppedItemRotation";

	void Start(){
		this.go = this.gameObject;
		//this.go.transform.parent = this.droppedItemHierarchy.transform;
		this.meshFilter = this.go.GetComponent<MeshFilter>();
		this.animator = this.go.GetComponent<Animator>();
		this.go.name = ItemEntity.NAME;
		this.mesh = new Mesh();
		this.animator.applyRootMotion = false;

		BuildMesh();
	}

	void OnDestroy(){
		this.mesh.Clear();
		this.mesh = null;
	}

	public void PlaySpinAnimation(){
		if(isRotation)
			this.animator.Play(ANIMATION_MEGASPIN);
	}

	public void PlayRotationAnimation(){
		if(!isRotation)
			this.animator.Play(ANIMATION_ROTATION);
	}

	public void RandomForce(){
		float x = ItemEntity.rng.Next(-100, 100)*0.01f;
		float y = ItemEntity.rng.Next(0, 100)*0.01f;
		float z = ItemEntity.rng.Next(-100, 100)*0.01f;

		this.initialForce = new Vector3(x, y, z);
	}

	public void AddForce(Vector3 force){
		this.initialForce = force;
	}

	public void SetItemStack(ItemStack its){
		this.its = its;
	}


	public void BuildMesh(){
		if(this.its == null)
			return;

		if(this.mesh == null)
			return;

		this.mesh.subMeshCount = 2;
		this.mesh.SetVertices(ItemMeshData.vertices);
		this.mesh.SetTriangles(ItemMeshData.imageTris, 0);
		this.mesh.SetTriangles(ItemMeshData.materialTris, 1);

		UpdateMeshUV(this.its.GetIconID());
		this.mesh.RecalculateNormals();

		this.meshFilter.sharedMesh = this.mesh;
	}

	public void ChangeItem(Item item){
		UpdateMeshUV(item.iconID);
	}

	private void UpdateMeshUV(uint iconID){
		this.mesh.SetUVs(0, Icon.GetItemEntityUV(iconID));
	}

	public ItemStack GetItemStack(){
		return this.its;
	}

	private bool CheckDestroy(byte itemsTaken){
		if(itemsTaken >= this.its.GetAmount()){
			GameObject.Destroy(this.go);
			return true;
		}
		return false;
	}
}
