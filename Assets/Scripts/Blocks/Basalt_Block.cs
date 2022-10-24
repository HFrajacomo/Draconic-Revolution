using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Basalt_Block : Blocks
{
	public Basalt_Block(){
		this.name = "Basalt";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 10;
		this.tileSide = 10;
		this.tileBottom = 10;

		this.maxHP = 400;
	}
}
