using UnityEngine;

public struct CharacterAppearance {
	public uint hairCode;
	public Color hairColor;
	public uint eyeCode;
	public Color eyeColor;
	public uint hatCode;
	public Color hatColor;
	public uint torsoCode;
	public Color torsoColor;
	public uint legsCode;
	public Color legsColor;
	public uint bootsCode;
	public Color bootsColor;
	public uint glovesCode;
	public Color glovesColor;

	public CharacterAppearance(uint hair, Color hairc, uint eye, Color eyec, uint hat, Color hatc, uint torso, Color torsoc, uint legs, Color legsc, uint boots, Color bootsc, uint gloves, Color glovesc){
		this.hairCode = hair;
		this.hairColor = hairc;
		this.eyeCode = eye;
		this.eyeColor = eyec;
		this.hatCode = hat;
		this.hatColor = hatc;
		this.torsoCode = torso;
		this.torsoColor = torsoc;
		this.legsCode = legs;
		this.legsColor = legsc;
		this.bootsCode = boots;
		this.bootsColor = bootsc;
		this.glovesCode = gloves;
		this.glovesColor = glovesc;
	}
}