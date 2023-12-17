using UnityEngine;

public struct CharacterAppearance {
	public Race race;
	public Color skinColor;
	public ClothingInfo hat;
	public ClothingInfo torso;
	public ClothingInfo legs;
	public ClothingInfo boots;

	public CharacterAppearance(Race r, Color skin, ClothingInfo h, ClothingInfo t, ClothingInfo l, ClothingInfo b){
		this.race = r;
		this.skinColor = skin;
		this.hat = h;
		this.torso = t;
		this.legs = l;
		this.boots = b;
	}

	public override string ToString(){
		return  this.race + " | Skin: " + this.skinColor + " | " + this.hat.ToString(ModelType.HEADGEAR) + " | " + this.torso.ToString(ModelType.CLOTHES) + " | " + this.legs.ToString(ModelType.LEGS) + " | " + this.boots.ToString(ModelType.FOOTGEAR);
	}
}