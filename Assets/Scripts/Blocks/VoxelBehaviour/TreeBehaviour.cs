using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using Random = UnityEngine.Random;


[Serializable]
public class TreeBehaviour : VoxelBehaviour{
	public int maxAnalysedBlocks;
	public int decayDistance;
	public int minDecayTime;
	public int maxDecayTime;
	public string assignedLeaf;
	public string thisWood;

	public byte maxDropQuantity;
	public string droppedItem;
	private Item item;

	// Leaves Checker
	private List<CastCoord> openList = new List<CastCoord>();
	private Dictionary<CastCoord, int> distances = new Dictionary<CastCoord, int>();
	private List<CastCoord> cache = new List<CastCoord>();

	// Wood Checker
	private List<CastCoord> currentList = new List<CastCoord>();
	private List<CastCoord> safeList = new List<CastCoord>();
	private List<CastCoord> validDirections = new List<CastCoord>();
	private List<CastCoord> toDestroy = new List<CastCoord>();
	private HashSet<ChunkPos> toUpdate = new HashSet<ChunkPos>();

	private NetMessage reloadMessage;


	private ushort assignedLeafCode;
	private ushort thisCode;

	public override void PostDeserializationSetup(bool isClient){
		this.assignedLeafCode = VoxelLoader.GetBlockID(assignedLeaf);
		this.thisCode = VoxelLoader.GetBlockID(thisWood);
		this.item = ItemLoader.GetCopy(this.droppedItem);
	}

	// Activates OnBreak event -> Emits normal BUD, emits special BUD to breadt-first search leaves
	public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, blockX, blockY, blockZ);
		int amountOfWoodDestroyed;
		int i;

		EmitBlockUpdate(BUDCode.BREAK, coord.GetWorldX(), coord.GetWorldY(), coord.GetWorldZ(), 0, cl);

		amountOfWoodDestroyed = TreeCheck(coord, cl) + maxDropQuantity;
		GetSurroundingLeaves(coord, decayDistance, cl);
		RunLeavesRecursion(cl, coord);

		// Drops excess wood
		for(i=0; i < amountOfWoodDestroyed/this.item.stacksize; i += this.item.stacksize){
	        cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
	            Item.GenerateForceVector(), this.item, this.item.stacksize, cl);
    }

		cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
            Item.GenerateForceVector(), this.item, (byte)(amountOfWoodDestroyed - i), cl);

		return 0;
	}


	// Does Search for invalid leaves
	private void RunLeavesRecursion(ChunkLoader_Server cl, CastCoord init){
		while(openList.Count > 0){
			GetSurroundingLeaves(openList[0], distances[openList[0]]-1, cl);
			openList.RemoveAt(0);
		}

		// Applies DECAY BUD to distant leaves
		foreach(CastCoord c in distances.Keys){
			if(distances[c] == 0){
				EmitBUDTo(BUDCode.DECAY, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), 1, cl);
			}
		}

		// Applies DECAY BUD to around blocks if there's no wood around
		EmitDelayedBUD(BUDCode.DECAY, init.GetWorldX(), init.GetWorldY(), init.GetWorldZ(), minDecayTime, maxDecayTime, cl);

		distances.Clear();
	}

	// Returns a filled cache list full of surrounding coords
	private void GetSurroundingLeaves(CastCoord init, int currentDistance, ChunkLoader_Server cl){
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
	private bool CheckWoodAround(CastCoord init, ChunkLoader_Server cl){
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


	// Does the Wood check to break unconnected wood blocks from tree
	private int TreeCheck(CastCoord pos, ChunkLoader_Server cl){
		CastCoord aux;
		validDirections.Clear();

		aux = pos.Add(0,0,1); // N
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(1,0,1); // NE
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(1,0,0); // E
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(1,0,-1); // SE
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(0,0,-1); // S
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(-1,0,-1); // SW
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(-1,0,0); // W
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(-1,0,1); // NW
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(0,1,0); // UP
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		aux = pos.Add(0,-1,0); // DOWN
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(cl.GetBlock(aux) == this.thisCode && cl.GetState(aux) == 0){
				validDirections.Add(aux);
			}
		}

		return RunWoodRecursion(cl) + 1;
	}

	// Handles search of Wood Blocks
	// Returns the amount of wood blocks disposed of
	private int RunWoodRecursion(ChunkLoader_Server cl){
		CastCoord current;
		int exitCode;
		int amountOfBlocks;

		currentList.Clear();
		safeList.Clear();
		toDestroy.Clear();
		toUpdate.Clear();

		while(validDirections.Count > 0){
			current = validDirections[0];
			validDirections.RemoveAt(0);

			// If found to-destroy blocks
			exitCode = SearchWood(current, cl);

			// Safe
			if(exitCode == 0){
				safeList.AddRange(currentList);
				currentList.Clear();
			}
			// Invalid
			else if(exitCode == 1){
				toDestroy.AddRange(currentList);
				currentList.Clear();
			}
			// PANIC
			else{
				return 0;
			}
		}

		amountOfBlocks = toDestroy.Count;

		foreach(CastCoord aux in toDestroy){
			cl.chunks[aux.GetChunkPos()].data.SetCell(aux.blockX, aux.blockY, aux.blockZ, 0);
			cl.chunks[aux.GetChunkPos()].metadata.Reset(aux.blockX, aux.blockY, aux.blockZ);
			EmitBlockUpdate(BUDCode.BREAK, aux.GetWorldX(), aux.GetWorldY(), aux.GetWorldZ(), 0, cl);
			EmitBlockUpdate(BUDCode.DECAY, aux.GetWorldX(), aux.GetWorldY(), aux.GetWorldZ(), 0, cl);
			toUpdate.Add(aux.GetChunkPos());
		}

		foreach(ChunkPos pos in toUpdate){
			this.Update(pos, cl);
		}

		return amountOfBlocks;
	}


	// Fills toDestroy and Safe list
	// Returns true if all blocks in currentList are connected to a stem
	// Returns false if all blocks in currentList doesn't connect to a stem or connects to a to-be-destroyed block
	private int SearchWood(CastCoord init, ChunkLoader_Server cl){
		ushort blockCode;
		ushort state;

		GetAroundCoords(init, cl);

		// Filters only Leaf blocks
		for(int i=0; i < currentList.Count; i++){
			// If it's connected to a marked-to-destroy block
			if(toDestroy.Contains(currentList[i])){
				return 1;
			}

			// If it's connected to a safe block
			if(safeList.Contains(currentList[i])){
				return 0;
			}

			// If current block is found in initial direction
			if(validDirections.Contains(currentList[i])){
				validDirections.Remove(currentList[i]);
			}

			// PANIC if there's too many blocks
			if(currentList.Count > this.maxAnalysedBlocks){
				currentList.Clear();
				return 2;
			}

			blockCode = cl.GetBlock(currentList[i]);
			state = cl.GetState(currentList[i]);

			// If it's a spreadable block
			if(blockCode == this.thisCode && state == 0){
				GetAroundCoords(currentList[i], cl);
			}
			
			// Check if it's a root
			else if(VoxelLoader.CheckSolid(blockCode)){
				return 0;
			}
			else{
				currentList.RemoveAt(i);
				i--;
			}
		}
		return 1;
	}

	// Adds around coords to currentList
	private void GetAroundCoords(CastCoord pos, ChunkLoader_Server cl){
		CastCoord aux;
		ushort blockCode;

		aux = pos.Add(0,0,1); // N
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(1,0,1); // NE
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(1,0,0); // E
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(1,0,-1); // SE
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(0,0,-1); // S
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(-1,0,-1); // SW
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(-1,0,0); // W
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(-1,0,1); // NW
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(0,1,0); // UP
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}

		aux = pos.Add(0,-1,0); // DOWN
		if(cl.chunks.ContainsKey(aux.GetChunkPos())){
			if(!currentList.Contains(aux)){
				blockCode = cl.GetBlock(aux);
				if((blockCode == this.thisCode && cl.GetState(aux) == 0) || VoxelLoader.CheckSolid(blockCode)){
					currentList.Add(aux);
				}
			}
		}	
	}


    // Handles the emittion of BUD to neighboring blocks
    public void EmitDelayedBUD(BUDCode type, int x, int y, int z, int minOffset, int maxOffset, ChunkLoader_Server cl){
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

    // Sends a DirectBlockUpdate call to users
	public void Update(ChunkPos pos, ChunkLoader_Server cl){
		if(cl.chunks.ContainsKey(pos)){
			this.reloadMessage = new NetMessage(NetCode.SENDCHUNK);
			this.reloadMessage.SendChunk(cl.chunks[pos]);
			cl.server.SendToClients(pos, this.reloadMessage);
		}
	}
}