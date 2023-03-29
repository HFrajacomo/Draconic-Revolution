using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Acaster_Block : Blocks
{
	public Acaster_Block(){
		this.name = "Acaster Rock";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 33;
		this.tileSide = 33;
		this.tileBottom = 33;

		this.maxHP = ushort.MaxValue;
		this.flags = new HashSet<BlockFlags>(){BlockFlags.IMMUNE};
	}
}
