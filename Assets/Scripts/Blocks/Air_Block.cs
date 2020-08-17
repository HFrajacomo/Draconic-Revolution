using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Air_Block : Blocks
{
	// Just loaded block
	public Air_Block(){
		this.name = "Air";
		this.solid = false;
		this.transparent = true;
		this.invisible = true;
		this.liquid = false;

		this.tileTop = 0;
		this.tileSide = 0;
		this.tileBottom = 0;
		//this.type = Air_Block;
	}
}
