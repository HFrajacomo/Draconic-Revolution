using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dirt_Block : Blocks
{
	public Dirt_Block(){
		this.name = "Dirt";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 3;
		this.tileSide = 3;
		this.tileBottom = 3;
		//this.type = Dirt_Block;	
	}


}
