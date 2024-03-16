using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blocks
{
	public static readonly int blockCount = 64;
	public static readonly int pixelSize = 32;
	public static readonly int atlasSizeX = 8;
	public static readonly int atlasSizeY = 10;
	public static readonly int transparentAtlasSizeX = 4;
	public static readonly int transparentAtlasSizeY = 1;

	public ShaderIndex shaderIndex = ShaderIndex.OPAQUE; // The material used in the rendering pipeline
	public string codename;
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
	public bool indestructible;
	

	// Texture tile code
	public int tileTop;
	public int tileSide;
	public int tileBottom;

	// Behaviours
	private VoxelBehaviour onBlockUpdate;
	private VoxelBehaviour onInteract;
	private VoxelBehaviour onPlace;
	private VoxelBehaviour onBreak;
	private VoxelBehaviour onLoad;
	private VoxelBehaviour onVFXBuild;
	private VoxelBehaviour onVFXChange;
	private VoxelBehaviour onVFXBreak;
	private VoxelBehaviour onSFXPlay;
	private VoxelBehaviour placementRule;



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

		if(this.indestructible)
	    	return 0;

    	return Mathf.CeilToInt(Mathf.Sqrt(blockDamage));
    }

    // Events GET/SET
    public VoxelBehaviour GetOnBlockUpdate() { return onBlockUpdate; }
    public void SetOnBlockUpdate(VoxelBehaviour val) { onBlockUpdate = val; }

    public VoxelBehaviour GetOnInteract() { return onInteract; }
    public void SetOnInteract(VoxelBehaviour val) { onInteract = val; }

    public VoxelBehaviour GetOnPlace() { return onPlace; }
    public void SetOnPlace(VoxelBehaviour val) { onPlace = val; }

    public VoxelBehaviour GetOnBreak() { return onBreak; }
    public void SetOnBreak(VoxelBehaviour val) { onBreak = val; }

    public VoxelBehaviour GetOnLoad() { return onLoad; }
    public void SetOnLoad(VoxelBehaviour val) { onLoad = val; }

    public VoxelBehaviour GetOnVFXBuild() { return onVFXBuild; }
    public void SetOnVFXBuild(VoxelBehaviour val) { onVFXBuild = val; }

    public VoxelBehaviour GetOnVFXChange() { return onVFXChange; }
    public void SetOnVFXChange(VoxelBehaviour val) { onVFXChange = val; }

    public VoxelBehaviour GetOnVFXBreak() { return onVFXBreak; }
    public void SetOnVFXBreak(VoxelBehaviour val) { onVFXBreak = val; }

    public VoxelBehaviour GetOnSFXPlay() { return onSFXPlay; }
    public void SetOnSFXPlay(VoxelBehaviour val) { onSFXPlay = val; }

    public VoxelBehaviour GetPlacementRule() { return placementRule; }
    public void SetPlacementRule(VoxelBehaviour val) { placementRule = val; }

	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
		"decay": When emitting block is wood and wants to decay leaves
	*/
	public virtual void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(this.onBlockUpdate == null)
			return;
		this.onBlockUpdate.OnBlockUpdate(type, myX, myY, myZ, budX, budY, budZ, facing, cl);
	}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		if(this.onInteract == null)
			return 0;
		return this.onInteract.OnInteract(pos, blockX, blockY, blockZ, cl);
	}

	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		if(this.onPlace == null)
			return 0;
		return this.onPlace.OnPlace(pos, blockX, blockY, blockZ, facing, cl);
	}

	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		if(this.onBreak == null)
			return 0;
		return this.onBreak.OnBreak(pos, blockX, blockY, blockZ, cl);
	}

	public virtual int OnLoad(CastCoord coord, ChunkLoader_Server cl){
		if(this.onLoad == null)
			return 0;
		return this.onLoad.OnLoad(coord, cl);
	}

	public virtual int OnVFXBuild(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){
		if(this.onVFXBuild == null)
			return 0;
		return this.onVFXBuild.OnVFXBuild(pos, blockX, blockY, blockZ, facing, state, cl);
	}

	public virtual int OnVFXChange(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){
		if(this.onVFXChange == null)
			return 0;
		return this.onVFXChange.OnVFXChange(pos, blockX, blockY, blockZ, facing, state, cl);
	}

	public virtual int OnVFXBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){
		if(this.onVFXBreak == null)
			return 0;
		return this.onVFXBreak.OnVFXBreak(pos, blockX, blockY, blockZ, state, cl);
	}

	public virtual int OnSFXPlay(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){
		if(this.onSFXPlay == null)
			return 0;
		return this.onSFXPlay.OnSFXPlay(pos, blockX, blockY, blockZ, state, cl);
	}

	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader_Server cl){
		if(this.placementRule == null)
			return true;
		return this.placementRule.PlacementRule(pos, blockX, blockY, blockZ, direction, cl);
	}

}