using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This class is used to, given the two weapon ItemStacks in a Player's equipment, figure out which BattleStyle they should be using
	Slots are order-agnostic: the resolution decides which item is the main hand (right) and which is the off-hand (left)
	A SubWeapon with a style of its own (like the Torch) uses it as the main style when equipped alone
	A carry-only SubWeapon (like a Book) is carried by the Unarmed style when equipped alone
	A Weapon equipped together with a SubWeapon uses the Weapon's style, plus the SubWeapon's Carry Style if the Weapon's style allows carrying
	Two Weapons use the exact pair map. Two SubWeapons are never valid
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
	public static readonly int NO_CARRY = -1;
	private static readonly int EQUIPMENT_INVENTORY_CODE = 3;
	private static readonly byte FIRST_SLOT = 0;
	private static readonly byte SECOND_SLOT = 1;
	private static readonly string UNARMED_STYLE = "BASE_Unarmed";

	private static readonly Dictionary<(WeaponType, WeaponType), string> _map =
		new Dictionary<(WeaponType, WeaponType), string>
		{
			{(WeaponType.FIST, WeaponType.FIST), "BASE_Unarmed"},
			{(WeaponType.FIST, WeaponType.BASTARDSWORD), "BASE_Sword"},
			{(WeaponType.FIST, WeaponType.PICKAXE), "BASE_Pickaxe"},
			{(WeaponType.FIST, WeaponType.TORCH), "BASE_Torch"}
		};

	// Resolves the code of the main BattleStyle of a player based on their equipment
	public static int Resolve(ChunkLoader_Server cl, ulong playerCode){
		ResolveEquipment(cl, playerCode, out string style, out AttachmentItems items);
		return AnimationLoader.GetBattleStyle(style).GetCode();
	}

	// Gets the IDs of the items in each hand (right is always the main one), used by clients to skin BattleStyle attachments and resolve carries
	public static AttachmentItems ResolveAttachmentItems(ChunkLoader_Server cl, ulong playerCode){
		if(!cl.playerServerInventory.HasInventory(playerCode))
			return AttachmentItems.NONE;

		ResolveEquipment(cl, playerCode, out string style, out AttachmentItems items);
		return items;
	}

	// Checks if two items can be equipped together. Used by clients before changing equipment
	public static bool IsValidCombination(Item first, Item second){
		if(!IsEquippable(first) || !IsEquippable(second))
			return false;

		return TryResolve(first as BaseWeapon, second as BaseWeapon, out string style, out BaseWeapon main, out BaseWeapon offhand);
	}

	/*
	Gets the code of the Carry Style that should be layered on top of the given main style
		Returns NO_CARRY if the off-hand item isn't a SubWeapon, if the main style doesn't allow carrying or if the Carry Style isn't available
	*/
	public static int GetCarryStyleCode(int mainStyleCode, AttachmentItems items){
		if(items.leftHand == 0)
			return NO_CARRY;

		SubWeapon sub = ItemLoader.GetItem(items.leftHand) as SubWeapon;

		if(sub == null)
			return NO_CARRY;
		if(!AnimationLoader.GetBattleStyle(mainStyleCode).AllowsCarry())
			return NO_CARRY;
		if(!TryGetCarryStyle(sub, out CarryStyleData carryStyle))
			return NO_CARRY;

		return carryStyle.GetCode();
	}

	private static void ResolveEquipment(ChunkLoader_Server cl, ulong playerCode, out string style, out AttachmentItems items){
		BaseWeapon first = null;
		BaseWeapon second = null;
		BaseWeapon main, offhand;

		if(cl.playerServerInventory.HasInventory(playerCode)){
			first = GetWeapon(cl.playerServerInventory.GetSlot(playerCode, EQUIPMENT_INVENTORY_CODE, FIRST_SLOT));
			second = GetWeapon(cl.playerServerInventory.GetSlot(playerCode, EQUIPMENT_INVENTORY_CODE, SECOND_SLOT));
		}

		if(!TryResolve(first, second, out style, out main, out offhand)){
			Debug.LogWarning($"[BattleStyleDeterminator] Player {playerCode} has an invalid equipment combination ({Describe(first)}, {Describe(second)}). Only one of them will be used");

			if(TryResolve(first, null, out style, out main, out offhand)){}
			else if(TryResolve(null, second, out style, out main, out offhand)){}
			else{
				style = UNARMED_STYLE;
				main = null;
				offhand = null;
			}
		}

		items = new AttachmentItems(GetID(main), GetID(offhand));
	}

	private static bool TryResolve(BaseWeapon first, BaseWeapon second, out string style, out BaseWeapon main, out BaseWeapon offhand){
		style = null;
		main = null;
		offhand = null;

		if(first == null && second == null)
			return TryGetStyle(WeaponType.FIST, WeaponType.FIST, out style);

		if(first == null || second == null)
			return TryResolveSingle(first != null ? first : second, out style, out main, out offhand);

		bool firstIsSub = first is SubWeapon;
		bool secondIsSub = second is SubWeapon;

		if(firstIsSub && secondIsSub)
			return false;

		if(firstIsSub || secondIsSub){
			main = firstIsSub ? second : first;
			offhand = firstIsSub ? first : second;

			// Bespoke pair styles take priority over carrying
			if(((SubWeapon)offhand).HasOwnStyle() && TryGetStyle(main.GetWeaponType(), offhand.GetWeaponType(), out style))
				return true;

			if(!TryGetStyle(WeaponType.FIST, main.GetWeaponType(), out style))
				return false;

			return CanCarry(style, (SubWeapon)offhand);
		}

		main = first;
		offhand = second;
		return TryGetStyle(first.GetWeaponType(), second.GetWeaponType(), out style);
	}

	private static bool TryResolveSingle(BaseWeapon weapon, out string style, out BaseWeapon main, out BaseWeapon offhand){
		SubWeapon sub = weapon as SubWeapon;

		main = weapon;
		offhand = null;

		if(sub == null)
			return TryGetStyle(WeaponType.FIST, weapon.GetWeaponType(), out style);

		if(sub.HasOwnStyle() && TryGetStyle(WeaponType.FIST, sub.GetWeaponType(), out style))
			return true;

		// Carry-only SubWeapons are carried in the off-hand of the Unarmed style
		main = null;
		offhand = sub;
		style = UNARMED_STYLE;
		return CanCarry(style, sub);
	}

	private static bool CanCarry(string mainStyle, SubWeapon sub){
		if(!AnimationLoader.GetBattleStyle(mainStyle).AllowsCarry())
			return false;

		return TryGetCarryStyle(sub, out CarryStyleData carryStyle);
	}

	private static bool TryGetCarryStyle(SubWeapon sub, out CarryStyleData carryStyle){
		carryStyle = null;

		if(!sub.HasCarryStyle() || !AnimationLoader.ContainsCarryStyle(sub.GetCarryStyle()))
			return false;

		carryStyle = AnimationLoader.GetCarryStyle(sub.GetCarryStyle());
		return carryStyle.IsAvailable();
	}

	private static bool TryGetStyle(WeaponType first, WeaponType second, out string style){
		return _map.TryGetValue(Normalize(first, second), out style);
	}

	private static BaseWeapon GetWeapon(PlayerServerInventorySlot slot){
		if(slot is EmptyPlayerInventorySlot)
			return null;

		return ItemLoader.GetItem((ushort)slot.GetItemID()) as BaseWeapon;
	}

	private static bool IsEquippable(Item item){return item == null || item is BaseWeapon;}
	private static ushort GetID(BaseWeapon weapon){return weapon == null ? (ushort)0 : weapon.GetID();}
	private static string Describe(BaseWeapon weapon){return weapon == null ? "Empty" : weapon.codename;}
	private static (WeaponType, WeaponType) Normalize(WeaponType first, WeaponType second){return first <= second ? (first, second) : (second, first);}
}
