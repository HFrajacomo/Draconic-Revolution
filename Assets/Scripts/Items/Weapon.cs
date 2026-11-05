using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class Weapon : Item {
	/*
	Saved Fields:

	ItemID (2)
	CurrentDurability (4)
	RefineLevel (1)
	ExtraEffect (1)
	*/

	public ushort damage;
	public uint maxDurability;
	public uint currentDurability;
	public ushort impact;
	public string weaponType;
	public byte refineLevel;
	public EnchantmentType extraEffect;
	public Dictionary<SkillType, byte> requiredLevels;

	private WeaponType type;

	public virtual void SetDamage(ushort damage){this.damage = damage;}
	public virtual void SetMaxDurability(uint dur){this.maxDurability = dur;}
	public virtual void SetDurability(uint dur){this.currentDurability = dur;}
	public virtual void SetImpact(ushort imp){this.impact = imp;}
	public virtual void SetRequirements(Dictionary<SkillType, byte> requirements){this.requiredLevels = requirements;}
	public virtual void SetWeaponType(WeaponType type){this.type = type;}
	public virtual void SetRefineLevel(byte level){this.refineLevel = level;}
	public virtual void SetExtraEffects(EnchantmentType enchant){this.extraEffect = enchant;}
	public virtual WeaponType GetWeaponType(){return this.type;}
	public override string[] GetDetails(){return new string[2]{this.name, this.GetStats()};}

	public virtual string GetStats(){
		StringBuilder sb = new StringBuilder();

		sb.Append("Damage: ");
		sb.Append(this.damage.ToString());
		sb.Append("\nDurability: ");
		sb.Append(this.currentDurability.ToString());
		sb.Append("\nImpact: ");
		sb.Append(this.impact.ToString());
		sb.Append("\nRefinement: ");
		sb.Append(this.refineLevel.ToString());
		sb.Append("\nExtra: ");
		sb.Append(this.extraEffect.ToString());

		return sb.ToString();
	}

	public virtual void ResolveWeaponType(){
		if(Enum.TryParse<WeaponType>(this.weaponType, false, out WeaponType parsed))
			this.type = parsed;
		else
			throw new DeserializationErrorException($"[Weapon] Failed on PostSerializationSetup because weaponType {this.weaponType} is invalid");
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

	public override Item Copy() {
		return CopyFieldsTo(new Weapon());
	}

	// Subclasses pass their own empty instance so copies keep their concrete type
	protected Weapon CopyFieldsTo(Weapon copy){
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
		copy.refineLevel = this.refineLevel;
		copy.extraEffect = this.extraEffect;
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
