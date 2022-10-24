using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenPlankRegular_Block : Blocks
{
	public WoodenPlankRegular_Block(){
		this.name = "Wooden Planks";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 13;
		this.tileSide = 13;
		this.tileBottom = 13;

		this.maxHP = 150;
	}
}
