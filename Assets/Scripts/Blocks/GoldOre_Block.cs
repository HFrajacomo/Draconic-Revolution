using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldOre_Block : Blocks
{
	public GoldOre_Block(){
		this.name = "Gold Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 23;
		this.tileSide = 23;
		this.tileBottom = 23;

		this.maxHP = 350;
	}
}
