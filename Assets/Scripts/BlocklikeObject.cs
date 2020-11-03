using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlocklikeObject
{
	public byte materialIndex = 3; // Assets
	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool liquid;
	public bool hasLoadEvent; // Should run code when loaded it's chunk

	public static int objectCount = 2;

	public VFXLoader vfx = GameObject.Find("/VFXLoader").GetComponent<VFXLoader>();
	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	// Texture
	public GameObject go;

	// Block Encyclopedia fill function
	public static BlocklikeObject Create(int blockID){
		if(blockID == 0)
			return new Torch_Object();
		else if(blockID == 1)
			return new Leaf_Object();
		else
			return new Torch_Object();
	}

	// Adds GameObject with correct offseting in the world and returns it
	public GameObject PlaceObject(ChunkPos pos, int x, int y, int z, ushort blockCode, ChunkLoader loader){
		if(!this.needsRotation)
			return GameObject.Instantiate(loader.blockBook.objects[ushort.MaxValue - blockCode].go, new Vector3(pos.x*Chunk.chunkWidth + x, y, pos.z*Chunk.chunkWidth + z), Quaternion.identity);
		else{
			GameObject GO = GameObject.Instantiate(loader.blockBook.objects[ushort.MaxValue - blockCode].go, new Vector3(pos.x*Chunk.chunkWidth + x, y, pos.z*Chunk.chunkWidth + z), Quaternion.identity);
			loader.blockBook.objects[ushort.MaxValue - blockCode].ApplyRotation(GO, loader.chunks[pos].metadata.GetMetadata(x,y,z).state, x, y, z);
			return GO;
		}
	}


    // Handles the emittion of BUD to neighboring blocks
    public void EmitBlockUpdate(string type, int x, int y, int z, int tickOffset, ChunkLoader cl){
      CastCoord thisPos = new CastCoord(new Vector3(x, y, z));

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

        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]), tickOffset);     
      
        faceCounter++;
      }
    }

	// Unassigns metadata from block (use after OnBreak events)
	public void EraseMetadata(ChunkPos pos, int x, int y, int z, ChunkLoader cl){cl.chunks[pos].metadata.GetMetadata(x,y,z).Reset();}
	
	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
	*/
	public virtual void OnBlockUpdate(string budType, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader cl){}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual int OnLoad(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader cl){return true;}
	public virtual void ApplyRotation(GameObject go, ushort? state, int blockX, int blockY, int blockZ){}
}
