using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DroppedItemAI : AbstractAI
{
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
        this.its = new ItemStack((ItemID)itemCode, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(this.position, this.rotation, move, this.its, false));
        this.Install(new ItemEntityRadar(this.position, this.rotation, this.coords, this.its, this.GetID(), handler));
    }

    public DroppedItemAI(float3 pos, float3 rot, float3 move, ulong code, ushort itemCode, byte amount, ulong playerCode, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct(EntityType.DROP, code);
        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.ITEM);
        this.SetChunkloader(cl);
        this.SetHandler(handler);
        this.playerCode = playerCode;
        this.its = new ItemStack((ItemID)itemCode, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(this.position, this.rotation, move, this.its, playerCode != ulong.MaxValue));
        this.Install(new ItemEntityRadar(this.position, this.rotation, this.coords, this.its, this.GetID(), handler));
    }

    // Used when loading dropped items from memory
    public DroppedItemAI(float3 pos, ulong code, Item item, byte amount, byte state, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct(EntityType.DROP, code);
        this.SetInitialPosition(pos, new float3(0,0,0));
        this.Install(EntityHitbox.ITEM);
        this.SetChunkloader(cl);
        this.SetHandler(handler);
        this.its = new ItemStack(item, amount);

        this.Install(new ProjectileTerrainVision(cl));
        this.Install(new ItemBehaviour(this.position, this.rotation, new float3(0,0,0), this.its, false));
        this.Install(new ItemEntityRadar(this.position, this.rotation, this.coords, this.its, this.GetID(), handler));

        LoadState(state);
    }


    public override void Tick(){
        byte moveCode;

        // Refresh Vision
        if(!IsOnPickupMode()){
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

    public bool IsStanding(){
        return ((ItemBehaviour)this.behaviour).IsStanding();
    }

    public void SetItemStackAmount(byte b){
        this.its.SetAmount(b);
    }

    public byte GetItemStackAmount(){
        return this.its.GetAmount();
    }

    public void SetLifespan(int life){
        ((ItemBehaviour)this.behaviour).SetLifespan(life);
    }

    private void SetStanding(bool flag){
        ((ItemBehaviour)this.behaviour).SetStanding(flag);
    }

    public int GetLifespan(){
        return ((ItemBehaviour)this.behaviour).GetLifespan();
    }

    public bool IsOnPickupMode(){
        return ((ItemBehaviour)this.behaviour).IsOnPickupMode();
    }

    public void SetPickupMode(){
        ((ItemBehaviour)this.behaviour).SetPickupMode();
    }

    public bool IsCreatedByPlayer(){
        return ((ItemBehaviour)this.behaviour).IsCreatedByPlayer();
    }

    public override byte GetState(){
        return (byte)SetRotationAsStopFlag((ItemBehaviour)this.behaviour);
    }

    public override void LoadState(byte state){
        // Is standing
        if(state == 1){
            this.SetStanding(true);
            this.message = new NetMessage(NetCode.ITEMENTITYDATA);
            this.message.ItemEntityData(this.position.x, this.position.y, this.position.z, SetRotationAsStopFlag((ItemBehaviour)this.behaviour), this.rotation.y, this.rotation.z, (ushort)this.its.GetID(), this.its.GetAmount(), this.entityCode);
            this.cl.server.SendToClients(this.coords.GetChunkPos(), message);
        }
    }
}
