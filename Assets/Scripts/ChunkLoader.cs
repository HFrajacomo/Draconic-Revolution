using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
	public GameObject prefabChunk;
	public const int renderDistance = 2;
	public Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>();
	public Transform player;
	public ChunkPos currentChunk;
	public ChunkPos newChunk;

	public float worldSeed = 0.1f;
	public float hashSeed;

    // Start is called before the first frame update
    void Start()
    {
    	/*
    	hashSeed can also be considered the aggressiveness of a terrain.
    	Smaller dividers will generate hilly areas, while bigger dividers will generate plains.
    	*/
    	hashSeed = LogisticFunction(worldSeed) / 30;

		player = GameObject.Find("Character").GetComponent<Transform>();
		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);
		LoadChunks(true);
    }

    void Update(){
    	LoadChunks(false);
    }

    // Builds Chunk Terrain and adds to active chunks dict
    private void BuildChunk(ChunkPos pos){
    	Chunk chunk;
    	GameObject chunkGO = Instantiate(prefabChunk, new Vector3(pos.x*Chunk.chunkWidth, 0, pos.z*Chunk.chunkWidth), Quaternion.identity);
    	chunk = chunkGO.GetComponent<Chunk>();
    	//chunk.GenerateRandomChunk();
    	chunk.BuildOnVoxelData(GeneratePerlin2D(pos.x, pos.z));
    	chunk.BuildChunk();
    	chunk.gameObject.SetActive(true);
    	chunks.Add(pos, chunk);
    }

    private void LoadChunks(bool reload){
		int playerX = Mathf.FloorToInt(player.position.x / Chunk.chunkWidth);
		int playerZ = Mathf.FloorToInt(player.position.z / Chunk.chunkWidth);
		newChunk = new ChunkPos(playerX, playerZ);

    	// Reload all Chunks nearby
    	if(reload){
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
    			Destroy(chunks[popChunk].gameObject);
    			chunks.Remove(popChunk);
    			BuildChunk(new ChunkPos(newChunk.x+renderDistance, newChunk.z+i));
    		}
    	}
    	else if(diff == 2){ // West
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+renderDistance+1, newChunk.z+i);
    			Destroy(chunks[popChunk].gameObject);
    			chunks.Remove(popChunk);
    			BuildChunk(new ChunkPos(newChunk.x-renderDistance, newChunk.z+i));
    		}
    	}
    	else if(diff == 1){ // South
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance+1);
    			Destroy(chunks[popChunk].gameObject);
    			chunks.Remove(popChunk);
   				BuildChunk(new ChunkPos(newChunk.x+i, newChunk.z-renderDistance));
    		}    		
    	}
    	else if(diff == 3){ // North
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			ChunkPos popChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance-1);
    			Destroy(chunks[popChunk].gameObject);
    			chunks.Remove(popChunk);
    			BuildChunk(new ChunkPos(newChunk.x+i, newChunk.z+renderDistance));
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

    // Calculates the logistic function
    private float LogisticFunction(float t){
    	return 1/(1+Mathf.Exp(-t));
    }
 
    /*
    Generates a Perlian VoxelData for chunks using chunkX and chunkY
    */
    private VoxelData GeneratePerlin2D(int chunkX, int chunkZ){
    	int size = Chunk.chunkWidth;
    	chunkX *= size;
    	chunkZ *= size;
    	int i = 0;
    	int j = 0;
    	int[,,] voxdata = new int[size, Chunk.chunkDepth, size];

    	for(int x=chunkX;x<chunkX+size;x++){
    		j = 0;
    		for(int z=chunkZ;z<chunkZ+size;z++){
    			// Heightmap Calculation
    			int height = Mathf.FloorToInt(Chunk.chunkDepth*(Mathf.PerlinNoise((x+i)*hashSeed, (z+j)*hashSeed)));

    			for(int y=0;y<Chunk.chunkDepth;y++){
    				if(y <= height)
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
