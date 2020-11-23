using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
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

    // Prefab System
    public StructureHandler structHandler;

    // Initialization
    public GameObject playerCharacter;

	// World Generation
	public int worldSeed = 1; // 6 number integer
    public float offsetHash;
    public float generationSeed;
    public BiomeHandler biomeHandler;

	// Chunk Rendering
	public ChunkRenderer rend;
    public RegionFileHandler regionHandler;

	// Flags
	public bool WORLD_GENERATED = false; 

	// Cache Information 
	private ushort[] cacheHeightMap = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private ushort[] cacheHeightMap2 = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private ushort[] cacheHeightMap3 = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private ushort[] cacheHeightMap4 = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
    private ushort[] cachePivotMap = new ushort[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)];
	private	ushort[] cacheVoxdata = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
	private List<ushort[]> cacheMaps = new List<ushort[]>();
	private List<ushort> cacheBlockCodes = new List<ushort>();
    private ChunkPos cachePos = new ChunkPos(0,0);
    private Chunk cacheChunk;
    private ushort[] cacheMetadataHP = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private ushort[] cacheMetadataState = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
    private Dictionary<ushort, ushort> cacheStateDict = new Dictionary<ushort, ushort>();

    void Awake(){
        this.playerCharacter.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        this.renderDistance = World.renderDistance;

        regionHandler = new RegionFileHandler(renderDistance, newChunk);

        worldSeed = regionHandler.GetRealSeed();

        biomeHandler = new BiomeHandler(BiomeSeedFunction(worldSeed));

        generationSeed = GenerationSeedFunction(worldSeed);
        offsetHash = OffsetHashFunction(worldSeed);

		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);


		GetChunks(true);
    }

    void Update(){

    	if(toLoad.Count == 0 && toDraw.Count == 0 && !WORLD_GENERATED){
    		WORLD_GENERATED = true;

            int spawnY = GetBlockHeight(new ChunkPos(Mathf.FloorToInt(player.position.x / Chunk.chunkWidth), Mathf.FloorToInt(player.position.z / Chunk.chunkWidth)), (int)(player.position.x%Chunk.chunkWidth), (int)(player.position.z%Chunk.chunkWidth));
            player.position -= new Vector3(0, player.position.y - spawnY, 0);

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

        if(toLoad.Count > 0)
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
            chunks[item].assetGrid.Unload();
            vfx.RemoveChunk(item);
    	}
        chunks.Clear();
    }

    // Builds Structure data in non-indexed Chunks
    private void SavePregenChunk(){
        cacheChunk = Structure.reloadChunks[0];

        // If it's loaded
        if(chunks.ContainsKey(cacheChunk.pos)){

            cacheChunk.needsGeneration = 0;

            // Rough Application of Structures
            Structure.RoughApply(chunks[cacheChunk.pos], cacheChunk);

            this.regionHandler.SaveChunk(cacheChunk);

            if(!toDraw.Contains(cacheChunk.pos))
                toDraw.Add(cacheChunk.pos);
        }

        // If is in an unloaded indexed chunk
        else if(this.regionHandler.GetFile().IsIndexed(cacheChunk.pos)){
        
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

            bool isPregen, isStructured;

            isStructured = Structure.Exists(toLoad[0]);

            // Gets correct region file
            regionHandler.GetCorrectRegion(toLoad[0]);

            // If current chunk toLoad was already generated
            if(regionHandler.GetFile().IsIndexed(toLoad[0])){

                isPregen = regionHandler.GetsNeedGeneration(toLoad[0]);

                if(isStructured && isPregen){
                    chunks.Add(toLoad[0], Structure.GetChunk(toLoad[0]).Clone());
                    chunks[toLoad[0]].needsGeneration = 1;
                    vfx.NewChunk(toLoad[0]); 
                    cacheVoxdata = chunks[toLoad[0]].data.GetData();
                    chunks[toLoad[0]].BuildOnVoxelData(AssignBiome(toLoad[0], worldSeed, pregen:true));
                    chunks[toLoad[0]].metadata = new VoxelMetadata(cacheMetadataHP, cacheMetadataState);
                    chunks[toLoad[0]].needsGeneration = 0;
                    regionHandler.SaveChunk(chunks[toLoad[0]]);                    
                    Structure.RemoveChunk(toLoad[0]);
                }

                // If it's a Structure Update
                else if(isStructured){
                    chunks.Add(toLoad[0], Structure.GetChunk(toLoad[0]).Clone());
                    vfx.NewChunk(toLoad[0]); 
                    regionHandler.SaveChunk(chunks[toLoad[0]]);
                    Structure.RemoveChunk(toLoad[0]);

                }

                // If chunk is Pre-Generated
                else if(isPregen){

                    chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this, fromMemory:true));
                    vfx.NewChunk(toLoad[0]);
                    regionHandler.LoadChunk(chunks[toLoad[0]]);
                    cacheVoxdata = chunks[toLoad[0]].data.GetData();
                    cacheMetadataHP = chunks[toLoad[0]].metadata.GetHPData();
                    cacheMetadataState = chunks[toLoad[0]].metadata.GetStateData();
                    chunks[toLoad[0]].BuildOnVoxelData(AssignBiome(toLoad[0], worldSeed, pregen:true));
                    chunks[toLoad[0]].metadata = new VoxelMetadata(cacheMetadataHP, cacheMetadataState);
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
                if(isStructured){
                    chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this, fromMemory:true));
                    vfx.NewChunk(toLoad[0]);
                    cacheVoxdata = Structure.GetChunk(toLoad[0]).data.GetData();
                    cacheMetadataHP = Structure.GetChunk(toLoad[0]).metadata.GetHPData();
                    cacheMetadataState = Structure.GetChunk(toLoad[0]).metadata.GetStateData();
                    chunks[toLoad[0]].BuildOnVoxelData(AssignBiome(toLoad[0], worldSeed, pregen:true));
                    chunks[toLoad[0]].metadata = new VoxelMetadata(cacheMetadataHP, cacheMetadataState);
                    chunks[toLoad[0]].needsGeneration = 0;
                    regionHandler.SaveChunk(chunks[toLoad[0]]);
                    Structure.RemoveChunk(toLoad[0]);

                }
                else{
                    chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this));
                    vfx.NewChunk(toLoad[0]);
                    chunks[toLoad[0]].BuildOnVoxelData(AssignBiome(toLoad[0], worldSeed)); 
                    chunks[toLoad[0]].metadata = new VoxelMetadata(cacheMetadataHP, cacheMetadataState);
                    chunks[toLoad[0]].needsGeneration = 0;
                    regionHandler.SaveChunk(chunks[toLoad[0]]);

                }                 



            }

            toDraw.Add(toLoad[0]);
    		toLoad.RemoveAt(0);	
    	}
    }

    // Unloads a chunk per frame from the Unloading Buffer
    private void UnloadChunk(){

        if(toUnload.Count > 0){

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
            popChunk.Unload();
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
                chunks[toDraw[0]].BuildSideBorder(reload:true);
            }
            toDraw.RemoveAt(0);
        }

        if(toRedraw.Count > 0){
            if(toDraw.Contains(toRedraw[0])){
                toRedraw.Add(toRedraw[0]);
                toRedraw.RemoveAt(0);
                return;
            }

            if(chunks.ContainsKey(toRedraw[0])){
                if(chunks[toRedraw[0]].drawMain){
                    chunks[toRedraw[0]].BuildSideBorder();
                }
                else{
                    toRedraw.Add(toRedraw[0]);
                }
                toRedraw.RemoveAt(0);
            }
        }

    }

    // Returns the biome that should be assigned to a given chunk
    private VoxelData AssignBiome(ChunkPos pos, float seed, bool pregen=false){
        string biome = biomeHandler.Assign(pos, seed);

        structHandler.LoadBiome(BiomeHandler.BiomeToByte(biome));

        chunks[pos].biomeName = biome;
        chunks[pos].features = biomeHandler.GetFeatures(pos, seed);


        if(biome == "Plains")
            return GeneratePlainsBiome(pos.x, pos.z, pregen:pregen);
        else if(biome == "Grassy Highlands")
            return GenerateGrassyHighLandsBiome(pos.x, pos.z, pregen:pregen);
        else if(biome == "Ocean")
            return GenerateOceanBiome(pos.x, pos.z, pregen:pregen);
        else if(biome == "Forest")
            return GenerateForestBiome(pos.x, pos.z, pregen:pregen);
        else 
            return GeneratePlainsBiome(pos.x, pos.z, pregen:pregen);
        
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
                    toRedraw.Add(new ChunkPos(newChunk.x+x, newChunk.z+z)); ///
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


    // Returns the heightmap value of a generated chunk in block position
    private int GetBlockHeight(ChunkPos pos, int blockX, int blockZ){
        for(int i=Chunk.chunkDepth-1; i >= 0 ; i--){
            if(chunks[pos].data.GetCell(blockX, i, blockZ) != 0){
                return i+2;
            }
        }

        if(blockX < 15)
            return GetBlockHeight(pos, blockX+1, blockZ);
        if(blockZ < 15)
            return GetBlockHeight(pos, blockX, blockZ+1);

        return GetBlockHeight(pos, 0, 0);
    }

    // Debug Feature
    private void ToFile(ushort[] heightMap, string filename){
    	string a = "";
    	for(int x=Chunk.chunkWidth;x>=0;x--){
    		for(int y=0;y<=Chunk.chunkWidth;y++){
    			a += (heightMap[x*(Chunk.chunkWidth+1)+y] + "\t");
    		}
    		a += "\n";
    	}
    	System.IO.File.WriteAllText(filename, a);
    }

    /*
    Used for Cave system generation and above ground turbulence
    */
    private NativeArray<ushort> GenerateRidgedMultiFractal3D(int chunkX, int chunkZ, float xhash, float yhash, float zhash, float threshold, int ceiling=20, float maskThreshold=0f){

        NativeArray<ushort> turbulanceMap = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);

        RidgedMultiFractalJob rmfJob = new RidgedMultiFractalJob{
            chunkX = chunkX*Chunk.chunkWidth,
            chunkZ = chunkZ*Chunk.chunkWidth,
            xhash = xhash,
            yhash = yhash,
            zhash = zhash,
            threshold = threshold,
            generationSeed = generationSeed,
            ceiling = ceiling,
            maskThreshold = maskThreshold,
            turbulenceMap = turbulanceMap
        };

        JobHandle handle = rmfJob.Schedule(Chunk.chunkWidth, 2);
        handle.Complete();

        return turbulanceMap;
    }

    /*
    Takes heightmaps of different map layers and combines them into a cacheVoxdata
    Layers are applied sequentially, so the bottom-most layer should be the first one
    blockCode is the block related to this layer.
    */
    // Takes cacheMaps and cacheBlockCodes and returns on cacheVoxdata
    private void ApplyHeightMaps(Dictionary<ushort, ushort> stateDict, bool pregen=false){
    	int size = Chunk.chunkWidth;
   		int i=0;

        // Builds the chunk normally
        if(!pregen){
       		for(i=0;i<cacheMaps.Count;i++){
        		// Heightmap Drawing
    	    	for(int x=0;x<size;x++){
    	    		for(int z=0;z<size;z++){
    	    			// If it's the first layer to be added
    	    			if(i == 0){
    		    			for(int y=0;y<Chunk.chunkDepth;y++){
    		    				if(y <= cacheMaps[i][x*(size+1)+z]){
                                    cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i]; // Adds block code
                                    if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                        cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                    }
                                }
    		    				else
    		    					cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
    		    			}
    	    			}
    	    			// If is not the first layer
    	    			else{
    		    			for(int y=cacheMaps[i-1][x*(size+1)+z]+1;y<=cacheMaps[i][x*(size+1)+z];y++){
    		    				cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i];

                                if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                    cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                }
    		    			}  				
    	    			}
    	    		}
    	    	}
    	    }
    	    cacheMaps.Clear();
    	    cacheBlockCodes.Clear();
        }
        // Builds chunk ignoring pregen blocks
        else{
            for(i=0;i<cacheMaps.Count;i++){
                // Heightmap Drawing
                for(int x=0;x<size;x++){
                    for(int z=0;z<size;z++){
                        // If it's the first layer to be added
                        if(i == 0){
                            for(int y=0;y<Chunk.chunkDepth;y++){
                                if(y <= cacheMaps[i][x*(size+1)+z]){
                                    // Only adds to air blocks
                                    if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == 0){
                                        cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i]; // Adds block code
                                        
                                        if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                            cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                        }
                                    }
                                    // Convertion of pregen air blocks
                                    if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == (ushort)(ushort.MaxValue/2))
                                        cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                                }
                                else
                                    if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == (ushort)(ushort.MaxValue/2))
                                        cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                            }
                        }
                        // If is not the first layer
                        else{
                            for(int y=cacheMaps[i-1][x*(Chunk.chunkWidth+1)+z]+1;y<=cacheMaps[i][x*(size+1)+z];y++){
                                if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == 0){
                                    // Convertion of pregen air blocks
                                    if(cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] == (ushort)(ushort.MaxValue/2))
                                        cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = 0;
                                    else
                                        cacheVoxdata[x*size*Chunk.chunkDepth+y*size+z] = cacheBlockCodes[i]; // Adds block code
                                    
                                    if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                        cacheMetadataState[x*size*Chunk.chunkDepth+y*size+z] = stateDict[cacheBlockCodes[i]];
                                    }
                                }
                            }               
                        }
                    }
                }
            }
            cacheMaps.Clear();
            cacheBlockCodes.Clear();
        }

    }

    // Generates Pivot heightmaps
    private void GeneratePivots(ushort[] selectedCache, int chunkX, int chunkZ, float xhash, float zhash, ref bool transitionChecker, ref string transitionBiome, int octave=0, int groundLevel=20, int ceilingLevel=100, string currentBiome=""){
    	int size = Chunk.chunkWidth;
    	int chunkXmult = chunkX * size;
    	int chunkZmult = chunkZ * size;
    	int i = 0;
    	int j = 0;

		// Heightmap Sampling
    	for(int x=chunkXmult;x<chunkXmult+size;x+=4){
    		j = 0;
    		for(int z=chunkZmult;z<chunkZmult+size;z+=4){
				selectedCache[i*(Chunk.chunkWidth+1)+j] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x*generationSeed/xhash+offsetHash, z*generationSeed/zhash+offsetHash))), 0, ceilingLevel));
    			j+=4;
    		}
    		i+=4;
    	}

        // Look Ahead to X+
        switch(biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ), worldSeed)){
            case "Plains":
                if("Plains" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Plains";
                }
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, false, octave);
                break;
            case "Grassy Highlands":
                if("Grassy Highlands" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Grassy Highlands";
                }
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, false, octave);
                break;
            case "Ocean":
                if("Ocean" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Ocean";
                }
                MixOceanBorderPivots(selectedCache, chunkX, chunkZ, false, octave, currentBiome:currentBiome);
                break;
            case "Forest":
                if("Forest" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Forest";
                }
                MixForestBorderPivots(selectedCache, chunkX, chunkZ, false, octave); 
                break;               
            default:
                print("Deu Merda");
                break;
        }

        // Look Ahead to Z+
        switch(biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1), worldSeed)){
            case "Plains":
                if("Plains" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Plains";
                }
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            case "Grassy Highlands":
                if("Grassy Highlands" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Grassy Highlands";
                }
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            case "Ocean":
                if("Ocean" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Ocean";
                }
                MixOceanBorderPivots(selectedCache, chunkX, chunkZ, true, octave, currentBiome:currentBiome);
                break;
            case "Forest":
                if("Forest" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Forest";
                }
                MixForestBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            default:
                print("Deu Merda");
                break;
        }

        // Look ahead into XZ+
        switch(biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1), worldSeed)){
            case "Plains":
                if("Plains" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Plains";
                }
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            case "Grassy Highlands":
                if("Grassy Highlands" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Grassy Highlands";
                }
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            case "Ocean":
                if("Ocean" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Ocean";
                }
                MixOceanBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true, currentBiome:currentBiome);
                break;
            case "Forest":
                if("Forest" != currentBiome){
                    transitionChecker = true;
                    transitionBiome = "Forest";
                }
                MixForestBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            default:
                break;
        }   
    }

    // Builds Lookahead pivots for biome border smoothing
    public void GenerateLookAheadPivots(ushort[] selectedMap, int chunkX, int chunkZ, float xhash, float zhash, bool isSide, int groundLevel=0, int ceilingLevel=99){
        int size = Chunk.chunkWidth;
        chunkX *= size;
        chunkZ *= size;
        int i = 0;

        // If is looking ahead into X+ Chunk
        if(isSide){
            for(int x=chunkX; x<=chunkX+size; x+=4){
                selectedMap[i*(Chunk.chunkWidth+1)+size] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x*generationSeed/xhash+offsetHash, (chunkZ+size)*generationSeed/zhash+offsetHash))), 0, ceilingLevel));
                i+=4;
            }
        }
        // If is looking ahead into Z+ Chunk
        else{
            for(int z=chunkZ; z<chunkZ+size; z+=4){ // Don't generate corner twice
                selectedMap[size*(Chunk.chunkWidth+1)+i] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise((chunkX+size)*generationSeed/xhash+offsetHash, z*generationSeed/zhash+offsetHash))), 0, ceilingLevel));
                i+=4;
            }
        }
    }

    // Builds Lookahead pivot of corner chunk for biome border smoothing
    public void GenerateLookAheadCorner(ushort[] selectedMap, int chunkX, int chunkZ, float xhash, float zhash, int groundLevel=0, int ceilingLevel=99){
        selectedMap[selectedMap.Length-1] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(((chunkX+1)*Chunk.chunkWidth*generationSeed)/xhash+offsetHash, ((chunkZ+1)*Chunk.chunkWidth*generationSeed)/zhash+offsetHash))), 0, ceilingLevel));
    }

    // Generates Flat Map of something
    // Consider using this for biomes that are relatively low altitude
    private void GenerateFlatMap(ushort[] selectedCache, ushort ceiling){
        if(ceiling >= Chunk.chunkDepth)
            ceiling = (ushort)(Chunk.chunkDepth-1);

        for(int x=0; x<Chunk.chunkWidth;x++){
            for(int z=0; z<Chunk.chunkWidth;z++){
                selectedCache[x*Chunk.chunkWidth+z] = ceiling;
            }
        }
    }

    // Applies bilinear interpolation to a given pivotmap
    // Takes any cache and returns on itself
    private void BilinearInterpolateMap(ushort[] heightMap, int interval=4){
    	int size = Chunk.chunkWidth;
    	int interpX = 0;
    	int interpZ = 0;
        float step = 1f/interval;
    	float scaleX = 0f;
    	float scaleZ = 0f;

		// Bilinear Interpolation
    	for(int z=0;z<size;z++){
    		if(z%interval == 0){
    			interpZ+=interval;
    			scaleZ = step;
    		}
    		for(int x=0;x<size;x++){
    			// If is a pivot in X axis
    			if(x%interval == 0){
    				interpX+=interval;
    				scaleX = step;
    			}

    			heightMap[x*(Chunk.chunkWidth+1)+z] = (ushort)(Mathf.RoundToInt(((heightMap[(interpX-interval)*(Chunk.chunkWidth+1)+(interpZ-interval)])*(1-scaleX)*(1-scaleZ)) + (heightMap[interpX*(Chunk.chunkWidth+1)+(interpZ-interval)]*scaleX*(1-scaleZ)) + (heightMap[(interpX-interval)*(Chunk.chunkWidth+1)+interpZ]*scaleZ*(1-scaleX)) + (heightMap[interpX*(Chunk.chunkWidth+1)+interpZ]*scaleX*scaleZ)));
    			scaleX += step;

    		}
    		interpX = 0;
    		scaleX = 0;
    		scaleZ += step;
    	}	
    }

    // Quick adds all elements in heightMap
    // This makes it easy to get a layer below surface blocks
    // Takes any cache and returns on cacheNumber
    private void AddFromMap(ushort[] map, int val, int cacheNumber=2){

    	if(cacheNumber == 1){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
                    int index = x*(Chunk.chunkWidth+1)+z;
	    			cacheHeightMap[index] = (ushort)(map[index] + val);
	    		}
	    	}
    	}

    	else if(cacheNumber == 2){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
                    int index = x*(Chunk.chunkWidth+1)+z;
                    cacheHeightMap2[index] = (ushort)(map[index] + val);
	    		}
	    	}
	    }
	    else if(cacheNumber == 3){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
                    int index = x*(Chunk.chunkWidth+1)+z;
                    cacheHeightMap3[index] = (ushort)(map[index] + val);
	    		}
	    	}	    	
	    }
	    else{
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
                    int index = x*(Chunk.chunkWidth+1)+z;
                    cacheHeightMap4[index] = (ushort)(map[index] + val);
	    		}
	    	}
	    }
    }

    // Applies Octaves to Pivot Map
    private void CombinePivotMap(ushort[] a, ushort[] b){
        for(int x=0;x<=Chunk.chunkWidth;x+=4){
            for(int z=0;z<=Chunk.chunkWidth;z+=4){
                int index = x*(Chunk.chunkWidth+1)+z;
                a[index] = (ushort)(Mathf.FloorToInt((a[index] + b[index])/2));
            }
        }
    }

    // Applies Octaves to border pivot maps
    private void CombineBorderPivots(int[] selectedMap, int[] auxMap, bool isSide){
        if(isSide){
            for(int x=0; x<=Chunk.chunkWidth; x+=4){
                int index = Chunk.chunkWidth*Chunk.chunkWidth+x;
                selectedMap[index] = Mathf.FloorToInt((selectedMap[index] + auxMap[index])/2);
            }
        }
        else{
            for(int x=0; x<Chunk.chunkWidth; x+=4){ // < sign to not calculate corner twice
                int index = x*Chunk.chunkWidth+Chunk.chunkWidth;
                selectedMap[index] = Mathf.FloorToInt((selectedMap[index] + auxMap[index])/2);
            }            
        }
    }

    // Applies Structures to a chunk
    /*
    Depth Values represent how deep below heightmap things will go.
    Range represents if structure always spawn at given Depth, or if it spans below as well
    */
    private void GenerateStructures(ChunkPos pos, float xhash, float zhash, byte biome, int structureCode, int depth, int heightlimit=0, bool range=false){
        // Gets index of amount and percentage
        int index = BiomeHandler.GetBiomeStructs(biome).IndexOf(structureCode);
        int amount = BiomeHandler.GetBiomeAmounts(biome)[index];

        float percentage = BiomeHandler.GetBiomePercentages(biome)[index];

        int x,y,z;
        int offsetX, offsetZ;
        int rotation = 0;
        float chance;

        // Offset
        offsetX = structHandler.LoadStructure(structureCode).offsetX;
        offsetZ = structHandler.LoadStructure(structureCode).offsetZ;

        // If structure is static at given heightmap depth
        if(!range){            
            for(int i=1; i <= amount; i++){
                chance = Perlin.Noise((pos.x ^ pos.z)*(zhash/xhash)*(i*0.17f)*structureCode);

                if(chance > percentage)
                    continue;

                rotation = Mathf.FloorToInt(Perlin.Noise((pos.z+pos.x)*xhash*zhash+i*generationSeed)*3.99f);

                x = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.z)*xhash*pos.x*generationSeed)*Chunk.chunkWidthMult);
                z = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.x)*zhash*pos.z*generationSeed)*Chunk.chunkWidthMult);

                // All >
                if(x + offsetX > 15 && z + offsetZ > 15)
                    y = HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, bothAxis:true) - depth;
                // X >
                else if(x + offsetX > 15 && z + offsetZ <= 15)
                    y = HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, xAxis:true) - depth;
                // Z >
                else if(x + offsetX <= 15 && z + offsetZ > 15)
                    y = HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, zAxis:true) - depth;
                // All <
                else
                    y = cacheHeightMap[(x+offsetX)*(Chunk.chunkWidth+1)+(z+offsetZ)] - depth;

                // Ignores structure on hard limit
                if(y <= heightlimit)
                    continue;
                
                this.structHandler.LoadStructure(structureCode).Apply(this, pos, cacheVoxdata, cacheMetadataHP, cacheMetadataState, x, y, z, rotation:rotation);
            }
        }
        // If can be placed in a range
        else{
            for(int i=1; i <= amount; i++){
                chance = Perlin.Noise((pos.x ^ pos.z)*(zhash/xhash)*(i*0.17f)*structureCode);

                if(chance > percentage)
                    continue;

                rotation = Mathf.FloorToInt(Perlin.Noise((pos.z+pos.x)*xhash*zhash+i*generationSeed)*3.99f);

                x = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.z)*xhash*pos.x*generationSeed)*Chunk.chunkWidthMult);
                z = Mathf.FloorToInt(Perlin.Noise((i+structureCode+pos.x)*zhash*pos.z*generationSeed)*Chunk.chunkWidthMult);
                float yMult = Perlin.Noise((i + structureCode + (pos.z & pos.x))*xhash*zhash);              

                // All >
                if(x + offsetX > 15 && z + offsetZ > 15){
                    y = (int)(heightlimit + yMult*(HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, bothAxis:true) - depth));
                }
                // X >
                else if(x + offsetX > 15 && z + offsetZ <= 15){
                    y = (int)(heightlimit + yMult*(HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, xAxis:true) - depth));
                }
                // Z >
                else if(x + offsetX <= 15 && z + offsetZ > 15){
                    y = (int)(heightlimit + yMult*(HalfConvolute(cacheHeightMap, x, z, offsetX, offsetZ, structureCode, zAxis:true) - depth));
                }
                // All <
                else{
                    y = (int)(heightlimit + yMult*(cacheHeightMap[(x+offsetX)*(Chunk.chunkWidth+1)+(z+offsetZ)] - depth));
                }

                this.structHandler.LoadStructure(structureCode).Apply(this, pos, cacheVoxdata, cacheMetadataHP, cacheMetadataState, x, y, z, rotation:rotation);
            }            
        }
    }

    // Returns the mean height for a given structure
    private int HalfConvolute(ushort[] heightmap, int x, int z, int offsetX, int offsetZ, int code, bool xAxis=false, bool zAxis=false, bool bothAxis=false){
        int sum=0;
        int amount=0;
        
        if(bothAxis){
            for(int i=x; i < Chunk.chunkWidth; i++){
                for(int c=z; c < Chunk.chunkWidth; c++){
                    sum += heightmap[i*Chunk.chunkWidth+c];
                    amount++;
                }
            }
            if(amount > 0)
                return (int)(sum / amount); 
            else
                return (int)heightmap[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)-1];        
        }
        else if(xAxis){
            int size = structHandler.LoadStructure(code).sizeZ;

            for(int i=x; i < Chunk.chunkWidth; i++){
                for(int c=z; c < Mathf.Min(z+size, Chunk.chunkWidth); c++){
                    sum += heightmap[i*Chunk.chunkWidth+c];
                    amount++;
                }
            }
            if(amount > 0)
                return (int)(sum / amount);
            else
                return (int)heightmap[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)-1]; 
        }
        else if(zAxis){
            int size = structHandler.LoadStructure(code).sizeX;

            for(int i=z; i < Chunk.chunkWidth; i++){
                for(int c=x; c < Mathf.Min(x+size, Chunk.chunkWidth); c++){
                    sum += heightmap[c*Chunk.chunkWidth+i];
                    amount++; 
                }
            }
            if(amount > 0)
                return (int)(sum / amount);
            else
                return (int)heightmap[(Chunk.chunkWidth+1)*(Chunk.chunkWidth+1)-1];         
        }
        
        
        return heightmap[x*Chunk.chunkWidth+z]-1;
    }

    // Generates Plains biome chunk
	public VoxelData GeneratePlainsBiome(int chunkX, int chunkZ, bool pregen=false){
		// Hash values for Plains Biomes
        float xhash = 41.21f;
        float zhash = 105.243f;
        bool transition = false;
        string transitionBiome = "";
        
		// Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
		GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, ref transitionBiome, octave:0, groundLevel:20, ceilingLevel:25, currentBiome:"Plains");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, ref transition, ref transitionBiome, octave:1, groundLevel:18, ceilingLevel:30, currentBiome:"Plains");
        CombinePivotMap(cacheHeightMap, cacheHeightMap2);

        // Does different interpolation for normal vs transition chunks
        if(!transition)
            BilinearInterpolateMap(cacheHeightMap);
        else
            BilinearInterpolateMap(cacheHeightMap, interval:16);

		// Underground is hold on Cache 2
		AddFromMap(cacheHeightMap, -5);

		// Adds Cache 2 to pipeline
		cacheMaps.Add(cacheHeightMap2);
		cacheBlockCodes.Add(3);

		// Dirt is hold on Cache 3
		AddFromMap(cacheHeightMap, -1, cacheNumber:3);

        // Add Water
        if(!transition){
            GenerateFlatMap(cacheHeightMap4, BiomeHandler.GetWaterLevel("Plains"));
        }
        else if(BiomeHandler.GetWaterLevel("Plains") >= BiomeHandler.GetWaterLevel(transitionBiome)){
            GenerateFlatMap(cacheHeightMap4, BiomeHandler.GetWaterLevel(transitionBiome));
        }

		// Adds rest to pipeline
		cacheMaps.Add(cacheHeightMap3);
		cacheBlockCodes.Add(2);
		cacheMaps.Add(cacheHeightMap);
		cacheBlockCodes.Add(1);
        cacheMaps.Add(cacheHeightMap4);
        cacheBlockCodes.Add(6);

        cacheStateDict = new Dictionary<ushort, ushort>{{6, 0}};

		// Adds to cacheVoxdata
		ApplyHeightMaps(cacheStateDict, pregen:pregen);
        cacheStateDict.Clear();

        // Structures
        GeneratePlainsStructures(new ChunkPos(chunkX, chunkZ), xhash, zhash, 0, transition);

        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:20, maskThreshold:0.64f), upper:20);
    }

    // Inserts Structures into Plain Biome
    private void GeneratePlainsStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){        
        foreach(int structCode in BiomeHandler.GetBiomeStructs(biomeCode)){
            if(structCode == 1 || structCode == 2){ // Trees
                if(!transition)
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1, heightlimit:22);
            }
            else if(structCode == 3 || structCode == 4){ // Dirt Piles
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 2, range:true);
            }
            else if(structCode == 5){ // Boulder
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 0);
            }
            else if(structCode >= 9 && structCode <= 11){ // Metal Veins
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 8, range:true);
            }
        }
    }

    // Inserts Plains biome border pivots on another selectedHeightMap
    private void MixPlainsBorderPivots(ushort[] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
        float xhash = 41.21f;
        float zhash = 105.243f;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:20, ceilingLevel:25);
            else
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, isSide, groundLevel:18, ceilingLevel:30);
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:20, ceilingLevel:25);
            else
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, groundLevel:18, ceilingLevel:30);
        }
    }

    // Generates Grassy Highlands biome chunk
    public VoxelData GenerateGrassyHighLandsBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Grassy Highlands Biomes
        float xhash = 41.21f;
        float zhash = 105.243f;
        bool transition = false;
        string transitionBiome = "";

        GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, ref transitionBiome, octave:0, groundLevel:30, ceilingLevel:50, currentBiome:"Grassy Highlands");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, ref transition, ref transitionBiome, octave:1, groundLevel:30, ceilingLevel:70, currentBiome:"Grassy Highlands");
        CombinePivotMap(cacheHeightMap, cacheHeightMap2);

        // Does different interpolation for normal vs transition chunks
        if(!transition)
            BilinearInterpolateMap(cacheHeightMap);
        else
            BilinearInterpolateMap(cacheHeightMap, interval:16);

        // Underground is hold on Cache 2
        AddFromMap(cacheHeightMap, -5);

        // Adds Cache 2 to pipeline
        cacheMaps.Add(cacheHeightMap2);
        cacheBlockCodes.Add(3);

        // Dirt is hold on Cache 3
        AddFromMap(cacheHeightMap, -1, cacheNumber:3);

        // Adds rest to pipeline
        cacheMaps.Add(cacheHeightMap3);
        cacheBlockCodes.Add(2);
        cacheMaps.Add(cacheHeightMap);
        cacheBlockCodes.Add(1);

        // Add Water
        if(!transition){
            GenerateFlatMap(cacheHeightMap4, BiomeHandler.GetWaterLevel("Grassy Highlands"));
            cacheMaps.Add(cacheHeightMap4);
            cacheBlockCodes.Add(6);
        }
        else if(BiomeHandler.GetWaterLevel("Grassy Highlands") >= BiomeHandler.GetWaterLevel(transitionBiome)){
            GenerateFlatMap(cacheHeightMap4, BiomeHandler.GetWaterLevel(transitionBiome));
            cacheMaps.Add(cacheHeightMap4);
            cacheBlockCodes.Add(6);
        }

        cacheStateDict = new Dictionary<ushort, ushort>{{6, 0}};

        // Adds to cacheVoxdata
        ApplyHeightMaps(cacheStateDict, pregen:pregen);
        cacheStateDict.Clear();

        // Structures
        GenerateGrassyHighLandsStructures(new ChunkPos(chunkX, chunkZ), xhash, zhash, 1, transition);

        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:40, maskThreshold:0.64f), upper:40);
    }

    // Inserts Structures into Plain Biome
    private void GenerateGrassyHighLandsStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(biomeCode)){
            if(structCode <= 2){ // Trees
                if(!transition){
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1, heightlimit:22);
                }
            }
            else if(structCode == 3 || structCode == 4){ // Dirt Piles
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 2, range:true);
            }
            else if(structCode == 5){ // Boulder
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 0);
            }
            else if(structCode >= 9 && structCode <= 11){ // Metal Veins
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 8, range:true);
            }
        }
    }

    // Inserts Grassy Highlands biome border pivots on another selectedHeightMap
    private void MixGrassyHighlandsBorderPivots(ushort[] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
        float xhash = 41.21f;
        float zhash = 105.243f;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:30, ceilingLevel:50);
            else
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, isSide, groundLevel:30, ceilingLevel:70);            
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:30, ceilingLevel:50);
            else
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, groundLevel:30, ceilingLevel:70);

        }
    }

    // Generates Ocean biome chunk
    public VoxelData GenerateOceanBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Ocean Biomes
        float xhash = 54.7f;
        float zhash = 69.3f;
        bool transition = false;
        string transitionBiome = "";

        GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, ref transitionBiome, octave:0, groundLevel:1, ceilingLevel:19, currentBiome:"Ocean");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, ref transition, ref transitionBiome, octave:1, groundLevel:1, ceilingLevel:19, currentBiome:"Ocean");
        CombinePivotMap(cacheHeightMap, cacheHeightMap2);

        // Does different interpolation for normal vs transition chunks
        if(!transition)
            BilinearInterpolateMap(cacheHeightMap);
        else
            BilinearInterpolateMap(cacheHeightMap, interval:16);

        // Adds Cache 2 to pipeline
        cacheMaps.Add(cacheHeightMap);
        cacheBlockCodes.Add(2);

        // Add Water
        GenerateFlatMap(cacheHeightMap4, BiomeHandler.GetWaterLevel("Ocean"));

        // Adds rest to pipeline
        cacheMaps.Add(cacheHeightMap4);
        cacheBlockCodes.Add(6);

        cacheStateDict = new Dictionary<ushort, ushort>{{6,0}};

        // Adds to cacheVoxdata
        ApplyHeightMaps(cacheStateDict, pregen:pregen);

        // Structures
        GenerateOceanStructures(new ChunkPos(chunkX, chunkZ), xhash, zhash, 2, transition);

        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:10, maskThreshold:0.64f), upper:10);
    }

    // Inserts Structures into Ocean Biome
    private void GenerateOceanStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){
        // Nothing Yet

    }

    // Inserts Ocean border pivots on another selectedHeightMap
    private void MixOceanBorderPivots(ushort[] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false, string currentBiome=""){
        float xhash = 54.7f;
        float zhash = 69.3f;

        if(currentBiome != "Ocean"){
            if(!corner){
                if(octave == 0){
                    GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, isSide, groundLevel:1, ceilingLevel:20);            
                }
            }
            else{
                if(octave == 0){
                    GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, groundLevel:1, ceilingLevel:20);
                }

            }
        }
        else{
            if(!corner){
                if(octave == 0){
                    GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, isSide, groundLevel:1, ceilingLevel:20);            
                }
            }
            else{
                if(octave == 0){
                    GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:1, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, groundLevel:1, ceilingLevel:20);
                }

            }            
        }
    }

    // Generates Forest biome chunk
    public VoxelData GenerateForestBiome(int chunkX, int chunkZ, bool pregen=false){
        // Hash values for Plains Biomes
        float xhash = 72.117f;
        float zhash = 45.483f;
        bool transition = false;
        string transitionBiome = "";
        
        // Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
        GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, ref transitionBiome, octave:0, groundLevel:25, ceilingLevel:32, currentBiome:"Forest");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, ref transition, ref transitionBiome, octave:1, groundLevel:25, ceilingLevel:45, currentBiome:"Forest");
        CombinePivotMap(cacheHeightMap, cacheHeightMap2);


        // Does different interpolation for normal vs transition chunks
        if(!transition)
            BilinearInterpolateMap(cacheHeightMap);
        else
            BilinearInterpolateMap(cacheHeightMap, interval:16);

        // Underground is hold on Cache 2
        AddFromMap(cacheHeightMap, -5);

        // Adds Cache 2 to pipeline
        cacheMaps.Add(cacheHeightMap2);
        cacheBlockCodes.Add(3);

        // Dirt is hold on Cache 3
        AddFromMap(cacheHeightMap, -1, cacheNumber:3);

        // Adds rest to pipeline
        cacheMaps.Add(cacheHeightMap3);
        cacheBlockCodes.Add(2);
        cacheMaps.Add(cacheHeightMap);
        cacheBlockCodes.Add(1);


        // Add Water
        if(transition){
            if(BiomeHandler.GetWaterLevel("Forest") >= BiomeHandler.GetWaterLevel(transitionBiome)){
                GenerateFlatMap(cacheHeightMap4, BiomeHandler.GetWaterLevel(transitionBiome));
                cacheMaps.Add(cacheHeightMap4);
                cacheBlockCodes.Add(6);
            }
        }

        cacheStateDict = new Dictionary<ushort, ushort>{{6, 0}};

        // Adds to cacheVoxdata
        ApplyHeightMaps(cacheStateDict, pregen:pregen);

        // Structures
        GenerateForestStructures(new ChunkPos(chunkX, chunkZ), xhash, zhash, 3, transition);
        
        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.07f, 0.073f, 0.067f, 0.45f, ceiling:30, maskThreshold:0.64f), upper:30); //0.271, 0.3, 0.313
    }

    // Inserts Structures into Forest Biome
    private void GenerateForestStructures(ChunkPos pos, float xhash, float zhash, byte biomeCode, bool transition){

        foreach(int structCode in BiomeHandler.GetBiomeStructs(biomeCode)){
            
            if(structCode == 6){ // Big Tree
                if(!transition){
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1);
                }
            }
            else if(structCode == 1 || structCode == 2 || structCode == 7 || structCode == 8){
                if(!transition){
                    GenerateStructures(pos, xhash, zhash, biomeCode, structCode, -1);
                }
            }
            else if(structCode == 3 || structCode == 4){ // Dirt Piles
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 2, range:true);
            }
            else if(structCode >= 9 && structCode <= 11){ // Metal Veins
                GenerateStructures(pos, xhash, zhash, biomeCode, structCode, 8, range:true);
            }

        }
    }

    // Inserts Forest biome border pivots on another selectedHeightMap
    private void MixForestBorderPivots(ushort[] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
        float xhash = 72.117f;
        float zhash = 45.483f;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:25, ceilingLevel:32);
            else
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, isSide, groundLevel:25, ceilingLevel:45);
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:25, ceilingLevel:32);
            else
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, groundLevel:25, ceilingLevel:45);
        }
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
















/*
MULTITHREADING JOBS
*/
[BurstCompile]
public struct RidgedMultiFractalJob : IJobParallelFor{

    public int chunkX;
    public int chunkZ;
    public float xhash;
    public float yhash;
    public float zhash;
    public float threshold;
    public float generationSeed;
    public int ceiling; // = 20
    public float maskThreshold; //=0f

    [NativeDisableParallelForRestriction]
    public NativeArray<ushort> turbulenceMap;

    public void Execute(int index){
        int size = Chunk.chunkWidth;
        int j = 0;
        float val;
        float mask;

        int x = chunkX+index;

        j = 0;
        for(int z=chunkZ;z<chunkZ+size;z++){

            mask = Perlin.Noise((x^z)*(xhash*yhash)*1.07f, z*zhash*0.427f);
            for(int y=0;y<Chunk.chunkDepth;y++){
                if(mask < maskThreshold){
                    turbulenceMap[(index*Chunk.chunkDepth*Chunk.chunkWidth)+y+(j*Chunk.chunkDepth)] = 0;
                    continue;
                }

                val = Perlin.RidgedMultiFractal(x*xhash*generationSeed, y*yhash*generationSeed, z*zhash*generationSeed);

                if(val >= threshold && y <= ceiling){
                    turbulenceMap[(index*Chunk.chunkDepth*Chunk.chunkWidth)+y+(j*Chunk.chunkDepth)] = 1;
                }
                else{
                    turbulenceMap[(index*Chunk.chunkDepth*Chunk.chunkWidth)+y+(j*Chunk.chunkDepth)] = 0;
                }
            }
            j++;
        }
    }
}
