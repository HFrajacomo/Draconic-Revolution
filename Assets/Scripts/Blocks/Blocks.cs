using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Blocks
{
	public const int pixelSize = 32;
	public static int atlasSizeX = 8;
	public static int atlasSizeY = 2;

	public string name;
	public bool solid; // Is part of chunk mesh ?
	public bool transparent; // Should render the back side?
	/*
	public List<UnityEngine.Vector2> topUV;
	public List<UnityEngine.Vector2> sideUV;
	public List<UnityEngine.Vector2> bottomUV;
	*/
	public string GetName(){
		return this.name;
	}

	public bool GetSolid(){
		return this.solid;
	}

	public bool GetTransparent(){
		return this.transparent;
	}

}

public class Grass : Blocks{
	public Grass(){
		this.name = "Grass";
		this.solid = true;
		this.transparent = false;
	}
}

public class Air : Blocks{
	public Air(){
		this.name = "Air";
		this.solid = false;
		this.transparent = true;
	}
}
