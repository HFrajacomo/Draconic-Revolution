using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Air_Block : Blocks
{
	// Just loaded block
	public Air_Block(){
		this.name = "Air";
		this.materialIndex = 0;
		this.solid = false;
		this.transparent = 1;
		this.invisible = true;
		this.liquid = false;
		this.affectLight = false;

		this.tileTop = 0;
		this.tileSide = 0;
		this.tileBottom = 0;
	}
	
	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		return 0;
	}
}
