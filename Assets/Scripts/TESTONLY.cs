using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{	
	public Inventory inv;
	public Inventory inv2;
	public GameObject physicalInventory;
	public InventoryUIPlayer inventory;

    // Start is called before the first frame update
    void Start()
    {
    	inv = new Inventory(InventoryType.PLAYER);
    	inv2 = new Inventory(InventoryType.HOTBAR);
    	ItemStack its = new ItemStack(ItemID.STONEBLOCK, 4);
    	ItemStack its2 = new ItemStack(ItemID.GRASSBLOCK, 10);
    	ItemStack its3 = new ItemStack(ItemID.DIRTBLOCK, 5);

    	inv.AddStack(its, inv.CanFit(its));
    	inv2.AddStack(its2, inv2.CanFit(its2));
    	inv.AddStack(its3, inv.CanFit(its3));
    	inv2.AddStack(its3, inv2.CanFit(its3));
    	InventoryStaticMessage.playerInventory = inv;
    	InventoryStaticMessage.SetInventory(inv2);

    	physicalInventory.SetActive(true);
    	inventory.OpenInventory();
    }
}
