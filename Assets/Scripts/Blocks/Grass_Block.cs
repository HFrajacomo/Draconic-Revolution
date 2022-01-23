using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass_Block : Blocks
{
	// Just loaded block
	public Grass_Block(){
		this.name = "Grass";
		this.materialIndex = 0;
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 1;
		this.tileSide = 2;
		this.tileBottom = 3;
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, 2);
		return 1;
	}
}
