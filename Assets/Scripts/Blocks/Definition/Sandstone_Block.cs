using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sandstone_Block : Blocks
{
	public Sandstone_Block(){
		this.name = "Sandstone";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 16;
		this.tileSide = 16;
		this.tileBottom = 16;

		this.maxHP = 200;
	}
}
