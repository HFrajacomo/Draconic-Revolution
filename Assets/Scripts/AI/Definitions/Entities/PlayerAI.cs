using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerAI : AbstractAI
{
    public PlayerAI(float3 pos, float3 rot, ulong code, EntityHandler_Server handler){
        this.Construct(EntityType.PLAYER, code);
        this.SetInitialPosition(pos, rot);
        this.Install(EntityHitbox.PLAYER);
        this.SetHandler(handler);
    }

    public override void Tick(){
        return;
    }
}
