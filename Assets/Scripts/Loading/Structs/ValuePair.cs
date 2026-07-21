using System;

[Serializable]
public struct ValuePair<T, U>{
	public T key;
	public U value;

	public override string ToString(){
		return "{" + this.key.ToString() + ": " + this.value.ToString() + "}";
	}
}