using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Dirt_Block : Blocks
{
	public Dirt_Block(){
		this.name = "Dirt";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;
		this.maxHP = 100;

		this.tileTop = 3;
		this.tileSide = 3;
		this.tileBottom = 3;


		this.droppedItem = Item.GenerateItem(ItemID.DIRTBLOCK);
		this.minDropQuantity = 1;
		this.maxDropQuantity = 1;
	}

	public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, blockX, blockY, blockZ);

		cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
			Item.GenerateForceVector(), this.droppedItem, Item.RandomizeDropQuantity(minDropQuantity, maxDropQuantity), cl);

		return 1;
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		// Changes to Grass
		cl.chunks[pos].data.SetCell(blockX, blockY, blockZ, (ushort)BlockID.GRASS);
		return 1;
	}
}
