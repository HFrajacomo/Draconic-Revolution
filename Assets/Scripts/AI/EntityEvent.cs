using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EntityEvent
{
    public EntityEventType type;
    public bool zeroCost;
    public Dictionary<string, string> metadata;

    public EntityEvent(EntityEventType type, bool zeroCost, Dictionary<string, string> meta){
        this.type = type;
        this.metadata = meta;
        this.zeroCost = zeroCost;
    }
}

public enum EntityEventType : ushort {
    NOTHING,
    BLOCKHIT
}
