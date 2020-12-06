using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Wood Block triggers a special BUD message called "decay". This is received by 
assigned leaf_objects and will be used to check whether the leaf is valid
*/
public class Wood_Block : Blocks
{
	List<CastCoord> openList = new List<CastCoord>();
	Dictionary<CastCoord, int> distances = new Dictionary<CastCoord, int>();
	List<CastCoord> cache = new List<CastCoord>();

	int decayDistance = 7;
	ushort assignedLeafCode = ushort.MaxValue-1;
	ushort thisCode = 4;

	public Wood_Block(){
		this.name = "Wood";
		this.materialIndex = 0;
		this.solid = true;
		this.transparent = false;
		this.invisible = false;
		this.liquid = false;

		this.tileTop = 5;
		this.tileSide = 6;
		this.tileBottom = 5;	
	}

	// Activates OnBreak event -> Emits normal BUD, emits special BUD to breadt-first search leaves
	public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		CastCoord thisCoord = new CastCoord(pos, blockX, blockY, blockZ);
		EmitBlockUpdate("break", thisCoord.GetWorldX(), thisCoord.GetWorldY(), thisCoord.GetWorldZ(), 0, cl);

		GetSurroundings(thisCoord, decayDistance, cl);

		RunLeavesRecursion(cl, thisCoord);

		return 0;
	}

	// Does Search for invalid leaves
	private void RunLeavesRecursion(ChunkLoader cl, CastCoord init){
		while(openList.Count > 0){
			GetSurroundings(openList[0], distances[openList[0]]-1, cl);
			openList.RemoveAt(0);
		}

		// Applies DECAY BUD to distant leaves
		foreach(CastCoord c in distances.Keys){
			if(distances[c] == 0){
				EmitBUDTo("decay", c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), 1, cl);
			}
		}

		// Applies DECAY BUD to around blocks if there's no wood around
		EmitDelayedBUD("decay", init.GetWorldX(), init.GetWorldY(), init.GetWorldZ(), 2, 15, cl);

		distances.Clear();
	}

	// Returns a filled cache list full of surrounding coords
	private void GetSurroundings(CastCoord init, int currentDistance, ChunkLoader cl){
		// End
		if(currentDistance == 0)
			return;

		cache.Clear();
		cache.Add(init.Add(0,0,1)); // North
		cache.Add(init.Add(0,0,-1)); // South
		cache.Add(init.Add(1,0,0)); // East
		cache.Add(init.Add(-1,0,0)); // West
		cache.Add(init.Add(0,1,0)); // Up
		cache.Add(init.Add(0,-1,0)); // Down

		// Filters only Leaf blocks
		foreach(CastCoord c in cache){
			if(cl.GetBlock(c) == this.assignedLeafCode && cl.GetState(c) == 0){
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
		}
	}

	// Check if there's any wood block around this block
	private bool CheckWoodAround(CastCoord init, ChunkLoader cl){
		cache.Clear();
		cache.Add(init.Add(0,0,1)); // North
		cache.Add(init.Add(0,0,-1)); // South
		cache.Add(init.Add(1,0,0)); // East
		cache.Add(init.Add(-1,0,0)); // West
		cache.Add(init.Add(0,1,0)); // Up
		cache.Add(init.Add(0,-1,0)); // Down

		foreach(CastCoord c in cache){
			if(cl.GetBlock(c) == this.thisCode && cl.GetState(c) == 0){
				return true;
			}
		}
		return false;
	}

    // Handles the emittion of BUD to neighboring blocks
    public void EmitDelayedBUD(string type, int x, int y, int z, int minOffset, int maxOffset, ChunkLoader cl){
      CastCoord thisPos = new CastCoord(new Vector3(x, y, z));

      cache.Clear();

      cache.Add(thisPos.Add(1,0,0));
      cache.Add(thisPos.Add(-1,0,0));
      cache.Add(thisPos.Add(0,1,0));
      cache.Add(thisPos.Add(0,-1,0));
      cache.Add(thisPos.Add(0,0,1));
      cache.Add(thisPos.Add(0,0,-1));


      foreach(CastCoord c in cache){
        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), 0), Random.Range(minOffset, maxOffset));     
      }
    }
}
