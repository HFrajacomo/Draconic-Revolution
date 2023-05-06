using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilverWoodLeaf_Block : Blocks
{

	List<CastCoord> openList = new List<CastCoord>();
	Dictionary<CastCoord, int> distances = new Dictionary<CastCoord, int>();
	List<CastCoord> cache = new List<CastCoord>();

	private const int decayTime = 7;
	private int decayDistance = 7;
	private ushort assignedWoodCode = (ushort)BlockID.SILVERWOOD;
	private ushort thisCode = (ushort)BlockID.SILVERWOOD_LEAF;
	private NetMessage reloadMessage;

	public SilverWoodLeaf_Block(){
		this.name = "Silverwood Leaf";
		this.shaderIndex = ShaderIndex.LEAVES;
		this.solid = false;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = false;
		this.hasLoadEvent = false;
		this.affectLight = false;
		this.seamless = true;
		this.maxHP = 1;

		this.tileTop = 2;
		this.tileSide = 2;
		this.tileBottom = 2;
	}

	// Makes Wood Block have state 1 when unnaturally placed
	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		cl.chunks[pos].metadata.SetState(blockX, blockY, blockZ, 1);
		return 0;
	}

	// Triggers DECAY BUD on this block
	public override void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(type == BUDCode.DECAY){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			
			GetSurroundings(thisPos, this.decayDistance, cl);

			if(!RunLeavesRecursion(cl)){
				if(cl.chunks.ContainsKey(thisPos.GetChunkPos())){
					cl.chunks[thisPos.GetChunkPos()].data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					cl.chunks[thisPos.GetChunkPos()].metadata.Reset(thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					cl.budscheduler.ScheduleSave(thisPos.GetChunkPos());
					cl.budscheduler.SchedulePropagation(thisPos.GetChunkPos());
				}

				// Applies Decay BUD to surrounding leaves if this one is invalid
				GetLastSurrounding(thisPos);

				foreach(CastCoord c in cache){
					EmitBUDTo(BUDCode.DECAY, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), decayTime, cl);
				}
			}

			distances.Clear();
			openList.Clear();
			cache.Clear();
		}
	}


	// Does Search for invalid leaves
	private bool RunLeavesRecursion(ChunkLoader_Server cl){
		bool foundWood = false;

		while(openList.Count > 0 && !foundWood){
			foundWood = GetSurroundings(openList[0], distances[openList[0]]-1, cl);
			openList.RemoveAt(0);
		}

		distances.Clear();

		if(foundWood)
			return true;
		else
			return false;
	}

	// Returns a filled cache list full of surrounding coords
	private bool GetSurroundings(CastCoord init, int currentDistance, ChunkLoader_Server cl){
		// End
		if(currentDistance == 0)
			return false;

		GetLastSurrounding(init);

		// Filters only Leaf blocks
		foreach(CastCoord c in cache){
			if(cl.GetBlock(c) == this.thisCode && cl.GetState(c) == 0){
				// If is already in dict
				if(distances.ContainsKey(c)){
					if(distances[c] > currentDistance){
						distances[c] = currentDistance;
						openList.Add(c);
					}
				}
				else{
					distances.Add(c, currentDistance);
					openList.Add(c);
				}
			}
			if(cl.GetBlock(c) == this.assignedWoodCode && cl.GetState(c) == 0){
				return true;
			}
		}

		return false;
	}

	// Gets surrounding on cache
	private void GetLastSurrounding(CastCoord init){
		cache.Clear();
		cache.Add(init.Add(0,0,1)); // North
		cache.Add(init.Add(0,0,-1)); // South
		cache.Add(init.Add(1,0,0)); // East
		cache.Add(init.Add(-1,0,0)); // West
		cache.Add(init.Add(0,1,0)); // Up
		cache.Add(init.Add(0,-1,0)); // Down
	}
}
