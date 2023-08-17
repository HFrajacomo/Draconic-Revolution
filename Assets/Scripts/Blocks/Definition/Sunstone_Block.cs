using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Sunstone_Block : Blocks
{
	public Sunstone_Block(){
		this.name = "Sunstone";
		this.solid = true;
		this.transparent = 0;
		this.invisible = false;
		this.liquid = false;
		this.affectLight = true;

		this.tileTop = 45;
		this.tileSide = 45;
		this.tileBottom = 45;

		this.luminosity = 15;

		this.maxHP = 240;

        this.droppedItem = Item.GenerateItem(ItemID.SUNSTONEBLOCK);
        this.minDropQuantity = 1;
        this.maxDropQuantity = 2;
	}

    public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
        CastCoord coord = new CastCoord(pos, blockX, blockY, blockZ);

        cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
            Item.GenerateForceVector(), this.droppedItem, Item.RandomizeDropQuantity(minDropQuantity, maxDropQuantity), cl);

        return 1;
    }
}
