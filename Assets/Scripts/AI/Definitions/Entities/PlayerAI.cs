using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerAI : AbstractAI
{
    private PlayerServerInventory psi;

    public PlayerAI(float3 pos, float3 rot, ulong code, EntityHandler_Server handler, ChunkLoader_Server cl){
        this.Construct(EntityType.PLAYER, code);
        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.PLAYER);
        this.SetHandler(handler);
        this.Install(new PlayerEntityRadar(this.position, this.rotation, this.coords, this.ID, handler, cl.playerServerInventory));
        this.cl = cl;
        this.psi = cl.playerServerInventory;
    }

    public override void Tick(){
        ((PlayerEntityRadar)this.radar).HAS_RECEIVED_ITEMS = false;
        this.radar.SetTransform(ref this.position, ref this.rotation, ref this.coords);
        this.radar.Search(ref this.inboundEventQueue);

        if(((PlayerEntityRadar)this.radar).HAS_RECEIVED_ITEMS){
            int length = this.psi.ConvertInventoryToBytes(this.ID.code);
            NetMessage message = new NetMessage(NetCode.SENDINVENTORY);
            
            message.SendInventory(this.psi.GetBuffer(), length);
            this.cl.server.Send(message.GetMessage(), message.size, this.ID.code);
        }
    }
}
