using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetalOre_Block : Blocks
{
	public MetalOre_Block(){
		this.name = "Metal Ore";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 7;
		this.tileSide = 7;
		this.tileBottom = 7;	
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		// Changes to Stone
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, (ushort)BlockID.STONE);
		return 1;
	}

}
