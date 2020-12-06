using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leaf_Object : BlocklikeObject
{

	List<CastCoord> openList = new List<CastCoord>();
	Dictionary<CastCoord, int> distances = new Dictionary<CastCoord, int>();
	List<CastCoord> cache = new List<CastCoord>();

	int decayDistance = 7;
	ushort assignedWoodCode = 4;
	ushort thisCode = ushort.MaxValue-1;

	public Leaf_Object(){
		this.name = "Leaf";
		this.solid = true;
		this.transparent = true;
		this.invisible = false;
		this.liquid = false;
		this.hasLoadEvent = false;
		this.go = GameObject.Find("----- PrefabObjects -----/Leaf_Object");
		this.mesh = this.go.GetComponent<MeshFilter>().sharedMesh;
		this.scaling = new Vector3(50, 50, 50);
	}

	// Triggers DECAY BUD on this block
	public override void OnBlockUpdate(string type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader cl){
		if(type == "decay"){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			
			GetSurroundings(thisPos, this.decayDistance, cl);

			if(!RunLeavesRecursion(cl)){
				if(cl.chunks.ContainsKey(thisPos.GetChunkPos())){
					cl.chunks[thisPos.GetChunkPos()].data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					cl.chunks[thisPos.GetChunkPos()].metadata.Reset(thisPos.blockX, thisPos.blockY, thisPos.blockZ);					
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 0, x:thisPos.blockX, y:thisPos.blockY, z:thisPos.blockZ);
				}

				// Applies Decay BUD to surrounding leaves if this one is invalid
				GetLastSurrounding(thisPos);

				foreach(CastCoord c in cache){
					EmitBUDTo("decay", c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), 10, cl);
				}
			}

			distances.Clear();
			openList.Clear();
			cache.Clear();
		}
	}


	// Does Search for invalid leaves
	private bool RunLeavesRecursion(ChunkLoader cl){
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
	private bool GetSurroundings(CastCoord init, int currentDistance, ChunkLoader cl){
		// End
		if(currentDistance == 0)
			return false;

		cache.Clear();
		cache.Add(init.Add(0,0,1)); // North
		cache.Add(init.Add(0,0,-1)); // South
		cache.Add(init.Add(1,0,0)); // East
		cache.Add(init.Add(-1,0,0)); // West
		cache.Add(init.Add(0,1,0)); // Up
		cache.Add(init.Add(0,-1,0)); // Down

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
