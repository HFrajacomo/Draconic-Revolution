using UnityEngine;

public struct CharacterAppearance {
	public Race race;
	public Color skinColor;
	public ClothingInfo hat;
	public ClothingInfo torso;
	public ClothingInfo legs;
	public ClothingInfo boots;
	public ClothingInfo face;
	public ClothingInfo hair;

	public CharacterAppearance(Race r, Color skin, ClothingInfo h, ClothingInfo t, ClothingInfo l, ClothingInfo b, ClothingInfo f, ClothingInfo ha){
		this.race = r;
		this.skinColor = skin;
		this.hat = h;
		this.torso = t;
		this.legs = l;
		this.boots = b;
		this.face = f;
		this.hair = h;
	}

	public ClothingInfo GetInfo(ModelType type){
		switch(type){
			case ModelType.HEADGEAR:
				return this.hat;
			case ModelType.CLOTHES:
				return this.torso;
			case ModelType.LEGS:
				return this.legs;
			case ModelType.FOOTGEAR:
				return this.boots;
			case ModelType.FACE:
				return this.face;
			case ModelType.HAIR:
				return this.hair;
			default:
				return this.torso;
		}
	}

	public override string ToString(){
		return  this.race + " | Skin: " + this.skinColor + " | " + this.hat.ToString(ModelType.HEADGEAR) + " | " + this.torso.ToString(ModelType.CLOTHES) + " | " + this.legs.ToString(ModelType.LEGS) + " | " + this.boots.ToString(ModelType.FOOTGEAR) + " | " + this.face.ToString(ModelType.FACE) + " | " + this.hair.ToString(ModelType.HAIR);
	}
}