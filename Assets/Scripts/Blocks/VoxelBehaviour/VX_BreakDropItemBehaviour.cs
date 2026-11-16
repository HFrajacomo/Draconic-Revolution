using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class VX_BreakDropItemBehaviour : VoxelBehaviour{
	public string droppedItem;
	public byte minDropQuantity;
	public byte maxDropQuantity;

	private ushort droppedItemID;

	// Items are loaded before voxels run this, so a wrong codename fails on load instead of on every break
	public override void PostDeserializationSetup(bool isClient){
		if(!ItemLoader.Contains(this.droppedItem))
			throw new DeserializationErrorException($"[VX_BreakDropItemBehaviour] Dropped item '{this.droppedItem}' is not a registered item");

		this.droppedItemID = ItemLoader.GetID(this.droppedItem);
	}

	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, x, y, z);
		cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()), Item.GenerateForceVector(), ItemLoader.GetCopy(this.droppedItemID), Item.RandomizeDropQuantity(this.minDropQuantity, this.maxDropQuantity), cl);
		return 1;
	}
}