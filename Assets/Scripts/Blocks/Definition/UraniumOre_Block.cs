using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UraniumOre_Block : Blocks
{
	public UraniumOre_Block(){
		this.name = "Uranium Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 25;
		this.tileSide = 25;
		this.tileBottom = 25;

		this.maxHP = 500;
	}
}
