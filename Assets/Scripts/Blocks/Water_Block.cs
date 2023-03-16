using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
WATER STATES:

0: Still 3
1: Still 2
2: Still 1
3: North2
4: NorthEast2
5: East2
6: SouthEast2
7: South2
8: SouthWest2
9: West2
10: NorthWest2
11: North1
12: NorthEast1
13: East1
14: SouthEast1
15: South1
16: SouthWest1
17: West1
18: NorthWest1
19: Falling3
20: Falling2
21: Falling1
*/

public class Water_Block : Blocks
{
	// Unique
	public ushort waterCode;
	private int viscosityDelay;
	private LiquidBehaviour behaviour;

	// Just loaded block
	public Water_Block(){
		this.shaderIndex = ShaderIndex.WATER; // Liquid
		this.name = "Water";
		this.solid = false;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = true;
		this.waterCode = (ushort)BlockID.WATER;
		this.customBreak = true;
		this.customPlace = true;
		this.hasLoadEvent = false;
		this.affectLight = true;
		this.seamless = true;
		this.drawRegardless = true;
		this.viscosityDelay = 12;
		this.maxHP = 1;
		this.flags = new HashSet<BlockFlags>(){BlockFlags.IMMUNE};

		this.behaviour = new LiquidBehaviour(this.waterCode, this.viscosityDelay);
	}

	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){
		if(!this.behaviour.IsChunkLoaderSet())
			this.behaviour.SetChunkLoader(cl);

		return this.behaviour.OnPlace(pos, x, y, z, facing);
	}

	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		if(!this.behaviour.IsChunkLoaderSet())
			this.behaviour.SetChunkLoader(cl);

		return this.behaviour.OnBreak(pos, x, y, z);
	}

	public override void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(!this.behaviour.IsChunkLoaderSet())
			this.behaviour.SetChunkLoader(cl);

		this.behaviour.OnBlockUpdate(type, myX, myY, myZ, budX, budY, budZ, facing);
	}

}
