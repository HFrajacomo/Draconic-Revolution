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
		ItemID id = its.GetID();

		foreach(InventoryTransaction t in spots){
			if(its.MoveTo(ref this.slots[t.slotNumber])){
				its = null;
				receivedAmount += t.amount;
				break;
			}

			receivedAmount += t.amount;
		}

		if(this.slots[this.GetLastEmptySlot()] != null){
			this.FindLastEmptySlot();
			this.SetFull();
		}

		if(!this.itemInInventory.Contains(id)){
			this.itemInInventory.Add(id);
		}

		return receivedAmount;
	}

	// Forcefully adds a Stack to a slot in inventory, overwriting whatever was there before
	public void ForceAddStack(ItemStack its, ushort slot){
		ItemStack previousIts;

		if(slot >= this.GetLimit())
			return;


		previousIts = this.DropStack((short)slot);
		this.slots[slot] = its;

		this.FindLastEmptySlot();

		if(this.slots[this.GetLastEmptySlot()] != null){
			this.SetFull();
		}
	}

	// Adds an ItemStack from a Split operation. Basically adds a new stack ignoring the originating stack
	// Returns true if there's an empty inventory slot for the split stack
	// Outputs newSlot as the slot occupied by the new Split
	public bool AddFromSplit(ItemStack its, ushort ignoreIndex, out ushort newSlot){
		if(its == null){
			newSlot = 0;
			return false;
		}

		if(this.IsFull()){
			newSlot = 0;
			return false;
		}

		for(ushort i=0; i < this.limit; i++){
			if(i == ignoreIndex)
				continue;

			if(this.slots[i] == null){
				this.slots[i] = its;
				newSlot = i;
				this.FindLastEmptySlot();
				this.SetFull();
				return true;
			}
		}
		newSlot = 0;
		return false;
	}

	// Switch slots in an one or two inventories
	public static bool SwitchSlots(Inventory inv1, ushort slot1, Inventory inv2, ushort slot2){
		// If clicked twice in the same slot
		if(inv1 == inv2 && slot1 == slot2)
			return false;

		// If destination slot is empty
		if(inv2.slots[slot2] == null){
			inv2.slots[slot2] = inv1.slots[slot1].Clone();
			inv1.slots[slot1] = null;

			if(inv1.GetLastEmptySlot() > slot1)
				inv1.SetLastEmptySlot((short)slot1);
			if(inv2.GetLastEmptySlot() <= slot2)
				inv2.FindLastEmptySlot();

			inv1.SetFull();
			inv2.SetFull();

			if(inv1 != inv2){
				if(!inv1.Contains(inv2.slots[slot2].GetID()))
					inv1.itemInInventory.Remove(inv2.slots[slot2].GetID());
			}

			return true;
		}

		// If both slots have the same elements
		if(inv1.slots[slot1].GetID() == inv2.slots[slot2].GetID()){
			if(inv1.slots[slot1].MoveTo(ref inv2.slots[slot2]))
				inv1.slots[slot1] = null;
			return true;
		}
		// If slots are different
		else{
			ItemStack aux = inv1.slots[slot1].Clone();
			inv1.slots[slot1] = inv2.slots[slot2];
			inv2.slots[slot2] = aux;

			if(inv1 != inv2){
				if(!inv1.Contains(inv2.slots[slot2].GetID()))
					inv1.itemInInventory.Remove(inv2.slots[slot2].GetID());
				if(!inv2.Contains(inv1.slots[slot1].GetID()))
					inv2.itemInInventory.Remove(inv1.slots[slot1].GetID());
			}

			return true;
		}
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
			InventoryTransaction trans;

			for(ushort i=0; i < this.limit; i++){
				if(this.slots[i] == null)
					continue;

				if(this.slots[i].IsEqual(its)){
					if(this.slots[i].IsFull())
						continue;

					difference = (byte)(stacksize - this.slots[i].GetAmount());
					if(difference >= remainder){
						trans = new InventoryTransaction(i, remainder);
						transactions.Add(trans);
						return transactions;
					}
					else{
						trans = new InventoryTransaction(i, difference);
						transactions.Add(trans);
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
	public void FindLastEmptySlot(){
		for(ushort i=0; i < this.limit; i++){
			if(this.slots[i] == null){
				this.lastEmptySlot = (short)i;
				return;
			}
		}

		this.SetLastEmptySlot(-1);
	}

	// Sets the lastEmptySlot
	public void SetLastEmptySlot(short a){
		this.lastEmptySlot = a;
	}

	// Returns this.isFull
	public bool IsFull(){
		return this.isFull;
	}

	// Sets an ItemStack as null
	public void SetNull(ushort slot){
		this.slots[slot] = null;
	}

	// Sets this.isFull when lastEmptyIndex = -1
	public void SetFull(){
		this.isFull = (this.GetLastEmptySlot() == -1);
	}

	// Returns this.lastEmptySlot
	public short GetLastEmptySlot(){
		return this.lastEmptySlot;
	}

	// Returns the limit of Inventory
	public ushort GetLimit(){return this.limit;}

	// Return the ItemStack at position pos
	#nullable enable
	public ItemStack? GetSlot(ushort pos){
		return this.slots[pos];
	}
	#nullable disable

	// Removes an element from itemInInventory
	public void RemoveFromRecords(ItemID id){
		if(!this.Contains(id))
			itemInInventory.Remove(id);
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

	public override string ToString(){
		return "Slot: " + this.slotNumber.ToString() + " | Amount: " + this.amount.ToString();
	}
}

public enum InventoryType{
	PLAYER,
	HOTBAR,
	CHEST
}