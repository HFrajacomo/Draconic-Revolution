using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEvent
{
    public EntityEventType type;
    public bool zeroCost;
    public int metaCode;

    public EntityEvent(EntityEventType type, bool zeroCost, int meta){
        this.type = type;
        this.metaCode = meta;
        this.zeroCost = zeroCost;
    }

    public EntityEvent(EntityEventType type, int meta){
        this.type = type;
        this.zeroCost = false;
        this.metaCode = meta;
    }

    public EntityEvent(EntityEventType type){
        this.type = type;
        this.zeroCost = false;
    }


}

public enum EntityEventType : ushort {
    NOTHING,
    BLOCKHIT, // Has hit something
    ISSTANDING, // Is not falling
    NONGROUNDCOLLISION,
    AIRBORN
}
