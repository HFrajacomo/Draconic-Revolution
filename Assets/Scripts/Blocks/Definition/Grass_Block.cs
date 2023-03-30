using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Grass_Block : Blocks
{
	// Just loaded block
	public Grass_Block(){
		this.name = "Grass";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;
		this.maxHP = 150;

		this.tileTop = 1;
		this.tileSide = 2;
		this.tileBottom = 3;

		this.droppedItem = ItemEncyclopedia.GetItem(ItemID.GRASSBLOCK);
		this.minDropQuantity = 1;
		this.maxDropQuantity = 1;
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, (ushort)BlockID.DIRT);
		return 1;
	}

	public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, blockX, blockY, blockZ);

		cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
			Item.GenerateForceVector(), this.droppedItem, Item.RandomizeDropQuantity(minDropQuantity, maxDropQuantity), cl);

		return 1;
	}
}
