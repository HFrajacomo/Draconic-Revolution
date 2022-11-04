using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopperOre_Block : Blocks
{
	public CopperOre_Block(){
		this.name = "Copper Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 21;
		this.tileSide = 21;
		this.tileBottom = 21;

		this.maxHP = 340;
	}
}
