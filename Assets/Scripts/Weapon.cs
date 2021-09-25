using System.Collections;
using System.Collections.Generic;

public abstract class Weapon : Item
{
	public ushort damage;
	public uint maxDurability;
	public uint currentDurability;
	public ushort impact;
	public Dictionary<SkillType, byte> requiredLevels;
	public WeaponType type;
	public byte refineLevel;
	public EnchantmentType extraEffect;
	

	public virtual void SetDamage(ushort damage){this.damage = damage;}
	public virtual void SetMaxDurability(uint dur){this.maxDurability = dur;}
	public virtual void SetDurability(uint dur){this.currentDurability = dur;}
	public virtual void SetImpact(ushort imp){this.impact = imp;}
	public virtual void SetRequirements(Dictionary<SkillType, byte> requirements){this.requiredLevels = requirements;}
	public virtual void SetWeaponType(WeaponType type){this.type = type;}
	public virtual void SetRefineLevel(byte level){this.refineLevel = level;}
	public virtual void SetExtraEffects(EnchantmentType enchant){this.extraEffect = enchant;}

	// Returns false if item should be broken
	public virtual bool LowerDurability(ushort damage){
		if(damage >= currentDurability){
			currentDurability = 0;
			return false;
		}

		currentDurability -= damage;
		return true;
	}
}

public enum WeaponType : byte{
	FIST,
	DAGGER,
	SHORTSWORD,
	BROADSWORD,
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
}
