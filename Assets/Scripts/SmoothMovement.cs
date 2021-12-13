using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class SmoothMovement
{
    private EntityHandler handler;
    private Dictionary<ulong, DeltaMove> players = new Dictionary<ulong, DeltaMove>();
    private Dictionary<ulong, DeltaMove> items = new Dictionary<ulong, DeltaMove>();

    public SmoothMovement(EntityHandler hand){
        this.handler = hand;
    }

    public bool AddPlayer(ulong id){
        DeltaMove dm = new DeltaMove(Vector3.zero, Vector3.zero);

        if(!this.players.ContainsKey(id)){
            this.players.Add(id, dm);
            return true;
        }

        return false;
    }

    public bool AddItem(ulong id){
        DeltaMove dm = new DeltaMove(Vector3.zero, Vector3.zero);

        if(!this.items.ContainsKey(id)){
            this.items.Add(id, dm);
            return true;
        }

        return false;
    }

    public bool Remove(EntityType type, ulong id){
        if(type == EntityType.PLAYER){
            if(this.players.ContainsKey(id)){
                this.players.Remove(id);
                return true;
            }
        }
        else if(type == EntityType.DROP){
            if(this.items.ContainsKey(id)){
                this.items.Remove(id);
                return true;
            }            
        }
        return false;
    }

    // Changes the DeltaMove for the current tick
    // Pos and Rot are the new coordinates received by server before changing them in EntityHandler
    public void DefineMovement(EntityType type, ulong id, float3 pos, float3 rot){
        Vector3 posV, rotV;
        posV = new Vector3(pos.x, pos.y, pos.z);
        rotV = new Vector3(rot.x, rot.y, rot.z);

        DeltaMove dm = new DeltaMove(posV - this.handler.GetLastPosition(type, id), rotV - this.handler.GetLastRotation(type, id));
        
        if(type == EntityType.PLAYER)
            if(this.players.ContainsKey(id))
                this.players[id] = dm;
        else if(type == EntityType.DROP)
            if(this.items.ContainsKey(id))
                this.items[id] = dm;
    }

    // Moves all entities according to their DeltaMoves
    public void MoveEntities(){
        foreach(ulong id in this.players.Keys){
            if(this.players[id].deltaPos == Vector3.zero && this.players[id].deltaRot == Vector3.zero){
                continue;
            }

            this.handler.Nudge(EntityType.PLAYER, id, this.players[id].deltaPos, this.players[id].deltaRot);
        }

        foreach(ulong id in this.items.Keys){
            if(this.items[id].deltaPos == Vector3.zero && this.items[id].deltaRot == Vector3.zero){
                continue;
            }

            this.handler.Nudge(EntityType.PLAYER, id, this.items[id].deltaPos, this.items[id].deltaRot);
        }
    }
}

public struct DeltaMove
{
    public Vector3 deltaPos;
    public Vector3 deltaRot;

    public DeltaMove(Vector3 p, Vector3 r){
        this.deltaPos = p;
        this.deltaRot = r;
    }

    public DeltaMove(float3 p, float3 r){
        this.deltaPos = new Vector3(p.x, p.y, p.z);
        this.deltaRot = new Vector3(r.x, r.y, r.z);
    }

    public bool Equals(DeltaMove dm){
        return this.deltaPos == dm.deltaPos && this.deltaRot == dm.deltaRot;
    }
}