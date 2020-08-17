using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stone_Block : Blocks
{
	public Stone_Block(){
		this.name = "Stone";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 4;
		this.tileSide = 4;
		this.tileBottom = 4;	
		//this.type = Stone_Block;	
	}
}
