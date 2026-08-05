using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ActionInventory : BaseInventory {
	private byte inventoryID;
	private EntityAction[] slots;
	private ushort limit;

	private ActionInventory(string type, ushort size){
		this.limit = size;
		this.InitSlots(this.limit);
		this.inventoryType = type;
		this.itemInventory = false;
	}

	// Creates a BLANK COPY of the Inventory
	public ActionInventory Copy(){
		ActionInventory inv = new ActionInventory(this.inventoryType, this.limit);
		inv.columnCount = this.columnCount;
		inv.inventoryID = this.inventoryID;

		return inv;
	}

	public void PostDeserializationSetup(byte inventoryID){
		this.inventoryID = inventoryID;
		this.limit = amountOfSlots;
		this.InitSlots(this.limit);
	}


	// Adds an EntityAction to Inventory
	// If had an EntityAction in the given slot, returns it. Else, returns null
	public override EntityAction AddStack(EntityAction ea, byte slot){
		EntityAction aux = this.slots[slot];
		this.slots[slot] = ea;

		return aux;
	}

	public override EntityAction ForceAddStack(EntityAction ea, ushort slot){
		return AddStack(ea, (byte)slot);
	}


	// Removes completely an EntityAction
	public void Remove(short slot){
		this.slots[slot] = null;
	}

	// Returns inventoryID
	public override byte GetID(){return this.inventoryID;}

	// Returns the limit of Inventory
	public override ushort GetLimit(){return this.limit;}

	// Return the ItemStack at position pos
	#nullable enable
	public override EntityAction? GetPos(ushort pos){
		return this.slots[pos];
	}
	#nullable disable

	public string GetInventoryType(){return this.inventoryType;}

	// Initializes slots
	private void InitSlots(ushort limit){
		this.slots = new EntityAction[limit];
	}

	private void SetLimitOnType(byte id){this.limit = (ushort)InventoryLoader.GetInventorySize(id);}
}