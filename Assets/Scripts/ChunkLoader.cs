using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	// World Generation
	public int worldSeed = 1; // 6 number integer
    public float offsetHash;
    public BiomeHandler biomeHandler;
    private bool isBorderXP = false;   // True if the assigned biomes on neighbor chunks are different
    private bool isBorderZP = false;   // True if the assigned biomes on neighbor chunks are different
    private bool isBorderXM = false;
    private bool isBorderZM = false;
    private bool isBorderXZP = false;

	// Chunk Rendering
	public ChunkRenderer rend;
    public RegionFileHandler regionHandler;

	// Flags
	public bool WORLD_GENERATED = false;

	// Cache Information
	private ushort[,] cacheHeightMap = new ushort[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private ushort[,] cacheHeightMap2 = new ushort[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private ushort[,] cacheHeightMap3 = new ushort[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private ushort[,] cacheHeightMap4 = new ushort[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
    private ushort[,] cachePivotMap = new ushort[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private	ushort[,,] cacheVoxdata = new ushort[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	private List<ushort[,]> cacheMaps = new List<ushort[,]>();
	private List<ushort> cacheBlockCodes = new List<ushort>();
    private ushort[,,] cacheTurbulanceMap = new ushort[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
    private ChunkPos cachePos = new ChunkPos(0,0);
    private VoxelMetadata cacheMetadata = new VoxelMetadata();
    private Dictionary<ushort, ushort> cacheStateDict = new Dictionary<ushort, ushort>();

    // Start is called before the first frame update
    void Start()
    {

    	if(worldSeed == 0){
    		worldSeed = 1;
    	}

        biomeHandler = new BiomeHandler(BiomeSeedFunction(worldSeed));
        offsetHash = OffsetHashFunction(worldSeed);

		player = GameObject.Find("Character").GetComponent<Transform>();
		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);

        regionHandler = new RegionFileHandler(worldSeed, renderDistance, newChunk);

		GetChunks(true);
    }

    void Update(){

    	if(toLoad.Count == 0 && !WORLD_GENERATED){
    		WORLD_GENERATED = true;
    	}

    	GetChunks(false);
    	UnloadChunk();

        //if(loadingCount >= 0)
        if(toLoad.Count > 0)
            LoadChunk();
        else
            DrawChunk();
    }
    
    // Erases loaded chunks dictionary
    private void ClearAllChunks(){
    	foreach(ChunkPos item in chunks.Keys){
    		Destroy(chunks[item].obj);
    		chunks.Remove(item);
            vfx.RemoveChunk(item);
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

            // Gets correct region file
            regionHandler.GetCorrectRegion(toLoad[0]);

            // If current chunk toLoad was already generated
            if(regionHandler.GetFile().IsIndexed(toLoad[0])){
                // If chunk is Pre-Generated
                if(regionHandler.GetsNeedGeneration(toLoad[0])){
                    print("ERROR: Tried to Generate pre-generated chunk");
                    // Does nothing because there is no Prefab System yet
                }
                // If it's just a normally generated chunk
                else{
                    chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this, fromMemory:true));
                    vfx.NewChunk(toLoad[0]);
                    regionHandler.LoadChunk(chunks[toLoad[0]]);
                }
            }
            // If it's a new chunk to be generated
            else{
        		chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook, this));
                vfx.NewChunk(toLoad[0]);
        		chunks[toLoad[0]].BuildOnVoxelData(AssignBiome(toLoad[0], worldSeed)); 
                chunks[toLoad[0]].metadata.metadata = cacheMetadata.metadata;
                cacheMetadata.Clear();

                regionHandler.SaveChunk(chunks[toLoad[0]]);
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
    private VoxelData AssignBiome(ChunkPos pos, float seed){
        string biome = biomeHandler.Assign(pos, seed);
        chunks[pos].biomeName = biome;
        chunks[pos].features = biomeHandler.GetFeatures(pos, seed);

        // Checks for X+ border
        cachePos.x = pos.x+1;
        cachePos.z = pos.z;

        if(biomeHandler.Assign(cachePos, seed) != biome)
            isBorderXP = true;

        // Checks for Z+ border
        cachePos.x = pos.x;
        cachePos.z = pos.z+1;

        if(biomeHandler.Assign(cachePos, seed) != biome)
            isBorderZP = true;

        // Checks for X- border
        cachePos.x = pos.x-1;
        cachePos.z = pos.z;

        if(biomeHandler.Assign(cachePos, seed) != biome)
            isBorderXM = true;

        // Checks for Z- border
        cachePos.x = pos.x;
        cachePos.z = pos.z-1;

        if(biomeHandler.Assign(cachePos, seed) != biome)
            isBorderZM = true;

        // Checks for XZ+ border
        cachePos.x = pos.x+1;
        cachePos.z = pos.z+1;

        if(biomeHandler.Assign(cachePos, seed) != biome)
            isBorderXZP = true;

        if(biome == "Plains")
            return GeneratePlainsBiome(pos.x, pos.z);
        else if(biome == "Grassy Highlands")
            return GenerateGrassyHighLandsBiome(pos.x, pos.z);
        else if(biome == "Ocean")
            return GenerateOceanBiome(pos.x, pos.z);
        else 
            return GeneratePlainsBiome(pos.x, pos.z);
        
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


    // Debug Feature
    private void ToFile(int[,] heightMap, string filename){
    	string a = "";
    	for(int x=16;x>=0;x--){
    		for(int y=0;y<=16;y++){
    			a += (heightMap[x,y] + "\t");
    		}
    		a += "\n";
    	}
    	System.IO.File.WriteAllText(filename, a);
    }

    /*
    Used for Cave system generation and above ground turbulence
    */
    private void GenerateRidgedMultiFractal3D(int chunkX, int chunkZ, float xhash, float yhash, float zhash, float threshold, int ceiling=20, float maskThreshold=0f){
    	int size = Chunk.chunkWidth;
    	chunkX *= size;
    	chunkZ *= size;
    	int i = 0;
    	int j = 0;
        float val;
        float mask;

    	for(int x=chunkX;x<chunkX+size;x++){
    		j = 0;
    		for(int z=chunkZ;z<chunkZ+size;z++){

                mask = Perlin.Noise((x^z)*(xhash*yhash)*1.07f, z*zhash*0.427f);
    			for(int y=0;y<Chunk.chunkDepth;y++){
                    
                    if(mask < maskThreshold){
                        cacheTurbulanceMap[i,y,j] = 0;
                        continue;
                    }

       				val = Perlin.RidgedMultiFractal(x*xhash, y*yhash, z*zhash);
    				
    				if(val >= threshold && y <= ceiling)
    					cacheTurbulanceMap[i,y,j] = 1;
    				else
    					cacheTurbulanceMap[i,y,j] = 0;
    			}
    			j++;
    		}
    		i++;
    	}
    }

    /*
    Takes heightmaps of different map layers and combines them into a cacheVoxdata
    Layers are applied sequentially, so the bottom-most layer should be the first one
    blockCode is the block related to this layer.
    */
    // Takes cacheMaps and cacheBlockCodes and returns on cacheVoxdata
    private void ApplyHeightMaps(Dictionary<ushort, ushort> stateDict){
    	int size = Chunk.chunkWidth;
   		int i=0;

   		for(i=0;i<cacheMaps.Count;i++){
    		// Heightmap Drawing
	    	for(int x=0;x<size;x++){
	    		for(int z=0;z<size;z++){
	    			// If it's the first layer to be added
	    			if(i == 0){
		    			for(int y=0;y<Chunk.chunkDepth;y++){
		    				if(y <= cacheMaps[i][x,z]){
		    					cacheVoxdata[x,y,z] = cacheBlockCodes[i]; // Adds block code
                                if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                    cacheMetadata.GetMetadata(x,y,z).state = stateDict[cacheBlockCodes[i]];
                                }
                            }
		    				else
		    					cacheVoxdata[x,y,z] = 0;
		    			}
	    			}
	    			// If is not the first layer
	    			else{
		    			for(int y=cacheMaps[i-1][x,z]+1;y<=cacheMaps[i][x,z];y++){
		    				cacheVoxdata[x,y,z] = cacheBlockCodes[i];
                            if(stateDict.ContainsKey(cacheBlockCodes[i])){ // Adds possible state
                                cacheMetadata.GetMetadata(x,y,z).state = stateDict[cacheBlockCodes[i]];
                            }
		    			}  				
	    			}
	    		}
	    	}
	    }
	    cacheMaps.Clear();
	    cacheBlockCodes.Clear();
    }

    // Generates Pivot heightmaps
    private void GeneratePivots(ushort[,] selectedCache, int chunkX, int chunkZ, float xhash, float zhash, ref bool transitionChecker, int octave=0, int groundLevel=20, int ceilingLevel=100, string currentBiome=""){
    	int size = Chunk.chunkWidth;
    	int chunkXmult = chunkX * size;
    	int chunkZmult = chunkZ * size;
    	int i = 0;
    	int j = 0;

		// Heightmap Sampling
    	for(int x=chunkXmult;x<chunkXmult+size;x+=4){
    		j = 0;
    		for(int z=chunkZmult;z<chunkZmult+size;z+=4){
				selectedCache[i, j] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x/xhash+offsetHash, z/zhash+offsetHash))), 0, ceilingLevel));
    			j+=4;
    		}
    		i+=4;
    	}


        // Look Ahead to X+
        switch(biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ), worldSeed)){
            case "Plains":
                if("Plains" != currentBiome)
                    transitionChecker = true;
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, false, octave);
                break;
            case "Grassy Highlands":
                if("Grassy Highlands" != currentBiome)
                    transitionChecker = true;
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, false, octave);
                break;
            case "Ocean":
                if("Ocean" != currentBiome)
                    transitionChecker = true;
                MixOceanBorderPivots(selectedCache, chunkX, chunkZ, false, octave, currentBiome:currentBiome);
                break;
            default:
                print("Deu Merda");
                break;
        }

        // Look Ahead to Z+
        switch(biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1), worldSeed)){
            case "Plains":
                if("Plains" != currentBiome)
                    transitionChecker = true;
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            case "Grassy Highlands":
                if("Grassy Highlands" != currentBiome)
                    transitionChecker = true;
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            case "Ocean":
                if("Ocean" != currentBiome)
                    transitionChecker = true;
                MixOceanBorderPivots(selectedCache, chunkX, chunkZ, true, octave, currentBiome:currentBiome);
                break;
            default:
                print("Deu Merda");
                break;
        }

        // Look ahead into XZ+
        switch(biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1), worldSeed)){
            case "Plains":
                if("Plains" != currentBiome)
                    transitionChecker = true;
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            case "Grassy Highlands":
                if("Grassy Highlands" != currentBiome)
                    transitionChecker = true;
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            case "Ocean":
                if("Ocean" != currentBiome)
                    transitionChecker = true;
                MixOceanBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true, currentBiome:currentBiome);
                break;
            default:
                print("Deu Merda");
                break;
        }        
    }

    // Builds Lookahead pivots for biome border smoothing
    public void GenerateLookAheadPivots(ushort[,] selectedMap, int chunkX, int chunkZ, float xhash, float zhash, bool isSide, int groundLevel=0, int ceilingLevel=99){
        int size = Chunk.chunkWidth;
        chunkX *= size;
        chunkZ *= size;
        int i = 0;

        // If is looking ahead into X+ Chunk
        if(isSide){
            for(int x=chunkX; x<=chunkX+size; x+=4){
                selectedMap[i, size] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x/xhash+offsetHash, (chunkZ+size)/zhash+offsetHash))), 0, ceilingLevel));
                i+=4;
            }
        }
        // If is looking ahead into Z+ Chunk
        else{
            for(int z=chunkZ; z<chunkZ+size; z+=4){ // Don't generate corner twice
                selectedMap[size, i] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise((chunkX+size)/xhash+offsetHash, z/zhash+offsetHash))), 0, ceilingLevel));
                i+=4;
            }
        }
    }

    // Builds Lookahead pivot of corner chunk for biome border smoothing
    public void GenerateLookAheadCorner(ushort[,] selectedMap, int chunkX, int chunkZ, float xhash, float zhash, int groundLevel=0, int ceilingLevel=99){
        selectedMap[Chunk.chunkWidth, Chunk.chunkWidth] = (ushort)(Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(((chunkX+1)*Chunk.chunkWidth)/xhash+offsetHash, ((chunkZ+1)*Chunk.chunkWidth)/zhash+offsetHash))), 0, ceilingLevel));
    }

    // Generates Flat Map of something
    // Consider using this for biomes that are relatively low altitude
    private void GenerateFlatMap(ushort[,] selectedCache, ushort ceiling){
        if(ceiling >= Chunk.chunkDepth)
            ceiling = (ushort)(Chunk.chunkDepth-1);

        for(int x=0; x<Chunk.chunkWidth;x++){
            for(int z=0; z<Chunk.chunkWidth;z++){
                selectedCache[x,z] = ceiling;
            }
        }
    }

    // Generates a flat map at Y level ceiling with deadzone protection
    private void GenerateWaterMap(ushort[,] selectedCache, ushort ceiling){
        int deadZoneXM=0;
        int deadZoneZM=0;
        int deadZoneXP=0;
        int deadZoneZP=0;

        if(ceiling >= Chunk.chunkDepth)
            ceiling = (ushort)(Chunk.chunkDepth-1);

        if(isBorderZM){
            deadZoneZM = 1;
            isBorderZM = false;
        }
        if(isBorderXM){
            deadZoneXM = 1;
            isBorderXM = false;
        }
        if(isBorderZP){
            deadZoneZP = 4;
            isBorderZP = false;
        }
        if(isBorderXP){
            deadZoneXP = 4;
            isBorderXP = false;
        }

        // Fill with Water
        for(int x=deadZoneXM;x<Chunk.chunkWidth-deadZoneXP;x++){
            for(int z=deadZoneZM;z<Chunk.chunkWidth-deadZoneZP;z++){
                selectedCache[x,z] = ceiling;
            }
        }

        // Fill X- Deadzone
        for(int x=0;x<deadZoneXM;x++){
            for(int z=0;z<Chunk.chunkWidth;z++){
                selectedCache[x,z] = 0;
            }
        }        

        // Fill X+ Deadzone
        for(int x=Chunk.chunkWidth-deadZoneXP;x<Chunk.chunkWidth;x++){
            for(int z=0;z<Chunk.chunkWidth;z++){
                selectedCache[x,z] = 0;
            }
        } 

        // Fill Z- Deadzone
        for(int z=0;z<deadZoneZM;z++){
            for(int x=0;x<Chunk.chunkWidth;x++){
                selectedCache[x,z] = 0;
            }
        }

        // Fill Z+ Deadzone
        for(int z=Chunk.chunkWidth-deadZoneZP;z<Chunk.chunkWidth;z++){
            for(int x=0;x<Chunk.chunkWidth;x++){
                selectedCache[x,z] = 0;
            }
        } 

        // Fill XZ+ Deadzone
        if(isBorderXZP){
            for(int x=Chunk.chunkWidth-4; x<Chunk.chunkWidth; x++){
                for(int z=Chunk.chunkWidth-4; z<Chunk.chunkWidth; z++){
                    selectedCache[x,z] = 0;
                }
            }
            isBorderXZP = false;
        }
    }


    // Applies bilinear interpolation to a given pivotmap
    // Takes any cache and returns on itself
    private void BilinearInterpolateMap(ushort[,] heightMap, int interval=4){
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

    			heightMap[x,z] = (ushort)(Mathf.RoundToInt(((heightMap[interpX-interval, interpZ-interval])*(1-scaleX)*(1-scaleZ)) + (heightMap[interpX, interpZ-interval]*scaleX*(1-scaleZ)) + (heightMap[interpX-interval, interpZ]*scaleZ*(1-scaleX)) + (heightMap[interpX, interpZ]*scaleX*scaleZ)));
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
    private void AddFromMap(ushort[,] map, int val, int cacheNumber=2){

    	if(cacheNumber == 1){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap[x,z] = (ushort)(map[x,z] + val);
	    		}
	    	}
    	}

    	else if(cacheNumber == 2){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap2[x,z] = (ushort)(map[x,z] + val);
	    		}
	    	}
	    }
	    else if(cacheNumber == 3){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap3[x,z] = (ushort)(map[x,z] + val);
	    		}
	    	}	    	
	    }
	    else{
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap4[x,z] = (ushort)(map[x,z] + val);
	    		}
	    	}
	    }
    }

    // Applies Octaves to Pivot Map
    private void CombinePivotMap(ushort[,] a, ushort[,] b){
        for(int x=0;x<=Chunk.chunkWidth;x+=4){
            for(int z=0;z<=Chunk.chunkWidth;z+=4){
                a[x,z] = (ushort)(Mathf.FloorToInt((a[x,z] + b[x,z])/2));
            }
        }
    }

    // Applies Octaves to border pivot maps
    private void CombineBorderPivots(int[,] selectedMap, int[,] auxMap, bool isSide){
        if(isSide){
            for(int x=0; x<=Chunk.chunkWidth; x+=4){
                selectedMap[Chunk.chunkWidth, x] = Mathf.FloorToInt((selectedMap[Chunk.chunkWidth, x] + auxMap[Chunk.chunkWidth, x])/2);
            }
        }
        else{
            for(int x=0; x<Chunk.chunkWidth; x+=4){ // < sign to not calculate corner twice
                selectedMap[x, Chunk.chunkWidth] = Mathf.FloorToInt((selectedMap[x, Chunk.chunkWidth] + auxMap[x, Chunk.chunkWidth])/2);
            }            
        }
    }

    // Generates Plains biome chunk
	public VoxelData GeneratePlainsBiome(int chunkX, int chunkZ){
		// Hash values for Plains Biomes
        float xhash = 41.21f;
        float zhash = 105.243f;
        bool transition = false;
        
		// Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
		GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, octave:0, groundLevel:20, ceilingLevel:25, currentBiome:"Plains");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, ref transition, octave:1, groundLevel:18, ceilingLevel:30, currentBiome:"Plains");
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
        GenerateWaterMap(cacheHeightMap4, 22);

		// Adds rest to pipeline
		cacheMaps.Add(cacheHeightMap3);
		cacheBlockCodes.Add(2);
		cacheMaps.Add(cacheHeightMap);
		cacheBlockCodes.Add(1);
        cacheMaps.Add(cacheHeightMap4);
        cacheBlockCodes.Add(6);

        cacheStateDict = new Dictionary<ushort, ushort>{{6, 0}};

		// Adds to cacheVoxdata
		ApplyHeightMaps(cacheStateDict);
        cacheStateDict.Clear();
        
        // Cave Systems
        GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.12f, 0.07f, 0.089f, 0.35f, ceiling:20, maskThreshold:0.7f);
        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), new VoxelData(cacheTurbulanceMap), upper:20);
    }

    // Inserts Plains biome border pivots on another selectedHeightMap
    private void MixPlainsBorderPivots(ushort[,] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
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
    public VoxelData GenerateGrassyHighLandsBiome(int chunkX, int chunkZ){
        // Hash values for Grassy Highlands Biomes
        float xhash = 41.21f;
        float zhash = 105.243f;
        bool transition = false;

        GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, octave:0, groundLevel:30, ceilingLevel:50, currentBiome:"Grassy Highlands");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, ref transition, octave:1, groundLevel:30, ceilingLevel:70, currentBiome:"Grassy Highlands");
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
            GenerateWaterMap(cacheHeightMap4, 42);
            cacheMaps.Add(cacheHeightMap4);
            cacheBlockCodes.Add(6);
        }

        cacheStateDict = new Dictionary<ushort, ushort>{{6, 0}};

        // Adds to cacheVoxdata
        ApplyHeightMaps(cacheStateDict);
        cacheStateDict.Clear();

        // Cave Systems
        GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.12f, 0.07f, 0.089f, 0.35f, ceiling:40, maskThreshold:0.7f);
        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), new VoxelData(cacheTurbulanceMap), upper:40);
    }

    // Inserts Grassy Highlands biome border pivots on another selectedHeightMap
    private void MixGrassyHighlandsBorderPivots(ushort[,] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
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
    public VoxelData GenerateOceanBiome(int chunkX, int chunkZ){
        // Hash values for Ocean Biomes
        float xhash = 54.7f;
        float zhash = 69.3f;
        bool transition = false;

        GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, ref transition, octave:0, groundLevel:1, ceilingLevel:19, currentBiome:"Ocean");
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, ref transition, octave:1, groundLevel:1, ceilingLevel:19, currentBiome:"Ocean");
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
        GenerateFlatMap(cacheHeightMap4, 20);

        // Adds rest to pipeline
        cacheMaps.Add(cacheHeightMap4);
        cacheBlockCodes.Add(6);

        cacheStateDict = new Dictionary<ushort, ushort>{{6,0}};

        // Adds to cacheVoxdata
        ApplyHeightMaps(cacheStateDict);

        // Cave Systems
        GenerateRidgedMultiFractal3D(chunkX, chunkZ, 0.12f, 0.07f, 0.089f, 0.35f, ceiling:10, maskThreshold:0.7f);
        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), new VoxelData(cacheTurbulanceMap), upper:10);
    }

    // Inserts Ocean border pivots on another selectedHeightMap
    private void MixOceanBorderPivots(ushort[,] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false, string currentBiome=""){
        float xhash = 54.7f;
        float zhash = 69.3f;

        if(currentBiome != "Ocean"){
            if(!corner){
                if(octave == 0){
                    GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:20, ceilingLevel:20);
                }
                else{
                    GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, isSide, groundLevel:20, ceilingLevel:20);            
                }
            }
            else{
                if(octave == 0){
                    GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:19, ceilingLevel:19);
                }
                else{
                    GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*0.112f, zhash*0.31f, groundLevel:19, ceilingLevel:19);
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
