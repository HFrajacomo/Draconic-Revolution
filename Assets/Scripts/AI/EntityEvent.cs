using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEvent
{
    public EntityEventType type;
    public bool zeroCost;
    public int metaCode;
    public EntityRadarEvent radarEvent;
    public Vector3 position;

    public EntityEvent(EntityEventType type, bool zeroCost, int meta, EntityRadarEvent radar){
        this.type = type;
        this.zeroCost = zeroCost;
        this.radarEvent = radar;
        this.metaCode = meta;
    }

    public EntityEvent(EntityEventType type, bool zeroCost, int meta){
        this.type = type;
        this.metaCode = meta;
        this.zeroCost = zeroCost;
    }

    public EntityEvent(EntityEventType type, bool zeroCost, EntityRadarEvent radar, Vector3 position){
        this.type = type;
        this.zeroCost = zeroCost;
        this.radarEvent = radar;
        this.position = position;
    }

    public EntityEvent(EntityEventType type, int meta, EntityRadarEvent radar){
        this.type = type;
        this.zeroCost = false;
        this.radarEvent = radar;
        this.metaCode = meta;
    }

    public EntityEvent(EntityEventType type, bool zeroCost, EntityRadarEvent radar){
        this.type = type;
        this.zeroCost = zeroCost;
        this.radarEvent = radar;
    }

    public EntityEvent(EntityEventType type, int meta){
        this.type = type;
        this.zeroCost = false;
        this.metaCode = meta;
    }

    public EntityEvent(EntityEventType type, EntityRadarEvent radar){
        this.type = type;
        this.zeroCost = false;
        this.radarEvent = radar;
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
    AIRBORN,
    VISION,
    ITEM_PICKUP,
    ITEMDEATH
}
