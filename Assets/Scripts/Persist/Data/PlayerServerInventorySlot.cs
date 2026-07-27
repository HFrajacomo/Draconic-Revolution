using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerServerInventorySlot{
	protected MemoryStorageType type;
	protected int slotMemorySize;
	protected ushort itemID;
	protected byte quantity;
	protected InventoryType inventoryType;
	protected byte slotID;

	public int GetSlotMemorySize(){return this.slotMemorySize;}
	public byte GetSlotID(){return this.slotID;}
	public ItemStack GetItemStack(){return new ItemStack(this.itemID, this.quantity);}
	public abstract int SaveToBuffer(byte[] buffer, int init);
	public virtual int GetItemID(){return (int)this.itemID;}
	public virtual int GetQuantity(){return 1;}
	public virtual void SetQuantity(byte quantity){}
	public InventoryType GetInventoryType(){return this.inventoryType;}
	public bool IsInGlobalWhitelist(ItemStack its){return InventoryLoader.GetInventory(GetInventoryType()).IsInGlobalWhitelist(its);}
	public bool IsInLocalWhitelist(ItemStack its){return InventoryLoader.GetInventory(GetInventoryType()).IsInLocalWhitelist(its, this.slotID);}

	public static List<PlayerServerInventorySlot> BuildInventory(byte[] data, int init, ref int bytesWritten, int readSize = -1){
		List<PlayerServerInventorySlot> slots = new List<PlayerServerInventorySlot>();

		// Cached data
		MemoryStorageType cachedType;
		ushort cachedId;
		byte cachedQuantity;
		uint cachedDurability;
		byte cachedRefine;
		EnchantmentType cachedEnchant;

		// Iterators
		int currentPosition = init;
		InventoryType invType;

		if(readSize == -1){
			readSize = data.Length;
		}

		while(currentPosition < readSize){
			invType = (InventoryType)NetDecoder.ReadByte(data, currentPosition);
			currentPosition++;

			for(byte i=0; i < InventoryLoader.GetInventorySize(invType); i++){
				cachedType = (MemoryStorageType)NetDecoder.ReadByte(data, currentPosition);
				currentPosition++;

				switch(cachedType){
					case MemoryStorageType.EMPTY:
						slots.Add(new EmptyPlayerInventorySlot(invType, i));
						break;
					case MemoryStorageType.ITEM:
						cachedId = NetDecoder.ReadUshort(data, currentPosition);
						currentPosition += 2;
						cachedQuantity = NetDecoder.ReadByte(data, currentPosition);
						currentPosition++;
						slots.Add(new ItemPlayerInventorySlot(cachedId, cachedQuantity, invType, i));
						break;
					case MemoryStorageType.WEAPON:
						cachedId = NetDecoder.ReadUshort(data, currentPosition);
						currentPosition += 2;
						cachedDurability = NetDecoder.ReadUint(data, currentPosition);
						currentPosition += 4;
						cachedRefine = NetDecoder.ReadByte(data, currentPosition);
						currentPosition++;
						cachedEnchant = (EnchantmentType)NetDecoder.ReadByte(data, currentPosition);
						currentPosition++;
						slots.Add(new WeaponPlayerInventorySlot(cachedId, cachedDurability, cachedRefine, cachedEnchant, invType, i));
						break;
				}
			}
		}

		return slots;
	}
}


/*
Empty Inventory Slot
*/
public class EmptyPlayerInventorySlot : PlayerServerInventorySlot {

	public EmptyPlayerInventorySlot(InventoryType invType, byte slotID){
		this.type = MemoryStorageType.EMPTY;
		this.slotMemorySize = 1;
		this.inventoryType = invType;
		this.slotID = slotID;
	}

	public override int SaveToBuffer(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)this.type, buffer, init);
		return this.slotMemorySize;
	}

	public static int WriteBlank(byte[] buffer, int init){
		NetDecoder.WriteByte((byte)MemoryStorageType.EMPTY, buffer, init);
		return 1;
	}

	public override int GetItemID(){return 0;}

	public override int GetQuantity(){return 0;}
}

/*
Inventory Slot that contains a basic and untagged item
*/
public class ItemPlayerInventorySlot : PlayerServerInventorySlot {
	public ItemPlayerInventorySlot(ushort id, byte quantity, InventoryType invType, byte slotID){
		this.type = MemoryStorageType.ITEM;
		this.slotMemorySize = 4;
		this.itemID = id;
		this.quantity = quantity;
		this.inventoryType = invType;
		this.slotID = slotID;
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

	public WeaponPlayerInventorySlot(ushort id, uint currentDurability, byte refineLevel, EnchantmentType enchant, InventoryType invType, byte slotID){
		this.type = MemoryStorageType.WEAPON;
		this.slotMemorySize = 9;
		this.itemID = id;
		this.currentDurability = currentDurability;
		this.refineLevel = refineLevel;
		this.enchant = enchant;
		this.inventoryType = invType;
		this.slotID = slotID;
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

	public StoragePlayerInventorySlot(ushort id, byte inventorySize, PlayerServerInventorySlot[] inventory, InventoryType invType, byte slotID){
		int size = 0;

		this.type = MemoryStorageType.STORAGE;
		this.itemID = id;
		this.inventorySize = inventorySize;
		this.inventory = inventory;
		this.slotID = slotID;

		if(inventory == null)
			this.slotMemorySize = 4 + this.inventorySize;
		else{
			for(int i=0; i < inventory.Length; i++){
				size += inventory[i].GetSlotMemorySize();
			}

			this.slotMemorySize = 4 + size;
		}

		this.inventoryType = invType;
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
