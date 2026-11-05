using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using Random = System.Random;


public class ItemEntity : MonoBehaviour {
	private static readonly string NAME = "DroppedItem";
	private static readonly Random rng = new Random();

    public GameObject droppedItemHierarchy;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private Mesh mesh;
	private ItemStack its;
	private Vector3 initialForce = new Vector3(0f, 0f, 0f);
	private Animator animator;
	private bool isRotation = false;

	private static readonly string ANIMATION_MEGASPIN = "DroppedItemMegaSpin";
	private static readonly string ANIMATION_ROTATION = "DroppedItemRotation";

	void Awake(){
		this.gameObject.transform.parent = this.droppedItemHierarchy.transform;
		this.meshFilter = GetComponent<MeshFilter>();
		this.meshRenderer = GetComponent<MeshRenderer>();
		this.animator = this.gameObject.GetComponent<Animator>();
		this.gameObject.name = ItemEntity.NAME;
		this.mesh = new Mesh();
		this.animator.applyRootMotion = false;
	}

	void Start(){
		BuildMesh();
	}

	void OnDestroy(){
		this.mesh.Clear();
		this.mesh = null;
	}

	public void SetVisible(bool flag){
		this.meshRenderer.enabled = flag;
	}

	public void PlaySpinAnimation(){
		if(isRotation){
			this.isRotation = false;
			this.animator.Play(ANIMATION_MEGASPIN);
		}
	}

	public void PlayRotationAnimation(){
		if(!isRotation){
			this.isRotation = true;
			this.animator.Play(ANIMATION_ROTATION);
		}
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
		UpdateMeshUV();
		this.mesh.RecalculateNormals();

		this.meshRenderer.materials[0].SetTexture("_Texture", ItemLoader.GetSprite(this.its));

		this.meshFilter.sharedMesh = this.mesh;
	}

	public void ChangeItem(Item item){
		UpdateMeshUV();
	}

	private void UpdateMeshUV(){
		this.mesh.SetUVs(0, Icon.GetItemEntityUV());
	}

	public ItemStack GetItemStack(){
		return this.its;
	}

	private bool CheckDestroy(byte itemsTaken){
		if(itemsTaken >= this.its.GetAmount()){
			GameObject.Destroy(this.gameObject);
			return true;
		}
		return false;
	}
}
