using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetiteOre_Block : Blocks
{
	public MagnetiteOre_Block(){
		this.name = "Magnetite Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 19;
		this.tileSide = 19;
		this.tileBottom = 19;

		this.maxHP = 450;
	}
}
