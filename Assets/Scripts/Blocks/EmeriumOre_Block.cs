using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmeriumOre_Block : Blocks
{
	public EmeriumOre_Block(){
		this.name = "Emerium Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 24;
		this.tileSide = 24;
		this.tileBottom = 24;

		this.maxHP = 400;
	}
}
