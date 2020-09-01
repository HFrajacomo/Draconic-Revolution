using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water_Block : Blocks
{
	// Just loaded block
	public Water_Block(){
		this.name = "Water";
		this.solid = false;
		this.transparent = true;
		this.invisible = false;
		this.liquid = true;
	}
}
