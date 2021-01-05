using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BUDScheduler : MonoBehaviour
{
	public TimeOfDay schedulerTime;
    public Transform playerTransform;
	private Dictionary<string, List<BUDSignal>> data = new Dictionary<string, List<BUDSignal>>();
	private Dictionary<string, List<ChunkPos>> toReload = new Dictionary<string, List<ChunkPos>>();
    private List<ChunkPos> cachedList = new List<ChunkPos>();
    private string currentTime;
	private string newTime;
	public ChunkLoader loader;
	public int BUDperFrame;
	private int currentBUDonFrame;
    private byte currentDealocTime;


    private ChunkPos cachePos;
	private CastCoord cachedCoord;
    private int cachedCode;
    private string cachedString;

	void Start(){
		this.currentTime = schedulerTime.GetBUDTime();
		this.BUDperFrame = 200;
		this.data.Add(currentTime, new List<BUDSignal>());
		this.toReload.Add(currentTime, new List<ChunkPos>());
	}

    // Update is called once per frame
    void Update()
    {
    	// Gets this Unity Tick time
    	this.newTime = schedulerTime.GetBUDTime(); 

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

                foreach(string s in this.toReload.Keys){
                    if(schedulerTime.IsPast(s)){
                        cachedString = s;
                        shouldClean = true;
                    }
                }

                if(shouldClean){
                    this.toReload.Remove(cachedString);
                }             
            }

            // Saves the World Data every second
            loader.regionHandler.SaveWorld();
            loader.regionHandler.SavePlayer(playerTransform.position);

    		this.currentBUDonFrame = 0;

    		// Pops all elements of BUD
            if(this.data.ContainsKey(this.currentTime)){
        		if(this.data[this.currentTime].Count > 0){
        			PassToNextTick(BUD:true);
        		}
            }

            if(this.toReload.ContainsKey(this.currentTime)){
                if(this.toReload[this.currentTime].Count > 0){
                    PassToNextTick(BUD:false);
                }
            }

    		// Frees memory of previous BUD Tick
            this.data.Remove(this.currentTime);
            this.toReload.Remove(this.currentTime);
    		this.currentTime = this.newTime;
    	}

    	// Iterates through frame's list and triggers BUD
        if(DataCount() > 0){
        	for(currentBUDonFrame=0;currentBUDonFrame<BUDperFrame;currentBUDonFrame++){
    	    	if(this.data[this.currentTime].Count > 0){
                    cachedCoord = new CastCoord(new Vector3(this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z));

                    // If BUDSignal is still in the loaded area
                    if(loader.chunks.ContainsKey(cachedCoord.GetChunkPos())){
                        cachedCode = loader.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
                        
                        if(cachedCode <= ushort.MaxValue/2)
                            loader.blockBook.blocks[cachedCode].OnBlockUpdate(this.data[this.currentTime][0].type, this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z, this.data[this.currentTime][0].budX, this.data[this.currentTime][0].budY, this.data[this.currentTime][0].budZ, this.data[this.currentTime][0].facing, loader);
        	    	    else{
                            loader.blockBook.objects[ushort.MaxValue - cachedCode].OnBlockUpdate(this.data[this.currentTime][0].type, this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z, this.data[this.currentTime][0].budX, this.data[this.currentTime][0].budY, this.data[this.currentTime][0].budZ, this.data[this.currentTime][0].facing, loader);                    
                        }
                    }

                    this.data[this.currentTime].RemoveAt(0);
                }
    	    	else{
    	    		break;
    	    	}
        	}
        }

    	// Chunk Reloader
    	if(DataCount() == 0 && ReloadCount() > 0){
            cachePos = this.toReload[this.currentTime][0];
            this.toReload[this.currentTime].RemoveAt(0);

            if(loader.chunks.ContainsKey(cachePos)){
                loader.chunks[cachePos].BuildChunk(); 
                if(!loader.chunks[cachePos].BuildSideBorder(reload:true))
                    loader.toRedraw.Add(cachePos);
                
                loader.regionHandler.SaveChunk(loader.chunks[cachePos]);
            }
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

    // Returns the Amount of elements in this.reload[currentTime]. Returns 0 if list is not initialized
    public int ReloadCount(){
        if(this.toReload.ContainsKey(this.currentTime)){
            return this.toReload[this.currentTime].Count;
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

    // Schedules a Chunk.Build() operation 
    public void ScheduleReload(ChunkPos pos, int tickOffset, int x=1, int y=1, int z=1){
    	if(tickOffset == 0){
            if(!this.toReload.ContainsKey(this.currentTime)){
                this.toReload.Add(this.currentTime, new List<ChunkPos>());
            }

            CheckSurroundingChunks(x, y, z, pos);

            foreach(ChunkPos p in cachedList){
                if(!this.toReload[this.currentTime].Contains(p)){
                    this.toReload[this.currentTime].Add(p);
                }

            }
            cachedList.Clear();
    	}
    	else{
    		string fakeTime = schedulerTime.FakeSum(tickOffset);

    		if(this.toReload.ContainsKey(fakeTime)){
                CheckSurroundingChunks(x, y, z, pos);
                foreach(ChunkPos p in cachedList){
                    this.toReload[fakeTime].Add(p);
                }
                cachedList.Clear();
    		}

    		else{
                CheckSurroundingChunks(x, y, z, pos);
                foreach(ChunkPos p in cachedList){
                    this.toReload.Add(fakeTime, new List<ChunkPos>());
                    this.toReload[fakeTime].Add(pos);
                }
                cachedList.Clear();
    		}
    	}
    }

    // Gets the different Chunks that should be updated
    private void CheckSurroundingChunks(int x, int y, int z, ChunkPos pos){
        if(x == 0)
            cachedList.Add(new ChunkPos(pos.x-1, pos.z));
        if(x == Chunk.chunkWidth-1)
            cachedList.Add(new ChunkPos(pos.x+1, pos.z));
        if(z == 0)
            cachedList.Add(new ChunkPos(pos.x, pos.z-1));
        if(z == Chunk.chunkWidth-1)
            cachedList.Add(new ChunkPos(pos.x, pos.z+1));

        cachedList.Add(pos);
    }

    // Passes all elements in a to-be-deleted schedule date to the next tick
    private void PassToNextTick(bool BUD=true){
    	int i=0;
        if(BUD){
            // If current tick has/had BUD
            if(this.data.ContainsKey(this.currentTime)){
                if(!this.data.ContainsKey(this.newTime)){
                    this.data.Add(this.newTime, new List<BUDSignal>());
                }

            	foreach(BUDSignal b in this.data[this.currentTime]){
            		this.data[this.newTime].Insert(i, b);
            		i++;
            	}
            }
        }

        else{
            if(this.toReload.ContainsKey(this.currentTime)){
                if(!this.toReload.ContainsKey(this.newTime)){
                    this.toReload.Add(this.newTime, new List<ChunkPos>());
                }

                foreach(ChunkPos pos in this.toReload[this.currentTime]){
                    this.toReload[this.newTime].Insert(i, pos);
                    i++;
                }
            }
        }
    }

    // Deschedules a BUD request (probably when block is broken or updated)
    public void RemoveBUD(BUDSignal b){
    	foreach(string key in this.data.Keys){
    		this.data[key].RemoveAll(bud => bud.Equals(b));
    	}
    }

    // DEBUG OPERATION
    private void CurrentToFile(string filename){
        string aux = this.currentTime + "\n\n";

        foreach(BUDSignal bud in this.data[this.currentTime]){
            aux += bud.ToString();
            aux += "\n";
        }

        System.IO.File.WriteAllText(filename, aux);
    }
}


public struct BUDSignal{
	public string type;
	public int x;
	public int y;
	public int z;
	public int budX;
	public int budY;
	public int budZ;
	public int facing;

	public BUDSignal(string t, int x, int y, int z, int bX, int bY, int bZ, int facing=-1){
		this.type = t;
		this.x = x;
		this.y = y;
		this.z = z;
		this.budX = bX;
		this.budY = bY;
		this.budZ = bZ;
		this.facing = facing;
	}

    public override string ToString(){
        return "(" + x + ", " + y + ", " + z + ")";
    }

	public bool Equals(BUDSignal b){
		if(this.x == b.x && this.y == b.y && this.z == b.z)
			return true;
		return false;
	}
}
