using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DroppedItemAI : AbstractAI
{
    public bool CREATED_BY_PLAYER;
    public ulong playerCode;
    public ItemStack its;

    private NetMessage message;
    private EntityTerrainCollision cachedTerrainCollision;
    private int collisionFlag;

    public DroppedItemAI(float3 pos, float3 rot, float3 move, ulong code, ushort itemCode, byte amount, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct(EntityType.DROP, code);

        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.ITEM);
        this.SetChunkloader(cl);
        this.SetHandler(handler);
        this.CREATED_BY_PLAYER = false;
        this.its = new ItemStack((ItemID)itemCode, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(this.position, this.rotation, move));
        this.Install(new ItemEntityRadar(this.position, this.rotation, this.coords, this.its, this.GetID(), handler));
    }

    public DroppedItemAI(float3 pos, float3 rot, float3 move, ulong code, ushort itemCode, byte amount, ulong playerCode, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct(EntityType.DROP, code);
        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.ITEM);
        this.SetChunkloader(cl);
        this.SetHandler(handler);
        this.CREATED_BY_PLAYER = true;
        this.playerCode = playerCode;
        this.its = new ItemStack((ItemID)itemCode, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(this.position, this.rotation, move));
        this.Install(new ItemEntityRadar(this.position, this.rotation, this.coords, this.its, this.GetID(), handler));
    }

    public override void Tick(){
        byte moveCode;

        // Refresh Vision
        if(this.terrainVision.RefreshView(this.coords) > 0){
            this.collisionFlag = this.terrainVision.CollidedAround();

            if(this.collisionFlag > 0){
                this.inboundEventQueue.Add(new EntityEvent(EntityEventType.NONGROUNDCOLLISION, this.collisionFlag));
            }
            else{
                this.cachedTerrainCollision = this.terrainVision.GroundCollision();
                if(this.cachedTerrainCollision != EntityTerrainCollision.NONE){
                    this.inboundEventQueue.Add(new EntityEvent(EntityEventType.ISSTANDING));
                }
                else{
                    this.inboundEventQueue.Add(new EntityEvent(EntityEventType.AIRBORN));
                }
            }
        }

        // Entity Vision
        if(!((ItemBehaviour)this.behaviour).IsStanding()){
            this.radar.Search(ref this.inboundEventQueue);
        }
            
        moveCode = this.behaviour.HandleBehaviour(ref this.inboundEventQueue);

        // If entity died
        if(moveCode == byte.MaxValue){
            this.KillEntity();
            return;
        }

        // Sends movement notification
        if(moveCode != 3){
            SetPosition(this.behaviour.position, this.behaviour.rotation);
            this.radar.SetTransform(ref this.behaviour.position, ref this.behaviour.rotation, ref this.coords);

            this.message = new NetMessage(NetCode.ITEMENTITYDATA);
            this.message.ItemEntityData(this.position.x, this.position.y, this.position.z, SetRotationAsStopFlag((ItemBehaviour)this.behaviour), this.rotation.y, this.rotation.z, (ushort)this.its.GetID(), this.its.GetAmount(), this.entityCode);
            this.cl.server.SendToClients(this.coords.GetChunkPos(), message);
        }
    }

    public ItemStack GetItemStack(){
        return this.its;
    }

    public void SetItemStackAmount(){
    }

    private float SetRotationAsStopFlag(ItemBehaviour behaviour){
        if(behaviour.IsStanding())
            return 1;
        return 0;
    }
}
