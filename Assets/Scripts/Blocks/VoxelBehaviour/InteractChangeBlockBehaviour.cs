using System;
using UnityEngine;

[Serializable]
public class InteractChangeBlockBehaviour : VoxelBehaviour{
	public string changeToBlock;
	private ushort blockID;

	public override void PostDeserializationSetup(bool isClient){
		this.blockID = VoxelLoader.GetBlockID(this.changeToBlock);
	}

	public override int OnInteract(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		cl.chunks[pos].data.SetCell(x, y, z, this.blockID);
		return 1;
	}
}