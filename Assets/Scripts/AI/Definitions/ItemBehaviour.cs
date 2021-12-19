using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ItemBehaviour : Behaviour
{
    public Vector3 deltaPos;
    public float weight;
    private NetMessage message = new NetMessage(NetCode.ITEMENTITYDATA);

    public ItemBehaviour(Vector3 pos, Vector3 rot, float3 move){
        this.SetTransform(ref pos, ref rot);
        this.deltaPos = new Vector3(move.x, move.y, move.z);
        this.weight = 0.5f;
    }

    public override byte HandleBehaviour(ref List<EntityEvent> ieq){
        if(ieq.Count == 0){
            this.position += this.deltaPos; 
            this.rotation = this.deltaPos.normalized;

            this.deltaPos = this.deltaPos + (Constants.GRAVITY_VECTOR * TimeOfDay.timeRate * weight);
            return 1;
        }

        this.cacheEvent = ieq[0];

        if(this.cacheEvent.type == EntityEventType.ISSTANDING){
            if(this.deltaPos.sqrMagnitude <= 0.02f){
                if(PopEventAndContinue(ref ieq))
                    return HandleBehaviour(ref ieq);
                else
                    return 3;
            }

            this.deltaPos -= new Vector3(0f, this.deltaPos.y, 0f);

            this.position += this.deltaPos;

            this.deltaPos = this.deltaPos * Constants.PHYSICS_ITEM_DRAG_MULTIPLIER;   

            if(PopEventAndContinue(ref ieq))
                return HandleBehaviour(ref ieq);
            else
                return 2;
        }

        if(PopEventAndContinue(ref ieq))
            return HandleBehaviour(ref ieq);
        else
            return 3;
    }
}
