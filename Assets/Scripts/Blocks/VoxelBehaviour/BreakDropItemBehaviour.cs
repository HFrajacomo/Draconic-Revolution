using System;
using UnityEngine;
using Unity.Mathematics;

public class BreakDropItemBehaviour : VoxelBehaviour{
	public Item droppedItem;
	public byte minDropQuantity;
	public byte maxDropQuantity;

	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, x, y, z);

		cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()), Item.GenerateForceVector(), this.droppedItem, Item.RandomizeDropQuantity(this.minDropQuantity, this.maxDropQuantity), cl);
		return 1;
	}
}