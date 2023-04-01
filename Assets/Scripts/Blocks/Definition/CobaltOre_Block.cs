using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CobaltOre_Block : Blocks
{
	public CobaltOre_Block(){
		this.name = "Cobalt Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 34;
		this.tileSide = 34;
		this.tileBottom = 34;

		this.maxHP = 500;
	}
}
