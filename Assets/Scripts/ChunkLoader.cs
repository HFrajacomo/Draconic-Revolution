using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
	public GameObject prefabChunk;
	public int renderDistance = 0;
	public Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>();
	public Transform player;
	public ChunkPos currentChunk;
	public ChunkPos newChunk;
	public List<ChunkPos> toLoad = new List<ChunkPos>();
	public List<ChunkPos> toUnload = new List<ChunkPos>();

	public int worldSeed = 1; // 6 number integer
	public float hashSeed;
	public float caveSeed;

	// Cave 3D Generation
	public float x_mask_div=575;
	public float y_mask_div=200;
	public float z_mask_div=575;
	public float threshold = 0.33f;
	public float thresholdMask = 0.63f;

	public int loadtick = 3;
	private bool isCreatingChunks;

    // Start is called before the first frame update
    void Start()
    {
    	/*
    	hashSeed can also be considered the aggressiveness of a terrain.
    	Smaller dividers will generate hilly areas, while bigger dividers will generate plains.
    	*/
    	if(worldSeed == 0){
    		worldSeed = 1;
    	}

    	hashSeed = TerrainSeedFunction(worldSeed);
    	caveSeed = LogSeedFunction(worldSeed);

		player = GameObject.Find("Character").GetComponent<Transform>();
		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);
		GetChunks(true);
    }

    void Update(){
    	GetChunks(false); 

    	UnloadChunk();

    	if(!isCreatingChunks && toLoad.Count > 0)
    		StartCoroutine("UpdateChunks");
    }

    IEnumerator UpdateChunks(){
    	isCreatingChunks = true;
    	while(toLoad.Count > 0){
    		BuildChunk(toLoad[0]);
    		toLoad.RemoveAt(0);
    		yield return null;
    	}
    	isCreatingChunks = false;
    }

    // Builds Chunk Terrain and adds to active chunks dict
    private void BuildChunk(ChunkPos pos){
    	Chunk chunk;
    	GameObject chunkGO = Instantiate(prefabChunk, new Vector3(pos.x*Chunk.chunkWidth, 0, pos.z*Chunk.chunkWidth), Quaternion.identity);
    	chunk = chunkGO.GetComponent<Chunk>();
		//chunk.BuildOnVoxelData(VoxelData.CutUnderground(NotchInterpolation(pos.x, pos.z, groundLevel:20), GeneratePerlin3D(pos.x, pos.z, groundLevel:35)));
    	//chunk.BuildOnVoxelData(NotchInterpolation(pos.x, pos.z));
    	chunk.BuildOnVoxelData(GeneratePlainsBiome(pos.x, pos.z));
    	//chunk.BuildOnVoxelData(GeneratePerlin3D(pos.x, pos.z));
    	chunk.BuildChunk();
    	chunk.gameObject.SetActive(true);
    	chunks.Add(pos, chunk);
    }

    // Erases loaded chunks dictionary
    private void ClearAllChunks(){
    	foreach(var item in chunks.Keys){
    		Destroy(chunks[item].gameObject);
    		chunks.Remove(item);
    	}
    }

    // Loads a chunk per frame from the Loading Buffer
    private void LoadChunk(){
    	if(toLoad.Count != 0){
    		BuildChunk(toLoad[0]);
    		toLoad.RemoveAt(0);
    	}
    }

    // Unloads a chunk per frame from the Unloading Buffer
    private void UnloadChunk(){
    	if(toUnload.Count != 0){
    		ChunkPos popChunk = toUnload[0];
    		Destroy(chunks[popChunk].gameObject);
    		toUnload.RemoveAt(0);
    		chunks.Remove(popChunk);    		
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
					BuildChunk(new ChunkPos(newChunk.x+x, newChunk.z+z));
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
    	else{
		    for(int x=-renderDistance; x<=renderDistance;x++){
		      	for(int z=-renderDistance; z< renderDistance;z++){
					BuildChunk(new ChunkPos(newChunk.x+x, newChunk.z+z));
		      	}
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


    // DEPRECATED
    /*
    Generates a Perlian VoxelData for chunks using chunkX and chunkY
    This process has been optimized with "Notch Interpolation". A way of
    	reducing the amount of Perlin Noise calls, and using linear interpolation
    	to guess the heights in between Noise samples.
    The Notch Interpolation also causes terrain to be smoother.
    */
    private VoxelData NotchInterpolation(int chunkX, int chunkZ, int groundLevel=1){
    	int size = Chunk.chunkWidth;
    	chunkX *= size;
    	chunkZ *= size;
    	int i = 0;
    	int j = 0;
    	int[,,] voxdata = new int[size, Chunk.chunkDepth, size];
    	int[,] heightMap = new int[size+1,size+1]; 


		// Heightmap Sampling
		// Chunk Look-ahead implemented as a <= check in for
    	for(int x=chunkX;x<=chunkX+size;x+=4){
    		j = 0;
    		for(int z=chunkZ;z<=chunkZ+size;z+=4){
				heightMap[i, j] = groundLevel + Mathf.FloorToInt((Chunk.chunkDepth-groundLevel)*(Perlin.Noise(x*hashSeed/10, z*hashSeed/30)));
    			j+=4;
    		}
    		i+=4;
    	}

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

    	// Heightmap Drawing
    	for(int x=0;x<size;x++){
    		for(int z=0;z<size;z++){
    			for(int y=0;y<Chunk.chunkDepth;y++){
    				if(y <= heightMap[x,z])
    					voxdata[x,y,z] = 1;
    				else
    					voxdata[x,y,z] = 0;
    			}
    		}
    	}


    	return new VoxelData(voxdata);
    }

    // Debug Feature
    private void ToFile(int[,] heightMap, string filename){
    	string a = "";
    	for(int x=15;x>=0;x--){
    		for(int y=0;y<16;y++){
    			a += (heightMap[x,y] + "\t");
    		}
    		a += "\n";
    	}
    	System.IO.File.WriteAllText(filename, a);
    }


    
    private VoxelData GeneratePerlin3D(int chunkX, int chunkZ, int groundLevel=20){
    	int size = Chunk.chunkWidth;
    	chunkX *= size;
    	chunkZ *= size;
    	int i = 0;
    	int j = 0;
    	int[,,] voxdata = new int[size, Chunk.chunkDepth, size];

    	for(int x=chunkX;x<chunkX+size;x++){
    		j = 0;
    		for(int z=chunkZ;z<chunkZ+size;z++){

    			for(int y=0;y<Chunk.chunkDepth;y++){

       				float val = Perlin.Noise(x*caveSeed, y*caveSeed, z*caveSeed);
       				float mask = (0.2f*Perlin.Noise(x*y*caveSeed) + 0.8f)*Perlin.Noise(x*caveSeed/x_mask_div, y*caveSeed/y_mask_div, z*caveSeed/z_mask_div);
    				
    				if(val >= threshold && mask >= thresholdMask && y <= groundLevel)
    					voxdata[i,y,j] = 1;
    				else
    					voxdata[i,y,j] = 0;
    			}
    			j++;
    		}
    		i++;
    	}

    	return new VoxelData(voxdata);

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
    Takes heightmaps of different map layers and combines them into a voxdata
    Layers are applied sequentially, so the bottom-most layer should be the first one
    blockCode is the block related to this layer.
    */
    private int [,,] ApplyHeightMaps(List<int[,]> heightMapArray, List<int> blockCode){
    	int size = Chunk.chunkWidth;
    	int[,,] voxdata = new int[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
   		int[,] prevMap = null;
   		int i=0;

    	foreach(int[,] heightMap in heightMapArray){

    		// Heightmap Drawing
	    	for(int x=0;x<size;x++){
	    		for(int z=0;z<size;z++){
	    			// If it's the first layer to be added
	    			if(prevMap == null){
		    			for(int y=0;y<Chunk.chunkDepth;y++){
		    				if(y <= heightMap[x,z])
		    					voxdata[x,y,z] = blockCode[i];
		    				else
		    					voxdata[x,y,z] = 0;
		    			}
	    			}
	    			// If is not the first layer
	    			else{
		    			for(int y=prevMap[x,z]+1;y<=heightMap[x,z];y++){
		    				voxdata[x,y,z] = blockCode[i];
		    			}  				
	    			}
	    		}
	    	}
	    	i++;
	    	prevMap = heightMap;
	    }

    	return voxdata;
    }

    // Generates Pivot heightmaps
    private int[,] GeneratePivots(int chunkX, int chunkZ, float xhash, float zhash, int groundLevel=20, int ceilingLevel=999){
    	int size = Chunk.chunkWidth;
    	chunkX *= size;
    	chunkZ *= size;
    	int i = 0;
    	int j = 0;
    	int[,] heightMap = new int[size+1,size+1];


		// Heightmap Sampling
		// Chunk Look-ahead implemented as a <= check in for
    	for(int x=chunkX;x<=chunkX+size;x+=4){
    		j = 0;
    		for(int z=chunkZ;z<=chunkZ+size;z+=4){
				heightMap[i, j] = Mathf.Clamp(groundLevel + Mathf.FloorToInt((Chunk.chunkDepth-groundLevel)*(Perlin.Noise(x*hashSeed/xhash, z*hashSeed/zhash))), 0, ceilingLevel);
    			j+=4;
    		}
    		i+=4;
    	}

    	return heightMap;
    }


    // Applies bilinear interpolation to a given pivotmap
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
    private int[,] AddFromMap(int[,] map, int a){
    	int[,] newMap = new int[Chunk.chunkWidth, Chunk.chunkWidth];

    	for(int x=0;x<Chunk.chunkWidth;x++){
    		for(int z=0;z<Chunk.chunkWidth;z++){
    			newMap[x,z] = map[x,z] + a;
    		}
    	}

    	return newMap;
    }

    // Generates plains biome chunk
    /*
    Layer 1: Grass groundLevel = 20
    Layer 2: Dirt groundLevel = 19
    Layer 3: Stone groundLevel = 17
    */
	public VoxelData GeneratePlainsBiome(int chunkX, int chunkZ){
		List<int[,]> maps = new List<int[,]>();
		List<int> blockCodes = new List<int>();

		// Hash values for Plains Biomes
		int xhash = 10;
		int zhash = 30;

		// Grass
		int[,] surface = GeneratePivots(chunkX, chunkZ, xhash, zhash, groundLevel:20);
		BilinearInterpolateMap(surface);

		// Dirt
		int[,] dirt = AddFromMap(surface, -1);

		// Underground
		int[,] underground = AddFromMap(surface, -5);

		maps.Add(underground);
		blockCodes.Add(3);
		maps.Add(dirt);
		blockCodes.Add(2);
		maps.Add(surface);
		blockCodes.Add(1);

		return new VoxelData(ApplyHeightMaps(maps, blockCodes));
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
	*/
	public int dir(){
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
