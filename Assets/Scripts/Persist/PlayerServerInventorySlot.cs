using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerServerInventorySlot{
	protected MemoryStorageType type;
	protected int slotMemorySize;
	protected ushort itemID;
	protected byte quantity;

	public int GetSlotMemorySize(){return this.slotMemorySize;}
	public abstract int SaveToBuffer(byte[] buffer, int init);
	public virtual int GetItemID(){return (int)this.itemID;}
	public virtual int GetQuantity(){return 1;}
	public virtual void SetQuantity(byte quantity){}

	public static PlayerServerInventorySlot[] BuildInventory(byte[] data, int init, int inventorySlotAmount, ref int bytesWritten, int initialSlot=0){
		PlayerServerInventorySlot[] slots = new PlayerServerInventorySlot[inventorySlotAmount];
		int currentPosition = init;
		int currentSlot = initialSlot;

		MemoryStorageType cachedType;
		ushort cachedId;
		byte cachedQuantity;
		uint cachedDurability;
		byte cachedRefine;
		EnchantmentType cachedEnchant;
		byte cachedInventorySize;
		PlayerServerInventorySlot[] cachedInventory;

		while(currentSlot < inventorySlotAmount){
			cachedType = (MemoryStorageType)NetDecoder.ReadByte(data, currentPosition);
			currentPosition++;

			switch(cachedType){
				case MemoryStorageType.EMPTY:
					slots[currentSlot] = new EmptyPlayerInventorySlot();
					break;
				case MemoryStorageType.ITEM:
					cachedId = NetDecoder.ReadUshort(data, currentPosition);
					currentPosition += 2;
					cachedQuantity = NetDecoder.ReadByte(data, currentPosition);
					currentPosition++;
					slots[currentSlot] = new ItemPlayerInventorySlot(cachedId, cachedQuantity);
					break;
				case MemoryStorageType.WEAPON:
					cachedId = NetDecoder.ReadUshort(data, currentPosition);
					currentPosition += 2;
					cachedDurability = NetDecoder.ReadUint(data, currentPosition);
					currentPosition += 8;
					cachedRefine = NetDecoder.ReadByte(data, currentPosition);
					currentPosition++;
					cachedEnchant = (EnchantmentType)NetDecoder.ReadByte(data, currentPosition);
					currentPosition++;
					break;
				case MemoryStorageType.STORAGE:
					cachedId = NetDecoder.ReadUshort(data, currentPosition);
					currentPosition += 2;
					cachedInventorySize = NetDecoder.ReadByte(data, currentPosition);
					currentPosition++;
					cachedInventory = BuildInventory(data, currentPosition, 30, ref bytesWritten, initialSlot:currentSlot);
					currentPosition += bytesWritten;
					break;
			}

			currentSlot++;
		}

		return slots;
	}
}


/*
Empty Inventory Slot
*/
public class EmptyPlayerInventorySlot : PlayerServerInventorySlot {

	public EmptyPlayerInventorySlot(){
		this.type = MemoryStorageType.EMPTY;
		this.slotMemorySize = 1;

	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		return this.slotMemorySize;
	}

	public override int GetItemID(){return -1;}

	public override int GetQuantity(){return 0;}
}

/*
Inventory Slot that contains a basic and untagged item
*/
public class ItemPlayerInventorySlot : PlayerServerInventorySlot {
	public ItemPlayerInventorySlot(ushort id, byte quantity){
		this.type = MemoryStorageType.ITEM;
		this.slotMemorySize = 4;
		this.itemID = id;
		this.quantity = quantity;
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		NetDecoder.WriteUshort(this.itemID, buffer, init+1);
		NetDecoder.WriteByte(this.quantity, buffer, init+3);
		return this.slotMemorySize;
	}

	public override void SetQuantity(byte quantity){
		this.quantity = quantity;
	}

	public override int GetQuantity(){
		return this.quantity;
	}
}

/*
Inventory Slot that contains a Weapon
*/
public class WeaponPlayerInventorySlot : PlayerServerInventorySlot {
	private uint currentDurability;
	private byte refineLevel;
	private EnchantmentType enchant;

	public WeaponPlayerInventorySlot(ushort id, uint currentDurability, byte refineLevel, EnchantmentType enchant){
		this.type = MemoryStorageType.WEAPON;
		this.slotMemorySize = 9;
		this.itemID = id;
		this.currentDurability = currentDurability;
		this.refineLevel = refineLevel;
		this.enchant = enchant;
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		NetDecoder.WriteUshort(this.itemID, buffer, init+1);
		NetDecoder.WriteUint(this.currentDurability, buffer, init+3);
		NetDecoder.WriteByte(this.refineLevel, buffer, init+7);
		NetDecoder.WriteByte((byte)this.enchant, buffer, init+8);
		return this.slotMemorySize;
	}

	public void SetDurability(uint dur){
		this.currentDurability = dur;
	}
}

/*
Inventory Slot for Storage items
*/
public class StoragePlayerInventorySlot : PlayerServerInventorySlot {
	private byte inventorySize;
	private PlayerServerInventorySlot[] inventory;

	public StoragePlayerInventorySlot(ushort id, byte inventorySize, PlayerServerInventorySlot[] inventory){
		int size = 0;

		this.type = MemoryStorageType.STORAGE;
		this.itemID = id;
		this.inventorySize = inventorySize;
		this.inventory = inventory;

		if(inventory == null)
			this.slotMemorySize = 4 + this.inventorySize;
		else{
			for(int i=0; i < inventory.Length; i++){
				size += inventory[i].GetSlotMemorySize();
			}

			this.slotMemorySize = 4 + size;
		}
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		int size = 0;

		NetDecoder.WriteByte((byte)this.type, buffer, init);
		NetDecoder.WriteUshort(this.itemID, buffer, init+1);
		
		if(this.inventory == null){
			for(int i=0; i < this.inventorySize; i++){
				NetDecoder.WriteByte(0, buffer, init+3+i);
			}
		}
		else{
			for(int i=0; i < this.inventory.Length; i++){
				size += this.inventory[i].SaveToBuffer(buffer, init+3+size);
			}
		}

		return this.slotMemorySize;
	}
}
