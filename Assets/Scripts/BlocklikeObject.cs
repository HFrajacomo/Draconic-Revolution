using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public static readonly int objectCount = 1;
	public int stateNumber; // If needsRotation is true, these objects need to tell the rendering engine their max number of sequential states from 0

	public VFXLoader vfx = GameObject.Find("/VFXLoader").GetComponent<VFXLoader>();
	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	public ushort maxHP;
	public HashSet<BlockFlags> flags;

	// Texture
	public GameObject go;
	public Mesh mesh;

	// Block Encyclopedia fill function
	public static BlocklikeObject Create(int blockID, bool isClient){
		if(blockID == 0)
			return new Torch_Object(isClient);
		else
			return new Torch_Object(isClient);
	}

	// Adds GameObject with correct offseting in the world and returns it
	public GameObject PlaceObject(ChunkPos pos, int x, int y, int z, ushort blockCode, ChunkLoader_Server loader){
		if(!this.needsRotation)
			return GameObject.Instantiate(loader.blockBook.objects[ushort.MaxValue - blockCode].go, new Vector3(pos.x*Chunk.chunkWidth + x, y, pos.z*Chunk.chunkWidth + z), Quaternion.identity);
		else{
			GameObject GO = GameObject.Instantiate(loader.blockBook.objects[ushort.MaxValue - blockCode].go, new Vector3(pos.x*Chunk.chunkWidth + x, y, pos.z*Chunk.chunkWidth + z), Quaternion.identity);
			loader.blockBook.objects[ushort.MaxValue - blockCode].ApplyRotation(GO, loader.chunks[pos].metadata.GetState(x,y,z), x, y, z);
			return GO;
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

    	if(this.flags.Contains(BlockFlags.IMMUNE))
    		return 0;

    	return Mathf.CeilToInt(Mathf.Sqrt(blockDamage));
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
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader_Server cl){return true;}
	public virtual void ApplyRotation(GameObject go, ushort? state, int blockX, int blockY, int blockZ){}
	public virtual Vector3 GetOffsetVector(ushort state){return new Vector3(0f,0f,0f);}
	public virtual int GetRotationValue(ushort state){return 0;}
}
