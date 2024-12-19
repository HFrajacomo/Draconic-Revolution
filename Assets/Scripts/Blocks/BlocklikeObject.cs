using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class BlocklikeObject
{
	public ShaderIndex shaderIndex = ShaderIndex.ASSETS; // Assets
	public string codename;
	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool affectLight; // Should drain light
	public bool liquid;
	public bool seamless;
	public bool hasLoadEvent; // Should run code when loaded it's chunk
	public byte luminosity = 0; // Emits VoxelLight

	public Vector3 scaling; 
	public Vector3 hitboxScaling;

	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	public ushort maximumRotationScaleState;
	public ushort maxHP;
	public bool indestructible;

	// Texture tile code
	private int textureID;

	// Behaviours
	private ModelIdentityBehaviour modelIdentity;
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
	private VoxelBehaviour offsetVector;
	private VoxelBehaviour rotationValue;


	public MeshData GetMeshData(){return this.modelIdentity.GetMeshData();}
	public void SetMeshData(MeshData meshData){this.modelIdentity.SetMeshData(meshData);}
	public string GetTextureName(){return this.modelIdentity.GetTextureName();}
	public int GetTexture(){return this.textureID;}

    public void SetupTextureIDs(){
    	if(GetTextureName() != null){
    		this.textureID = VoxelLoader.GetTextureID(GetTextureName());
    	}
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

	    int blockCode;
	    int faceCounter=0;

	    foreach(CastCoord c in neighbors){
	        blockCode = cl.GetChunk(c.GetChunkPos()).data.GetCell(c.blockX, c.blockY, c.blockZ);

	        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]), tickOffset);     
	      
	        faceCounter++;
	    }
    }

    // Emits a BUD signal with no information about sender
    public void EmitBUDTo(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){
    	cl.budscheduler.ScheduleBUD(new BUDSignal(type, x, y, z, 0, 0, 0, 0), tickOffset);
    }

	// Unassigns metadata from block (use after OnBreak events)
	public void EraseMetadata(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){cl.GetChunk(pos).metadata.Reset(x,y,z);}
	
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

    public ModelIdentityBehaviour GetModelIdentity() { return modelIdentity; }
    public void SetModelIdentity(ModelIdentityBehaviour val) { modelIdentity = val; }

    public VoxelBehaviour GetOffsetVector() { return offsetVector; }
    public void SetOffsetVector(VoxelBehaviour val) { offsetVector = val; }

    public VoxelBehaviour GetRotationValue() { return rotationValue; }
    public void SetRotationValue(VoxelBehaviour val) { rotationValue = val; }

	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
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

	public virtual Vector3 GetOffsetVector(ushort state){
		if(this.offsetVector == null)
			return Vector3.zero;
		return this.offsetVector.GetOffsetVector(state);
	}

	public virtual int2 GetRotationValue(ushort state){
		if(this.rotationValue == null)
			return new int2(0,0);
		return this.rotationValue.GetRotationValue(state);
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
		if(this.modelIdentity != null)
			modelIdentity.PostDeserializationSetup(isClient);
		if(this.offsetVector != null)
			offsetVector.PostDeserializationSetup(isClient);
		if(this.rotationValue != null)
			rotationValue.PostDeserializationSetup(isClient);
	}
}
