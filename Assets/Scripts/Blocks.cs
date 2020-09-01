using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Blocks
{
	public static int blockCount = 6;
	public const int pixelSize = 32;
	public static int atlasSizeX = 8;
	public static int atlasSizeY = 2;

	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool liquid;
	public bool needsRotation = false;

	// Texture tile code
	public int tileTop;
	public int tileSide;
	public int tileBottom;

	/*
	Every new block addition must have a scope in the IF tree
	*/
	public static Blocks Block(int blockID){
		// The actual block encyclopedia

			if(blockID == 0){
				Air_Block aux = new Air_Block();
				return aux;
			}
			else if(blockID == 1){
				Grass_Block aux = new Grass_Block();
				return aux;
			}
			else if(blockID == 2){
				Dirt_Block aux = new Dirt_Block();
				return aux;
			}
			else if(blockID == 3){
				Stone_Block aux = new Stone_Block();
				return aux;
			}
			else if(blockID == 4){
				Wood_Block aux = new Wood_Block();
				return aux;
			}
			else if(blockID == 5){
				MetalOre_Block aux = new MetalOre_Block();
				return aux;
			}
			else if(blockID == 6)
				return new Water_Block();
			else{
				Air_Block aux = new Air_Block();
				return aux;
			}
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

		float x = textureID%Blocks.atlasSizeX;
		float y = Mathf.FloorToInt(textureID/Blocks.atlasSizeX);
 
		x *= 1f / Blocks.atlasSizeX;
		y *= 1f / Blocks.atlasSizeY;

		
		UVs[0] = new Vector2(x,y+(1f/Blocks.atlasSizeY));
		UVs[1] = new Vector2(x+(1f/Blocks.atlasSizeX),y+(1f/Blocks.atlasSizeY));
		UVs[2] = new Vector2(x+(1f/Blocks.atlasSizeX),y);
		UVs[3] = new Vector2(x,y);

		return UVs;
	}

	// Gets UV Map for Liquid blocks
	public Vector2[] LiquidTexture(Direction dir){
		Vector2[] UVs = {
			new Vector2(0,0),
			new Vector2(0,1),
			new Vector2(1,1),
			new Vector2(1,0)
		};

		return UVs;
	}

	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
	*/
	public virtual void OnBlockUpdate(string type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader cl){}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader cl){return true;}

}