using System;
using System.Collections;
using System.Collections.Generic;

/*
This class is used to, given the two weapon ItemStacks in a Player's inventory, it figures out which BattleStyle they should be using
TODO: Adequate this to handle Combat BattleStyles AND dual-wielding in the future
*/

/*
	FIST,
	DAGGER,
	SHORTSWORD,
	BASTARDSWORD,
	LONGSWORD,
	RAPIER,
	AXE,
	PICKAXE,
	MACE,
	BOW,
	GREATSWORD,
	SCYTHE,
	HAMMER,
	CLEAVER,
	SPEAR,
	SHIELD,
	ULTRASWORD,
	ULTRASCYTHE,
	ULTRAHAMMER,
	ULTRACLEAVER,
	BALLISTA
*/

public static class BattleStyleDeterminator {
	private static readonly Dictionary<(WeaponType, WeaponType), string> _map =
		new Dictionary<(WeaponType, WeaponType), string>
		{
			{(WeaponType.FIST, WeaponType.FIST), "BASE_Unarmed"},
			{(WeaponType.FIST, WeaponType.BASTARDSWORD), "BASE_Sword"},
			{(WeaponType.FIST, WeaponType.PICKAXE), "BASE_Pickaxe"}
		};

	public static string Resolve(ItemStack its1, ItemStack its2){
		WeaponType w1 = WeaponType.FIST;
		WeaponType w2 = WeaponType.FIST;

		if(its1 != null)
			w1 = ((Weapon)its1.GetItem()).GetWeaponType();
		if(its2 != null)
			w2 = ((Weapon)its2.GetItem()).GetWeaponType();

		(WeaponType, WeaponType) key = Normalize(w1, w2);

		if(_map.ContainsKey(key)){
			return _map[key];
		}
		else{
			throw new InvalidEquipmentComboException($"[BattleStyleDeterminator] Couldn't resolve any style that contains -> {key}");
		}
	}

	public static string Resolve(PlayerServerInventorySlot slot1, PlayerServerInventorySlot slot2){
		ItemStack its1 = null;
		ItemStack its2 = null;

		if(slot1 is not EmptyPlayerInventorySlot){
			its1 = new ItemStack((ushort)slot1.GetItemID(), (byte)slot1.GetQuantity());
		}

		if(slot2 is not EmptyPlayerInventorySlot){
			its2 = new ItemStack((ushort)slot2.GetItemID(), (byte)slot2.GetQuantity());
		}

		return Resolve(its1, its2);
	}

	private static (WeaponType, WeaponType) Normalize(WeaponType first, WeaponType second){return first <= second ? (first, second) : (second, first);}
}