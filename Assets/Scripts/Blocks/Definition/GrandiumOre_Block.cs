using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrandiumOre_Block : Blocks
{
	public GrandiumOre_Block(){
		this.name = "Grandium Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 37;
		this.tileSide = 37;
		this.tileBottom = 37;

		this.maxHP = 800;
	}
}
