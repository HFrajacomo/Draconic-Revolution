using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerAI : AbstractAI
{
    private PlayerServerInventory psi;

    // Cache
    private Vector3 eyePosition;

    public PlayerAI(float3 pos, float3 rot, ulong code, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct(EntityType.PLAYER, code);
        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.PLAYER);
        this.SetHandler(handler);
        this.Install(new PlayerEntityRadar(this.position + Constants.CHARACTER_MODEL_EYE_Y_OFFSET, this.rotation, this.coords, this.ID, handler, cl.playerServerInventory, cl));
        this.cl = cl;
        this.psi = cl.playerServerInventory;
    }

    public override void Tick(){
        ((PlayerEntityRadar)this.radar).HAS_RECEIVED_ITEMS = false;

        this.eyePosition = this.position + Constants.CHARACTER_MODEL_EYE_Y_OFFSET;
        this.radar.SetTransform(ref this.eyePosition, ref this.rotation, ref this.coords);
        this.radar.Search(ref this.inboundEventQueue);

        if(((PlayerEntityRadar)this.radar).HAS_RECEIVED_ITEMS){
            int length = this.psi.ConvertInventoryToBytes(this.ID.code);
            NetMessage message = new NetMessage(NetCode.SENDINVENTORY);
            
            message.SendInventory(this.psi.GetBuffer(), length);
            this.cl.server.Send(message.GetMessage(), message.size, this.ID.code);
        }
    }

    public override byte GetState(){return 0;}
    public override void LoadState(byte state){}
}
