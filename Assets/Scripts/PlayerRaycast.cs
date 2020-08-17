using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRaycast : MonoBehaviour
{
	public ChunkLoader loader;
	public Transform cam;
	public float reach = 3.6f;
	public float step = 0.1f;
	private Vector3 position;
	private Vector3 direction;
	private Vector3 cachePos;
	public CastCoord current;
	private CastCoord lastCoord;

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;

	/*
	0 = X+
	1 = Z-
	2 = X-
	3 = Z+
	4 = Y-
	5 = Y+
	*/
	public int facing;

    // Update is called once per frame
    void Update()
    {
    	if(!loader.WORLD_GENERATED)
    		return;

    	// Updates player block position
    	playerHead = new CastCoord(cam.position);
    	playerBody = new CastCoord(cam.position);
    	playerBody.blockY -= 1;

    	float traveledDistance = 0f;
    	lastCoord = new CastCoord(false);
    	bool FOUND = false;


    	// Raycast Detection
    	position = cam.position;
        direction = Vector3.Normalize(cam.forward);

        // Shoots Raycast
        while(traveledDistance <= reach){
        	cachePos = position + direction*traveledDistance;
        	current = new CastCoord(cachePos);

        	// Out of bounds control
        	if(current.blockY >= Chunk.chunkDepth || current.blockY < 0f){
        		return;
        	}

        	// Checks for solid block hit
        	if(HitSolid(current)){
        		facing = current - lastCoord;
        		FOUND = true;
        		break;
        	}
        	else{
        		lastCoord = current;
        		traveledDistance += step;
        	}
        }
        if(!FOUND){
        	current = new CastCoord(false);
        }

        // Click to break block
        if(Input.GetButtonDown("Fire1")){
        	BreakBlock();
        }

        // Click to place block
        if(Input.GetButtonDown("Fire2")){
        	PlaceBlock(4);
        }

    }

    // Detects hit of solid block
    private bool HitSolid(CastCoord coords){
    	ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ);

    	if(loader.chunks.ContainsKey(ck)){
  			if(loader.blockBook.blocks[loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ)].solid){
  				return true;
  			}

    	}
    	return false;
    }

    // Block Breaking mechanic
    private void BreakBlock(){
    	if(!current.active){
    		return;
    	}

    	ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);

		loader.chunks[toUpdate].data.SetCell(current.blockX, current.blockY, current.blockZ, 0);
    	loader.chunks[toUpdate].BuildChunk();
    }

    // Block Placing mechanic
    private void PlaceBlock(int blockCode){
    	// Won't happen if not raycasting something or if block is in player's body or head
    	if(!current.active || CastCoord.Eq(lastCoord, playerHead) || CastCoord.Eq(lastCoord, playerBody)){
    		return;
    	}

    	ChunkPos toUpdate = new ChunkPos(lastCoord.chunkX, lastCoord.chunkZ);

		loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
    	loader.chunks[toUpdate].BuildChunk();    	
    }


   public struct CastCoord{
   		public int chunkX;
   		public int chunkZ;
   		public int blockX;
   		public int blockY;
   		public int blockZ;
   		public bool active;

   		public CastCoord(bool a){
   			chunkX = 0;
   			chunkZ = 0;
   			blockX = 0;
   			blockZ = 0;
   			blockY = 0;
   			active = false;
   		}

   		public CastCoord(Vector3 mark){
   			chunkX = Mathf.FloorToInt(mark.x/Chunk.chunkWidth);
   			chunkZ = Mathf.FloorToInt(mark.z/Chunk.chunkWidth);

   			// X Processing
   			if(chunkX >= 0)
   				blockX = Mathf.RoundToInt(mark.x%Chunk.chunkWidth);
   			else
   				blockX = Mathf.RoundToInt(((Chunk.chunkWidth*-chunkX)+mark.x)%Chunk.chunkWidth);
			if(blockX == Chunk.chunkWidth)
				blockX = 0;


			// Z Processing
   			if(chunkZ >= 0)
   				blockZ = Mathf.RoundToInt(mark.z%Chunk.chunkWidth);
   			else
   				blockZ = Mathf.RoundToInt(((Chunk.chunkWidth*-chunkZ)+mark.z)%Chunk.chunkWidth);
			if(blockZ == Chunk.chunkWidth)
				blockZ = 0;

   			blockY = Mathf.RoundToInt(mark.y);
   			active = true;
   		}

   		public override string ToString(){
   			return "ChunkX: " + chunkX + "\tChunkZ: " + chunkZ + "\tX, Y, Z: " + blockX + ", " + blockY + ", " + blockZ;
   		}

   		public static int operator-(CastCoord a, CastCoord b){
   			int x,y,z;

   			if(!b.active){
   				return -1;
   			}

   			x = (a.chunkX*Chunk.chunkWidth+a.blockX) - (b.chunkX*Chunk.chunkWidth+b.blockX);
   			z = (a.chunkZ*Chunk.chunkWidth+a.blockZ) - (b.chunkZ*Chunk.chunkWidth+b.blockZ);
   			y = a.blockY - b.blockY;

			/*
			0 = X+
			1 = Z-
			2 = X-
			3 = Z+
			4 = Y-
			5 = Y+
			*/

   			if(x == -1)
   				return 2;
   			else if(x == 1)
   				return 0;
   			else if(z == -1)
   				return 1;
   			else if(z == 1)
   				return 3;
   			else if(y == -1)
   				return 4;
   			else if(y == 1)
   				return 5;
   			else
   				return -1;
   		}

   		public static bool Eq(CastCoord a, CastCoord b){
   			if(a.chunkX != b.chunkX || a.chunkZ != b.chunkZ || a.blockX != b.blockX || a.blockY != b.blockY || a.blockZ != b.blockZ)
   				return false;
   			return true;

   		}
   }


}