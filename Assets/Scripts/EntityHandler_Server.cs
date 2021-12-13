using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler_Server
{
    public Dictionary<ChunkPos, Dictionary<ulong, AbstractAI>> playerObject;
    public Dictionary<ChunkPos, Dictionary<ulong, DroppedItemAI>> dropObject;
    private AvailabilityQueue availableDropCodes;


    public EntityHandler_Server(){
        this.playerObject = new Dictionary<ChunkPos, Dictionary<ulong, AbstractAI>>();
        this.dropObject = new Dictionary<ChunkPos, Dictionary<ulong, DroppedItemAI>>();
        this.availableDropCodes = new AvailabilityQueue();
    }

    // Runs all Tick() functions from loaded entities
    public void RunEntities(){
        foreach(ChunkPos key in this.playerObject.Keys){
            foreach(ulong code in this.playerObject[key].Keys){
                this.playerObject[key][code].Tick();
            }
        }
        foreach(ChunkPos key in this.dropObject.Keys){
            foreach(ulong code in this.dropObject[key].Keys){
                this.dropObject[key][code].Tick();
            }
        }
    }

    // Returns true if current chunk is loaded and has entities on it
    public bool Contains(EntityType type, ChunkPos pos){
        if(type == EntityType.PLAYER)
            return this.playerObject.ContainsKey(pos);
        else if(type == EntityType.DROP)
            return this.dropObject.ContainsKey(pos);
        return false;
    }

    // Only works while there is no other EntityTypes here other than player
    public bool Contains(EntityType type, ChunkPos pos, ulong code){
        if(type == EntityType.PLAYER){
            if(this.playerObject.ContainsKey(pos))
                return this.playerObject[pos].ContainsKey(code);
            return false;
        }
        else if(type == EntityType.DROP){
            if(this.dropObject.ContainsKey(pos))
                return this.dropObject[pos].ContainsKey(code);
            return false;
        }
        return false;
    }

    // Only works while there is no other EntityType
    public void AddPlayer(ChunkPos chunk, ulong code, float3 pos, float3 dir){
        if(!this.playerObject.ContainsKey(chunk))
            this.playerObject.Add(chunk, new Dictionary<ulong, AbstractAI>());

        this.playerObject[chunk].Add(code, new PlayerAI(pos, dir, code, this));
    }

    public ulong AddItem(float3 pos, float3 rot, float3 move, ushort itemCode, byte amount, ulong playerCode, ChunkLoader_Server cl){
        CastCoord coord = new CastCoord(pos);
        ChunkPos chunk = coord.GetChunkPos();
        ulong assignedCode = this.availableDropCodes.Pop();

        if(!this.dropObject.ContainsKey(chunk))
            this.dropObject.Add(chunk, new Dictionary<ulong, DroppedItemAI>());
        
        this.dropObject[chunk].Add(assignedCode, new DroppedItemAI(pos, rot, move, assignedCode, itemCode, amount, playerCode, this, cl));

        return assignedCode;
    }

    // ...
    public void Remove(EntityType type, ChunkPos pos, ulong code){
        if(type == EntityType.PLAYER){
            if(this.playerObject.ContainsKey(pos)){
                this.playerObject[pos].Remove(code);
                if(this.playerObject[pos].Count == 0)
                    this.playerObject.Remove(pos);
            }
        }
        else if(type == EntityType.DROP){
            if(this.dropObject.ContainsKey(pos)){
                this.dropObject[pos].Remove(code);
                if(this.dropObject[pos].Count == 0)
                    this.dropObject.Remove(pos);

                this.availableDropCodes.Add(code);
            }
        }
    }

    public void SetPosition(EntityType type, ulong code, ChunkPos chunk, float3 pos){
        if(type == EntityType.PLAYER){
            if(this.playerObject.ContainsKey(chunk)){
                if(this.playerObject[chunk].ContainsKey(code)){
                    this.playerObject[chunk][code].position = new Vector3(pos.x, pos.y, pos.z);
                }
            }
        }
    }

    public void SetRotation(EntityType type, ulong code, ChunkPos chunk, float3 rot){
        if(type == EntityType.PLAYER){
            if(this.playerObject.ContainsKey(chunk)){
                if(this.playerObject[chunk].ContainsKey(code)){
                    this.playerObject[chunk][code].rotation = new Vector3(rot.x, rot.y, rot.z);
                }
            }
        }
    }

    // Sets TerrainVision Refresh on all entities of a given chunk and EntityType
    public void SetRefreshVision(EntityType type, ChunkPos pos){
        if(type == EntityType.DROP){
            if(this.Contains(type, pos)){
                foreach(ulong code in this.dropObject[pos].Keys){
                    this.dropObject[pos][code].SetRefreshVision();
                }
            }
        }
    }
}
