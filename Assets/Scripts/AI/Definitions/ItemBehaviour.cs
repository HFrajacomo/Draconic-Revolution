using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ItemBehaviour : Behaviour
{
    public Vector3 deltaPos;
    public float weight;
    private bool IS_STANDING = false;
    private int currentLifeTick = 0;

    private NetMessage message = new NetMessage(NetCode.ITEMENTITYDATA);
    private Vector3 gravityVector = Vector3.zero;

    private static readonly float blockOffset = Constants.WORLD_COORDINATES_BLOCK_FLOATOFFSET;
    private static readonly float itemRotationYOffset = .4f;
    private static readonly float itemCollisionOffset = 1f;
    private static readonly float itemBuoyancyOffset = .3f;
    private static readonly EntityEvent waterElevatorEvent = new EntityEvent(EntityEventType.NONGROUNDCOLLISION, 64);

    public ItemBehaviour(Vector3 pos, Vector3 rot, float3 move){
        this.SetTransform(ref pos, ref rot);
        this.deltaPos = new Vector3(move.x, move.y, move.z);
        this.weight = 0.4f;
    }

    public override byte HandleBehaviour(ref List<EntityEvent> ieq){
        currentLifeTick++;

        if(currentLifeTick >= Constants.ITEM_ENTITY_LIFE_SPAN_TICKS){
            return byte.MaxValue;
        }

        if(ieq.Count == 0 && !this.IS_STANDING){
            this.position += this.deltaPos + this.gravityVector; 
            this.rotation = this.deltaPos.normalized;

            this.gravityVector += (Constants.GRAVITY_VECTOR * TimeOfDay.timeRate * weight);

            // Limit gravity pull
            if(this.gravityVector.y < Constants.PHYSICS_MAXIMUM_GRAVITY_SPEED)
                this.gravityVector.y = Constants.PHYSICS_MAXIMUM_GRAVITY_SPEED;

            return 1;
        }
        if(ieq.Count == 0){
            return 3;
        }

        this.cacheEvent = ieq[0];

        if(this.cacheEvent.type == EntityEventType.AIRBORN){
            this.IS_STANDING = false;

            PopEventAndContinue(ref ieq);
            return 1;
        }

        if(this.cacheEvent.type == EntityEventType.ISSTANDING){
            this.IS_STANDING = true;
            this.gravityVector = Vector3.zero;
            this.position = new Vector3(this.position.x, (int)Math.Round(this.position.y, MidpointRounding.AwayFromZero) + itemRotationYOffset, this.position.z);
            this.deltaPos = new Vector3(0f, 0f, 0f);


            if(PopEventAndContinue(ref ieq))
                return HandleBehaviour(ref ieq);
            else
                return 1;
        }

        if(this.cacheEvent.type == EntityEventType.NONGROUNDCOLLISION){

            if((this.cacheEvent.metaCode & 1) > 0){
                this.position.x = (int)Math.Round(this.position.x, MidpointRounding.AwayFromZero) - itemCollisionOffset;
                this.deltaPos.x = 0;
            }
            if((this.cacheEvent.metaCode & 2) > 0){
                this.position.x = (int)Math.Round(this.position.x, MidpointRounding.AwayFromZero) + itemCollisionOffset;
                this.deltaPos.x = 0;
            }
            if((this.cacheEvent.metaCode & 4) > 0){
                this.position.z = (int)Math.Round(this.position.z, MidpointRounding.AwayFromZero) - itemCollisionOffset;
                this.deltaPos.z = 0;
            }
            if((this.cacheEvent.metaCode & 8) > 0){
                this.position.z = (int)Math.Round(this.position.z, MidpointRounding.AwayFromZero) + itemCollisionOffset;
                this.deltaPos.z = 0;
            }
            if((this.cacheEvent.metaCode & 16) > 0){
                this.position.y = (int)Math.Round(this.position.y, MidpointRounding.AwayFromZero) - itemCollisionOffset;
                this.deltaPos.y = 0;
            }
            if((this.cacheEvent.metaCode & 32) > 0){
                this.position.y = (int)Math.Round(this.position.y, MidpointRounding.AwayFromZero) + itemBuoyancyOffset;
                this.deltaPos = Vector3.zero;
                this.gravityVector = Vector3.zero;
                this.IS_STANDING = true;
            }
            if((this.cacheEvent.metaCode & 64) > 0){
                this.position += this.deltaPos + this.gravityVector; 
                this.rotation = this.deltaPos.normalized;

                this.deltaPos *= Constants.PHYSICS_WATER_RESISTANCE_MULTIPLIER;

                // Apply buoyancy effect
                this.gravityVector += (Constants.BUOYANCY_VECTOR * TimeOfDay.timeRate / weight);

                // Limit gravity pull
                if(this.gravityVector.y > Constants.PHYSICS_MAXIMUM_BUOYANCY_SPEED)
                    this.gravityVector.y = Constants.PHYSICS_MAXIMUM_BUOYANCY_SPEED;

                // Recycle this event until another one comes into the queue
                if(ieq.Count == 1)
                    ieq.Add(waterElevatorEvent);
            }
            if((this.cacheEvent.metaCode & 128) > 0){
                this.position.y += 1;
            }

            if(PopEventAndContinue(ref ieq))
                return HandleBehaviour(ref ieq);
            else{
                return 1;
            }
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
