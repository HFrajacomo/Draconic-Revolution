using UnityEngine;

public struct ClothingInfo{
	public bool isMale;
	public ushort code;
	public Color primary;
	public Color secondary;
	public Color terciary;

	public ClothingInfo(ushort c, Color p, Color s, Color t, bool isMale){
		this.code = c;
		this.primary = p;
		this.secondary = s;
		this.terciary = t;
		this.isMale = isMale;
	}
}