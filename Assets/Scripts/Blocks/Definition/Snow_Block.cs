using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Snow_Block : Blocks
{
    public Snow_Block(){
        this.name = "Snow";
        this.solid = true;
        this.transparent = 0;
        this.invisible = false;
        this.liquid = false;
        this.affectLight = true;

        this.tileTop = 8;
        this.tileSide = 8;
        this.tileBottom = 8;

        this.maxHP = 50;
    
        this.droppedItem = Item.GenerateItem(ItemID.SNOWBLOCK);
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
