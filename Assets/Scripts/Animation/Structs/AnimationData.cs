public struct AnimationData {
	public string name;
	public int layer;

	public AnimationData(string n, int l){
		this.name = n;
		this.layer = l;
	}

	public override string ToString(){return $"State: {this.name} -- Layer: {this.layer}";}
}