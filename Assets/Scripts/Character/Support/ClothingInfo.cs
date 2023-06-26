using UnityEngine;

public struct ClothingInfo{
	public uint code;
	public Color primary;
	public Color secondary;

	public ClothingInfo(uint c, Color p, Color s){
		this.code = c;
		this.primary = p;
		this.secondary = s;
	}
}