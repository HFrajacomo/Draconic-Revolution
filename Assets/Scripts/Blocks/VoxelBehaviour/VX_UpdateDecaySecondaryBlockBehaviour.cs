using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
Secondary blocks (like leaves) that decay when no main block (like a log) is close enough
	On a DECAY BUD, searches through the connected secondary blocks up to decayDistance steps away
	The block survives if any of them touches a main block, or a chunk that isn't loaded
	Otherwise it's removed and sends a DECAY to its neighbors after decayTime ticks
*/
[Serializable]
public class VX_UpdateDecaySecondaryBlockBehaviour : VoxelBehaviour{
	
	public int decayTime;
	public int decayDistance;
	public string assignedMainBlock;
	public string thisBlock;

	private ushort mainBlockCode;
	private ushort thisBlockCode; 

	private VoxelReader_Server reader = new VoxelReader_Server();
	private List<int4> queue = new List<int4>();
	private HashSet<int3> visited = new HashSet<int3>();

	private static readonly int3[] FACE_OFFSETS = {
		new int3(0,0,1),
		new int3(0,0,-1),
		new int3(1,0,0),
		new int3(-1,0,0),
		new int3(0,1,0),
		new int3(0,-1,0)
	};

	public override void PostDeserializationSetup(bool isClient){
		this.mainBlockCode = VoxelLoader.GetBlockID(assignedMainBlock);
		this.thisBlockCode = VoxelLoader.GetBlockID(thisBlock);
	}

	// Triggers DECAY BUD on this block
	public override void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(type != BUDCode.DECAY)
			return;

		int3 thisPos = new int3(myX, myY, myZ);
		int3 neighbor;
		ChunkPos pos;

		this.reader.Begin(cl);

		if(IsSupported(thisPos))
			return;

		if(this.reader.Clear(thisPos, out pos)){
			cl.budscheduler.ScheduleSave(pos);
			cl.budscheduler.SchedulePropagation(pos);
		}

		// Surrounding blocks may have lost their support through this one
		for(int i=0; i < FACE_OFFSETS.Length; i++){
			neighbor = thisPos + FACE_OFFSETS[i];
			EmitBUDTo(BUDCode.DECAY, neighbor.x, neighbor.y, neighbor.z, this.decayTime, cl);
		}
	}

	// Breadth-first search through the secondary blocks around start. xyz of the queue is the position and w the steps left
	private bool IsSupported(int3 start){
		int4 current;
		int3 neighbor;
		ushort block, state;
		int head = 0;
		bool supported = false;

		this.queue.Clear();
		this.visited.Clear();
		this.queue.Add(new int4(start, this.decayDistance));
		this.visited.Add(start);

		while(head < this.queue.Count && !supported){
			current = this.queue[head];
			head++;

			if(current.w <= 0)
				continue;

			for(int i=0; i < FACE_OFFSETS.Length; i++){
				neighbor = current.xyz + FACE_OFFSETS[i];

				if(!this.reader.TryRead(neighbor, out block, out state)){
					supported = true;
					break;
				}

				if(state != 0)
					continue;

				if(block == this.mainBlockCode){
					supported = true;
					break;
				}

				if(block == this.thisBlockCode && this.visited.Add(neighbor))
					this.queue.Add(new int4(neighbor, current.w - 1));
			}
		}

		this.queue.Clear();
		this.visited.Clear();
		return supported;
	}
}
