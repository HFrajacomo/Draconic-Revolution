using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wood_Block : Blocks
{
	List<CastCoord> breakList = new List<CastCoord>();

	public Wood_Block(){
		this.name = "Wood";
		this.materialIndex = 0;
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 5;
		this.tileSide = 6;
		this.tileBottom = 5;	
	} 
}
