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
    public BUDScheduler budscheduler;
    public VFXLoader vfx;
    public TimeOfDay time;
    public GameObject gameUI;
    public WorldGenerator worldGen;
    public StructureHandler structHandler;

    // Initialization
    public GameObject playerCharacter;

	// World Generation
	public int worldSeed = 1; // 6 number integer
    public BiomeHandler biomeHandler;

	// Chunk Rendering
	public ChunkRenderer rend;
    public RegionFileHandler regionHandler;

	// Flags
	public bool WORLD_GENERATED = false; 
    public int reloadMemoryCounter = 30;
    public bool PLAYERSPAWNED = false;
    public bool DRAWFLAG = false;

    // Cache Data
    private ChunkPos cachePos = new ChunkPos(0,0);
    private Chunk cacheChunk;


    void OnApplicationQuit(){
        regionHandler.CloseAll();
    }

    void Awake(){
        this.playerCharacter.SetActive(false);
        this.gameUI.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        this.renderDistance = World.renderDistance;

        regionHandler = new RegionFileHandler(renderDistance, newChunk);

        worldSeed = regionHandler.GetRealSeed();

        biomeHandler = new BiomeHandler(BiomeSeedFunction(worldSeed));

        this.worldGen = new WorldGenerator(worldSeed, BiomeSeedFunction(worldSeed), OffsetHashFunction(worldSeed), GenerationSeedFunction(worldSeed), biomeHandler, structHandler, this);

        // If character has been loaded
        if(regionHandler.playerFile.Length > 0){
            player.position = regionHandler.LoadPlayer();
            PLAYERSPAWNED = true;
        }

		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);


		GetChunks(true);
    }

    void Update(){ 

    	if(toLoad.Count == 0 && toDraw.Count == 0 && !WORLD_GENERATED){
    		WORLD_GENERATED = true;

            if(!PLAYERSPAWNED){
                int spawnY = GetBlockHeight(new ChunkPos(Mathf.FloorToInt(player.position.x / Chunk.chunkWidth), Mathf.FloorToInt(player.position.z / Chunk.chunkWidth)), (int)(player.position.x%Chunk.chunkWidth), (int)(player.position.z%Chunk.chunkWidth));
                player.position -= new Vector3(0, player.position.y - spawnY, 0);
                PLAYERSPAWNED = true;
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

    	UnloadChunk();

        // Decides whether DRAW Flag should be activates or deactivated
        if(toDraw.Count > this.renderDistance*2+1){
            this.DRAWFLAG = true;
        }
        else if(toDraw.Count == 0){
            this.DRAWFLAG = false;
        }

        // Decides what to do for current tick
        if(toLoad.Count > 0 && !this.DRAWFLAG)
            LoadChunk();
        else if(Structure.reloadChunks.Count > 0)
            SavePregenChunk();
        else
            DrawChunk();
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

    // Builds Structure data in non-indexed Chunks
    private void SavePregenChunk(){
        cacheChunk = Structure.reloadChunks[0];
        regionHandler.GetCorrectRegion(cacheChunk.pos);

        // If it's loaded
        if(chunks.ContainsKey(cacheChunk.pos)){
            cacheChunk.needsGeneration = 0;

            // Rough Application of Structures
            Structure.RoughApply(chunks[cacheChunk.pos], cacheChunk);
            chunks[cacheChunk.pos] = cacheChunk;

            this.regionHandler.SaveChunk(cacheChunk);

            if(!toDraw.Contains(cacheChunk.pos))
                toDraw.Add(cacheChunk.pos);
        }

        // If is in an unloaded indexed chunk
        else if(this.regionHandler.IsIndexed(cacheChunk.pos)){
            Chunk c = new Chunk(cacheChunk.pos, this.rend, this.blockBook, this, fromMemory:true);
            this.regionHandler.LoadChunk(c);

            // Rough Application of Structures
            Structure.RoughApply(c, cacheChunk);

            this.regionHandler.SaveChunk(c);
        }

        // If is in an unloaded unknown chunk
        else{
            this.regionHandler.SaveChunk(cacheChunk);
        }


        Structure.reloadChunks.RemoveAt(0);
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

            bool isPregen;

            // Gets correct region file
            regionHandler.GetCorrectRegion(toLoad[0]);

            // If current chunk toLoad was already generated
            if(regionHandler.IsIndexed(toLoad[0])){

                isPregen = regionHandler.GetsNeedGeneration(toLoad[0]);

                // If chunk is Pre-Generated
                if(isPregen){
                    chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this, fromMemory:true));
                    vfx.NewChunk(toLoad[0]);
                    regionHandler.LoadChunk(chunks[toLoad[0]]);
                    this.worldGen.SetVoxdata(chunks[toLoad[0]].data.GetData());
                    this.worldGen.SetCacheHP(chunks[toLoad[0]].metadata.GetHPData());
                    this.worldGen.SetCacheState(chunks[toLoad[0]].metadata.GetStateData());
                    chunks[toLoad[0]].BuildOnVoxelData(this.worldGen.AssignBiome(toLoad[0], pregen:true));
                    chunks[toLoad[0]].metadata = new VoxelMetadata(this.worldGen.GetCacheHP(), this.worldGen.GetCacheState());
                    chunks[toLoad[0]].needsGeneration = 0;
                    regionHandler.SaveChunk(chunks[toLoad[0]]);
                }
                // If it's just a normally generated chunk
                else{
                    chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this, fromMemory:true));
                    vfx.NewChunk(toLoad[0]);
                    regionHandler.LoadChunk(chunks[toLoad[0]]);
                    chunks[toLoad[0]].needsGeneration = 0;
                }
            }
            // If it's a new chunk to be generated
            else{
                chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this));
                vfx.NewChunk(toLoad[0]);
                chunks[toLoad[0]].BuildOnVoxelData(this.worldGen.AssignBiome(toLoad[0]));
                chunks[toLoad[0]].metadata = new VoxelMetadata(this.worldGen.GetCacheHP(), this.worldGen.GetCacheState());
                chunks[toLoad[0]].needsGeneration = 0;
                regionHandler.SaveChunk(chunks[toLoad[0]]);
            }

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

