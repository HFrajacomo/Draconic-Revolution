using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   
	public GameObject itemPrefab;
	private ItemEntity ie;
	private ItemStack its;

	public void Start(){
		this.its = new ItemStack(ItemID.GRASSBLOCK, 10);
		this.ie = GameObject.Instantiate(this.itemPrefab, new Vector3(0, 3, 0), Quaternion.Euler(0, 0, 0)).GetComponent<ItemEntity>();
		this.ie.SetItemStack(its);
	}
}
