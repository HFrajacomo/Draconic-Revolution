using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BUDScheduler : MonoBehaviour
{
	public TimeOfDay schedulerTime;
	private Dictionary<string, List<BUDSignal>> data = new Dictionary<string, List<BUDSignal>>();
	private Dictionary<string, bool> reload = new Dictionary<string, bool>();
	private Dictionary<string, ChunkPos> toReload = new Dictionary<string, ChunkPos>();
	private string currentTime;
	private string newTime;
	public ChunkLoader loader;
	public int BUDperFrame;
	private int currentBUDonFrame;

	private CastCoord cachedCoord;
	private BUDSignal cachedBUD;

	void Start(){
		this.currentTime = schedulerTime.GetBUDTime();
		this.BUDperFrame = 1000;
		this.data.Add(currentTime, new List<BUDSignal>());
		this.reload.Add(currentTime, false);
	}

    // Update is called once per frame
    void Update()
    {
    	// Gets this Unity Tick time
    	this.newTime = schedulerTime.GetBUDTime(); 

    	// Checks if BUD Tick has changed
    	if(this.newTime != this.currentTime){
    		// Creates new list for newTime if doesn't exist
    		if(!this.data.ContainsKey(this.newTime)){
    			this.data.Add(this.newTime, new List<BUDSignal>());
    		}

    		// Creates new value of reload to newTime if doesn't exist
    		if(!this.reload.ContainsKey(this.newTime)){
    			this.reload.Add(this.newTime, false);
    			this.toReload.Add(this.newTime, new ChunkPos(0,0));
    		}


    		this.currentBUDonFrame = 0;

    		// Pops all elements of this tick if there is still any
    		if(this.data[this.currentTime].Count > 0){
    			PassToNextTick();
    		}

    		// Frees memory of previous BUD Tick
    		this.data.Remove(this.currentTime);
    		this.reload.Remove(this.currentTime);
    		this.toReload.Remove(this.currentTime);
    		this.currentTime = this.newTime;

    		this.BUDperFrame = (int)this.data[this.newTime].Count/30;

    		// Unclogs system if no BUD request incoming
    		if(this.BUDperFrame < 1000){
    			this.BUDperFrame = 1000;
    		}
    	}

    	// Iterates through frame's list and triggers BUD
    	for(currentBUDonFrame=0;currentBUDonFrame<BUDperFrame;currentBUDonFrame++){
	    	if(this.data[this.currentTime].Count > 0){
	    		cachedBUD = this.data[this.currentTime][0];
	    		this.data[this.currentTime].RemoveAt(0);
	    		cachedCoord = new CastCoord(new Vector3(cachedBUD.x, cachedBUD.y, cachedBUD.z));
	    		loader.blockBook.Get(loader.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ)).OnBlockUpdate(cachedBUD.type, cachedBUD.x, cachedBUD.y, cachedBUD.z, cachedBUD.budX, cachedBUD.budY, cachedBUD.budZ, cachedBUD.facing, loader);
	    	}
	    	else{
	    		break;
	    	}
    	}

    	// Chunk Reloader
    	if(this.data[this.currentTime].Count == 0 && this.reload[this.currentTime]){
    		this.reload[this.currentTime] = false;
        	loader.chunks[toReload[this.currentTime]].BuildChunk();  
        	loader.chunks[toReload[this.currentTime]].BuildSideBorder(reload:true);     		
    	}



    }


    // Schedules a BUD request in the system
    public void ScheduleBUD(BUDSignal b, int tickOffset){
    	if(tickOffset == 0){
    		this.data[this.currentTime].Add(b);
    	}
    	else{
    		string fakeTime = schedulerTime.FakeSum(tickOffset);

    		if(this.data.ContainsKey(fakeTime)){
    			this.data[fakeTime].Add(b);
    		}
    		else{
    			this.data.Add(fakeTime, new List<BUDSignal>());
    			this.data[fakeTime].Add(b);
    		}
    	}
    }

    // Schedules a Chunk.Build() operation 
    public void ScheduleReload(ChunkPos pos, int tickOffset){
    	if(tickOffset == 0){
    		this.reload[this.currentTime] = true;
    		this.toReload[this.currentTime] = pos;
    	}
    	else{
    		string fakeTime = schedulerTime.FakeSum(tickOffset);

    		if(this.reload.ContainsKey(fakeTime)){
    			this.reload[fakeTime] = true;
    			this.toReload[fakeTime] = pos;
    		}
    		else{
    			this.reload.Add(fakeTime, true);
    			this.toReload.Add(fakeTime, pos);
    		}
    	}

    }

    // Passes all elements in a to-be-deleted schedule date to the next tick
    private void PassToNextTick(){
    	int i=0;
    	foreach(BUDSignal b in this.data[this.currentTime]){
    		this.data[this.newTime].Insert(i, b);
    		i++;
    	}
    }

    // Deschedules a BUD request (probably when block is broken or updated)
    public void RemoveBUD(BUDSignal b){
    	foreach(string key in this.data.Keys){
    		this.data[key].RemoveAll(bud => bud.Equals(b));
    	}
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

	public bool Equals(BUDSignal b){
		if(this.x == b.x && this.y == b.y && this.z == b.z)
			return true;
		return false;
	}
}
