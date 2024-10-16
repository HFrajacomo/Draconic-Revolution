using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BUDScheduler : MonoBehaviour
{
	public TimeOfDay schedulerTime;
	private Dictionary<string, List<BUDSignal>> data = new Dictionary<string, List<BUDSignal>>();
	private Dictionary<string, HashSet<ChunkPos>> toSave = new Dictionary<string, HashSet<ChunkPos>>();
    private HashSet<ChunkPos> toSaveThisFrame = new HashSet<ChunkPos>();
    private HashSet<ChunkPos> toPropagate = new HashSet<ChunkPos>();
    private List<ChunkPos> cachedList = new List<ChunkPos>();
    private string currentTime;
	private string newTime;
	public ChunkLoader_Server loader;
    private byte currentDealocTime;
    private NetMessage message = new NetMessage(NetCode.SENDCHUNK);


    private ChunkPos cachePos;
	private CastCoord cachedCoord;
    private ushort cachedCode;
    private string cachedString;

	void Start(){
		this.currentTime = schedulerTime.GetBUDTime();
		this.data.Add(currentTime, new List<BUDSignal>());
		this.toSave.Add(currentTime, new HashSet<ChunkPos>());
	}

    // Update is called once per frame
    void Update()
    {
    	// Gets this Unity Tick time
    	this.newTime = schedulerTime.GetBUDTime(); 

        this.toSaveThisFrame.Clear();

    	// Checks if BUD Tick has changed
    	if(this.newTime != this.currentTime){
            bool shouldClean = false;

            this.currentDealocTime++;

            // Removes past queues if frame skipped to prevent Memory Leak
            if(currentDealocTime == byte.MaxValue){
                currentDealocTime = 0;

                foreach(string s in this.data.Keys){
                    if(schedulerTime.IsPast(s)){
                        cachedString = s;
                        shouldClean = true;
                        break;
                    }
                }

                if(shouldClean){
                    shouldClean = false;
                    this.data.Remove(cachedString);
                }
            
            }

            // Saves the World Data every second
            if(loader.regionHandler != null)
                loader.regionHandler.SaveWorld();

            // Batch Chunk Saver
            if(SaveCount() > 0){
                foreach(ChunkPos pos in this.toSave[this.currentTime]){
                    if(loader.chunks.ContainsKey(pos)){
                        loader.regionHandler.SaveChunk(loader.chunks[pos]);
                    }
                }
            }

            // Propagates saved chunks to users
            foreach(ChunkPos pos in this.toPropagate){
                this.message = new NetMessage(NetCode.SENDCHUNK);
                this.message.SendChunk(loader.chunks[pos]);
                loader.server.SendToClients(pos, this.message);
            }

            this.toPropagate.Clear();


    		// Frees memory of previous BUD Tick
            this.data.Remove(this.currentTime);
            this.toSave.Remove(this.currentTime);
    		this.currentTime = this.newTime;
    	}

    	// Iterates through frame's list and triggers BUD
        while(DataCount() > 0){
            cachedCoord = new CastCoord(new Vector3(this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z));

            // If BUDSignal is still in the loaded area
            if(loader.chunks.ContainsKey(cachedCoord.GetChunkPos())){
                cachedCode = loader.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);

                if(cachedCode <= ushort.MaxValue/2)
                    VoxelLoader.GetBlock(cachedCode).OnBlockUpdate(this.data[this.currentTime][0].type, this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z, this.data[this.currentTime][0].budX, this.data[this.currentTime][0].budY, this.data[this.currentTime][0].budZ, this.data[this.currentTime][0].facing, loader);
	    	    else{
                    VoxelLoader.GetObject(cachedCode).OnBlockUpdate(this.data[this.currentTime][0].type, this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z, this.data[this.currentTime][0].budX, this.data[this.currentTime][0].budY, this.data[this.currentTime][0].budZ, this.data[this.currentTime][0].facing, loader);                    
                }
            }

            this.data[this.currentTime].RemoveAt(0);
        }
    }


    // Schedules a BUD request in the system
    public void ScheduleBUD(BUDSignal b, int tickOffset){
    	if(tickOffset == 0){
            if(this.data.ContainsKey(this.currentTime)){
                if(!this.data[this.currentTime].Contains(b)){
        		  this.data[this.currentTime].Add(b);
                }
            }
            else{
                this.data.Add(this.currentTime, new List<BUDSignal>());
                this.data[this.currentTime].Add(b);
            }
    	}
    	else{
    		string fakeTime = schedulerTime.FakeSum(tickOffset);

    		if(this.data.ContainsKey(fakeTime)){
                if(!this.data[fakeTime].Contains(b))
                    this.data[fakeTime].Add(b);
    		}
    		else{
    			this.data.Add(fakeTime, new List<BUDSignal>());
    			this.data[fakeTime].Add(b);
    		}
    	}
    }

    // Returns the Amount of elements in this.data[currentTime]. Returns 0 if list is not initialized
    public int DataCount(){
        if(this.data.ContainsKey(this.currentTime)){
            return this.data[this.currentTime].Count;
        }
        return 0;
    }


    // Returns the Amount of elements in this.save[currentTime]. Returns 0 if list is not initialized
    public int SaveCount(){
        if(this.toSave.ContainsKey(this.currentTime)){
            return this.toSave[this.currentTime].Count;
        }
        return 0;
    }


    // Schedules a BUD request in the system in the current tick
    public void ScheduleBUDNow(BUDSignal b){
        if(this.data.ContainsKey(this.currentTime)){ 
            this.data[this.currentTime].Add(b);
        }
        else{
            this.data.Add(this.currentTime, new List<BUDSignal>());
            this.data[this.currentTime].Add(b);
        }
    }

    // Schedules a SaveChunk() operation 
    public void ScheduleSave(ChunkPos pos){
        if(toSaveThisFrame.Contains(pos))
            return;

        string fakeTime = schedulerTime.FakeSum(1);

        if(!this.toSave.ContainsKey(fakeTime))
            this.toSave.Add(fakeTime, new HashSet<ChunkPos>());

        if(this.toSave[fakeTime].Contains(pos))
            return;

        this.toSave[fakeTime].Add(pos);
        toSaveThisFrame.Add(pos);
    }

    // Schedules a Server.SendToClients() operation
    public void SchedulePropagation(ChunkPos pos){
        this.toPropagate.Add(pos);
    }

    // Gets the different Chunks that should be updated
    private void CheckSurroundingChunks(int x, int y, int z, ChunkPos pos){
        if(x == 0)
            cachedList.Add(new ChunkPos(pos.x-1, pos.z, pos.y));
        if(x == Chunk.chunkWidth-1)
            cachedList.Add(new ChunkPos(pos.x+1, pos.z, pos.y));
        if(z == 0)
            cachedList.Add(new ChunkPos(pos.x, pos.z-1, pos.y));
        if(z == Chunk.chunkWidth-1)
            cachedList.Add(new ChunkPos(pos.x, pos.z+1, pos.y));
        if(y == 0 && pos.y > 0)
            cachedList.Add(new ChunkPos(pos.x, pos.z, pos.y-1));
        if(y == Chunk.chunkDepth-1 && pos.y < Chunk.chunkMaxY)
            cachedList.Add(new ChunkPos(pos.x, pos.z, pos.y+1));

        cachedList.Add(pos);
    }

    // Deschedules a BUD request (probably when block is broken or updated)
    public void RemoveBUD(BUDSignal b){
    	foreach(string key in this.data.Keys){
    		this.data[key].RemoveAll(bud => bud.Equals(b));
    	}
    }
}
