using System;
using UnityEngine;

/*
Reads and writes a single inventory slot using the MemoryStorageType layout of inventory saves (ItemStack.ConvertToMemory)
	EMPTY		[Type 1]
	ITEM		[Type 1][ItemID 2][Amount 1]
	WEAPON		[Type 1][ItemID 2][Durability 4][RefineLevel 1][Enchantment 1]
	SUBWEAPON	[Type 1][ItemID 2][Durability 4][Amount 1]

Used by container messages and container files, so their layout must be kept
*/
public static class InventorySlotCodec{
	public static readonly int MAX_SLOT_SIZE = 9;

	// Writes a slot and returns the amount of written bytes. Null and empty stacks are written as EMPTY
	public static int Write(ItemStack its, byte[] data, int pos){
		if(its == null || its.GetAmount() == 0){
			data[pos] = (byte)MemoryStorageType.EMPTY;
			return 1;
		}

		return its.ConvertToMemory(data, pos);
	}

	// Reads a slot written by Write. Returns null for EMPTY
	public static ItemStack Read(byte[] data, int pos, out int bytesRead){
		ushort id;
		byte amount;

		switch((MemoryStorageType)data[pos]){
			case MemoryStorageType.ITEM:
				id = NetDecoder.ReadUshort(data, pos+1);
				amount = data[pos+3];
				bytesRead = 4;
				return new ItemStack(ItemLoader.GetCopy(id), amount);
			case MemoryStorageType.WEAPON:
				Weapon weapon;

				id = NetDecoder.ReadUshort(data, pos+1);
				weapon = (Weapon)ItemLoader.GetCopy(id);
				weapon.SetDurability(NetDecoder.ReadUint(data, pos+3));
				weapon.SetRefineLevel(data[pos+7]);
				weapon.SetExtraEffects((EnchantmentType)data[pos+8]);
				bytesRead = 9;
				return new ItemStack(weapon, 1);
			case MemoryStorageType.SUBWEAPON:
				SubWeapon subWeapon;

				id = NetDecoder.ReadUshort(data, pos+1);
				subWeapon = (SubWeapon)ItemLoader.GetCopy(id);
				subWeapon.SetDurability(NetDecoder.ReadUint(data, pos+3));
				amount = data[pos+7];
				bytesRead = 8;
				return new ItemStack(subWeapon, amount);
			default:
				bytesRead = 1;
				return null;
		}
	}

	// Size of the slot written at pos, without reading it
	public static int GetSize(byte[] data, int pos){
		return GetSize((MemoryStorageType)data[pos]);
	}

	public static int GetSize(MemoryStorageType type){
		switch(type){
			case MemoryStorageType.ITEM:
				return 4;
			case MemoryStorageType.WEAPON:
				return 9;
			case MemoryStorageType.SUBWEAPON:
				return 8;
			default:
				return 1;
		}
	}

	// Whether two written slots are identical
	public static bool AreEqual(byte[] a, int posA, byte[] b, int posB){
		int size = GetSize(a, posA);

		if(size != GetSize(b, posB))
			return false;

		for(int i=0; i < size; i++){
			if(a[posA+i] != b[posB+i])
				return false;
		}

		return true;
	}

	// Copies a written slot to an array of its own
	public static byte[] Copy(byte[] data, int pos){
		int size = GetSize(data, pos);
		byte[] output = new byte[size];

		Buffer.BlockCopy(data, pos, output, 0, size);
		return output;
	}

	// Writes a slot to an array of its own
	public static byte[] ToArray(ItemStack its){
		byte[] cache = new byte[MAX_SLOT_SIZE];
		int size = Write(its, cache, 0);
		byte[] output = new byte[size];

		Buffer.BlockCopy(cache, 0, output, 0, size);
		return output;
	}

	/*
	Checks if a slot sent by a client describes a real item that can be stored
	Returns false for unknown items, a storage type that doesn't match the item, or an amount outside of 1 to its stacksize
	Empty slots are valid
	*/
	public static bool IsValid(byte[] data, int pos, int length){
		MemoryStorageType type;
		ushort id;
		Item item;
		byte amount;

		if(pos >= length)
			return false;

		type = (MemoryStorageType)data[pos];

		if(type == MemoryStorageType.EMPTY)
			return true;

		if(type != MemoryStorageType.ITEM && type != MemoryStorageType.WEAPON && type != MemoryStorageType.SUBWEAPON)
			return false;

		if(pos + GetSize(type) > length)
			return false;

		id = NetDecoder.ReadUshort(data, pos+1);

		if(id == 0 || id >= ItemLoader.GetItemCount())
			return false;

		item = ItemLoader.GetItem(id);

		if(item == null || GetWrittenType(item) != type)
			return false;

		switch(type){
			case MemoryStorageType.ITEM:
				amount = data[pos+3];
				break;
			case MemoryStorageType.SUBWEAPON:
				amount = data[pos+7];
				break;
			default:
				amount = 1;
				break;
		}

		return amount > 0 && amount <= item.stacksize;
	}

	// Type an item is written as by ItemStack.ConvertToMemory
	private static MemoryStorageType GetWrittenType(Item item){
		switch(item.GetMemoryStorageType()){
			case MemoryStorageType.WEAPON:
				return MemoryStorageType.WEAPON;
			case MemoryStorageType.SUBWEAPON:
				return MemoryStorageType.SUBWEAPON;
			default:
				return MemoryStorageType.ITEM;
		}
	}
}
