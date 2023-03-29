using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HellMarble_Block : Blocks
{
	public HellMarble_Block(){
		this.name = "Hell Marble";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 32;
		this.tileSide = 32;
		this.tileBottom = 32;

		this.maxHP = 240;
	}
}
