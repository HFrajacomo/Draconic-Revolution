using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stone_Block : Blocks
{
	public Stone_Block(){
		this.name = "Stone";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 4;
		this.tileSide = 4;
		this.tileBottom = 4;

		this.maxHP = 300;
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		// Changes to Iron
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, (ushort)BlockID.IRON_ORE);
		return 1;
	}
}
