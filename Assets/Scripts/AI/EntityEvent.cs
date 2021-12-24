using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEvent
{
    public EntityEventType type;
    public bool zeroCost;
    public Dictionary<string, Object> metadata;

    public EntityEvent(EntityEventType type, bool zeroCost, Dictionary<string, Object> meta){
        this.type = type;
        this.metadata = meta;
        this.zeroCost = zeroCost;
    }

    public EntityEvent(EntityEventType type, bool zeroCost){
        this.type = type;
        this.zeroCost = zeroCost;
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
    AIRBORN
}
