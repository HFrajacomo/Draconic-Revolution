using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TinOre_Block : Blocks
{
	public TinOre_Block(){
		this.name = "Tin Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 22;
		this.tileSide = 22;
		this.tileBottom = 22;

		this.maxHP = 320;
	}
}
