using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandstoneBrick_Block : Blocks
{
	public SandstoneBrick_Block(){
		this.name = "Sandstone Bricks";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 17;
		this.tileSide = 17;
		this.tileBottom = 17;

		this.maxHP = 240;
	}
}
