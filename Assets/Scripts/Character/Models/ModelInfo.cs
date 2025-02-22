using UnityEngine;

public struct ModelInfo{
	public ModelType type;
	public string name;
	public string blenderReference;
	public char sex; // M, F
	public char coverHair; // N = no, Y = Totally, P = Partially, so hair needs to trigger shape keys
	public bool hasModel; // If should be drawn at all (like bald hair or not)

	public ModelInfo(ModelType t, string n, string br, char s){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
		this.coverHair = 'N';
		this.hasModel = true;
	}

	public ModelInfo(ModelType t, string n, string br, char s, char hm){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
		this.coverHair = 'N';

		if(hm == 'Y')
			this.hasModel = true;
		else
			this.hasModel = false;	}

	public ModelInfo(ModelType t, string n, string br, char s, char ch, char hm){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
		this.coverHair = ch;

		if(hm == 'Y')
			this.hasModel = true;
		else
			this.hasModel = false;
	}

	public string GetHandlerName(){
		return this.name + "/" + this.sex;
	}

	public override string ToString(){
		return this.name + "/" + this.sex;
	}
}