using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneBrick_Block : Blocks
{
	public StoneBrick_Block(){
		this.name = "Stone Bricks";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 12;
		this.tileSide = 12;
		this.tileBottom = 12;

		this.maxHP = 500;
	}
}
