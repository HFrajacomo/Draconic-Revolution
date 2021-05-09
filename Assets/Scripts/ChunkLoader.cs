using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class ChunkLoader : MonoBehaviour
{
	// Basic ChunkLoader Data
	public int renderDistance = 0;
	public Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>();
	public Transform player;
	public ChunkPos currentChunk;
	public ChunkPos newChunk;
	public List<ChunkPos> toLoad = new List<ChunkPos>();
	public List<ChunkPos> toUnload = new List<ChunkPos>();
    public List<ChunkPos> toDraw = new List<ChunkPos>();
    public List<ChunkPos> toRedraw = new List<ChunkPos>();
	public BlockEncyclopedia blockBook;
    public VFXLoader vfx;
    public TimeOfDay time;
    public GameObject gameUI;
    public StructureHandler structHandler;
    public Client client;
    public BiomeHandler biomeHandler = new BiomeHandler(0);

    // Receoved from Server
    public int playerX;
    public int playerZ;
    public int playerY;

    // Initialization
    public GameObject playerCharacter;

	// Chunk Rendering
	public ChunkRenderer rend;

	// Flags
	public bool WORLD_GENERATED = false; 
    public int reloadMemoryCounter = 30;
    public bool PLAYERSPAWNED = false;
    public bool PLAYERLOADED = false;
    public bool REQUESTEDCHUNKS = false;
    public bool DRAWFLAG = false;
    public bool CONNECTEDTOSERVER = false;
    public bool SENTINFOTOSERVER = false;

    // Cache Data
    private ChunkPos cachePos = new ChunkPos(0,0);
    private Chunk cacheChunk;


    void Awake(){
        this.playerCharacter.SetActive(false);
        this.gameUI.SetActive(false);
        this.client = new Client(this);
        HandleClientCommunication();
        this.player.position = new Vector3(0,0,0);
    }

    void OnApplicationQuit(){
        NetMessage message = new NetMessage(NetCode.DISCONNECT);
        this.client.Send(message.GetMessage(), message.size);
    }

    void Update(){
        // If hasn't connected to the server yet
        if(this.CONNECTEDTOSERVER && !this.SENTINFOTOSERVER){
            NetMessage playerInformation = new NetMessage(NetCode.SENDCLIENTINFO);
            playerInformation.SendClientInfo(World.renderDistance, World.worldSeed, World.worldName);
            this.renderDistance = World.renderDistance;
            this.client.Send(playerInformation.GetMessage(), playerInformation.size);
            this.SENTINFOTOSERVER = true;
        }
        else if(!this.CONNECTEDTOSERVER){
            HandleClientCommunication();
            return;
        }

        // If client hasn't received player data from server yet 
        if(!this.PLAYERSPAWNED){
            HandleClientCommunication();
        }
        // If has received chunks and needs to load them
        else if(this.PLAYERSPAWNED && !this.REQUESTEDCHUNKS){
            this.player.position = new Vector3(playerX, playerY, playerZ);
            newChunk = new ChunkPos(playerX/Chunk.chunkWidth, playerZ/Chunk.chunkWidth);
            GetChunks(true);  
            this.REQUESTEDCHUNKS = true;
            HandleClientCommunication();
        }

        else{
            // If current chunk is drawn and world is generated
        	if(CheckChunkDrawn(this.playerX, this.playerZ) && !WORLD_GENERATED){
                HandleClientCommunication();
        		WORLD_GENERATED = true;

                if(!this.PLAYERLOADED){
                    int spawnY = GetBlockHeight(new ChunkPos(Mathf.FloorToInt(player.position.x / Chunk.chunkWidth), Mathf.FloorToInt(player.position.z / Chunk.chunkWidth)), (int)(player.position.x%Chunk.chunkWidth), (int)(player.position.z%Chunk.chunkWidth));
                    player.position -= new Vector3(0, player.position.y - spawnY, 0);
                    this.PLAYERLOADED = true;
                }

                this.time.SetLock(false);
                this.gameUI.SetActive(true);
                playerCharacter.SetActive(true);
        	}

            // DEV TOOLS
            if(MainControllerManager.reload){
                GetChunks(true);
                MainControllerManager.reload = false;
            }
            else{
        	   GetChunks(false);
            }

            HandleClientCommunication();
        	UnloadChunk();

            // Decides whether DRAW Flag should be activates or deactivated
            if(toDraw.Count > this.renderDistance*2+1){
                this.DRAWFLAG = true;
            }
            else if(toDraw.Count == 0){
                this.DRAWFLAG = false;
            }

            // Decides what to do for current tick
            if(toLoad.Count > 0)
                LoadChunk();

            DrawChunk();
        }
    }
    
    // Handles communication received from Server
    private void HandleClientCommunication(){
        int queueCount = this.client.queue.Count;

        if(queueCount > 0){
            for(int i=0; i<queueCount; i++){
                this.client.HandleReceivedMessage(this.client.queue[0].GetData());
                this.client.queue.RemoveAt(0);
            }
        }
    }

    // Erases loaded chunks dictionary
    private void ClearAllChunks(){
    	foreach(ChunkPos item in chunks.Keys){
    		Destroy(chunks[item].obj);
            vfx.RemoveChunk(item);
            Resources.UnloadUnusedAssets();
    	}
        chunks.Clear();
    }

    // Check if the chunkpos in a given (x,z) position is loaded and drawn
    private bool CheckChunkDrawn(float x, float z){
        ChunkPos pos = new ChunkPos(Mathf.FloorToInt(x/Chunk.chunkWidth), Mathf.FloorToInt(z/Chunk.chunkWidth));
    
        if(this.chunks.ContainsKey(pos)){
            return this.chunks[pos].drawMain;
        }
        return false;
    }

    // Adds chunk to Draw List with priority
    public void AddToDraw(ChunkPos pos){
        if(!toDraw.Contains(pos)){
            toDraw.Insert(0, pos);
        }
        else{
            toDraw.Remove(pos);
            toDraw.Insert(0, pos);
        }
    }


    // Loads Chunk data, but doesn't draw them
    private void LoadChunk(){
    	if(toLoad.Count > 0){
    		// Prevention
    		if(toUnload.Contains(toLoad[0])){
    			toUnload.Remove(toLoad[0]);
    			toLoad.RemoveAt(0);
    			return;
    		}

            if(chunks.ContainsKey(toLoad[0])){
                toLoad.RemoveAt(0);
                return;
            }

           // Asks server to hand over chunk info
            NetMessage message = new NetMessage(NetCode.REQUESTCHUNKLOAD);
            message.RequestChunkLoad(toLoad[0]);
            this.client.Send(message.GetMessage(), message.size);

            toDraw.Add(toLoad[0]);
    		toLoad.RemoveAt(0);
    	}
    }

    // Unloads a chunk per frame from the Unloading Buffer
    private void UnloadChunk(){

        if(toUnload.Count > 0){

            // Runs Unity Garbage Collector
            if(this.reloadMemoryCounter <= 0){
                this.reloadMemoryCounter = this.renderDistance-1;
                Resources.UnloadUnusedAssets();
            }
            else{
                this.reloadMemoryCounter--;
            }


            if(toRedraw.Contains(toUnload[0])){
                toRedraw.Remove(toUnload[0]);
            }

            // Prevention
            if(toLoad.Contains(toUnload[0])){
                toLoad.Remove(toUnload[0]);
                toUnload.RemoveAt(0);
                return;
            }

            if(!chunks.ContainsKey(toUnload[0])){
                toUnload.RemoveAt(0);
                return;
            }
            
            
            Chunk popChunk = chunks[toUnload[0]];
            chunks.Remove(popChunk.pos);          
            Destroy(popChunk.obj);
            vfx.RemoveChunk(popChunk.pos);

            toUnload.RemoveAt(0);
        }
    }


    // Actually builds the mesh for loaded chunks
    private void DrawChunk(){
        if(toDraw.Count > 0){
            // If chunk is still loaded
            if(chunks.ContainsKey(toDraw[0])){
                chunks[toDraw[0]].BuildChunk(load:true);
                // If hasn't been drawn entirely, put on Redraw List
                if(!chunks[toDraw[0]].BuildSideBorder(reload:true)){
                    toRedraw.Add(toDraw[0]);
                }
            }
            toDraw.RemoveAt(0);
        }

        for(int i=0; i < 2; i++){
            if(toRedraw.Count > 0){
                if(toDraw.Contains(toRedraw[0])){
                    toRedraw.Add(toRedraw[0]);
                    toRedraw.RemoveAt(0);
                    continue;
                }

                if(chunks.ContainsKey(toRedraw[0])){
                    if(chunks[toRedraw[0]].drawMain){
                        // If hasn't been drawn entirely, put on Redraw again
                        if(!chunks[toRedraw[0]].BuildSideBorder()){
                            toRedraw.Add(toRedraw[0]);
                        }
                    }
                    else{
                        toRedraw.Add(toRedraw[0]);
                    }
                }
                toRedraw.RemoveAt(0);
            }
        }

    }


    // Gets all chunks around player's render distance
    // GetChunks automatically rebuilds chunks if reload=True
    private void GetChunks(bool reload){
		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);

    	// Reload all Chunks nearby
    	if(reload){
    		ClearAllChunks();
    		toLoad.Clear();
    		toUnload.Clear();
            toRedraw.Clear();
    		
	        for(int x=-renderDistance; x<=renderDistance;x++){
	        	for(int z=-renderDistance; z<=renderDistance;z++){
	        		toLoad.Add(new ChunkPos(newChunk.x+x, newChunk.z+z));
                    toRedraw.Add(new ChunkPos(newChunk.x+x, newChunk.z+z));
	        	}
	        }
	        
	        currentChunk = newChunk;
	        return;
	    }

    	// If didn't move to another chunk
    	if(currentChunk == newChunk){
    		return;
    	}

    	int diff = (newChunk - currentChunk).dir();


    	if(diff == 0){ // East
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x-renderDistance-1, newChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+renderDistance-1, newChunk.z+i);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    	}
    	else if(diff == 2){ // West
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+renderDistance+1, newChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x-renderDistance+1, newChunk.z+i);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    	}
    	else if(diff == 1){ // South
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance+1);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance+1);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
      		}
    	}
    	else if(diff == 3){ // North
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance-1);
    			toUnload.Add(popChunk);
      			ChunkPos addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance-1);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
       		}	
    	}
    	else if(diff == 5){ // Southeast
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+renderDistance-1, newChunk.z+i);
       			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    		for(int i=-renderDistance+1; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i-1, newChunk.z-renderDistance);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+i-1, newChunk.z-renderDistance+1);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    	}
    	else if(diff == 6){ // Southwest
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x-renderDistance+1, newChunk.z+i);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z-renderDistance);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+i+1, newChunk.z-renderDistance+1);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    	}
    	else if(diff == 7){ // Northwest
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x-renderDistance+1, newChunk.z+i);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z-renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z+renderDistance);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+i+1, newChunk.z+renderDistance-1);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    	}
    	else if(diff == 4){ // Northeast
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+renderDistance-1, newChunk.z+i);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i+1, currentChunk.z-renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance);
                ChunkPos refreshChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance-1);
    			toLoad.Add(addChunk);
                toRedraw.Add(refreshChunk);
    		}
    	}

	    currentChunk = newChunk;
    }

    // Calculates the biomeSeed of BiomeHandler
    private float BiomeSeedFunction(int t){
        return 0.04f*(0.03f*Mathf.Sin(t));
    }

    // Calculates general offset hash
    private float OffsetHashFunction(int t){
        return (t*0.71928590287457694671f)%1;
    }

    // Calculates the generationSeed used in World Generation
    private float GenerationSeedFunction(int t){
        return Perlin.Noise(t/1000000f)+0.5f;
    }

    // Returns block code of a castcoord
    public ushort GetBlock(CastCoord c){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);
        }
        else{
            return (ushort)(ushort.MaxValue/2); // Error Code
        }
    }

    // Returns block code of a castcoord
    public ushort GetState(CastCoord c){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].metadata.GetState(c.blockX, c.blockY, c.blockZ);
        }
        else{
            return (ushort)(ushort.MaxValue/2); // Error Code
        }
    }

    // Returns the heightmap value of a generated chunk in block position
    private int GetBlockHeight(ChunkPos pos, int blockX, int blockZ){
        for(int i=Chunk.chunkDepth-1; i >= 0 ; i--){
            if(chunks[pos].data.GetCell(Mathf.Abs(blockX), i, Mathf.Abs(blockZ)) != 0){
                return i+2;
            }
        }

        if(blockX < 15)
            return GetBlockHeight(pos, blockX+1, blockZ);
        if(blockZ < 15)
            return GetBlockHeight(pos, blockX, blockZ+1);

        return GetBlockHeight(pos, 0, 0);
    }
}


public struct Coord3D{
	public int x;
	public int y;
	public int z;

	public Coord3D(int a, int b, int c){
		this.x = a;
		this.y = b;
		this.z = c;
	}

	public Coord3D(Coord3D coord, int x=0, int y=0, int z=0){
		this.x = coord.x + x;
		this.y = coord.y + y;
		this.z = coord.z + z;
	}

	public float Sum(){
		return this.x + this.y + this.z;
	}

	public override string ToString(){
		return "(" + this.x + ", " + this.y + ", " + this.z + ")";
	}

}


public struct ChunkPos{
	public int x;
	public int z;

	public ChunkPos(int a, int b){
		this.x = a;
		this.z = b;
	}

	public override string ToString(){
		return "(" + this.x + ", " + this.z + ")";
	}

	public static bool operator==(ChunkPos a, ChunkPos b){
		if(a.x == b.x && a.z == b.z)
			return true;
		return false;
	}

	public static bool operator!=(ChunkPos a, ChunkPos b){
		if(a.x == b.x && a.z == b.z)
			return false;
		return true;
	}

	public override int GetHashCode(){
		return this.x ^ this.z;
	}

	public override bool Equals(System.Object a){
		if(a == null)
			return false;

		ChunkPos item = (ChunkPos)a;
		return this == item;
	}

	/*
	Returns the direction the player must have moved to find a new chunk
	Used after ChunkPos - ChunkPos
	0 = East
	1 = South
	2 = West
	3 = North

	4 = Northeast
	5 = Southeast
	6 = Southwest
	7 = Northwest
	*/
	public int dir(){
		if(this.x == 1 && this.z == 1){
			return 4;
		}
		if(this.x == 1 && this.z == -1){
			return 5;
		}
		if(this.x == -1 && this.z == -1){
			return 6;
		}
		if(this.x == -1 && this.z == 1){
			return 7;
		}
		if(this.x == 1){
			return 0;
		}
		if(this.x == -1){
			return 2;
		}
		if(this.z == 1){
			return 3;
		}
		if(this.z == -1){
			return 1;
		}
		return -1;
	}


	public static ChunkPos operator-(ChunkPos a, ChunkPos b){
		return new ChunkPos(a.x - b.x, a.z - b.z);
	}
}

