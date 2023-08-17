using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteonyxOre_Block : Blocks
{
	public SteonyxOre_Block(){
		this.name = "Steonyx Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 43;
		this.tileSide = 43;
		this.tileBottom = 43;

		this.maxHP = 1100;
	}
}
