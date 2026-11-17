using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/*
A Weapon that can be refined and enchanted
*/
[Serializable]
public class Weapon : BaseWeapon {
	/*
	Saved Fields:

	ItemID (2)
	CurrentDurability (4)
	RefineLevel (1)
	ExtraEffect (1)
	*/

	public byte refineLevel;
	public EnchantmentType extraEffect;

	public virtual void SetRefineLevel(byte level){this.refineLevel = level;}
	public virtual void SetExtraEffects(EnchantmentType enchant){this.extraEffect = enchant;}

	protected override void AppendStats(StringBuilder sb){
		sb.Append("\nRefinement: ");
		sb.Append(this.refineLevel.ToString());
		sb.Append("\nExtra: ");
		sb.Append(this.extraEffect.ToString());
	}

	public override Item Copy() {
		Weapon copy = (Weapon)CopyFieldsTo(new Weapon());
		copy.refineLevel = this.refineLevel;
		copy.extraEffect = this.extraEffect;
		return copy;
	}
}