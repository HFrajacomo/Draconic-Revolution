using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerServerInventorySlot{
	private MemoryStorageType type;
	private int slotMemorySize;

	public int GetSlotMemorySize(){return this.slotMemorySize;}
	public abstract int SaveToBuffer(byte[] buffer, int init);
}


/*
Empty Inventory Slot
*/
public class EmptyPlayerInventorySlot : PlayerServerInventorySlot {
	private MemoryStorageType type;
	private int slotMemorySize = 1;

	public EmptyPlayerInventorySlot(){
		this.type = MemoryStorageType.EMPTY;
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		return this.slotMemorySize;
	}
}

/*
Inventory Slot that contains a basic and untagged item
*/
public class ItemPlayerInventorySlot : PlayerServerInventorySlot {
	private MemoryStorageType type;
	private int slotMemorySize = 4;
	private ItemID itemID;
	private byte quantity;

	public ItemPlayerInventorySlot(ItemID id, byte quantity){
		this.type = MemoryStorageType.ITEM;
		this.itemID = id;
		this.quantity = quantity;
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		NetDecoder.WriteUshort((ushort)this.itemID, buffer, init+1);
		NetDecoder.WriteByte(this.quantity, buffer, init+3);
		return this.slotMemorySize;
	}
}

/*
Inventory Slot that contains a Weapon
*/
public class WeaponPlayerInventorySlot : PlayerServerInventorySlot {
	private MemoryStorageType type;
	private int slotMemorySize = 9;
	private ItemID itemID;
	private uint currentDurability;
	private byte refineLevel;
	private EnchantmentType enchant;

	public WeaponPlayerInventorySlot(ItemID id, uint currentDurability, byte refineLevel, EnchantmentType enchant){
		this.type = MemoryStorageType.WEAPON;
		this.itemID = id;
		this.currentDurability = currentDurability;
		this.refineLevel = refineLevel;
		this.enchant = enchant;
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		NetDecoder.WriteUshort((ushort)this.itemID, buffer, init+1);
		NetDecoder.WriteUint(this.currentDurability, buffer, init+3);
		NetDecoder.WriteByte(this.refineLevel, buffer, init+7);
		NetDecoder.WriteByte((byte)this.enchant, buffer, init+8);
		return this.slotMemorySize;
	}
}

/*
Inventory Slot for Storage items
*/
public class StoragePlayerInventorySlot : PlayerServerInventorySlot {
	private MemoryStorageType type;
	private ItemID itemID;
	private byte inventorySize;
	private PlayerServerInventorySlot inventory;
	private int slotMemorySize;

	public StoragePlayerInventorySlot(ItemID id, byte inventorySize, PlayerServerInventorySlot inventory){
		this.itemID = id;
		this.inventorySize = inventorySize;
		this.inventory = inventory;

		if(inventory == null)
			this.slotMemorySize = 4 + this.inventorySize;
		else
			this.slotMemorySize = 4 + this.inventory.GetSlotMemorySize();
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		NetDecoder.WriteUshort((ushort)this.itemID, buffer, init+1);
		
		if(this.inventory == null){
			for(int i=0; i < this.inventorySize; i++){
				NetDecoder.WriteByte(0, buffer, init+2+i);
			}
		}
		else{
			this.inventory.SaveToBuffer(buffer, init+2);
		}

		return this.slotMemorySize;
	}
}
