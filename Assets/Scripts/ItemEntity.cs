using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using Random = System.Random;


public class ItemEntity : MonoBehaviour
{
	private static string NAME = "DroppedItem";
	private static Random rng = new Random();

	public TimeOfDay time;
	public MeshFilter meshFilter;
	private Mesh mesh;
	private ItemStack its;
	public GameObject go;
	private Rigidbody rb;
    public GameObject droppedItemHierarchy;
	private Vector3 initialForce = new Vector3(0f, 0f, 0f);
	private string timeToRelease;

	void Start(){
		this.go = this.gameObject;
		this.go.transform.parent = this.droppedItemHierarchy.transform;
		this.meshFilter = this.go.GetComponent<MeshFilter>();
		this.go.name = ItemEntity.NAME;
		this.mesh = new Mesh();

		BuildMesh();
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

	private void ApplyForce(){
		this.rb.useGravity = true;
		this.rb.AddForce(this.initialForce);
	}

	public void SetItemStack(ItemStack its){
		this.its = its;
	}

	public void SetTime(){
		this.timeToRelease = time.FakeSum(30);
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

	/*
	private void OnTriggerEnter(Collider other){
		if(this.its == null)
			return;

		byte itemsTaken = 0;

		if(other.tag == "Player"){
			if(this.timeToRelease != null){
				if(!time.IsPast(this.timeToRelease)){
					return;
				}
			}

			PlayerEvents pe = other.GetComponent<PlayerEvents>();

			itemsTaken = pe.hotbar.AddStack(this.its, pe.hotbar.CanFit(this.its));
			if(itemsTaken >= 0)
				pe.DrawHotbar();

			if(CheckDestroy(itemsTaken))
				return;

			itemsTaken += pe.inventory.AddStack(this.its, pe.inventory.CanFit(this.its));

			if(CheckDestroy(itemsTaken))
				return;
		}
		else if(other.tag == "DroppedItem"){
			if(this.rb.IsSleeping() == false)
				return;

			ItemEntity ie = other.GetComponent<ItemEntity>();
			ItemStack itemStack = ie.GetItemStack();

			if(this.its.MoveTo(ref itemStack)){
				this.its = null;
				GameObject.Destroy(this.go);
			}
		}
	}
	*/

	private bool CheckDestroy(byte itemsTaken){
		if(itemsTaken >= this.its.GetAmount()){
			GameObject.Destroy(this.go);
			return true;
		}
		return false;
	}
}
