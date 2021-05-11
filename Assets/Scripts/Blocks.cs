using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Blocks
{
	public static readonly int blockCount = 7;
	public static readonly int pixelSize = 32;
	public static readonly int atlasSizeX = 8;
	public static readonly int atlasSizeY = 2;
	public static readonly int transparentAtlasSizeX = 8;
	public static readonly int transparentAtlasSizeY = 4;

	public byte materialIndex = 0; // The material used in the rendering pipeline
	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool liquid;
	public bool hasLoadEvent = false;
	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	// Texture tile code
	public int tileTop;
	public int tileSide;
	public int tileBottom;

	/*
	Every new block addition must have a scope in the IF tree
	*/
	public static Blocks Block(int blockID){
		// The actual block encyclopedia

		if(blockID == 0)
			return new Air_Block();
		else if(blockID == 1)
			return new Grass_Block();
		else if(blockID == 2)
			return new Dirt_Block();
		else if(blockID == 3)
			return new Stone_Block();	
		else if(blockID == 4)
			return new Wood_Block();		
		else if(blockID == 5)
			return new MetalOre_Block();		
		else if(blockID == 6)
			return new Water_Block();
		else
			return new Air_Block();
			
	}


	// Sets UV mapping for a direction
	public Vector2[] AddTexture(Direction dir){
		Vector2[] UVs = new Vector2[4];
		int textureID;

		if(dir == Direction.Up)
			textureID = this.tileTop;
		else if(dir == Direction.Down)
			textureID = this.tileBottom;
		else
			textureID = this.tileSide;

		// If should use normal atlas
		if(this.materialIndex == 0){
			float x = textureID%Blocks.atlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.atlasSizeX);
	 
			x *= 1f / Blocks.atlasSizeX;
			y *= 1f / Blocks.atlasSizeY;

			UVs[0] = new Vector2(x,y+(1f/Blocks.atlasSizeY));
			UVs[1] = new Vector2(x+(1f/Blocks.atlasSizeX),y+(1f/Blocks.atlasSizeY));
			UVs[2] = new Vector2(x+(1f/Blocks.atlasSizeX),y);
			UVs[3] = new Vector2(x,y);
		}
		// If should use transparent atlas
		else if(this.materialIndex == 2){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeY);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			UVs[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			UVs[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			UVs[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			UVs[3] = new Vector2(x,y);
		}

		return UVs;
	}

	// Gets UV Map for Liquid blocks
	public Vector2[] LiquidTexture(int x, int z){
		int size = Chunk.chunkWidth;
		int tileSize = 1/size;

		Vector2[] UVs = {
			new Vector2(x*tileSize,z*tileSize),
			new Vector2(x*tileSize,(z+1)*tileSize),
			new Vector2((x+1)*tileSize,(z+1)*tileSize),
			new Vector2((x+1)*tileSize,z*tileSize)
		};

		return UVs;
	}

    // Handles the emittion of BUD to neighboring blocks
    public void EmitBlockUpdate(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){
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

      int faceCounter=0;

      foreach(CastCoord c in neighbors){
		if(c.blockY < 0 || c.blockY > Chunk.chunkDepth-1){
			continue;
		}
		
        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]), tickOffset);
      
        faceCounter++;
      }
    }	

    // Emits a BUD signal with no information about sender
    public void EmitBUDTo(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){
    	cl.budscheduler.ScheduleBUD(new BUDSignal(type, x, y, z, 0, 0, 0, 0), tickOffset);
    }

	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
		"decay": When emitting block is wood and wants to decay leaves
	*/
	public virtual  void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){return 0;}
	public virtual int OnLoad(CastCoord coord, ChunkLoader_Server cl){return 0;}
	public virtual int OnVFXBuild(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader_Server cl){return true;}

}