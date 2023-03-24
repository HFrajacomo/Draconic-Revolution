using System;
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
    private Vector3 gravityVector = Vector3.zero;

    private static readonly float blockOffset = Constants.WORLD_COORDINATES_BLOCK_FLOATOFFSET;
    private static readonly float itemRotationYOffset = 1.3f;

    public ItemBehaviour(Vector3 pos, Vector3 rot, float3 move){
        this.SetTransform(ref pos, ref rot);
        this.deltaPos = new Vector3(move.x, move.y, move.z);
        this.weight = 0.4f;
    }

    public override byte HandleBehaviour(ref List<EntityEvent> ieq){
        if(ieq.Count == 0 && !this.IS_STANDING){
            this.position += this.deltaPos + this.gravityVector; 
            this.rotation = this.deltaPos.normalized;

            this.gravityVector += (Constants.GRAVITY_VECTOR * TimeOfDay.timeRate * weight);

            // Limit gravity pull
            if(this.gravityVector.y > Constants.PHYSICS_MAXIMUM_GRAVITY_SPEED)
                this.gravityVector.y = Constants.PHYSICS_MAXIMUM_GRAVITY_SPEED;

            return 1;
        }
        if(ieq.Count == 0){
            return 3;
        }

        this.cacheEvent = ieq[0];

        if(this.cacheEvent.type == EntityEventType.AIRBORN){
            this.IS_STANDING = false;
        }

        if(this.cacheEvent.type == EntityEventType.ISSTANDING){
            this.IS_STANDING = true;
            this.gravityVector = Vector3.zero;
            Debug.Log(this.position.y);
            this.position = new Vector3(this.position.x, (int)Math.Round(this.position.y, MidpointRounding.AwayFromZero) + itemRotationYOffset, this.position.z);
            this.deltaPos = new Vector3(0f, 0f, 0f);


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
