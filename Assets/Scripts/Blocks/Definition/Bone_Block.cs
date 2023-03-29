using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bone_Block : Blocks
{
	public Bone_Block(){
		this.name = "Bone";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 15;
		this.tileSide = 15;
		this.tileBottom = 15;

		this.maxHP = 120;
	}
}
