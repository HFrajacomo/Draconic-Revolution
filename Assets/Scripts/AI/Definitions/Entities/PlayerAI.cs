using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerAI : AbstractAI
{
    public PlayerAI(float3 pos, float3 rot, ulong code, EntityHandler_Server handler){
        this.Construct();
        this.SetPosition(pos, rot);
        this.Install(EntityHitbox.PLAYER);
        this.SetHandler(handler);
        this.entityCode = code;
        this.type = EntityType.PLAYER;
    }

    public override void Tick(){return;}
}
