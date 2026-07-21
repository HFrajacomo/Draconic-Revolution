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
	private VoxelBehaviour offsetVector;
	private VoxelBehaviour rotationValue;
	private List<VoxelBehaviour> onBlockUpdate;
	private List<VoxelBehaviour> onInteract;
	private List<VoxelBehaviour> onPlace;
	private List<VoxelBehaviour> onBreak;
	private List<VoxelBehaviour> onLoad;
	private List<VoxelBehaviour> onVFXBuild;
	private List<VoxelBehaviour> onVFXChange;
	private List<VoxelBehaviour> onVFXBreak;
	private List<VoxelBehaviour> onSFXPlay;
	private List<VoxelBehaviour> onPlayerStepEnter;
	private List<VoxelBehaviour> onPlayerStepExit;
	private List<VoxelBehaviour> onPlayerBodyEnter;
	private List<VoxelBehaviour> onPlayerBodyExit;
	private List<VoxelBehaviour> onPlayerHeadEnter;
	private List<VoxelBehaviour> onPlayerHeadExit;
	private List<VoxelBehaviour> placementRule;



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
    public List<VoxelBehaviour> GetOnBlockUpdate() { return onBlockUpdate; }
    public void SetOnBlockUpdate(List<VoxelBehaviour> val) { onBlockUpdate = val; }

    public List<VoxelBehaviour> GetOnInteract() { return onInteract; }
    public void SetOnInteract(List<VoxelBehaviour> val) { onInteract = val; }

    public List<VoxelBehaviour> GetOnPlace() { return onPlace; }
    public void SetOnPlace(List<VoxelBehaviour> val) { onPlace = val; }

    public List<VoxelBehaviour> GetOnBreak() { return onBreak; }
    public void SetOnBreak(List<VoxelBehaviour> val) { onBreak = val; }

    public List<VoxelBehaviour> GetOnLoad() { return onLoad; }
    public void SetOnLoad(List<VoxelBehaviour> val) { onLoad = val; }

    public List<VoxelBehaviour> GetOnVFXBuild() { return onVFXBuild; }
    public void SetOnVFXBuild(List<VoxelBehaviour> val) { onVFXBuild = val; }

    public List<VoxelBehaviour> GetOnVFXChange() { return onVFXChange; }
    public void SetOnVFXChange(List<VoxelBehaviour> val) { onVFXChange = val; }

    public List<VoxelBehaviour> GetOnVFXBreak() { return onVFXBreak; }
    public void SetOnVFXBreak(List<VoxelBehaviour> val) { onVFXBreak = val; }

    public List<VoxelBehaviour> GetOnSFXPlay() { return onSFXPlay; }
    public void SetOnSFXPlay(List<VoxelBehaviour> val) { onSFXPlay = val; }

    public List<VoxelBehaviour> GetPlacementRule() { return placementRule; }
    public void SetPlacementRule(List<VoxelBehaviour> val) { placementRule = val; }

    public ModelIdentityBehaviour GetModelIdentity() { return modelIdentity; }
    public void SetModelIdentity(ModelIdentityBehaviour val) { modelIdentity = val; }

    public VoxelBehaviour GetOffsetVector() { return offsetVector; }
    public void SetOffsetVector(VoxelBehaviour val) { offsetVector = val; }

    public VoxelBehaviour GetRotationValue() { return rotationValue; }
    public void SetRotationValue(VoxelBehaviour val) { rotationValue = val; }

    public List<VoxelBehaviour> GetOnPlayerStepEnter() { return onPlayerStepEnter; }
    public void SetOnPlayerStepEnter(List<VoxelBehaviour> val) { onPlayerStepEnter = val; }

    public List<VoxelBehaviour> GetOnPlayerStepExit() { return onPlayerStepExit; }
    public void SetOnPlayerStepExit(List<VoxelBehaviour> val) { onPlayerStepExit = val; }

    public List<VoxelBehaviour> GetOnPlayerBodyEnter() { return onPlayerBodyEnter; }
    public void SetOnPlayerBodyEnter(List<VoxelBehaviour> val) { onPlayerBodyEnter = val; }

    public List<VoxelBehaviour> GetOnPlayerBodyExit() { return onPlayerBodyExit; }
    public void SetOnPlayerBodyExit(List<VoxelBehaviour> val) { onPlayerBodyExit = val; }

    public List<VoxelBehaviour> GetOnPlayerHeadEnter() { return onPlayerHeadEnter; }
    public void SetOnPlayerHeadEnter(List<VoxelBehaviour> val) { onPlayerHeadEnter = val; }

    public List<VoxelBehaviour> GetOnPlayerHeadExit() { return onPlayerHeadExit; }
    public void SetOnPlayerHeadExit(List<VoxelBehaviour> val) { onPlayerHeadExit = val; }

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

		if(this.onBlockUpdate.Count == 0)
			return;

		for(int i=0; i < this.onBlockUpdate.Count; i++){
			this.onBlockUpdate[i].OnBlockUpdate(type, myX, myY, myZ, budX, budY, budZ, facing, cl);
		}
	}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		if(this.onInteract == null || this.onInteract.Count == 0)
			return 0;

		int returnCode = 0;

		for(int i=0; i < this.onInteract.Count; i++){
			returnCode = returnCode | this.onInteract[i].OnInteract(pos, blockX, blockY, blockZ, cl);
		}

		return returnCode;
	}

	public virtual void OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		if(this.onPlace == null || this.onPlace.Count == 0)
			return;

		for(int i=0; i < this.onPlace.Count; i++){
			this.onPlace[i].OnPlace(pos, blockX, blockY, blockZ, facing, cl);
		}
	}

	public virtual void OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		if(this.onBreak == null || this.onBreak.Count == 0)
			return;

		for(int i=0; i < this.onBreak.Count; i++){
			this.onBreak[i].OnBreak(pos, blockX, blockY, blockZ, cl);
		}
	}

	public virtual void OnLoad(CastCoord coord, ChunkLoader_Server cl){
		if(this.onLoad == null || this.onLoad.Count == 0)
			return;

		for(int i=0; i < this.onLoad.Count; i++){
			this.onLoad[i].OnLoad(coord, cl);
		}
	}

	public virtual void OnVFXBuild(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){
		if(this.onVFXBuild == null || this.onVFXBuild.Count == 0)
			return;

		for(int i=0; i < this.onVFXBuild.Count; i++){
			this.onVFXBuild[i].OnVFXBuild(pos, blockX, blockY, blockZ, facing, state, cl);
		}
	}

	public virtual void OnVFXChange(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){
		if(this.onVFXChange == null || this.onVFXChange.Count == 0)
			return;

		for(int i=0; i < this.onVFXChange.Count; i++){
			this.onVFXChange[i].OnVFXChange(pos, blockX, blockY, blockZ, facing, state, cl);
		}
	}

	public virtual void OnVFXBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){
		if(this.onVFXBreak == null || this.onVFXBreak.Count == 0)
			return;

		for(int i=0; i < this.onVFXBreak.Count; i++){
			this.onVFXBreak[i].OnVFXBreak(pos, blockX, blockY, blockZ, state, cl);
		}
	}

	public virtual void OnSFXPlay(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){
		if(this.onSFXPlay == null || this.onSFXPlay.Count == 0)
			return;

		for(int i=0; i < this.onSFXPlay.Count; i++){
			this.onSFXPlay[i].OnSFXPlay(pos, blockX, blockY, blockZ, state, cl);
		}
	}

	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader_Server cl){
		if(this.placementRule == null || this.placementRule.Count == 0)
			return true;

		for(int i=0; i < this.placementRule.Count; i++){
			if(!this.placementRule[i].PlacementRule(pos, blockX, blockY, blockZ, direction, cl)){
				return false;
			}
		}
		
		return true;
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

	public virtual void OnPlayerStepEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(this.onPlayerStepEnter == null || this.onPlayerStepEnter.Count == 0)
			return;

		for(int i=0; i < this.onPlayerStepEnter.Count; i++){
			this.onPlayerStepEnter[i].OnPlayerStepEnter(location, sheet, cl);
		}
	}

	public virtual void OnPlayerStepExit(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(this.onPlayerStepExit == null || this.onPlayerStepExit.Count == 0)
			return;

		for(int i=0; i < this.onPlayerStepExit.Count; i++){
			this.onPlayerStepExit[i].OnPlayerStepExit(location, sheet, cl);
		}
	}

	public virtual void OnPlayerHeadEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(this.onPlayerHeadEnter == null || this.onPlayerHeadEnter.Count == 0)
			return;

		for(int i=0; i < this.onPlayerHeadEnter.Count; i++){
			this.onPlayerHeadEnter[i].OnPlayerHeadEnter(location, sheet, cl);
		}
	}

	public virtual void OnPlayerHeadExit(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(this.onPlayerHeadExit == null || this.onPlayerHeadExit.Count == 0)
			return;

		for(int i=0; i < this.onPlayerHeadExit.Count; i++){
			this.onPlayerHeadExit[i].OnPlayerHeadExit(location, sheet, cl);
		}
	}

	public virtual void OnPlayerBodyEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(this.onPlayerBodyEnter == null || this.onPlayerBodyEnter.Count == 0)
			return;

		for(int i=0; i < this.onPlayerBodyEnter.Count; i++){
			this.onPlayerBodyEnter[i].OnPlayerBodyEnter(location, sheet, cl);
		}
	}

	public virtual void OnPlayerBodyExit(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(this.onPlayerBodyExit == null || this.onPlayerBodyExit.Count == 0)
			return;

		for(int i=0; i < this.onPlayerBodyExit.Count; i++){
			this.onPlayerBodyExit[i].OnPlayerBodyExit(location, sheet, cl);
		}
	}

	public void SetupAfterSerialize(bool isClient){
		if(this.onBlockUpdate != null){
			for(int i=0; i < onBlockUpdate.Count; i++){
				onBlockUpdate[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onInteract != null){
			for(int i=0; i < onInteract.Count; i++){
				onInteract[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onPlace != null){
			for(int i=0; i < onPlace.Count; i++){
				onPlace[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onBreak != null){
			for(int i=0; i < onBreak.Count; i++){
				onBreak[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onLoad != null){
			for(int i=0; i < onLoad.Count; i++){
				onLoad[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onVFXBuild != null){
			for(int i=0; i < onVFXBuild.Count; i++){
				onVFXBuild[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onVFXChange != null){
			for(int i=0; i < onVFXChange.Count; i++){
				onVFXChange[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onVFXBreak != null){
			for(int i=0; i < onVFXBreak.Count; i++){
				onVFXBreak[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onSFXPlay != null){
			for(int i=0; i < onSFXPlay.Count; i++){
				onSFXPlay[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.placementRule != null){
			for(int i=0; i < placementRule.Count; i++){
				placementRule[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.modelIdentity != null){
			modelIdentity.PostDeserializationSetup(isClient);
		}
		if(this.offsetVector != null){
			offsetVector.PostDeserializationSetup(isClient);
		}
		if(this.rotationValue != null){
			rotationValue.PostDeserializationSetup(isClient);
		}
		if(this.onPlayerBodyEnter != null){
			for(int i=0; i < onPlayerBodyEnter.Count; i++){
				onPlayerBodyEnter[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onPlayerBodyExit != null){
			for(int i=0; i < onPlayerBodyExit.Count; i++){
				onPlayerBodyExit[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onPlayerHeadEnter != null){
			for(int i=0; i < onPlayerHeadEnter.Count; i++){
				onPlayerHeadEnter[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onPlayerHeadExit != null){
			for(int i=0; i < onPlayerHeadExit.Count; i++){
				onPlayerHeadExit[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onPlayerStepEnter != null){
			for(int i=0; i < onPlayerStepEnter.Count; i++){
				onPlayerStepEnter[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onPlayerStepExit != null){
			for(int i=0; i < onPlayerStepExit.Count; i++){
				onPlayerStepExit[i].PostDeserializationSetup(isClient);
			}
		}
	}
}
