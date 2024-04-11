using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Blocks
{
	public ShaderIndex shaderIndex = ShaderIndex.OPAQUE; // The material used in the rendering pipeline
	public string codename;
	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
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
	public string tileTop;
	public string tileSide;
	public string tileBottom;
	private int textureTop;
	private int textureSide;
	private int textureBottom;

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

    public int GetTextureTop(){return this.textureTop;}
    public int GetTextureBottom(){return this.textureBottom;}
    public int GetTextureSide(){return this.textureSide;}

    public void SetupTextureIDs(){
    	if(this.tileTop != null)
    		this.textureTop = VoxelLoader.GetTextureID(this.tileTop);

    	if(this.tileSide != null)
    		this.textureSide = VoxelLoader.GetTextureID(this.tileSide);

    	if(this.tileBottom != null)
    		this.textureBottom = VoxelLoader.GetTextureID(this.tileBottom);
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

	public void SetupAfterSerialize(bool isClient){
		if(this.onBlockUpdate != null)
			onBlockUpdate.PostDeserializationSetup(isClient);
		if(this.onInteract != null)
			onInteract.PostDeserializationSetup(isClient);
		if(this.onPlace != null)
			onPlace.PostDeserializationSetup(isClient);
		if(this.onBreak != null)
			onBreak.PostDeserializationSetup(isClient);
		if(this.onLoad != null)
			onLoad.PostDeserializationSetup(isClient);
		if(this.onVFXBuild != null)
			onVFXBuild.PostDeserializationSetup(isClient);
		if(this.onVFXChange != null)
			onVFXChange.PostDeserializationSetup(isClient);
		if(this.onVFXBreak != null)
			onVFXBreak.PostDeserializationSetup(isClient);
		if(this.onSFXPlay != null)
			onSFXPlay.PostDeserializationSetup(isClient);
		if(this.placementRule != null)
			placementRule.PostDeserializationSetup(isClient);
	}
}