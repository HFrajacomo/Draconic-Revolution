using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dirt_Block : Blocks
{
	public Dirt_Block(){
		this.name = "Dirt";
		this.solid = true;
		this.materialIndex = 0;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 3;
		this.tileSide = 3;
		this.tileBottom = 3;
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		// Changes to Grass
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, 1);
		return 1;
	}
}
