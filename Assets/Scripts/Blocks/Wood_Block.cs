using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wood_Block : Blocks
{
	public Wood_Block(){
		this.name = "Wood";
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 5;
		this.tileSide = 6;
		this.tileBottom = 5;	
	} 
	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		return 0;
	}
}
