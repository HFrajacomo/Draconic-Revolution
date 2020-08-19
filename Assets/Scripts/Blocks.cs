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

	// Texture tile code
	public int tileTop;
	public int tileSide;
	public int tileBottom;

	public Type type;


	public static Blocks Block(int blockID){
		// The actual block encyclopedia

			if(blockID == 0){
				Air_Block aux = new Air_Block();
				aux.type = aux.GetType();
				return aux;
			}
			else if(blockID == 1){
				Grass_Block aux = new Grass_Block();
				aux.type = aux.GetType();
				return aux;
			}
			else if(blockID == 2){
				Dirt_Block aux = new Dirt_Block();
				aux.type = aux.GetType();
				return aux;
			}
			else if(blockID == 3){
				Stone_Block aux = new Stone_Block();
				aux.type = aux.GetType();
				return aux;
			}
			else if(blockID == 4){
				Wood_Block aux = new Wood_Block();
				aux.type = aux.GetType();
				return aux;
			}
			else if(blockID == 5){
				MetalOre_Block aux = new MetalOre_Block();
				aux.type = aux.GetType();
				return aux;
			}
			else{
				Air_Block aux = new Air_Block();
				aux.type = aux.GetType();
				return aux;
			}
	}

	//public abstract int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl);
	public abstract int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl);

	/*
	// Easy setter
	public void Set(string name, int[] tileCode, bool solid, bool transparent, bool invisible, bool liquid){
		this.name = name;
		this.solid = solid;
		this.transparent = transparent;
		this.invisible = invisible;
		this.liquid = liquid;

		this.tileTop = tileCode[0];
		this.tileSide = tileCode[1];
		this.tileBottom = tileCode[2];
	}
	*/

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


}