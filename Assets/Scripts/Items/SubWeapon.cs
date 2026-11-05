using System;

/*
A Weapon that can be carried in the off-hand (Torch, Book, Lantern...)
	carryStyle names the Carry Style (Resources/CarryStyles) layered on top of the main BattleStyle while it is carried
	weaponType is optional:
		- If it maps to a BattleStyle of its own (like TORCH), that style is used when it is the only equipped weapon
		- If it's empty (like a Book), the SubWeapon is carry-only and is carried by the Unarmed style when equipped alone
	Two SubWeapons can never be equipped at the same time
*/
[Serializable]
public class SubWeapon : Weapon {
	public string carryStyle;
	private bool hasOwnStyle = false;

	public override void ResolveWeaponType(){
		if(string.IsNullOrEmpty(this.weaponType)){
			SetWeaponType(WeaponType.FIST);
			this.hasOwnStyle = false;
		}
		else{
			base.ResolveWeaponType();
			this.hasOwnStyle = true;
		}

		if(string.IsNullOrEmpty(this.carryStyle) && !this.hasOwnStyle)
			throw new DeserializationErrorException($"[SubWeapon] {this.codename} has neither a weaponType nor a carryStyle, so it could never be equipped");
	}

	// Whether this SubWeapon declares a weaponType that may map to a standalone BattleStyle
	public bool HasOwnStyle(){return this.hasOwnStyle;}
	public bool HasCarryStyle(){return !string.IsNullOrEmpty(this.carryStyle);}
	public string GetCarryStyle(){return this.carryStyle;}

	public override Item Copy(){
		SubWeapon copy = (SubWeapon)CopyFieldsTo(new SubWeapon());
		copy.carryStyle = this.carryStyle;
		copy.hasOwnStyle = this.hasOwnStyle;
		return copy;
	}
}
