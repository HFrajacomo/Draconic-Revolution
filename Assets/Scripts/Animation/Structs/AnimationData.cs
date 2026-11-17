// An Animator state played in a layer. States are identified by their short name hash (Animator.StringToHash), which is what travels in the network
public struct AnimationData {
	public int stateHash;
	public int layer;

	public AnimationData(int stateHash, int l){
		this.stateHash = stateHash;
		this.layer = l;
	}

	public override string ToString(){return $"State: {this.stateHash} -- Layer: {this.layer}";}
}