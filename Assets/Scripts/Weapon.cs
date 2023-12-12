using System.Collections;
using System.Collections.Generic;
using System.Text;

public abstract class Weapon : Item
{
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
