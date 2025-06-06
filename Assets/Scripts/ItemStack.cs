using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStack
{
	private Item item;
	private byte amount;
	private bool isFull;

	public ItemStack(ushort id, byte amount){
		this.item = ItemLoader.GetCopy(id);
		this.amount = amount;

		this.CheckFull();
	}

	// Create based on a previously owned item
	public ItemStack(Item item, byte amount){
		this.item = item;
		this.amount = amount;
	}

	// Returns the amount of items in the stack
	public byte GetAmount(){
		return this.amount;
	}

	// Hard Sets the amount
	public void SetAmount(byte amount){
		this.amount = amount;
		this.SetFull(this.amount >= this.item.stacksize);
	}

	// Returns the item ID
	public ushort GetID(){
		return item.GetID();
	}

	// Returns the stacksize of the item
	public byte GetStacksize(){
		return item.stacksize;
	}

	// Returns if stack is currently full
	public bool IsFull(){
		return this.isFull;
	}

	// Checks if stack should be full
	private void CheckFull(){
		if(this.amount >= this.item.stacksize)
			this.isFull = true;
		else
			this.isFull = false;
	}
	// Checks if stack should be null
	private bool CheckEmpty(){
		if(this.amount == 0)
			return true;
		return false;
	}

	// Sets isFull flag
	private void SetFull(bool b){
		this.isFull = b;
	}

	// Clones this element
	public ItemStack Clone(){
		return new ItemStack(this.GetID(), this.GetAmount());
	}

	// Compares two stacks to check whether they have the same ItemID
	public bool IsEqual(ItemStack its){
		if(this == null && its == null)
			return true;
		else if(its == null)
			return false;
		if(this != null && its != null)
			if(this.GetID() == its.GetID())
				return true;
		return false;
	}
	public override int GetHashCode(){
		return (int)this.GetID();
	}
	public override bool Equals(System.Object a){
		if(a == null)
			return false;

		ItemStack item = (ItemStack)a;
		return this == item;
	}

	// Moves items from an inventory to another in Inventory
	// RETURNS TRUE IF MOVER SHOULD BE DESTROYED IN INVENTORY
	#nullable enable
	public bool MoveTo(ref ItemStack its){
		// In case moving stack into an empty space
		if(its == null){
			its = this.Clone();
			return true;
		}

		// If stacks are different (have different items in them)
		if(!this.IsEqual(its))
			return false;


		// In case current stack is full and wanting to move
		if(this.IsFull()){
			byte aux = this.GetAmount();
			this.SetAmount(its.GetAmount());
			this.SetFull(false);
			its.SetAmount(aux);
			its.SetFull(true);
			return false;
		}

		// In case it moves but doesn't kill any stack
		if(this.GetAmount() + its.GetAmount() > this.GetStacksize()){
			byte remainder = (byte)((ushort)(this.GetAmount() + its.GetAmount()) - this.GetStacksize());

			its.SetAmount(its.GetStacksize());
			its.SetFull(true);
			this.SetAmount(remainder);
			return false;
		}


		// In case move will destroy initial mover stack
		its.SetAmount((byte)(this.GetAmount() + its.GetAmount()));
		its.CheckFull();
		return true;
	}
	#nullable disable

	// Adds an item to the stack
	public void Increment(){
		if(this.IsFull())
			return;

		this.SetAmount((byte)(this.GetAmount() + 1));
		this.CheckFull();
	}

	// Removes an item from the stack
	// RETURNS TRUE IF STACK SHOULD BE DESTROYED
	public bool Decrement(){
		if(this.GetAmount() <= 1){
			return true;
		}

		this.SetAmount((byte)(this.GetAmount() - 1));
		return false;
	}

	// Adds x amount to the stack
	// THIS FUNCTION WON'T CHECK OVERAGES
	public void Add(byte x){
		this.SetAmount((byte)(this.GetAmount() + x));
		this.CheckFull();
	}

	// Subs x amount from the stack
	// THIS FUNCTION WON'T CHECK OVERAGES
	// WILL RETURN TRUE IF STACK SHOULD BE DESTROYED
	public bool Subtract(byte x){
		this.SetAmount((byte)(this.GetAmount() - x));
		
		if(this.CheckEmpty())
			return true;
		return false;
	}

	// Splits the stack into a new one
	public ItemStack Split(){
		// If stack only has one element
		if(this.GetAmount() <= 1 || this.GetStacksize() == 1 || this.GetAmount() == 1)
			return null;
		
		// If stack is even
		if(this.GetAmount()%2 == 0){
			this.SetAmount((byte)(this.GetAmount() >> 1)); // Divides by 2
			this.SetFull(false);
			return this.Clone();
		}
		// If stack is odd
		else{
			byte newStackValue = (byte)(this.GetAmount() >> 1);
			this.SetAmount(newStackValue);
			this.SetFull(false);
			ItemStack newStack = this.Clone();
			this.SetAmount((byte)(newStackValue+1));

			return newStack;
		}
	}

	// Writes the memory representation of an item to a byte[]
	public int ConvertToMemory(byte[] data, int pos){
		if(this.item is Weapon)
			return ConvertWeapon(data, pos);
		//if is storage item //else if(this.item is)
		else
			return ConvertItem(data, pos);
	}

	private int ConvertWeapon(byte[] data, int pos){
		Weapon weap = this.GetItem() as Weapon;

		data[pos] = (byte)MemoryStorageType.WEAPON;
		NetDecoder.WriteUshort(weap.GetID(), data, pos+1);
		NetDecoder.WriteLong(weap.currentDurability, data, pos+3);
		data[pos+11] = weap.refineLevel;
		data[pos+12] = (byte)weap.extraEffect;

		return 13;
	}

	private int ConvertItem(byte[] data, int pos){
		data[pos] = (byte)MemoryStorageType.ITEM;
		NetDecoder.WriteUshort((ushort)this.GetID(), data, pos+1);
		data[pos+3] = this.GetAmount();

		return 4;
	}

	public override string ToString(){
		return this.GetID().ToString() + " " + this.GetAmount();
	}

	// Returns the Item associated
	public Item GetItem(){
		return this.item;
	}
}
