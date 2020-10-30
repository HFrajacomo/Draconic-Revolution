using System;
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

  // DEBUG
  public ushort blockToBePlaced = 6;
  public ushort placeState = 0;

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;
  public MainControllerManager control;

  // Cached
  private BUDSignal cachedBUD;

	/*
	0 = X+ => side
	1 = Z- => side
	2 = X- => side
	3 = Z+ => side
	4 = Y- => ceiling
	5 = Y+ => ground
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
      	PlaceBlock(blockToBePlaced, placeState);
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
      ushort blockID = loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

      // If hits a full block
      if(blockID <= ushort.MaxValue/2){
      	if(loader.chunks.ContainsKey(ck)){
    			if(loader.blockBook.blocks[blockID].solid){
    				return true;
    			}
      	}
      }
        // If hits an Asset
      else{
        if(loader.chunks.ContainsKey(ck)){
          blockID = (ushort)(ushort.MaxValue - blockID);
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
      ushort blockID = loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

      // If hits something
      if(blockID != 0)
        if(loader.chunks.ContainsKey(ck)){
          //print(loader.blockBook.Get(blockID).name + " : " + loader.chunks[ck].metadata.GetMetadata(coords.blockX, coords.blockY, coords.blockZ).state);

          return true;
        }
      return false;
    }

    // Block Breaking mechanic
    private void BreakBlock(){
    	if(!current.active){
    		return;
    	}

    	ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
      ushort blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);

      // If doesn't has special break handling
      if(!loader.blockBook.CheckCustomBreak(blockCode)){
        // Triggers OnBreak
        if(blockCode >= 0)
          loader.blockBook.blocks[blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
        else
          loader.blockBook.objects[(blockCode*-1)-1].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);

        // Actually breaks new block and updates chunk
        loader.chunks[toUpdate].data.SetCell(current.blockX, current.blockY, current.blockZ, 0);
        loader.chunks[toUpdate].metadata.CreateNull(current.blockX, current.blockY, current.blockZ);

        // Passes "break" block update to neighboring blocks IF object doesn't implement it OnBreak
        EmitBlockUpdate("break", current.GetWorldX(), current.GetWorldY(), current.GetWorldZ(), 0, loader);
        
        if(blockCode >= 0)
          loader.budscheduler.ScheduleReload(toUpdate, 0);
        else
          loader.chunks[toUpdate].assetGrid.Remove(current.blockX, current.blockY, current.blockZ);
      }
      // If has special break handlings
      else{

        if(blockCode >= 0)
          loader.blockBook.blocks[blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
        else
          loader.blockBook.objects[(blockCode*-1)-1].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
      }

    }

    // Block Placing mechanic
    private void PlaceBlock(ushort blockCode, ushort? state){
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

      // Applies OnPlace operation for given block
      if(!isAsset)
        loader.blockBook.blocks[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
      else{
        loader.blockBook.objects[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
      }
 

      // If doesn't have special place handling
      if(!loader.blockBook.CheckCustomPlace(blockCode)){
        // Actually places block/asset into terrain
  		  loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
        if(state != null){
          // SETS STATE
          //loader.chunks[toUpdate].metadata.GetMetadata(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ).state = state;
        }

        // Sends Reload Request to BUDScheduler
        if(blockCode >= 0)
          loader.budscheduler.ScheduleReload(toUpdate, 0);
        // Sends a AddDraw call to chunk's AssetGrid
        else
          loader.chunks[toUpdate].assetGrid.AddDraw(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode, state, loader);
      }

      // If has special handling
      else{
        // Actually places block/asset into terrain
        loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
        if(state != null){
          loader.chunks[toUpdate].metadata.GetMetadata(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ).state = state;
        }

        if(blockCode >= 0)
          loader.blockBook.blocks[blockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
        else
          loader.blockBook.objects[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);

      }
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
      CallbackHandler(callback, toUpdate, current, facing);
    }

    /*
    Main Callback function for block interactions
    (REFER TO THESE CODES WHENEVER ADDING NEW BLOCK INTERACTIONS)
    (MAY BE NEEDED IN ORDER TO IMPLEMENT NEW POST HANDLERS FOR NEW BLOCKS)
    */
    private void CallbackHandler(int code, ChunkPos targetChunk, CastCoord thisPos, int facing){
      // 0: No further actions necessary
      if(code == 0)
        return;
      // 1: Interaction forces the target chunk to reload/rebuild
      else if(code == 1){
        loader.chunks[targetChunk].BuildChunk();
        loader.chunks[targetChunk].BuildSideBorder(reload:true);
      }
      // 2: Emits BUD instantly and forces chunk reload
      else if(code == 2){
        EmitBlockUpdate("change", current.GetWorldX(), current.GetWorldY(), current.GetWorldZ(), 0, loader);
        loader.budscheduler.ScheduleReload(targetChunk, 0);  
      }
      // 3: Emits BUD in next tick and forces chunk reload
      else if(code == 2){
        EmitBlockUpdate("change", current.GetWorldX(), current.GetWorldY(), current.GetWorldZ(), 1, loader);
        loader.budscheduler.ScheduleReload(targetChunk, 1);
      }

    }

    // Handles the emittion of BUD to neighboring blocks
    public void EmitBlockUpdate(string type, int x, int y, int z, int tickOffset, ChunkLoader cl){
      CastCoord thisPos = GetCoordinates(x, y, z);

      CastCoord[] neighbors = {
      thisPos.Add(1,0,0),
      thisPos.Add(-1,0,0),
      thisPos.Add(0,1,0),
      thisPos.Add(0,-1,0),
      thisPos.Add(0,0,1),
      thisPos.Add(0,0,-1)
      };

      int[] facings = {2,0,4,5,1,3};


      int blockCode;
      int faceCounter=0;

      foreach(CastCoord c in neighbors){
        blockCode = cl.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);

        cachedBUD = new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]);
        cl.budscheduler.ScheduleBUD(cachedBUD, tickOffset);       
      
        faceCounter++;
      }
    }

    private CastCoord GetCoordinates(int x, int y, int z){
      return new CastCoord(new Vector3(x ,y ,z));
    }

    // Checks if neighbor chunk from chunkpos needs to update it's sides
    private void UpdateNeighborChunk(ChunkPos pos, int x, int y, int z){
      if(x == 0)
        loader.chunks[new ChunkPos(pos.x-1, pos.z)].BuildSideBorder(reloadXM:true);
      else if(x == Chunk.chunkWidth-1)
        loader.chunks[new ChunkPos(pos.x+1, pos.z)].BuildSideBorder(reloadXm:true);
      else if(z == 0)
        loader.chunks[new ChunkPos(pos.x, pos.z-1)].BuildSideBorder(reloadZM:true);
      else if(z == Chunk.chunkWidth-1)
        loader.chunks[new ChunkPos(pos.x, pos.z+1)].BuildSideBorder(reloadZm:true);
    }

}