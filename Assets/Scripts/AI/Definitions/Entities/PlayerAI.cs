using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerAI : AbstractAI
{
    public PlayerAI(float3 pos, float3 rot, ulong code, EntityHandler_Server handler, PlayerServerInventory psi){
        this.Construct(EntityType.PLAYER, code);
        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.PLAYER);
        this.SetHandler(handler);
        this.Install(new PlayerEntityRadar(this.position, this.rotation, this.coords, this.ID, handler, psi));
    }

    public override void Tick(){
        this.radar.SetTransform(ref this.position, ref this.rotation, ref this.coords);
        this.radar.Search(ref this.inboundEventQueue);
    }
}
