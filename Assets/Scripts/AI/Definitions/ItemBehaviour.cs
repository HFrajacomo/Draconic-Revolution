using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ItemBehaviour : Behaviour
{
    public Vector3 deltaPos;
    public float weight;
    private NetMessage message = new NetMessage(NetCode.ITEMENTITYDATA);
    private bool IS_STANDING = false;
    private static readonly float blockOffset = Constants.WORLD_COORDINATES_BLOCK_FLOATOFFSET - EntityHitbox.ITEM.GetDiameter().y/2;
    private static readonly float itemRotationYOffset = 1.7f;

    public ItemBehaviour(Vector3 pos, Vector3 rot, float3 move){
        this.SetTransform(ref pos, ref rot);
        this.deltaPos = new Vector3(move.x, move.y, move.z);
        this.weight = 0.5f;
    }

    public override byte HandleBehaviour(ref List<EntityEvent> ieq){
        if(ieq.Count == 0 && !this.IS_STANDING){
            this.position += this.deltaPos; 
            this.rotation = this.deltaPos.normalized;

            this.deltaPos = this.deltaPos + (Constants.GRAVITY_VECTOR * TimeOfDay.timeRate * weight);
            return 1;
        }
        if(ieq.Count == 0){
            if(this.deltaPos.sqrMagnitude <= 0.02f){
                if(PopEventAndContinue(ref ieq))
                    return HandleBehaviour(ref ieq);
                else
                    return 3;
            }

            this.deltaPos = new Vector3(0f, 0f, 0f);
            this.position = new Vector3(this.position.x, (int)this.position.y + itemRotationYOffset, this.position.z);

            if(PopEventAndContinue(ref ieq))
                return HandleBehaviour(ref ieq);
            else
                return 2;
        }

        this.cacheEvent = ieq[0];

        if(this.cacheEvent.type == EntityEventType.AIRBORN){
            this.IS_STANDING = false;
        }

        if(this.cacheEvent.type == EntityEventType.ISSTANDING){
            this.IS_STANDING = true;
            this.position = new Vector3(this.position.x, Mathf.CeilToInt(this.position.y) - ItemBehaviour.blockOffset, this.position.z);

            if(PopEventAndContinue(ref ieq))
                return HandleBehaviour(ref ieq);
            else
                return 1;
        }

        if(PopEventAndContinue(ref ieq))
            return HandleBehaviour(ref ieq);
        else
            return 3;
    }

    public bool IsStanding(){
        return this.IS_STANDING;
    }
}
