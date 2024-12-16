using System;
using UnityEngine;

[Serializable]
public class PlaceSetStateBehaviour : VoxelBehaviour{
	public ushort changeToState;

	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){
		cl.GetChunk(pos).metadata.SetState(x, y, z, this.changeToState);
		return 1;
	}
}