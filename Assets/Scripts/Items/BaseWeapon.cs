using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/*
Everything that can be equipped as a Weapon (Weapons and SubWeapons)
	Holds the data shared by both, such as damage, durability and the WeaponType that decides BattleStyles
	Refinement and Enchantments are exclusive to Weapons
*/
[Serializable]
public abstract class BaseWeapon : Item {
	/*
	Saved Fields:

	ItemID (2)
	CurrentDurability (4)
	*/

	public ushort damage;
	public uint maxDurability;
	public uint currentDurability;
	public ushort impact;
	public string weaponType;
	public Dictionary<SkillType, byte> requiredLevels;

	private WeaponType type;

	public virtual void SetDamage(ushort damage){this.damage = damage;}
	public virtual void SetMaxDurability(uint dur){this.maxDurability = dur;}
	public virtual void SetDurability(uint dur){this.currentDurability = dur;}
	public virtual void SetImpact(ushort imp){this.impact = imp;}
	public virtual void SetRequirements(Dictionary<SkillType, byte> requirements){this.requiredLevels = requirements;}
	public virtual void SetWeaponType(WeaponType type){this.type = type;}
	public virtual WeaponType GetWeaponType(){return this.type;}
	public override string[] GetDetails(){return new string[2]{this.name, this.GetStats()};}

	public string GetStats(){
		StringBuilder sb = new StringBuilder();

		sb.Append("Damage: ");
		sb.Append(this.damage.ToString());
		sb.Append("\nDurability: ");
		sb.Append(this.currentDurability.ToString());
		sb.Append("\nImpact: ");
		sb.Append(this.impact.ToString());
		AppendStats(sb);

		return sb.ToString();
	}

	// Subclasses append the stats that only they have
	protected virtual void AppendStats(StringBuilder sb){}

	public virtual void ResolveWeaponType(){
		if(Enum.TryParse<WeaponType>(this.weaponType, false, out WeaponType parsed))
			this.type = parsed;
		else
			throw new DeserializationErrorException($"[{this.GetType().Name}] Failed on PostSerializationSetup because weaponType {this.weaponType} is invalid");
	}


	// Returns false if item should be broken
	public virtual bool LowerDurability(ushort damage){
		if(damage >= currentDurability){
			currentDurability = 0;
			return false;
		}

		currentDurability -= damage;
		return true;
	}

	// Subclasses pass their own empty instance so copies keep their concrete type
	protected BaseWeapon CopyFieldsTo(BaseWeapon copy){
		// Copy base Item fields
		copy.codename = this.codename;
		copy.name = this.name;
		copy.description = this.description;
		copy.id = this.id;
		copy.memoryType = this.memoryType;
		copy.memoryStorageType = this.memoryStorageType;
		copy.stacksize = this.stacksize;
		copy.hasDurability = this.hasDurability;
		copy.attachmentMaterials = this.attachmentMaterials;
		copy.onHoldPlayerBehaviour = this.onHoldPlayerBehaviour != null ? new List<ItemBehaviour>(this.onHoldPlayerBehaviour) : null;
		copy.onHoldClientBehaviour = this.onHoldClientBehaviour != null ? new List<ItemBehaviour>(this.onHoldClientBehaviour) : null;
		copy.onHoldServerBehaviour = this.onHoldServerBehaviour != null ? new List<ItemBehaviour>(this.onHoldServerBehaviour) : null;
		copy.onUnholdPlayerBehaviour = this.onUnholdPlayerBehaviour != null ? new List<ItemBehaviour>(this.onUnholdPlayerBehaviour) : null;
		copy.onUnholdClientBehaviour = this.onUnholdClientBehaviour != null ? new List<ItemBehaviour>(this.onUnholdClientBehaviour) : null;
		copy.onUnholdServerBehaviour = this.onUnholdServerBehaviour != null ? new List<ItemBehaviour>(this.onUnholdServerBehaviour) : null;
		copy.onUseClientBehaviour = this.onUseClientBehaviour != null ? new List<ItemBehaviour>(this.onUseClientBehaviour) : null;
		copy.onUseServerBehaviour = this.onUseServerBehaviour != null ? new List<ItemBehaviour>(this.onUseServerBehaviour) : null;

		copy.damage = this.damage;
		copy.maxDurability = this.maxDurability;
		copy.currentDurability = this.currentDurability;
		copy.impact = this.impact;
		copy.type = this.type;
		copy.requiredLevels = this.requiredLevels != null ? new Dictionary<SkillType, byte>(this.requiredLevels) : null;

		return copy;
	}
}

public enum WeaponType : byte{
	FIST,
	DAGGER,
	SHORTSWORD,
	BASTARDSWORD,
	LONGSWORD,
	RAPIER,
	AXE,
	PICKAXE,
	MACE,
	TORCH,
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
}
