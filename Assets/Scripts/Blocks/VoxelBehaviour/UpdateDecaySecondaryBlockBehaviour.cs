using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateDecaySecondaryBlockBehaviour : VoxelBehaviour{
	
	public int decayTime;
	public int decayDistance;
	public string assignedMainBlock;
	public string thisBlock;

	private ushort mainBlockCode;
	private ushort thisBlockCode; 

	private List<CastCoord> openList = new List<CastCoord>();
	private Dictionary<CastCoord, int> distances = new Dictionary<CastCoord, int>();
	private List<CastCoord> cache = new List<CastCoord>();
	private NetMessage reloadMessage;

	public override void PostDeserializationSetup(bool isClient){
		// TODO: Get main block code via assignedMainBlock string
		// this.mainBlockCode = <something>.Get(assignedMainBlock);

		// TODO: Get this block code via thisBlock string
		// this.thisBlockCode = <something>.Get(thisBlock);
	}

	// Triggers DECAY BUD on this block
	public override void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(type == BUDCode.DECAY){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			
			GetSurroundings(thisPos, this.decayDistance, cl);

			if(!RunMainRecursion(cl)){
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
	private bool RunMainRecursion(ChunkLoader_Server cl){
		bool foundMain = false;

		while(openList.Count > 0 && !foundMain){
			foundMain = GetSurroundings(openList[0], distances[openList[0]]-1, cl);
			openList.RemoveAt(0);
		}

		distances.Clear();

		if(foundMain)
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

		// Filters only secondary Blocks
		foreach(CastCoord c in cache){
			if(cl.GetBlock(c) == this.thisBlockCode && cl.GetState(c) == 0){
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
			if(cl.GetBlock(c) == this.mainBlockCode && cl.GetState(c) == 0){
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