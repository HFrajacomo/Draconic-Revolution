using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenPlankPine_Block : Blocks
{
	public WoodenPlankPine_Block(){
		this.name = "Pine Planks";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 14;
		this.tileSide = 14;
		this.tileBottom = 14;

		this.maxHP = 150;
	}
}
