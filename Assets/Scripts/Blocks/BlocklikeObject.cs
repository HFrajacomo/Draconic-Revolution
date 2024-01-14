using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class BlocklikeObject
{
	public ShaderIndex shaderIndex = ShaderIndex.ASSETS; // Assets
	public string name;
	public bool solid; // Is collidable
	public byte transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool affectLight; // Should drain light
	public bool liquid;
	public bool seamless;
	public bool hasLoadEvent; // Should run code when loaded it's chunk
	public byte luminosity = 0; // Emits VoxelLight

	public Vector3 positionOffset;
	public Vector3 scaling; 
	public Vector3 hitboxScaling;

	public static readonly int objectCount = 9;
	public int stateNumber; // If needsRotation is true, these objects need to tell the rendering engine their max number of sequential states from 0

	public VFXLoader vfx = GameObject.Find("/VFXLoader").GetComponent<VFXLoader>();
	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	public ushort maxHP;
	public HashSet<BlockFlags> flags;

	// Mesh and Hitbox
	public GameObject go;
	public GameObject hitboxObject;
	public Mesh mesh;
	public Mesh hitboxMesh;

	// Texture
	protected static readonly int SOLID_ATLAS_X = 10;
	protected static readonly int SOLID_ATLAS_Y = 2;
	protected static readonly int NORMAL_ATLAS_X = 8;
	protected static readonly int NORMAL_ATLAS_Y = 2;
	public int2 atlasPosition;

	// Item Drops
	public Item droppedItem;
	public byte minDropQuantity;
	public byte maxDropQuantity;

	// Block Encyclopedia fill function
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

    	if(this.flags != null){
	    	if(this.flags.Contains(BlockFlags.IMMUNE))
	    		return 0;
    	}

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

	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
	*/
	public virtual void OnBlockUpdate(BUDCode budType, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){return 0;}
	public virtual int OnLoad(CastCoord coord, ChunkLoader_Server cl){return 0;}
	public virtual int OnVFXBuild(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnVFXChange(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnVFXBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnSFXPlay(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader_Server cl){return true;}
	public virtual Vector3 GetOffsetVector(ushort state){return Vector3.zero;}
	public virtual int2 GetRotationValue(ushort state){return new int2(0,0);}
}
