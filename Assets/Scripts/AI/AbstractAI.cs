using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class AbstractAI
{
    // EntityHandler Reference
    protected EntityHandler_Server entityHandler;

    public Vector3 position;
    public Vector3 rotation;
    protected List<EntityEvent> inboundEventQueue;
    protected CastCoord coords;
    protected TerrainVision terrainVision;
    protected EntityHitbox hitbox;
    protected Behaviour behaviour;

    // Main function to move everything in AI's power
    public abstract void Tick();

    public void Construct(){
        this.inboundEventQueue = new List<EntityEvent>();
        this.coords = new CastCoord(this.position);
    }

    // Sets World transform of AI
    public void SetPosition(float3 pos, float3 rot){
        this.position = new Vector3(pos.x, pos.y, pos.z);
        this.rotation = new Vector3(rot.x, rot.y, rot.z);
    }

    // TerrainVision operation
    protected void RefreshView(){
        if(this.terrainVision != null)
            this.terrainVision.RefreshView(this.coords);
    }

    protected byte HandleBehaviour(EntityEvent ev){
        if(this.behaviour != null)
            return this.behaviour.HandleBehaviour(ev);
        return 0;
    }

    protected void SetHandler(EntityHandler_Server handler){
        this.entityHandler = handler;
    }

    protected void Install(TerrainVision tv){
        this.terrainVision = tv;
    }

    protected void Install(Behaviour b){
        this.behaviour = b;
    }

    protected void Install(EntityHitbox hit){
        this.hitbox = hit;
    }
}
