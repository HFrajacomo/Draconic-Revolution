using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DroppedItemAI : AbstractAI
{
    public ulong entityCode;
    public bool CREATED_BY_PLAYER;
    public ulong playerCode;
    public ItemStack its;

    public DroppedItemAI(float3 pos, float3 rot, float3 move, ulong code, ushort itemCode, byte amount, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct();
        this.SetPosition(pos, rot);
        this.Install(EntityHitbox.ITEM);
        this.SetChunkloader(cl);
        this.SetHandler(handler);
        this.entityCode = code;
        this.CREATED_BY_PLAYER = false;
        this.its = new ItemStack((ItemID)itemCode, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(pos, rot, move));
    }

    public DroppedItemAI(float3 pos, float3 rot, float3 move, ulong code, ushort itemCode, byte amount, ulong playerCode, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct();
        this.SetPosition(pos, rot);
        this.Install(EntityHitbox.ITEM);
        this.SetChunkloader(cl);
        this.SetHandler(handler);
        this.entityCode = code;
        this.CREATED_BY_PLAYER = true;
        this.playerCode = playerCode;
        this.its = new ItemStack((ItemID)itemCode, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(pos, rot, move));
    }

    public override void Tick(){
        // Refresh Vision
        if(this.terrainVision.RefreshView(this.coords))
            if(this.terrainVision.GroundCollision(this.position))
                this.inboundEventQueue.Add(new EntityEvent(EntityEventType.ISSTANDING));
            
        this.behaviour.HandleBehaviour(ref this.inboundEventQueue);
    }
}
