using System;
using UnityEngine;

public class PlaceSetStateBehaviour : VoxelBehaviour{
	public ushort changeToState;

	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){
		cl.chunks[pos].metadata.SetState(x, y, z, this.changeToState);
		return 1;
	}
}