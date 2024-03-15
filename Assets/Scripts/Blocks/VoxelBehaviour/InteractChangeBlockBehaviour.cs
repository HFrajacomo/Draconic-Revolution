using System;
using UnityEngine;

[Serializable]
public class InteractChangeBlockBehaviour : VoxelBehaviour{
	public BlockID changeToBlock;

	public override int OnInteract(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		cl.chunks[pos].data.SetCell(x, y, z, (ushort)this.changeToBlock);
		return 1;
	}
}