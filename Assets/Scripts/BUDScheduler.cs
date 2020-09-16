using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BUDScheduler : MonoBehaviour
{
	public TimeOfDay schedulerTime;
	private Dictionary<string, List<BUDSignal>> data = new Dictionary<string, List<BUDSignal>>();
	private Dictionary<string, List<ChunkPos>> toReload = new Dictionary<string, List<ChunkPos>>();
	private string currentTime;
	private string newTime;
	public ChunkLoader loader;
	public int BUDperFrame;
	private int currentBUDonFrame;

    private ChunkPos cachePos;
	private CastCoord cachedCoord;

	void Start(){
		this.currentTime = schedulerTime.GetBUDTime();
		this.BUDperFrame = 1200;
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
    		// Creates new list for newTime if doesn't exist
    		if(!this.data.ContainsKey(this.newTime)){
    			this.data.Add(this.newTime, new List<BUDSignal>());
    		}

    		// Creates new value of reload to newTime if doesn't exist
    		if(!this.toReload.ContainsKey(this.newTime)){
    			this.toReload.Add(this.newTime, new List<ChunkPos>());
    		}


    		this.currentBUDonFrame = 0;

    		// Pops all elements of this tick if there is still any
    		if(this.data[this.currentTime].Count > 0){
                // DEBUG LIMITER TO NOT LET GAME EXPLODE
                print("BUDScheduler stackoverflow");
                Debug.Break();
    			PassToNextTick();
    		}

    		// Frees memory of previous BUD Tick
            this.data[this.currentTime] = null;
            this.toReload[this.currentTime] = null;
    		this.data.Remove(this.currentTime);
    		this.toReload.Remove(this.currentTime);
    		this.currentTime = this.newTime;

    		this.BUDperFrame = (int)(this.data[this.newTime].Count/30)+1;

    		// Unclogs system if no BUD request incoming
    		if(this.BUDperFrame < 1200){
    			this.BUDperFrame = 1200;
    		}
    	}

        // DEBUG
        if(this.data[this.currentTime].Count > 0){
            //CurrentToFile("test.txt");
            //Debug.Break();
        }

    	// Iterates through frame's list and triggers BUD
    	for(currentBUDonFrame=0;currentBUDonFrame<BUDperFrame;currentBUDonFrame++){
	    	if(this.data[this.currentTime].Count > 0){
                cachedCoord = new CastCoord(new Vector3(this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z));
	    		loader.blockBook.Get(loader.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ)).OnBlockUpdate(this.data[this.currentTime][0].type, this.data[this.currentTime][0].x, this.data[this.currentTime][0].y, this.data[this.currentTime][0].z, this.data[this.currentTime][0].budX, this.data[this.currentTime][0].budY, this.data[this.currentTime][0].budZ, this.data[this.currentTime][0].facing, loader);
	    	    this.data[this.currentTime].RemoveAt(0);
            }
	    	else{
	    		break;
	    	}
    	}

    	// Chunk Reloader
    	if(this.data[this.currentTime].Count == 0 && this.toReload.ContainsKey(this.currentTime)){
    		if(this.toReload[this.currentTime].Count > 0){
                cachePos = this.toReload[this.currentTime][0];
                this.toReload[this.currentTime].RemoveAt(0);

                loader.chunks[cachePos].BuildChunk();  
                loader.chunks[cachePos].BuildSideBorder(reload:true); 
            }    		
    	}



    }


    // Schedules a BUD request in the system
    public void ScheduleBUD(BUDSignal b, int tickOffset){
    	if(tickOffset == 0){
            if(!this.data[this.currentTime].Contains(b))
    		  this.data[this.currentTime].Add(b);
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

    // Schedules a Chunk.Build() operation 
    public void ScheduleReload(ChunkPos pos, int tickOffset){
    	if(tickOffset == 0){
            if(!this.toReload[this.currentTime].Contains(pos))
    		  this.toReload[this.currentTime].Add(pos);
    	}
    	else{
    		string fakeTime = schedulerTime.FakeSum(tickOffset);

    		if(this.toReload.ContainsKey(fakeTime)){
    			this.toReload[fakeTime].Add(pos);
    		}
    		else{
    			this.toReload.Add(fakeTime, new List<ChunkPos>());
    			this.toReload[fakeTime].Add(pos);
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
