using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetalOre_Block : Blocks
{
	public MetalOre_Block(){
		this.name = "Metal Ore";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 7;
		this.tileSide = 7;
		this.tileBottom = 7;	
		//this.type = MetalOre_Block;	
	}


}
