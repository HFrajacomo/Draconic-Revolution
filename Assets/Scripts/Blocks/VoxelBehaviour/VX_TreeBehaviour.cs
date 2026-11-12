using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using Random = UnityEngine.Random;

/*
Breaks the wood of a tree that lost its support when one of its logs is broken
	Logs are connected through their 8 horizontal neighbors (diagonals included) and the voxels above and below them
	A group of connected logs is supported if it touches any other solid block (ground, placed logs, other woods...)
	or a chunk that isn't loaded, so trees never fall because of what the server can't see
	Unsupported groups are broken. If more than maxAnalysedBlocks logs need to be scanned, nothing breaks
	Every leaf touching the broken wood receives a single DECAY, spread between minDecayTime and maxDecayTime ticks
*/
[Serializable]
public class VX_TreeBehaviour : VoxelBehaviour{
	public int maxAnalysedBlocks;
	public int decayDistance;
	public int minDecayTime;
	public int maxDecayTime;
	public string assignedLeaf;
	public string thisWood;

	public byte maxDropQuantity;
	public string droppedItem;
	private Item item;

	private ushort assignedLeafCode;
	private ushort thisCode;

	// Wood search
	private VoxelReader_Server reader = new VoxelReader_Server();
	private HashSet<int3> safe = new HashSet<int3>();
	private HashSet<int3> doomed = new HashSet<int3>();
	private HashSet<int3> component = new HashSet<int3>();
	private List<int3> queue = new List<int3>();
	private int scanned;

	// Destruction
	private HashSet<int3> leaves = new HashSet<int3>();
	private HashSet<ChunkPos> touchedChunks = new HashSet<ChunkPos>();

	private static readonly int SEARCH_ROOTED = 0;
	private static readonly int SEARCH_DOOMED = 1;
	private static readonly int SEARCH_OVERFLOW = 2;

	// Down comes first, so the search reaches the ground as soon as possible
	private static readonly int3[] SUPPORT_OFFSETS = {
		new int3(0,-1,0),
		new int3(0,0,1),
		new int3(1,0,1),
		new int3(1,0,0),
		new int3(1,0,-1),
		new int3(0,0,-1),
		new int3(-1,0,-1),
		new int3(-1,0,0),
		new int3(-1,0,1),
		new int3(0,1,0)
	};

	// Same neighbors and facings as VoxelBehaviour.EmitBlockUpdate
	private static readonly int3[] FACE_OFFSETS = {
		new int3(1,0,0),
		new int3(-1,0,0),
		new int3(0,0,1),
		new int3(0,0,-1),
		new int3(0,1,0),
		new int3(0,-1,0)
	};
	private static readonly int[] FACE_FACINGS = {2,0,1,3,4,5};


	public override void PostDeserializationSetup(bool isClient){
		this.assignedLeafCode = VoxelLoader.GetBlockID(assignedLeaf);
		this.thisCode = VoxelLoader.GetBlockID(thisWood);
		this.item = ItemLoader.GetCopy(this.droppedItem);
	}

	public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		int3 origin = VoxelReader_Server.ToWorld(pos, blockX, blockY, blockZ);
		int destroyed = 0;

		this.reader.Begin(cl);
		this.leaves.Clear();
		this.touchedChunks.Clear();

		EmitBlockUpdate(BUDCode.BREAK, origin.x, origin.y, origin.z, 0, cl);

		if(SearchWood(origin))
			destroyed = DestroyDoomed(origin, cl);

		CollectLeavesAround(origin);
		EmitLeafDecay(cl);
		SyncTouchedChunks(cl);
		DropWood(origin, destroyed + 1 + this.maxDropQuantity, cl);

		this.safe.Clear();
		this.doomed.Clear();
		this.component.Clear();
		this.queue.Clear();

		return 0;
	}

	// Fills the doomed set with every log that lost its support. Returns false if the scan cap was reached
	private bool SearchWood(int3 origin){
		int3 start;
		ushort block, state;
		int result;

		this.safe.Clear();
		this.doomed.Clear();
		this.scanned = 0;

		for(int i=0; i < SUPPORT_OFFSETS.Length; i++){
			start = origin + SUPPORT_OFFSETS[i];

			if(this.safe.Contains(start) || this.doomed.Contains(start))
				continue;
			if(!this.reader.TryRead(start, out block, out state) || !IsTreeWood(block, state))
				continue;

			result = SearchComponent(start, origin);

			if(result == SEARCH_OVERFLOW){
				this.doomed.Clear();
				return false;
			}

			if(result == SEARCH_ROOTED)
				this.safe.UnionWith(this.component);
			else
				this.doomed.UnionWith(this.component);
		}

		return true;
	}

	// Breadth-first search through the logs connected to start, stopping as soon as a support is found
	// Doomed logs never need checking: a doomed group was fully explored, so it would already contain any log touching it
	private int SearchComponent(int3 start, int3 origin){
		int3 current, neighbor;
		ushort block, state;
		int head = 0;

		this.component.Clear();
		this.queue.Clear();
		this.component.Add(start);
		this.queue.Add(start);

		if(++this.scanned > this.maxAnalysedBlocks)
			return SEARCH_OVERFLOW;

		while(head < this.queue.Count){
			current = this.queue[head];
			head++;

			for(int i=0; i < SUPPORT_OFFSETS.Length; i++){
				neighbor = current + SUPPORT_OFFSETS[i];

				// The broken block is air, even if a caller didn't clear it yet
				if(neighbor.Equals(origin) || this.component.Contains(neighbor))
					continue;

				if(!this.reader.TryRead(neighbor, out block, out state))
					return SEARCH_ROOTED;

				if(IsTreeWood(block, state)){
					if(this.safe.Contains(neighbor))
						return SEARCH_ROOTED;

					this.component.Add(neighbor);
					this.queue.Add(neighbor);

					if(++this.scanned > this.maxAnalysedBlocks)
						return SEARCH_OVERFLOW;
				}
				else if(VoxelLoader.CheckSolid(block)){
					return SEARCH_ROOTED;
				}
			}
		}

		return SEARCH_DOOMED;
	}

	// Breaks the doomed logs and notifies the blocks around the fallen wood. Returns the amount of logs broken
	private int DestroyDoomed(int3 origin, ChunkLoader_Server cl){
		ChunkPos chunk;
		int amount = 0;

		foreach(int3 log in this.doomed){
			if(this.reader.Clear(log, out chunk)){
				this.touchedChunks.Add(chunk);
				amount++;
			}
		}

		foreach(int3 log in this.doomed)
			NotifyAround(log, origin, cl);

		return amount;
	}

	// Leaves get a DECAY later, other blocks get a BREAK. Air and the fallen wood itself get nothing
	private void NotifyAround(int3 log, int3 origin, ChunkLoader_Server cl){
		int3 neighbor;
		ushort block, state;

		for(int i=0; i < FACE_OFFSETS.Length; i++){
			neighbor = log + FACE_OFFSETS[i];

			if(neighbor.Equals(origin) || this.doomed.Contains(neighbor))
				continue;
			if(!this.reader.TryRead(neighbor, out block, out state) || block == 0)
				continue;

			if(block == this.assignedLeafCode){
				if(state == 0)
					this.leaves.Add(neighbor);
				continue;
			}

			cl.budscheduler.ScheduleBUD(new BUDSignal(BUDCode.BREAK, neighbor.x, neighbor.y, neighbor.z, log.x, log.y, log.z, FACE_FACINGS[i]), 0);
		}
	}

	private void CollectLeavesAround(int3 origin){
		int3 neighbor;
		ushort block, state;

		for(int i=0; i < FACE_OFFSETS.Length; i++){
			neighbor = origin + FACE_OFFSETS[i];

			if(this.reader.TryRead(neighbor, out block, out state) && block == this.assignedLeafCode && state == 0)
				this.leaves.Add(neighbor);
		}
	}

	// Spreads the leaf searches over many ticks instead of running all of them in the same frame
	private void EmitLeafDecay(ChunkLoader_Server cl){
		foreach(int3 leaf in this.leaves)
			EmitBUDTo(BUDCode.DECAY, leaf.x, leaf.y, leaf.z, Random.Range(this.minDecayTime, this.maxDecayTime), cl);

		this.leaves.Clear();
	}

	// Saves every chunk the fallen wood was in and sends its changes to the players
	private void SyncTouchedChunks(ChunkLoader_Server cl){
		foreach(ChunkPos pos in this.touchedChunks){
			cl.budscheduler.ScheduleSave(pos);
			cl.budscheduler.SchedulePropagation(pos);
			cl.server.entityHandler.SetRefreshVision(EntityType.DROP, pos);
		}

		this.touchedChunks.Clear();
	}

	private void DropWood(int3 origin, int amount, ChunkLoader_Server cl){
		float3 position = new float3(origin.x, origin.y + Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, origin.z);
		int stackSize = Mathf.Max(1, (int)this.item.stacksize);
		int quantity;

		while(amount > 0){
			quantity = Mathf.Min(amount, stackSize);
			cl.server.entityHandler.AddItem(position, Item.GenerateForceVector(), this.item, (byte)quantity, cl);
			amount -= quantity;
		}
	}

	private bool IsTreeWood(ushort block, ushort state){
		return block == this.thisCode && state == 0;
	}
}
