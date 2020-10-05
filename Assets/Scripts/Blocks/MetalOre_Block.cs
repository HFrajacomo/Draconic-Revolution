using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetalOre_Block : Blocks
{
	public MetalOre_Block(){
		this.name = "Metal Ore";
		this.materialIndex = 0;
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 7;
		this.tileSide = 7;
		this.tileBottom = 7;	
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		// Changes to Stone
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, 3);
		return 1;
	}

}
