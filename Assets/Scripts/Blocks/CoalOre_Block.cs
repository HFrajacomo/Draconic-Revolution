using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoalOre_Block : Blocks
{
	public CoalOre_Block(){
		this.name = "Coal Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 18;
		this.tileSide = 18;
		this.tileBottom = 18;

		this.maxHP = 320;
	}
}
