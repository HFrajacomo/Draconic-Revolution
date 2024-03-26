using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class BlocklikeObject
{
	public ShaderIndex shaderIndex = ShaderIndex.ASSETS; // Assets
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

	public static readonly int objectCount = 9;

	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	public ushort maximumRotationScaleState;
	public ushort maxHP;
	public bool indestructible;

	public int textureCode;

	// Mesh and Hitbox
	protected Mesh mesh;
	protected Mesh hitboxMesh;

	// Texture
	protected static readonly int SOLID_ATLAS_X = 10;
	protected static readonly int SOLID_ATLAS_Y = 2;
	protected static readonly int NORMAL_ATLAS_X = 8;
	protected static readonly int NORMAL_ATLAS_Y = 2;
	public int2 atlasPosition;

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


	// Block Encyclopedia fill function
	/*
	public static BlocklikeObject Create(int blockID, bool isClient){
		switch(blockID){

			case 0:
				return new Torch_Object(isClient);
			case 1:
				return new IgnisCrystal_Object(isClient);
			case 2:
				return new AquaCrystal_Object(isClient);
			case 3:
				return new AerCrystal_Object(isClient);
			case 4:
				return new TerraCrystal_Object(isClient);
			case 5:
				return new OrdoCrystal_Object(isClient);
			case 6:
				return new PerditioCrystal_Object(isClient);
			case 7:
				return new PrecantioCrystal_Object(isClient);

			default:
				return new Torch_Object(isClient);
		}
	}
	*/

	public Mesh GetMesh(){return this.mesh;}
	public Mesh GetHitboxMesh(){return this.hitboxMesh;}

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
	        blockCode = cl.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);

	        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]), tickOffset);     
	      
	        faceCounter++;
	    }
    }

    // Emits a BUD signal with no information about sender
    public void EmitBUDTo(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){
    	cl.budscheduler.ScheduleBUD(new BUDSignal(type, x, y, z, 0, 0, 0, 0), tickOffset);
    }

	// Unassigns metadata from block (use after OnBreak events)
	public void EraseMetadata(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){cl.chunks[pos].metadata.Reset(x,y,z);}
	
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

    /*
    Correctly re-arranges the object's mesh UVs to match their respective texture atlas position
    */
    public virtual Vector2 AddTexture(Vector2 uv){
    	Vector2 finalUV = new Vector2(0,0);
    	float initX, initY, finalX, finalY;

    	if(this.shaderIndex == ShaderIndex.ASSETS){
    		initX = (float)this.atlasPosition.x / NORMAL_ATLAS_X;
    		finalX = (float)(this.atlasPosition.x + 1) / NORMAL_ATLAS_X;
    		initY = (float)this.atlasPosition.y / NORMAL_ATLAS_Y;
    		finalY = (float)(this.atlasPosition.y + 1) / NORMAL_ATLAS_Y;
    	}
    	else{
    		initX = (float)this.atlasPosition.x / SOLID_ATLAS_X;
    		finalX = (float)(this.atlasPosition.x + 1) / SOLID_ATLAS_X;
    		initY = (float)this.atlasPosition.y / SOLID_ATLAS_Y;
    		finalY = (float)(this.atlasPosition.y + 1) / SOLID_ATLAS_Y;    		
    	}

    	finalUV.x = Mathf.Lerp(initX, finalX, uv.x);
    	finalUV.y = Mathf.Lerp(initY, finalY, uv.y);

    	return finalUV;
    }

    // Gets the Input UVs of a mesh and transforms them using the AddTexture function
    protected void RemapMeshUV(){
    	List<Vector2> uvs = new List<Vector2>(); 
    	this.mesh.GetUVs(0, uvs);

    	for(int i=0; i < uvs.Count; i++){
    		uvs[i] = AddTexture(uvs[i]);
    	}

    	this.mesh.SetUVs(0, uvs);
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
	public void LoadModel(bool isClient){
		if(isClient){
			this.mesh = this.modelIdentity.GetMesh();
			this.hitboxMesh = this.modelIdentity.GetHitboxMesh();
		}
	}

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
}
