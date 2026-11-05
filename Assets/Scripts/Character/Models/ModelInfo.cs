using UnityEngine;

public struct ModelInfo{
	public ModelType type;
	public string name;
	public string blenderReference;
	public char sex; // M, F
	public char coverHair; // N = no, Y = Totally, P = Partially, so hair needs to trigger shape keys
	public bool hasModel; // If should be drawn at all (like bald hair or not)
	public string faceTextureImage;
	public bool doubleSided; // If every material of this model should render back faces

	public ModelInfo(ModelType t, string n, string br, char s, char ch, bool hm, string fti, bool ds){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
		this.coverHair = ch;
		this.hasModel = hm;
		this.faceTextureImage = fti;
		this.doubleSided = ds;
	}

	public string GetHandlerName(){
		return this.name + "/" + this.sex;
	}

	public override string ToString(){
		return this.name + "/" + this.sex;
	}
}