using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory
{
	private ItemStack[] slots;
	private InventoryType type;
	private ushort limit;
	private short lastEmptySlot;
	private bool isFull;
	private HashSet<ItemID> itemInInventory;

	// Creates an empty inventory
	public Inventory(InventoryType type){
		switch(type){
			case InventoryType.PLAYER:
				this.limit = 36;
				break;
			case InventoryType.HOTBAR:
				this.limit = 9;
				break;
			case InventoryType.CHEST:
				this.limit = 25;
				break;
			default:
				this.limit = 1;
				break;
		}

		this.InitSlots(this.limit);
		this.type = type;
		this.lastEmptySlot = 0;
		this.isFull = false;
		this.itemInInventory = new HashSet<ItemID>();
	}

	// Adds an ItemStack to Inventory
	public byte AddStack(ItemStack its, List<InventoryTransaction> spots){
		if(spots.Count == 0)
			return 0;

		byte receivedAmount = 0;

		foreach(InventoryTransaction t in spots){
			if(its.MoveTo(this.slots[t.slotNumber])){
				its = null;
				receivedAmount += t.amount;
				return receivedAmount;
			}

			receivedAmount += t.amount;
		}

		if(this.slots[this.GetLastEmptySlot()] != null){
			this.FindLastEmptySlot();
			this.SetFull();
		}

		if(!this.itemInInventory.Contains(its.GetID())){
			this.itemInInventory.Add(its.GetID());
		}

		return receivedAmount;
	}

	// Adds an ItemStack from a Split operation. Basically adds a new stack ignoring the originating stack
	// Returns true if there's an empty inventory slot for the split stack
	public bool AddFromSplit(ItemStack its, ushort ignoreIndex){
		if(this.IsFull())
			return false;

		for(int i=0; i < this.limit; i++){
			if(i == ignoreIndex)
				continue;

			if(this.slots[i] == null){
				this.slots[i] = its;
				this.FindLastEmptySlot();
				return true;
			}
		}
		return false;
	}

	// Switch slots in an one or two inventories and returns true if the change was made
	public static bool SwitchSlots(Inventory inv1, ushort slot1, Inventory inv2, ushort slot2){
		// If selected an empty slot first
		if(inv1 == null)
			return false;

		// If destination slot is empty
		if(inv2 == null){
			inv2.slots[slot2] = inv1.slots[slot1];
			inv1.slots[slot1] = null;

			if(inv1.GetLastEmptySlot() > slot1)
				inv1.SetLastEmptySlot((short)slot1);
			if(inv2.GetLastEmptySlot() <= slot2)
				inv2.FindLastEmptySlot();

			inv1.SetFull();
			inv2.SetFull();

			if(inv1 != inv2){
				if(!inv1.Contains(inv2.slots[slot2]))
					inv1.itemInInventory.Remove(inv2.slots[slot2].GetID());
				if(!inv2.Contains(inv1.slots[slot1]))
					inv2.itemInInventory.Remove(inv1.slots[slot1].GetID());
			}

			return true;
		}

		// If both slots have elements
		ItemStack aux = inv1.slots[slot1].Clone();
		inv1.slots[slot1] = inv2.slots[slot2];
		inv2.slots[slot2] = aux;

		if(inv1 != inv2){
			if(!inv1.Contains(inv2.slots[slot2]))
				inv1.itemInInventory.Remove(inv2.slots[slot2].GetID());
			if(!inv2.Contains(inv1.slots[slot1]))
				inv2.itemInInventory.Remove(inv1.slots[slot1].GetID());
		}

		return true;
	}


	// Drops a single item from a given stack
	public ItemStack DropSingle(short slot){
		// If slot is null
		if(this.slots[slot] == null)
			return null;

		ItemStack returnStack;

		// If slot only has one item
		if(this.slots[slot].GetAmount() == 1){
			returnStack = this.slots[slot].Clone();
			this.slots[slot] = null;

			if(this.lastEmptySlot > slot)
				this.SetLastEmptySlot(slot);

			if(!this.Contains(returnStack.GetID()))
				this.itemInInventory.Remove(returnStack.GetID());

			return returnStack;
		}

		// If stack has more than one item
		returnStack = this.slots[slot].Clone();
		returnStack.SetAmount(1);
		this.slots[slot].Decrement();
		return returnStack;
	}

	// Drops entire selected stack
	public ItemStack DropStack(short slot){
		// If slot is null
		if(this.slots[slot] == null)
			return null;

		ItemStack returnStack = this.slots[slot].Clone();
		this.slots[slot] = null;

		if(this.GetLastEmptySlot() > slot)
			this.SetLastEmptySlot(slot);

		if(!this.Contains(returnStack.GetID()))
			this.itemInInventory.Remove(returnStack.GetID());

		return returnStack;		
	}

	// Initializes slots
	private void InitSlots(ushort limit){
		this.slots = new ItemStack[limit];
	}

	// Checks if an ItemStack can be fitted into the inventory
	// Returns a Transaction List
	// fitItems returns the amount of items taken into the transactions
	public List<InventoryTransaction> CanFit(ItemStack its){
		List<InventoryTransaction> transactions = new List<InventoryTransaction>();

		// If there's no space and no available stack
		if(this.IsFull() && !this.itemInInventory.Contains(its.GetID()))
			return transactions;

		// If inventory is full but has space for an ItemID
		else if(this.IsFull()){
			byte remainder = its.GetAmount();
			byte difference;
			byte stacksize = its.GetStacksize();

			for(ushort i=0; i < this.limit; i++){
				if(this.slots[i] == its){
					if(this.slots[i].IsFull())
						continue;

					difference = (byte)(stacksize - this.slots[i].GetAmount());
					if(difference >= remainder){
						transactions.Add(new InventoryTransaction(i, remainder));
						return transactions;
					}
					else{
						transactions.Add(new InventoryTransaction(i, difference));
						remainder = (byte)(remainder - difference);
					}
				}
			}

			return transactions;
		}

		// If there's free space in inventory but there's a stack of that given item
		else if(this.itemInInventory.Contains(its.GetID())){
			byte remainder = its.GetAmount();
			byte difference;
			byte stacksize = its.GetStacksize();

			for(ushort i=0; i < this.limit; i++){
				if(this.slots[i] == its){
					if(this.slots[i].IsFull())
						continue;

					difference = (byte)(stacksize - this.slots[i].GetAmount());
					if(difference >= remainder){
						transactions.Add(new InventoryTransaction(i, remainder));
						return transactions;
					}
					else{
						transactions.Add(new InventoryTransaction(i, difference));
						remainder = (byte)(remainder - difference);
					}
				}
			}

			transactions.Add(new InventoryTransaction((ushort)this.GetLastEmptySlot(), remainder));
			this.FindLastEmptySlot();
			return transactions;

		}

		// If there's no ItemStack of given ItemID and there's free space
		transactions.Add(new InventoryTransaction((ushort)this.GetLastEmptySlot(), its.GetAmount()));
		this.FindLastEmptySlot();
		return transactions;

	}

	// Iterates until it finds the first empty slot
	private void FindLastEmptySlot(){
		for(short i=(short)(this.lastEmptySlot+1); i < this.limit; i++){
			if(this.slots[i] == null){
				this.lastEmptySlot = i;
				return;
			}
		}

		this.SetLastEmptySlot(-1);
	}

	// Sets the lastEmptySlot
	private void SetLastEmptySlot(short a){
		this.lastEmptySlot = a;
	}

	// Returns this.isFull
	public bool IsFull(){
		return this.isFull;
	}

	// Sets this.isFull when lastEmptyIndex = -1
	public void SetFull(){
		this.isFull = (this.GetLastEmptySlot() == -1);
	}

	// Returns this.lastEmptySlot
	private short GetLastEmptySlot(){
		return this.lastEmptySlot;
	}

	// Discovers whether an item is contained in the inventory manually
	public bool Contains(ItemID id){
		for(int i=0; i < this.limit; i++){
			if(this.slots[i] == null)
				continue;
			if(this.slots[i].GetID() == id)
				return true;
		}
		return false;
	}

}

public struct InventoryTransaction{
	public ushort slotNumber;
	public byte amount;

	public InventoryTransaction(ushort slot, byte x){
		this.slotNumber = slot;
		this.amount = x;
	}
}

public enum InventoryType{
	PLAYER,
	HOTBAR,
	CHEST
}