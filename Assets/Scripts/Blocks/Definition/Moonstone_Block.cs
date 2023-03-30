using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Moonstone_Block : Blocks
{
    public Moonstone_Block(){
        this.name = "Moonstone";
        this.solid = true;
        this.transparent = 0;
        this.invisible = false;
        this.liquid = false;
        this.affectLight = true;

        this.tileTop = 31;
        this.tileSide = 31;
        this.tileBottom = 31;

        this.maxHP = 400;

        this.droppedItem = Item.GenerateItem(ItemID.MOONSTONEBLOCK);
        this.minDropQuantity = 1;
        this.maxDropQuantity = 1;
    }

    public override int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
        CastCoord coord = new CastCoord(pos, blockX, blockY, blockZ);

        cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
            Item.GenerateForceVector(), this.droppedItem, Item.RandomizeDropQuantity(minDropQuantity, maxDropQuantity), cl);

        return 1;
    }
}
