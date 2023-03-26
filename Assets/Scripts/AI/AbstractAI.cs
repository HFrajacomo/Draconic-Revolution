using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class AbstractAI
{
    // EntityHandler Reference
    protected EntityHandler_Server entityHandler;
    protected ChunkLoader_Server cl;

    public ulong entityCode;
    public Vector3 position;
    public Vector3 rotation;
    public EntityType type;
    protected CastCoord coords;
    protected List<EntityEvent> inboundEventQueue;
    protected TerrainVision terrainVision;
    protected EntityHitbox hitbox;
    protected Behaviour behaviour;
    protected EntityRadar radar;

    protected EntityID ID;
    protected EntityID auxID;

    public bool markedForDelete = false;
    public bool markedForChange = false;

    // Main function to move everything in AI's power
    public abstract void Tick();

    public void Construct(EntityType t, ulong code){
        this.inboundEventQueue = new List<EntityEvent>();
        this.type = t;
        this.entityCode = code;
    }

    public void AddToInboundEventQueue(EntityEvent e){
        this.inboundEventQueue.Add(e);
    }

    protected virtual void KillEntity(){
        NetMessage deathMessage = new NetMessage(NetCode.ENTITYDELETE);

        deathMessage.EntityDelete(this.type, this.entityCode);
        this.cl.server.SendToClients(this.coords.GetChunkPos(), deathMessage);
        this.entityHandler.ScheduleRemove(this.ID);
        this.markedForDelete = true;
    }

    // Used to set initial position in the Constructor of every Child of AbstractAI
    protected void SetInitialPosition(float3 pos, float3 rot){
        this.position = new Vector3(pos.x, pos.y, pos.z);
        this.rotation = new Vector3(rot.x, rot.y, rot.z);
        this.coords = new CastCoord(this.position);
        this.ID = GetID();
    }

    // Sets World transform of AI
    public void SetPosition(float3 pos, float3 rot){
        this.position = new Vector3(pos.x, pos.y, pos.z);
        this.rotation = new Vector3(rot.x, rot.y, rot.z);
        this.coords = new CastCoord(this.position);

        SetIdentityAndNotifyHandler();
    }

    // Sets World transform of AI
    public void SetPosition(Vector3 pos, Vector3 rot){
        this.position = pos;
        this.rotation = rot;
        this.coords = new CastCoord(this.position);

        SetIdentityAndNotifyHandler();
    }

    private void SetIdentityAndNotifyHandler(){
        EntityID newID = GetID();

        if(this.ID.IsDiffPosition(newID)){
            this.entityHandler.SchedulePositionChange(new EntityChunkTransaction(this.ID, newID, this));
            this.ID = newID;
            this.markedForChange = true;
        }
    }

    public Vector3 GetPosition(){
        return this.position;
    }

    public Vector3 GetRotation(){
        return this.rotation;
    }

    // Forces a TerrainVision.RefreshView() operation
    public void SetRefreshVision(){
        this.terrainVision.SetRefresh();
    }

    // TerrainVision operation
    protected void RefreshView(){
        if(this.terrainVision != null)
            this.terrainVision.RefreshView(this.coords);
    }

    protected byte HandleBehaviour(){
        if(this.behaviour != null)
            return this.behaviour.HandleBehaviour(ref this.inboundEventQueue);
        return 0;
    }

    protected void SetHandler(EntityHandler_Server handler){
        this.entityHandler = handler;
    }

    protected void SetChunkloader(ChunkLoader_Server cl){
        this.cl = cl;
    }

    protected void Install(TerrainVision tv){
        this.terrainVision = tv;
        tv.SetHitbox(this.hitbox);
    }

    protected void Install(Behaviour b){
        this.behaviour = b;
    }

    protected void Install(EntityHitbox hit){
        this.hitbox = hit;
    }

    protected void Install(EntityRadar radar){
        this.radar = radar;
    }

    public EntityID GetID(){
        return new EntityID(this.type, this.coords.GetChunkPos(), this.entityCode);
    }
}
