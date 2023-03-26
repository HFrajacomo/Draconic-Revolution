using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler_Server
{
    public Dictionary<ChunkPos, Dictionary<ulong, AbstractAI>> playerObject;
    public Dictionary<ChunkPos, Dictionary<ulong, AbstractAI>> dropObject;

    private List<EntityID> toRemove;
    private AvailabilityQueue availableDropCodes;
    private List<EntityChunkTransaction> toChangePosition;


    public EntityHandler_Server(){
        this.playerObject = new Dictionary<ChunkPos, Dictionary<ulong, AbstractAI>>();
        this.dropObject = new Dictionary<ChunkPos, Dictionary<ulong, AbstractAI>>();
        this.availableDropCodes = new AvailabilityQueue();
        this.toRemove = new List<EntityID>();
        this.toChangePosition = new List<EntityChunkTransaction>();
    }

    // DEBUG
    private void PrintDrops(){
        StringBuilder sb = new StringBuilder();

        foreach(ChunkPos cp in playerObject.Keys){
            sb.Append(cp + " {\n");

            foreach(ulong u in playerObject[cp].Keys){
                sb.Append("\t" + u + "\n");
            }

            sb.Append("}");
        }

        Debug.Log(sb.ToString());
    }

    // Runs all Tick() functions from loaded entities
    public void RunEntities(){
        DeleteScheduled();
        ApplyChangesScheduled();

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
    public void AddPlayer(ChunkPos chunk, ulong code, float3 pos, float3 dir, ChunkLoader_Server cl){
        if(!this.playerObject.ContainsKey(chunk))
            this.playerObject.Add(chunk, new Dictionary<ulong, AbstractAI>());

        this.playerObject[chunk].Add(code, new PlayerAI(pos, dir, code, this, cl));
    }

    public ulong AddItem(float3 pos, float3 rot, float3 move, ushort itemCode, byte amount, ulong playerCode, ChunkLoader_Server cl){
        CastCoord coord = new CastCoord(pos);
        ChunkPos chunk = coord.GetChunkPos();
        ulong assignedCode = this.availableDropCodes.Pop();

        if(!this.dropObject.ContainsKey(chunk))
            this.dropObject.Add(chunk, new Dictionary<ulong, AbstractAI>());
        
        this.dropObject[chunk].Add(assignedCode, new DroppedItemAI(pos, rot, move, assignedCode, itemCode, amount, playerCode, this, cl));

        return assignedCode;
    }

    // Adds an element to the Handler without creating an AI
    // Used for internal re-allocation of entities
    private void InternalAdd(EntityID novel, AbstractAI ai){
        switch(novel.type){
            case EntityType.PLAYER:
                if(!this.playerObject.ContainsKey(novel.pos))
                    this.playerObject.Add(novel.pos, new Dictionary<ulong, AbstractAI>());

                this.playerObject[novel.pos].Add(novel.code, ai);
                break;
            case EntityType.DROP:
                if(!this.dropObject.ContainsKey(novel.pos))
                    this.dropObject.Add(novel.pos, new Dictionary<ulong, AbstractAI>());

                this.dropObject[novel.pos].Add(novel.code, ai);
                break;            
        }
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

    // Called as remove but doesn't pop values in AvailabilityList
    public void InternalRemove(EntityType type, ChunkPos pos, ulong code){
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
            }
        }
    }

    // Called from inside AbstractAI handlers to schedule current entity for deletion
    public void ScheduleRemove(EntityID id){
        this.toRemove.Add(id);
    }

    // Triggered whenever an entity changes chunk
    public void SchedulePositionChange(EntityChunkTransaction ect){
        this.toChangePosition.Add(ect);
    }

    // Runs removal of marked entities
    private void DeleteScheduled(){
        EntityID entity;
        for(int i=0; i < toRemove.Count; i++){
            entity = this.toRemove[i];

            Remove(entity.type, entity.pos, entity.code);
        }

        this.toRemove.Clear();
    }

    // Applies chunk position changes
    private void ApplyChangesScheduled(){
        EntityChunkTransaction ect;

        for(int i=0; i < this.toChangePosition.Count; i++){
            ect = this.toChangePosition[i];

            InternalAdd(ect.novel, ect.ai);
            InternalRemove(ect.old.type, ect.old.pos, ect.old.code);
            ResetAIChangeFlag(ect);
        }

        this.toChangePosition.Clear();
    }

    private void ResetAIChangeFlag(EntityChunkTransaction e){
        switch(e.novel.type){
            case EntityType.PLAYER:
                this.playerObject[e.novel.pos][e.novel.code].markedForChange = false;
                break;
            case EntityType.DROP:
                this.dropObject[e.novel.pos][e.novel.code].markedForChange = false;
                break;
        }
    }

    public void SetPosition(EntityType type, ulong code, ChunkPos chunk, float3 pos, float3 rot){
        if(type == EntityType.PLAYER){
            if(this.playerObject.ContainsKey(chunk)){
                if(this.playerObject[chunk].ContainsKey(code)){
                    this.playerObject[chunk][code].SetPosition(new Vector3(pos.x, pos.y, pos.z), new Vector3(rot.x, rot.y, rot.z));
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
