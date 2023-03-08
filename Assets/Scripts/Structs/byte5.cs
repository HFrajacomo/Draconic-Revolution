using UnityEngine;
using Unity.Mathematics;

public struct byte5{
	public byte x;
	public byte y;
	public byte z;
	public byte w;
	public byte k;

	public byte5(byte x, byte y, byte z, byte w, byte k){
		this.x = x;
		this.y = y;
		this.z = z;
		this.w = w;
		this.k = k;
	}

	public byte5(int x, int y, int z, int w, int k){
		this.x = (byte)x;
		this.y = (byte)y;
		this.z = (byte)z;
		this.w = (byte)w;
		this.k = (byte)k;
	}

	public byte5(int3 c, byte w, byte k){
		this.x = (byte)c.x;
		this.y = (byte)c.y;
		this.z = (byte)c.z;
		this.w = w;
		this.k = k;
	}

	public int3 GetCoords(){
		return new int3(this.x, this.y, this.z);
	}

	public override string ToString(){
		return "(" + this.x + ", " + this.y + ", " + this.z + ", " + this.w + ", " + this.k + ")";
	}
}