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

    private NetMessage message;

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
        this.Install(new ItemBehaviour(this.position, this.rotation, move));
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
        byte moveCode;

        // Refresh Vision
        if(this.terrainVision.RefreshView(this.coords))
            if(this.terrainVision.GroundCollision(this.position))
                this.inboundEventQueue.Add(new EntityEvent(EntityEventType.ISSTANDING));
            
        moveCode = this.behaviour.HandleBehaviour(ref this.inboundEventQueue);

        // Sends movement notification
        if(moveCode != 3){
            this.position = this.behaviour.position;
            this.rotation = this.behaviour.rotation;
            this.coords = new CastCoord(this.position);
            this.message = new NetMessage(NetCode.ITEMENTITYDATA);
            this.message.ItemEntityData(this.position.x, this.position.y, this.position.z, this.rotation.x, this.rotation.y, this.rotation.z, (ushort)this.its.GetID(), this.its.GetAmount(), this.entityCode);
            this.cl.server.SendToClients(this.coords.GetChunkPos(), message);
        }
    }
}
