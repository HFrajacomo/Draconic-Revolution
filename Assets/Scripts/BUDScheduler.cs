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

    void Update(){
        this.newTime = schedulerTime.GetBUDTime();

        // If frame changes, process all remaining operations from last frame
        if(this.newTime != this.currentTime){
            // Saves the World Data every tick
            if(loader.regionHandler != null)
                loader.regionHandler.SaveWorld();

            RunOnFrame(this.currentTime);
        }

        // Run on current Frame
        this.currentTime = this.newTime;

        RunOnFrame(this.currentTime);
    }

    private void RunOnFrame(string frame){
        // Runs BUD
        if(DataCount() > 0){
            while(DataCount() > 0){
                cachedCoord = new CastCoord(new Vector3(this.data[frame][0].x, this.data[frame][0].y, this.data[frame][0].z));

                // If BUDSignal is still in the loaded area
                if(loader.Contains(cachedCoord.GetChunkPos())){
                    cachedCode = loader.GetChunk(cachedCoord.GetChunkPos()).data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);

                    if(cachedCode <= ushort.MaxValue/2)
                        VoxelLoader.GetBlock(cachedCode).OnBlockUpdate(this.data[frame][0].type, this.data[frame][0].x, this.data[frame][0].y, this.data[frame][0].z, this.data[frame][0].budX, this.data[frame][0].budY, this.data[frame][0].budZ, this.data[frame][0].facing, loader);
                    else{
                        VoxelLoader.GetObject(cachedCode).OnBlockUpdate(this.data[frame][0].type, this.data[frame][0].x, this.data[frame][0].y, this.data[frame][0].z, this.data[frame][0].budX, this.data[frame][0].budY, this.data[frame][0].budZ, this.data[frame][0].facing, loader);                    
                    }
                }

                this.data[frame].RemoveAt(0);
            }

            this.data.Remove(frame);
        }

        // Saves chunks
        if(SaveCount() > 0){
            foreach(ChunkPos pos in this.toSave[frame]){
                if(loader.Contains(pos)){
                    loader.regionHandler.SaveChunk(loader.GetChunk(pos));
                }
            }

            this.toSave.Remove(frame);
        }

        // Propagates saved chunks to users
        foreach(ChunkPos pos in this.toPropagate){
            this.message = new NetMessage(NetCode.SENDCHUNK);
            this.message.SendChunk(loader.GetChunk(pos));
            loader.server.SendToClients(pos, this.message);
        }

        this.toPropagate.Clear();
        this.toSaveThisFrame.Clear();
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

    // Returns the Amount of elements in this.data[currentTime]. Returns 0 if list is not initialized
    private int DataCount(){
        if(this.data.ContainsKey(this.currentTime)){
            return this.data[this.currentTime].Count;
        }
        return 0;
    }


    // Returns the Amount of elements in this.save[currentTime]. Returns 0 if list is not initialized
    private int SaveCount(){
        if(this.toSave.ContainsKey(this.currentTime)){
            return this.toSave[this.currentTime].Count;
        }
        return 0;
    }
}
