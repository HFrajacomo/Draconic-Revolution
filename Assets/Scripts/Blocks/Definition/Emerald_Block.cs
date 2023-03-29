using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emerald_Block : Blocks
{
	public Emerald_Block(){
		this.name = "Emerald";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 26;
		this.tileSide = 26;
		this.tileBottom = 26;

		this.maxHP = 380;
	}
}
