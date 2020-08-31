using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRaycast : MonoBehaviour
{
	public ChunkLoader loader;
	public Transform cam;
	public float reach = 4.0f;
	public float step = 0.025f;
	private Vector3 position;
	private Vector3 direction;
	private Vector3 cachePos;
	public CastCoord current;
	private CastCoord lastCoord;

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;
  public MainControllerManager control;

  //DEBUG
  bool debug = false;

	/*
	0 = X+ => side
	1 = Z- => side
	2 = X- => side
	3 = Z+ => side
	4 = Y- => ceiling
	5 = Y+ => ground
	*/
	public int facing;

    public void ClearLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }

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
      cachePos = position;

      // Shoots Raycast
      while(traveledDistance <= reach){
      	cachePos = cachePos + (direction*step);
      	current = new CastCoord(cachePos);

      	// Out of bounds control
      	if(current.blockY >= Chunk.chunkDepth || current.blockY < 0f){
      		return;
      	}

      	// Checks for solid block hit
        // Checks for hit
      	if(HitAll(current)){ // HitSolid
      		FOUND = true;
      		break;
      	}

        traveledDistance += step;
      }

      if(!FOUND){
      	current = new CastCoord(false);
      }
      else
        lastCoord = new CastCoord(cachePos - direction*step);
      
      facing = current - lastCoord;

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

      if(debug)
        ClearLog();
      else{
        print(current.ToString());
      }

      Debug.DrawLine(position, cachePos, Color.red);
      debug = !debug;

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

    // Detects hit in any block except air
    public bool HitAll(CastCoord coords){
      ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ);
      int blockID = loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

      // If hits something
      if(blockID != 0)
        if(loader.chunks.ContainsKey(ck))
          return true;
      return false;
    }

    // Block Breaking mechanic
    private void BreakBlock(){
    	if(!current.active){
    		return;
    	}

    	ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
      int blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);


		  loader.chunks[toUpdate].data.SetCell(current.blockX, current.blockY, current.blockZ, 0);
    	loader.chunks[toUpdate].BuildChunk();

      // Triggers OnBreak
      if(blockCode >= 0)
        loader.blockBook.blocks[blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
      else
        loader.blockBook.objects[(blockCode*-1)-1].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
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

      // Checks if specific block has specific placement rules that may hinder the placement
      if(!isAsset){
        if(!loader.blockBook.blocks[translatedBlockCode].PlacementRule(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader)){
          return;
        }
      }
      else{
        if(!loader.blockBook.objects[translatedBlockCode].PlacementRule(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader)){
          return;
        }
      }

      // Actually places block/asset into terrain
		  loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
      loader.chunks[toUpdate].BuildChunk();

      // Applies OnPlace operation for given block
      if(!isAsset)
        loader.blockBook.blocks[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, loader);
      else
        loader.blockBook.objects[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, loader);
      
    }

    // Triggers Blocktype.OnInteract()
    private void Interact(){
      if(!current.active)
        return;

      ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
      int callback;
      int blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);
      
      
      if(blockCode >= 0)
        callback = loader.blockBook.blocks[blockCode].OnInteract(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
      else
        callback = loader.blockBook.objects[(blockCode*-1)-1].OnInteract(toUpdate, current.blockX, current.blockY, current.blockZ, loader);

      // Actual handling of message
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
        Vector3 nMark = new Vector3(mark.x, mark.y, mark.z);

        // X Processing
        if(nMark.x >= 0)
          nMark.x = (int)Math.Round(nMark.x, MidpointRounding.AwayFromZero); //floor
        else
          nMark.x = (int)(nMark.x - 0.5f); // ceil

        chunkX = Mathf.FloorToInt(nMark.x/Chunk.chunkWidth); // floor


        if(chunkX >= 0)
          blockX = Mathf.FloorToInt(nMark.x%Chunk.chunkWidth); // int cast
        else
          blockX = Mathf.CeilToInt(((Chunk.chunkWidth*-chunkX)+nMark.x)%Chunk.chunkWidth); // int cast

        // Z Processing
        if(nMark.z >= 0)
          nMark.z = (int)Math.Round(nMark.z, MidpointRounding.AwayFromZero);
        else
          nMark.z = (int)(nMark.z - 0.5f);

        chunkZ = Mathf.FloorToInt(nMark.z/Chunk.chunkWidth);

        if(chunkZ >= 0)
          blockZ = Mathf.FloorToInt(nMark.z%Chunk.chunkWidth);
        else
          blockZ = Mathf.CeilToInt(((Chunk.chunkWidth*-chunkZ)+nMark.z)%Chunk.chunkWidth);



        blockY = Mathf.RoundToInt(nMark.y); // Round

     		active = true;

        if(blockZ >= 16)
          print("Fudeu: " + this.ToString());
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

        if(y == -1)
          return 4;
        else if(y == 1)
          return 5;
   			else if(x == -1)
   				return 2;
   			else if(x == 1)
   				return 0;
   			else if(z == -1)
   				return 1;
   			else if(z == 1)
   				return 3;
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