using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ice_Block : Blocks
{
	public Ice_Block(){
		this.shaderIndex = ShaderIndex.ICE;
		this.name = "Ice";
		this.solid = true;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;
		this.seamless = true;
	}
}
