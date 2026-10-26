using System;

/*
Item IDs equipped in each hand of a character, used to decide which item skins each BattleStyle attachment
	An ID of 0 means that no item is equipped in that hand
*/
public struct AttachmentItems : IEquatable<AttachmentItems> {
	public static readonly AttachmentItems NONE = new AttachmentItems(0, 0);

	public ushort rightHand;
	public ushort leftHand;

	public AttachmentItems(ushort rightHand, ushort leftHand){
		this.rightHand = rightHand;
		this.leftHand = leftHand;
	}

	/*
	Returns the item linked to the attachment in the given index, or 0 if there is none
		Attachments that declare a weaponType claim the first unclaimed equipped item of that type, right hand first
		BattleStyles are hand-agnostic, so the claiming order is what links attachments to hands
	*/
	public ushort GetItemFor(AttachmentData[] attachments, int attachmentIndex){
		bool rightClaimed = false;
		bool leftClaimed = false;
		ushort chosen;

		for(int i=0; i <= attachmentIndex; i++){
			chosen = 0;

			if(attachments[i].HasWeaponType()){
				if(!rightClaimed && Matches(this.rightHand, attachments[i].GetWeaponType())){
					rightClaimed = true;
					chosen = this.rightHand;
				}
				else if(!leftClaimed && Matches(this.leftHand, attachments[i].GetWeaponType())){
					leftClaimed = true;
					chosen = this.leftHand;
				}
			}

			if(i == attachmentIndex)
				return chosen;
		}

		return 0;
	}

	private static bool Matches(ushort itemID, WeaponType type){
		if(itemID == 0)
			return false;

		Weapon weapon = ItemLoader.GetItem(itemID) as Weapon;

		return weapon != null && weapon.GetWeaponType() == type;
	}

	public bool Equals(AttachmentItems other){return this.rightHand == other.rightHand && this.leftHand == other.leftHand;}
	public override bool Equals(object obj){return obj is AttachmentItems other && Equals(other);}
	public override int GetHashCode(){return (this.rightHand << 16) | this.leftHand;}
	public override string ToString(){return $"R:{this.rightHand} L:{this.leftHand}";}
}
