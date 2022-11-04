using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ruby_Block : Blocks
{
	public Ruby_Block(){
		this.name = "Ruby";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 27;
		this.tileSide = 27;
		this.tileBottom = 27;

		this.maxHP = 380;
	}
}
