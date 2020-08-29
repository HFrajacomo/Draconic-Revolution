using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRaycast : MonoBehaviour
{
	public ChunkLoader loader;
	public Transform cam;
	public float reach = 4.0f;
	public float step = 0.04f;
	private Vector3 position;
	private Vector3 direction;
	private Vector3 cachePos;
	public CastCoord current;
	private CastCoord lastCoord;

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;
  public MainControllerManager control;

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
      if(control.primaryAction){
      	BreakBlock();
        control.primaryAction = false;
      }

      // Click to place block
      if(control.secondaryAction){
      	PlaceBlock(-1);
        control.secondaryAction = false;
      }

      // Click for interact
      if(control.interact){
        Interact();
        control.interact = false;
      }

    }

    // Detects hit of solid block
    public bool HitSolid(CastCoord coords){
    	ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ);
      int blockID = loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

      // If hits a full block
      if(blockID >= 0){
      	if(loader.chunks.ContainsKey(ck)){
    			if(loader.blockBook.blocks[blockID].solid){
    				return true;
    			}
      	}
      }
        // If hits an Asset
      else{
        if(loader.chunks.ContainsKey(ck)){
          blockID = (blockID * -1) - 1;
          if(loader.blockBook.objects[blockID].solid){
            return true;
          }
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
      int translatedBlockCode;
      bool isAsset;

      // Encodes for Block Mode
      if(blockCode >= 0){
        isAsset = false;
        translatedBlockCode = blockCode;

        // Won't happen if not raycasting something or if block is in player's body or head
        if(!current.active || (CastCoord.Eq(lastCoord, playerHead) && loader.blockBook.blocks[translatedBlockCode].solid) || (CastCoord.Eq(lastCoord, playerBody) && loader.blockBook.blocks[blockCode].solid)){
          return;
        }
      }
      // Encodes for Asset Mode
      else{
        translatedBlockCode = (blockCode * -1) - 1;
        isAsset = true;

        // Won't happen if not raycasting something or if block is in player's body or head
        if(!current.active || (CastCoord.Eq(lastCoord, playerHead) && loader.blockBook.objects[translatedBlockCode].solid) || (CastCoord.Eq(lastCoord, playerBody) && loader.blockBook.objects[translatedBlockCode].solid)){
          return;
        }
      }

    	ChunkPos toUpdate = new ChunkPos(lastCoord.chunkX, lastCoord.chunkZ);

      // Actually places block/asset into terrain
		  loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
      loader.chunks[toUpdate].BuildChunk();

      // Applies OnPlace operation for given block
      if(!isAsset)
        loader.blockBook.blocks[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ);
      else
        loader.blockBook.objects[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ);
      
    }

    // Triggers Blocktype.OnInteract()
    private void Interact(){
      if(!current.active)
        return;

      ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);

      int blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);
      Blocks selectedBlock = loader.blockBook.blocks[blockCode];

      // Actual handling of message
      int callback = selectedBlock.OnInteract(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
      CallbackHandler(callback, toUpdate);
    }

    /*
    Main Callback function for block interactions
    (REFER TO THESE CODES WHENEVER ADDING NEW BLOCK INTERACTIONS)
    (MAY BE NEEDED IN ORDER TO IMPLEMENT NEW POST HANDLERS FOR NEW BLOCKS)
    */
    private void CallbackHandler(int code, ChunkPos targetChunk){
      // 0: No further actions necessary
      if(code == 0)
        return;
      // 1: Interaction forces the target chunk to reload/rebuild
      else if(code == 1)
          loader.chunks[targetChunk].BuildChunk();

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