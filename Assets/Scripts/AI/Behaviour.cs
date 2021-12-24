using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Behaviour
{
    public Vector3 position;
    public Vector3 rotation;
    protected EntityEvent cacheEvent;

    public abstract byte HandleBehaviour(ref List<EntityEvent> inboundEventQueue);

    public void SetTransform(ref Vector3 pos, ref Vector3 rot){
        this.position = pos;
        this.rotation = rot;
    }

    protected virtual bool PopEventAndContinue(ref List<EntityEvent> inboundEventQueue){
        if(inboundEventQueue.Count == 0)
            return false;

        if(inboundEventQueue[0].zeroCost){
            inboundEventQueue.RemoveAt(0);
            return true;
        }
        else{
            inboundEventQueue.RemoveAt(0);
            return false;
        }
    }
}
