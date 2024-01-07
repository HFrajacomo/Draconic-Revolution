public struct ModelInfo{
	public ModelType type;
	public string name;
	public string blenderReference;
	public char sex;

	public ModelInfo(ModelType t, string n, string br, char s){
		this.type = t;
		this.name = n;
		this.blenderReference = br;
		this.sex = s;
	}

	public string GetHandlerName(){
		return this.name + "/" + this.sex;
	}
}