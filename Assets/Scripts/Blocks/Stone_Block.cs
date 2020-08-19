using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stone_Block : Blocks
{
	public Stone_Block(){
		this.name = "Stone";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 4;
		this.tileSide = 4;
		this.tileBottom = 4;	
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		// Changes to Metal
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, 5);
		return 1;
	}
}
