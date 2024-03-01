public struct ModelInfo{
	public ModelType type;
	public string name;
	public string blenderReference;
	public char sex; // M, F
	public char coverHair; // N = no, Y = Totally, P = Partially, so hair needs to trigger shape keys

	public ModelInfo(ModelType t, string n, string br, char s){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
		this.coverHair = 'N';
	}

	public ModelInfo(ModelType t, string n, string br, char s, char ch){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
		this.coverHair = ch;
	}

	public string GetHandlerName(){
		return this.name + "/" + this.sex;
	}
}