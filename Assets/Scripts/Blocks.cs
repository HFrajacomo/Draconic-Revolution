using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Blocks
{
	public static readonly int blockCount = 35;
	public static readonly int pixelSize = 32;
	public static readonly int atlasSizeX = 8;
	public static readonly int atlasSizeY = 5;
	public static readonly int transparentAtlasSizeX = 2;
	public static readonly int transparentAtlasSizeY = 1;

	public ShaderIndex shaderIndex = ShaderIndex.OPAQUE; // The material used in the rendering pipeline
	public string name;
	public bool solid; // Is collidable
	public byte transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool affectLight; // Should drain light level
	public bool liquid;
	public bool seamless = false;
	public byte luminosity = 0; // Emits VoxelLight -> 0x0F represents the light level and 0xF0 represents the state offset to be emmiting light
	public bool hasLoadEvent = false;
	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;
	public bool drawRegardless = false;
	public ushort maxHP;

	public HashSet<BlockFlags> flags;

	// Texture tile code
	public int tileTop;
	public int tileSide;
	public int tileBottom;

	/*
	Every new block addition must have a scope in the IF tree
	*/
	public static Blocks Block(int blockID){
		// The actual block encyclopedia
		switch(blockID){
			case 0:
				return new Air_Block();
			case 1:
				return new Grass_Block();
			case 2:
				return new Dirt_Block();
			case 3:
				return new Stone_Block();
			case 4:
				return new Wood_Block();
			case 5:
				return new IronOre_Block();
			case 6:
				return new Water_Block();
			case 7:
				return new Leaf_Block();
			case 8:
				return new Sand_Block();
			case 9:
				return new Snow_Block();
			case 10:
				return new Ice_Block();
			case 11:
				return new Basalt_Block();
			case 12:
				return new Clay_Block();
			case 13:
				return new StoneBrick_Block();
			case 14:
				return new WoodenPlankRegular_Block();
			case 15:
				return new WoodenPlankPine_Block();
			case 16:
				return new Bone_Block();
			case 17:
				return new SandstoneBrick_Block();
			case 18:
				return new Sandstone_Block();
			case 19:
				return new CoalOre_Block();
			case 20:
				return new MagnetiteOre_Block();
			case 21:
				return new AluminiumOre_Block();
			case 22:
				return new CopperOre_Block();
			case 23:
				return new TinOre_Block();
			case 24:
				return new GoldOre_Block();
			case 25:
				return new EmeriumOre_Block();
			case 26:
				return new UraniumOre_Block();
			case 27:
				return new Emerald_Block();
			case 28:
				return new Ruby_Block();
			case 29:
				return new PineWood_Block();
			case 30:
				return new PineLeaf_Block();
			case 31:
				return new Gravel_Block();
			case 32:
				return new Moonstone_Block();
			case 33:
				return new Lava_Block();
			case 34:
				return new HellMarble_Block();

			default:
				return new Air_Block();
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

		// If should use normal atlas
		if(this.shaderIndex == ShaderIndex.OPAQUE){
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
		else if(this.shaderIndex == ShaderIndex.WATER){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			UVs[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			UVs[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			UVs[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			UVs[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(this.shaderIndex == ShaderIndex.LEAVES){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
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
    Calculates how damage should be calculated for a block
	based on damage, damage type and damange flags on blocks
    */
    public int CalculateDamage(ushort blockDamage){
    	if(blockDamage <= 0)
    		return 0;

    	if(this.flags != null){
	    	if(this.flags.Contains(BlockFlags.IMMUNE))
	    		return 0;
    	}

    	return Mathf.CeilToInt(Mathf.Sqrt(blockDamage));
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
	public virtual void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){return 0;}
	public virtual int OnLoad(CastCoord coord, ChunkLoader_Server cl){return 0;}
	public virtual int OnVFXBuild(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnVFXChange(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnVFXBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnSFXPlay(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader_Server cl){return true;}

}