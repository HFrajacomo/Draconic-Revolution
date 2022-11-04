using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AluminiumOre_Block : Blocks
{
	public AluminiumOre_Block(){
		this.name = "Aluminium Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 20;
		this.tileSide = 20;
		this.tileBottom = 20;

		this.maxHP = 370;
	}
}
