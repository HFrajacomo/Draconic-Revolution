using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clay_Block : Blocks
{
	public Clay_Block(){
		this.name = "Clay";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 11;
		this.tileSide = 11;
		this.tileBottom = 11;

		this.maxHP = 80;
	}
}
