using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Blocks
{
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

	// Default constructor
	public Blocks(int blockID){
		Block(blockID);
	}

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

	// The actual block encyclopedia
	public void Block(int blockID){
		switch(blockID){
			// Air
			case 0:
				Set("Air", new int[3]{0,0,0},false, true, true, false);
				break;
			case 1:
				Set("Grass", new int[3]{1,2,3}, true, false, false, false);
				break;

			default:
				Set("???", new int[3]{0,0,0}, false, false, true, false);
				break;
		}
	}

}