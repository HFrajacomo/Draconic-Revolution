using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArditeOre_Block : Blocks
{
	public ArditeOre_Block(){
		this.name = "Ardite Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 35;
		this.tileSide = 35;
		this.tileBottom = 35;

		this.maxHP = 540;
	}
}
