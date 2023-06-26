using UnityEngine;

public struct CharacterAppearance {
	public Color skinColor;
	public ClothingInfo hat;
	public ClothingInfo torso;
	public ClothingInfo legs;
	public ClothingInfo boots;

	public CharacterAppearance(Color skin, ClothingInfo h, ClothingInfo t, ClothingInfo l, ClothingInfo b){
		this.skinColor = skin;
		this.hat = h;
		this.torso = t;
		this.legs = l;
		this.boots = b;
	}
}