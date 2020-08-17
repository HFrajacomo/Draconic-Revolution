using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass_Block : Blocks
{
	// Just loaded block
	public Grass_Block(){
		this.name = "Grass";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 1;
		this.tileSide = 2;
		this.tileBottom = 3;
		//this.type = Grass_Block;	
	}
}
