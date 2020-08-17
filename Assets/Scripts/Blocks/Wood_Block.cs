using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wood_Block : Blocks
{
	public Wood_Block(){
		this.name = "Wood";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 5;
		this.tileSide = 6;
		this.tileBottom = 5;	
		//this.type = Wood_Block;	
	}

}
