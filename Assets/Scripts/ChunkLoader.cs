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
	public BlockEncyclopedia blockBook;
    public VFXLoader vfx;

	// World Generation
	public int worldSeed = 1; // 6 number integer
	public float hashSeed;
	public float caveSeed;
    public BiomeHandler biomeHandler;

	// Chunk Rendering
	public ChunkRenderer rend;

	// Flags
	public bool WORLD_GENERATED = false;

	// Cache Information
	private int[,] cacheHeightMap = new int[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private int[,] cacheHeightMap2 = new int[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private int[,] cacheHeightMap3 = new int[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private int[,] cacheHeightMap4 = new int[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
    private int[,] cachePivotMap = new int[Chunk.chunkWidth+1,Chunk.chunkWidth+1];
	private	int[,,] cacheVoxdata = new int[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	private List<int[,]> cacheMaps = new List<int[,]>();
	private List<int> cacheBlockCodes = new List<int>();
    private int[,,] cacheTurbulanceMap = new int[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];

    // Start is called before the first frame update
    void Start()
    {

    	if(worldSeed == 0){
    		worldSeed = 1;
    	}

        biomeHandler = new BiomeHandler(BiomeSeedFunction(worldSeed));
    	hashSeed = TerrainSeedFunction(worldSeed);
    	caveSeed = LogSeedFunction(worldSeed);

		player = GameObject.Find("Character").GetComponent<Transform>();
		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);
		GetChunks(true);
    }

    void Update(){

    	if(toLoad.Count == 0 && !WORLD_GENERATED){
    		WORLD_GENERATED = true;
    	}

    	GetChunks(false);
    	UnloadChunk();
    	LoadChunk();
    }
    
    // Erases loaded chunks dictionary
    private void ClearAllChunks(){
    	foreach(ChunkPos item in chunks.Keys){
    		Destroy(chunks[item].obj);
    		chunks.Remove(item);
            vfx.RemoveChunk(item);
    	}
    }

    private void LoadChunk(){
    	if(toLoad.Count > 0){
    		// Prevention
    		if(toUnload.Contains(toLoad[0])){
    			toUnload.Remove(toLoad[0]);
    			toLoad.RemoveAt(0);
    			return;
    		}

     		
    		chunks.Add(toLoad[0], new Chunk(toLoad[0], this.rend, this.blockBook));
            vfx.NewChunk(toLoad[0]);
    		chunks[toLoad[0]].BuildOnVoxelData(AssignBiome(toLoad[0], worldSeed)); 
    		chunks[toLoad[0]].BuildChunk();
    		toLoad.RemoveAt(0);	
    	}
    }

    private VoxelData AssignBiome(ChunkPos pos, float seed){
        string biome = biomeHandler.Assign(pos, seed);
        chunks[pos].biomeName = biome;
        chunks[pos].features = biomeHandler.GetFeatures(pos, seed);
        
        if(biome == "Plains")
            return GeneratePlainsBiome(pos.x, pos.z);
        else if(biome == "Grassy Highlands")
            return GenerateGrassyHighLandsBiome(pos.x, pos.z);
        else 
            return GeneratePlainsBiome(pos.x, pos.z);
        
    }


    // Unloads a chunk per frame from the Unloading Buffer
    private void UnloadChunk(){
    	if(toUnload.Count > 0){

    		// Prevention
    		if(toLoad.Contains(toUnload[0])){
    			toLoad.Remove(toUnload[0]);
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
    		
	        for(int x=-renderDistance; x<=renderDistance;x++){
	        	for(int z=-renderDistance; z<=renderDistance;z++){
	        		toLoad.Add(new ChunkPos(newChunk.x+x, newChunk.z+z));
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
    			toLoad.Add(addChunk);
    		}
    	}
    	else if(diff == 2){ // West
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+renderDistance+1, newChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i);
    			toLoad.Add(addChunk);
    		}
    	}
    	else if(diff == 1){ // South
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance+1);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance);
    			toLoad.Add(addChunk);
      		}
    	}
    	else if(diff == 3){ // North
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance-1);
    			toUnload.Add(popChunk);
      			ChunkPos addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance);
    			toLoad.Add(addChunk);
       		}	
    	}
    	else if(diff == 5){ // Southeast
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i);
    			toLoad.Add(addChunk);
    		}
    		for(int i=-renderDistance+1; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i-1, newChunk.z-renderDistance);
    			toLoad.Add(addChunk);
    		}
    	}
    	else if(diff == 6){ // Southwest
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i);
    			toLoad.Add(addChunk);
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z-renderDistance);
    			toLoad.Add(addChunk);
    		}
    	}
    	else if(diff == 7){ // Northwest
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i);
    			toLoad.Add(addChunk);
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z-renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z+renderDistance);
    			toLoad.Add(addChunk);
    		}
    	}
    	else if(diff == 4){ // Northeast
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i);
    			toUnload.Add(popChunk);
    			ChunkPos addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i);
    			toLoad.Add(addChunk);
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			ChunkPos popChunk = new ChunkPos(currentChunk.x+i+1, currentChunk.z-renderDistance);
    			toUnload.Add(popChunk);
     			ChunkPos addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance);
    			toLoad.Add(addChunk);
    		}
    	}


	    currentChunk = newChunk;
    }

    // Calculates the caveSeed of caveworms
    private float LogSeedFunction(int t){
    	return Mathf.Log(t+2, 3f)+0.5f;
    }
 
    // Calculates hashSeed of terrain
    private float TerrainSeedFunction(int t){
    	return (0.3f/999999)*t + 0.1f;
    }

    // Calculates the biomeSeed of BiomeHandler
    private float BiomeSeedFunction(int t){
        return 0.04f*(0.03f*Mathf.Sin(t));
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
    Implementation of a Worm Algorithm to generate Cave-like Terrain
    */
    private VoxelData CaveWorm(int chunkX, int chunkZ, int lowerY=0, int upperY=-1, float killChance=0.2f, float recoveryChance=0.8f, int maxRecovery=3, float caveRoughness=0.4f, int lifeLimit=20){
    	if(upperY == -1)
    		upperY = Chunk.chunkDepth;

    	bool isAlive = true;
    	int radius;
    	int[] directions;
    	int currentDir = -1;
    	int recoveries = 0;

    	int[,,] vd = new int[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];

    	// Picked a random block in chunk
    	float pickedBlockValue = Perlin.Noise(chunkX*caveSeed, chunkZ*caveSeed);
    	Coord3D pickedBlock = HashBlock(pickedBlockValue, Chunk.chunkWidth, Chunk.chunkDepth);

    	// Iterating while worm is alive
    	while(isAlive){
    		radius = HashRadius(pickedBlockValue, min:2, max:5);
    		directions = HashDirection(pickedBlockValue, currentDir);

    		foreach(int item in directions){
    			currentDir = item;

			    // 0 = X+, 1 = X-, 2 = Z+, 3 = Z-, 4 = Y+, 5 = Y-
				if(item == 0)
					pickedBlock.x += 1;
				else if(item == 1)
					pickedBlock.x -= 1;
				else if(item == 2)
					pickedBlock.y += 1;
				else if(item == 3)
					pickedBlock.y  -= 1;
				else if(item == 4)
					pickedBlock.z  += 1;
				else if(item == 5)
					pickedBlock.z  -= 1;

				// Hazard Detection
				if(pickedBlock.y < lowerY || pickedBlock.y > upperY || pickedBlock.x < 0 || pickedBlock.x >= Chunk.chunkWidth || pickedBlock.y < 0 || pickedBlock.y >= Chunk.chunkDepth || pickedBlock.z < 0 || pickedBlock.z >= Chunk.chunkWidth){
					// Hazard Recovery System
					if(recoveries < maxRecovery && Perlin.Noise(chunkX*caveSeed*11.21f, pickedBlock.Sum()*caveSeed*4f, chunkZ*caveSeed*2.42f) <= recoveryChance){
    					recoveries++;
    					pickedBlockValue = Perlin.Noise(chunkX*caveSeed, pickedBlock.y*caveSeed*5, chunkZ*caveSeed);
						break;
					}
					else{
						return new VoxelData(vd);
					}
				}

				// Apply flood fill on pickedBlock
				FloodFill(vd, pickedBlock, radius, caveRoughness);	
    		}
    		// Kill Attempt
    		if(lifeLimit == 0 || Perlin.Noise(chunkX*caveSeed*23.21f, pickedBlock.Sum()*caveSeed*12f, chunkZ*caveSeed) <= killChance){
    			isAlive = false;
    			continue;
    		}
    		// Renew seed to new block
    		pickedBlockValue = Perlin.Noise(chunkX*caveSeed, pickedBlock.Sum()*caveSeed*5, chunkZ*caveSeed);
    		lifeLimit--;
    	}
    	return new VoxelData(vd);
    }

    /*
    Takes a float and returns an [int, int, int] code for block location in a chunk
    vOpt is the amount of vertical blocks in a chunk, and hOpt is the same for horizontal
    */
    private Coord3D HashBlock(float t, int vOpt, int hOpt){
    	int newCode = Mathf.FloorToInt(t*1000000);
    	Coord3D hCode;

    	vOpt = Mathf.FloorToInt(100/vOpt);
    	hOpt = Mathf.FloorToInt(100/hOpt);

    	hCode.x = Mathf.FloorToInt((newCode / 10000)/hOpt);
    	newCode = (newCode % 10000);
    	hCode.y = Mathf.FloorToInt((newCode / 100)/vOpt);
    	newCode = (newCode % 100);
    	hCode.z = Mathf.FloorToInt(newCode/hOpt);

    	return hCode;
    }

    /*
    Takes a float and returns a [int, int, int, int] code for block direction
    CurrentDir works on following calls just so the hash knows where the worm was going
    */
    private int[] HashDirection(float t, int currentDir=-1){
    	int[] directions = new int[4];
    	int newCode = Mathf.FloorToInt(t*10000);
    	int dir = currentDir;
    	int dec;

    	for(int i=0; i<4;i++){
	    	if(dir == -1){
	    		dec = Mathf.FloorToInt((newCode/Mathf.Pow(10, 3-i))/2);
	    		newCode = Mathf.FloorToInt(newCode % Mathf.Pow(10, 3-i));
	    		dir = dec;
	    		directions[i] = dir;
	    	} 
	    	else{
	    		dec = Mathf.FloorToInt(newCode/Mathf.Pow(10, 3-i));
	    		newCode = Mathf.FloorToInt(newCode % Mathf.Pow(10, 3-i));
	    		dir = EasyMapFunction(dec, dir);
	    		directions[i] = dir;
	    	}
   		}
   		return directions;
    }

    // Easy mapping used in HashDirection Function
    /*
    0 = X+, 1 = X-, 2 = Z+, 3 = Z-, 4 = Y+, 5 = Y-
    */
    private int EasyMapFunction(int t, int currentDir){
    	List<int> choices = new List<int>(){0,1,2,3,4,5};

    	if((currentDir % 2) == 1){
    		choices.Remove(currentDir+1);
    		choices.Remove(currentDir);
    	}
    	else{
    		choices.Remove(currentDir);
    		choices.Remove(currentDir-1);
    	}

    	if(t <= 5)
    		return currentDir;
    	else
    		return choices[t-6]; 
    }

    // Function to get radius in Caveworms
    private int HashRadius(float t, int min=0, int max=4){
    	return Mathf.FloorToInt((max+1)*t+min);
    }

    // FloodFill operation for Caveworms
    private void FloodFill(int[,,] vd, Coord3D block, int radius, float caveRoughness){
    	List<Coord3D> marked = new List<Coord3D>();
    	List<Coord3D> queue = new List<Coord3D>();
    	List<int> radiuses = new List<int>();

    	Coord3D currentBlock;
    	int currentRadius; 

    	queue.Add(block);
    	radiuses.Add(radius);

    	while(queue.Count != 0){
    		currentBlock = queue[0];
    		queue.RemoveAt(0);
    		currentRadius = radiuses[0];
    		radiuses.RemoveAt(0);

    		if(marked.Contains(currentBlock) || currentRadius == 0)
    			continue;

    		marked.Add(currentBlock);

    		// Set value
    		if(currentRadius <= 1 && Perlin.Noise(caveSeed*block.Sum()*0.76f, caveSeed*currentBlock.Sum()*0.41f) >= caveRoughness) // 0.9 = Cave Roughness
    			vd[currentBlock.x, currentBlock.y, currentBlock.z] = 1;

    		// Breadth-First Search
    		if(currentBlock.x < Chunk.chunkWidth-1){
    			queue.Add(new Coord3D(currentBlock, x:1));
    			radiuses.Add(currentRadius-1);
    		}
    		if(currentBlock.x > 1){
	    		queue.Add(new Coord3D(currentBlock, x:-1));
	    		radiuses.Add(currentRadius-1);
	    	}
	    	if(currentBlock.y < Chunk.chunkDepth-1){
    			queue.Add(new Coord3D(currentBlock, y:1));
    			radiuses.Add(currentRadius-1);
    		}
    		if(currentBlock.y > 1){
    			queue.Add(new Coord3D(currentBlock, y:-1));
    			radiuses.Add(currentRadius-1);
    		}
    		if(currentBlock.z < Chunk.chunkWidth-1){
    			queue.Add(new Coord3D(currentBlock, z:1));
    			radiuses.Add(currentRadius-1);
    		}
    		if(currentBlock.z > 1){
    			queue.Add(new Coord3D(currentBlock, z:-1));
    			radiuses.Add(currentRadius-1);
    		}

    		currentRadius -= 1;
    	}

    }

    /*
    Takes heightmaps of different map layers and combines them into a cacheVoxdata
    Layers are applied sequentially, so the bottom-most layer should be the first one
    blockCode is the block related to this layer.
    */
    // Takes cacheMaps and cacheBlockCodes and returns on cacheVoxdata
    private void ApplyHeightMaps(){
    	int size = Chunk.chunkWidth;
   		int i=0;

   		for(i=0;i<cacheMaps.Count;i++){
    		// Heightmap Drawing
	    	for(int x=0;x<size;x++){
	    		for(int z=0;z<size;z++){
	    			// If it's the first layer to be added
	    			if(i == 0){
		    			for(int y=0;y<Chunk.chunkDepth;y++){
		    				if(y <= cacheMaps[i][x,z])
		    					cacheVoxdata[x,y,z] = cacheBlockCodes[i];
		    				else
		    					cacheVoxdata[x,y,z] = 0;
		    			}
	    			}
	    			// If is not the first layer
	    			else{
		    			for(int y=cacheMaps[i-1][x,z]+1;y<=cacheMaps[i][x,z];y++){
		    				cacheVoxdata[x,y,z] = cacheBlockCodes[i];
		    			}  				
	    			}
	    		}
	    	}
	    }
	    cacheMaps.Clear();
	    cacheBlockCodes.Clear();
    }

    // Generates Pivot heightmaps
    private void GeneratePivots(int[,] selectedCache, int chunkX, int chunkZ, float xhash, float zhash, int octave=0, int groundLevel=20, int ceilingLevel=100){
    	int size = Chunk.chunkWidth;
    	int chunkXmult = chunkX * size;
    	int chunkZmult = chunkZ * size;
    	int i = 0;
    	int j = 0;

		// Heightmap Sampling
    	for(int x=chunkXmult;x<chunkXmult+size;x+=4){
    		j = 0;
    		for(int z=chunkZmult;z<chunkZmult+size;z+=4){
				selectedCache[i, j] = Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x*hashSeed/xhash, z*hashSeed/zhash))), 0, ceilingLevel);
    			j+=4;
    		}
    		i+=4;
    	}


        // Look Ahead to X+
        switch(biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ), worldSeed)){
            case "Plains":
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, false, octave);
                break;
            case "Grassy Highlands":
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, false, octave);
                break;
            default:
                print("Deu Merda");
                break;
        }

        // Look Ahead to Z+
        switch(biomeHandler.Assign(new ChunkPos(chunkX, chunkZ+1), worldSeed)){
            case "Plains":
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            case "Grassy Highlands":
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, true, octave);
                break;
            default:
                print("Deu Merda");
                break;
        }

        // Look ahead into XZ+
        switch(biomeHandler.Assign(new ChunkPos(chunkX+1, chunkZ+1), worldSeed)){
            case "Plains":
                MixPlainsBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            case "Grassy Highlands":
                MixGrassyHighlandsBorderPivots(selectedCache, chunkX, chunkZ, true, octave, corner:true);
                break;
            default:
                print("Deu Merda");
                break;
        }        
    }

    // Builds Lookahead pivots for biome border smoothing
    public void GenerateLookAheadPivots(int[,] selectedMap, int chunkX, int chunkZ, float xhash, float zhash, bool isSide, int groundLevel=0, int ceilingLevel=99){
        int size = Chunk.chunkWidth;
        chunkX *= size;
        chunkZ *= size;
        int i = 0;

        // If is looking ahead into X+ Chunk
        if(isSide){
            for(int x=chunkX; x<=chunkX+size; x+=4){
                selectedMap[i, size] = Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(x*hashSeed/xhash, (chunkZ+size)*hashSeed/zhash))), 0, ceilingLevel);
                i+=4;
            }
        }
        // If is looking ahead into Z+ Chunk
        else{
            for(int z=chunkZ; z<chunkZ+size; z+=4){ // Don't generate corner twice
                selectedMap[size, i] = Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise((chunkX+size)*hashSeed/xhash, z*hashSeed/zhash))), 0, ceilingLevel);
                i+=4;
            }
        }
    }

    // Builds Lookahead pivot of corner chunk for biome border smoothing
    public void GenerateLookAheadCorner(int[,] selectedMap, int chunkX, int chunkZ, float xhash, float zhash, int groundLevel=0, int ceilingLevel=99){
        selectedMap[Chunk.chunkWidth, Chunk.chunkWidth] = Mathf.Clamp(groundLevel + Mathf.FloorToInt((ceilingLevel-groundLevel)*(Perlin.Noise(((chunkX+1)*Chunk.chunkWidth)*hashSeed/xhash, ((chunkZ+1)*Chunk.chunkWidth)*hashSeed/zhash))), 0, ceilingLevel);
    }

    // Generates a flat map at Y level ceiling
    private void GenerateFlatMap(int[,] selectedCache, int ceiling){
        if(ceiling > Chunk.chunkDepth)
            ceiling = Chunk.chunkDepth;

        for(int x=0;x<Chunk.chunkWidth;x++){
            for(int z=0;z<Chunk.chunkWidth;z++){
                selectedCache[x,z] = ceiling;
            }
        }
    }


    // Applies bilinear interpolation to a given pivotmap
    // Takes any cache and returns on itself
    private void BilinearInterpolateMap(int[,] heightMap){
    	int size = Chunk.chunkWidth;
    	int interpX = 0;
    	int interpZ = 0;
    	float scaleX = 0f;
    	float scaleZ = 0f;

		// Bilinear Interpolation
    	for(int z=0;z<size;z++){
    		if(z%4 == 0){
    			interpZ+=4;
    			scaleZ = 0.25f;
    		}
    		for(int x=0;x<size;x++){
    			// If is a pivot in X axis
    			if(x%4 == 0){
    				interpX+=4;
    				scaleX = 0.25f;
    			}

    			heightMap[x,z] = Mathf.RoundToInt(((heightMap[interpX-4, interpZ-4])*(1-scaleX)*(1-scaleZ)) + (heightMap[interpX, interpZ-4]*scaleX*(1-scaleZ)) + (heightMap[interpX-4, interpZ]*scaleZ*(1-scaleX)) + (heightMap[interpX, interpZ]*scaleX*scaleZ));
    			scaleX += 0.25f;

    		}
    		interpX = 0;
    		scaleX = 0;
    		scaleZ += 0.25f;
    	}	
    }

    // Quick adds all elements in heightMap
    // This makes it easy to get a layer below surface blocks
    // Takes any cache and returns on cacheNumber
    private void AddFromMap(int[,] map, int val, int cacheNumber=2){

    	if(cacheNumber == 1){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap[x,z] = map[x,z] + val;
	    		}
	    	}
    	}

    	else if(cacheNumber == 2){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap2[x,z] = map[x,z] + val;
	    		}
	    	}
	    }
	    else if(cacheNumber == 3){
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap3[x,z] = map[x,z] + val;
	    		}
	    	}	    	
	    }
	    else{
	    	for(int x=0;x<Chunk.chunkWidth;x++){
	    		for(int z=0;z<Chunk.chunkWidth;z++){
	    			cacheHeightMap4[x,z] = map[x,z] + val;
	    		}
	    	}
	    }
    }


    // Applies Octaves to Pivot Map
    private void CombinePivotMap(int[,] a, int[,] b){
        for(int x=0;x<=Chunk.chunkWidth;x+=4){
            for(int z=0;z<=Chunk.chunkWidth;z+=4){
                a[x,z] = Mathf.FloorToInt((a[x,z] + b[x,z])/2);
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
		int xhash = 10;
		int zhash = 30;

        
		// Grass Heightmap is hold on Cache 1 and first octave on Cache 2    
		GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, octave:0, groundLevel:20, ceilingLevel:25);
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*1.712f, zhash*2.511f, octave:1, groundLevel:18, ceilingLevel:30);
        CombinePivotMap(cacheHeightMap, cacheHeightMap2);

		BilinearInterpolateMap(cacheHeightMap);

		// Underground is hold on Cache 2
		AddFromMap(cacheHeightMap, -5);

		// Adds Cache 2 to pipeline
		cacheMaps.Add(cacheHeightMap2);
		cacheBlockCodes.Add(3);

		// Dirt is hold on Cache 3
		AddFromMap(cacheHeightMap, -1, cacheNumber:3);

        GenerateFlatMap(cacheHeightMap4, 20);

		// Adds rest to pipeline
		cacheMaps.Add(cacheHeightMap3);
		cacheBlockCodes.Add(2);
		cacheMaps.Add(cacheHeightMap);
		cacheBlockCodes.Add(1);
        cacheMaps.Add(cacheHeightMap4);
        cacheBlockCodes.Add(6);

		// Adds to cacheVoxdata
		ApplyHeightMaps();
        
        // Cave Systems
        GenerateRidgedMultiFractal3D(chunkX, chunkZ, caveSeed*0.012f, caveSeed*0.007f, caveSeed*0.0089f, 0.35f, ceiling:20, maskThreshold:0.7f);
        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), new VoxelData(cacheTurbulanceMap), upper:20);
    }

    // Inserts Plains biome border pivots on another selectedHeightMap
    private void MixPlainsBorderPivots(int[,] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
        int xhash = 10;
        int zhash = 30;

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
        int xhash = 10;
        int zhash = 30;

        GeneratePivots(cacheHeightMap, chunkX, chunkZ, xhash, zhash, octave:0, groundLevel:30, ceilingLevel:60);
        GeneratePivots(cacheHeightMap2, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, octave:1, groundLevel:30, ceilingLevel:60);
        CombinePivotMap(cacheHeightMap, cacheHeightMap2);
        BilinearInterpolateMap(cacheHeightMap);

        // Underground is hold on Cache 2
        AddFromMap(cacheHeightMap, -5);

        // Adds Cache 2 to pipeline
        cacheMaps.Add(cacheHeightMap2);
        cacheBlockCodes.Add(3);

        // Dirt is hold on Cache 3
        AddFromMap(cacheHeightMap, -1, cacheNumber:3);

        GenerateFlatMap(cacheHeightMap4, 42); //35

        // Adds rest to pipeline
        cacheMaps.Add(cacheHeightMap3);
        cacheBlockCodes.Add(2);
        cacheMaps.Add(cacheHeightMap);
        cacheBlockCodes.Add(1);
        cacheMaps.Add(cacheHeightMap4);
        cacheBlockCodes.Add(6);

        // Adds to cacheVoxdata
        ApplyHeightMaps();

        // Cave Systems
        GenerateRidgedMultiFractal3D(chunkX, chunkZ, caveSeed*0.012f, caveSeed*0.007f, caveSeed*0.0089f, 0.35f, ceiling:40, maskThreshold:0.7f);
        return VoxelData.CutUnderground(new VoxelData(cacheVoxdata), new VoxelData(cacheTurbulanceMap), upper:40);
    }

    // Inserts Plains biome border pivots on another selectedHeightMap
    private void MixGrassyHighlandsBorderPivots(int[,] selectedMap, int chunkX, int chunkZ, bool isSide, int octave, bool corner=false){
        int xhash = 10;
        int zhash = 30;

        if(!corner){
            if(octave == 0)
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash, zhash, isSide, groundLevel:30, ceilingLevel:60);
            else
                GenerateLookAheadPivots(selectedMap, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, isSide, groundLevel:30, ceilingLevel:60);            
        }
        else{
            if(octave == 0)
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash, zhash, groundLevel:30, ceilingLevel:60);
            else
                GenerateLookAheadCorner(selectedMap, chunkX, chunkZ, xhash*0.712f, zhash*0.2511f, groundLevel:30, ceilingLevel:60);

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
