using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using Object = UnityEngine.Object;

public class Chunk
{
	// Chunk Settings
	public VoxelData data;
	public VoxelMetadata metadata;
	public static readonly int chunkWidth = 16;
	public static readonly int chunkDepth = 256;
	public static readonly int chunkMaxY = 3;
	public static float chunkWidthMult = 15.99f; 
	public ChunkPos pos;
	public byte biome; // BiomeCode
	public byte needsGeneration;
	public float4 features;
	public string lastVisitedTime;

	// Server-side record of changed voxels, used to send only them to clients. Null in Clients
	public ChunkChangeSet changeSet;

	// Collider meshes are cooked in worker threads (BakeColliderJob) with the same options used by the MeshColliders
	public static readonly MeshColliderCookingOptions COLLIDER_COOKING_OPTIONS = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices | MeshColliderCookingOptions.UseFastMidphase;

	// Debug Settings
	private static bool showHitbox = false;
	private MeshFilter hitboxFilter;

	// Draw Flags
	public bool drawMain = false;

	// Load Event Flags
	// Every request to run load events increments requestedLoadVersion. Builds keep collecting the load list
	// until a build sends a BATCHLOADBUD built on top of the latest request
	private int requestedLoadVersion = 0;
	private int sentLoadVersion = 0;


	/*
		Unity Settings
	*/
	// Block Chunks 
	public ChunkRenderer renderer;
	public MeshFilter meshFilter;
	public MeshCollider meshCollider;
	public GameObject obj;
	public ChunkLoader loader;

	// Decal Chunk
	public MeshFilter meshFilterDecal;
	public GameObject objDecal;

	// Raycast Collider Chunk
	public MeshCollider meshColliderRaycast;
	public GameObject objRaycast;

	// Meshes
	// Collider meshes are double-buffered: builds write into the mesh that isn't used by the MeshCollider,
	// so the current collision stays valid while the new one is cooked
	private Mesh mesh;
	private Mesh meshDecal;
	private Mesh[] meshCollision;
	private Mesh[] meshRaycast;
	private int activeCollider = 0;
	private Mesh[] buildTargets;

	// Build currently running for this chunk
	private ChunkBuildSlot buildSlot;


	public Chunk(ChunkPos pos, ChunkRenderer r, ChunkLoader loader){
		this.pos = pos;
		this.needsGeneration = 0;
		this.renderer = r;
		this.loader = loader;

		// Game Object Settings
		this.obj = new GameObject();
		this.obj.name = "Chunk " + pos.x + ", " + pos.z + ", " + ((ChunkDepthID)pos.y).ToString()[0];
		this.obj.transform.SetParent(this.renderer.transform);
		this.obj.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.objDecal = new GameObject();
		this.objDecal.name = "Decals " + pos.x + ", " + pos.z + ", " + ((ChunkDepthID)pos.y).ToString()[0];
		this.objDecal.transform.SetParent(this.renderer.transform);
		this.objDecal.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.objRaycast = new GameObject();
		this.objRaycast.name = "RaycastCollider " + pos.x + ", " + pos.z + ", " + ((ChunkDepthID)pos.y).ToString()[0];
		this.objRaycast.transform.SetParent(this.renderer.transform);
		this.objRaycast.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.objRaycast.layer = 11;

		this.data = new VoxelData(pos);
		this.metadata = new VoxelMetadata();

		this.meshFilter = this.obj.AddComponent<MeshFilter>();
		MeshRenderer msr = this.obj.AddComponent<MeshRenderer>() as MeshRenderer;
		this.meshCollider = this.obj.AddComponent<MeshCollider>();
		this.meshCollider.cookingOptions = COLLIDER_COOKING_OPTIONS;
		this.meshFilterDecal = this.objDecal.AddComponent<MeshFilter>();
		MeshRenderer msrDecal = this.objDecal.AddComponent<MeshRenderer>() as MeshRenderer;
		msrDecal.shadowCastingMode = ShadowCastingMode.Off;
		this.meshColliderRaycast = this.objRaycast.AddComponent<MeshCollider>() as MeshCollider;
		this.meshColliderRaycast.cookingOptions = COLLIDER_COOKING_OPTIONS;

		msr.sharedMaterials = this.renderer.GetComponent<MeshRenderer>().sharedMaterials;
		msrDecal.material = this.renderer.decalMaterial;
		this.obj.layer = 8;

		// Mesh Initialization
		this.mesh = new Mesh();
		this.meshDecal = new Mesh();
		this.meshCollision = new Mesh[]{new Mesh(), new Mesh()};
		this.meshRaycast = new Mesh[]{new Mesh(), new Mesh()};
		this.buildTargets = new Mesh[WriteChunkMeshJob.MESH_COUNT];

		if(showHitbox){
			this.hitboxFilter = this.objRaycast.AddComponent<MeshFilter>() as MeshFilter;
			this.objRaycast.AddComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		}
	}

	// Dummy Chunk Generation
	// CANNOT BE USED TO DRAW, ONLY TO ADD ELEMENTS AND SAVE
	public Chunk(ChunkPos pos, bool server=false){
		this.biome = (byte)BiomeCode.PLAINS;
		this.pos = pos;
		this.needsGeneration = 1;

		if(!server)
			this.data = new VoxelData(new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth], pos);
		else
			this.data = new VoxelData();

		this.metadata = new VoxelMetadata();
	}

	// Makes the next drawn build send a BATCHLOADBUD, recreating VFX, SFX and lights of load event blocks
	public void RequestLoadEvents(){
		this.requestedLoadVersion++;
	}

	public int GetRequestedLoadVersion(){return this.requestedLoadVersion;}
	public int GetSentLoadVersion(){return this.sentLoadVersion;}

	// Sends the load events found by a build. loadVersion is the load request the build was made on top of
	public void SendLoadEvents(NativeList<int3> coords, int loadVersion){
		NetMessage message = new NetMessage(NetCode.BATCHLOADBUD);
		message.BatchLoadBUD(this.pos);

		for(int i=0; i < coords.Length; i++){
			message.AddBatchLoad(coords[i].x, coords[i].y, coords[i].z);
		}

		this.loader.client.Send(message);

		if(loadVersion > this.sentLoadVersion)
			this.sentLoadVersion = loadVersion;
	}

	public bool IsBuilding(){return this.buildSlot != null;}
	public void SetBuildSlot(ChunkBuildSlot slot){this.buildSlot = slot;}

	// Meshes a build writes into, in the order of WriteChunkMeshJob
	public Mesh[] GetBuildTargets(){
		int next = 1 - this.activeCollider;

		this.buildTargets[WriteChunkMeshJob.RENDER_MESH] = this.mesh;
		this.buildTargets[WriteChunkMeshJob.COLLIDER_MESH] = this.meshCollision[next];
		this.buildTargets[WriteChunkMeshJob.DECAL_MESH] = this.meshDecal;
		this.buildTargets[WriteChunkMeshJob.RAYCAST_MESH] = this.meshRaycast[next];

		return this.buildTargets;
	}

	// Called once a build applied its render meshes
	public void AttachRenderMeshes(){
		if(this.meshFilter.sharedMesh != this.mesh)
			this.meshFilter.sharedMesh = this.mesh;
		if(this.meshFilterDecal.sharedMesh != this.meshDecal)
			this.meshFilterDecal.sharedMesh = this.meshDecal;
	}

	// Makes the MeshColliders use the meshes written by the last build, which must already be cooked
	public void SwapColliders(bool hasCollider, bool hasRaycast){
		int next = 1 - this.activeCollider;

		this.meshCollider.sharedMesh = hasCollider ? this.meshCollision[next] : null;
		this.meshColliderRaycast.sharedMesh = hasRaycast ? this.meshRaycast[next] : null;

		if(showHitbox){
			this.hitboxFilter.sharedMesh = this.meshRaycast[next];
		}

		// Previous collider meshes are no longer referenced
		this.meshCollision[this.activeCollider].Clear();
		this.meshRaycast[this.activeCollider].Clear();
		this.activeCollider = next;
	}

	// Clone
	public Chunk Clone(){
		Chunk c = new Chunk(this.pos, this.renderer, this.loader);

		c.biome = this.biome;
		c.data = new VoxelData(this.data.GetData(), this.pos);
		c.metadata = new VoxelMetadata(this.metadata);

		return c;
	}

	public void Destroy(){
		// Jobs of a running build must finish before its meshes are destroyed
		if(this.buildSlot != null)
			this.buildSlot.Abort();

		GameObject.Destroy(this.obj);
		GameObject.Destroy(this.objDecal);
		GameObject.Destroy(this.objRaycast);
		this.obj = null;
		this.objDecal = null;
		this.objRaycast = null;

		Object.Destroy(this.mesh);
		Object.Destroy(this.meshDecal);

		if(this.meshCollision != null){
			for(int i=0; i < this.meshCollision.Length; i++){
				Object.Destroy(this.meshCollision[i]);
				Object.Destroy(this.meshRaycast[i]);
			}
		}

		this.meshFilter = null;
		this.meshCollider = null; 
		this.meshColliderRaycast = null;
		this.meshFilterDecal = null;
		this.hitboxFilter = null;
		this.loader = null;

		if(this.data != null)
			this.data.Destroy();
		if(this.metadata != null)
			this.metadata.Destroy();

		this.data = null;
		this.metadata = null;
		this.mesh = null;
		this.meshDecal = null;
		this.meshCollision = null;
		this.meshRaycast = null;
		this.buildTargets = null;
	}
	
	// Writes the chunk's header into a buffer, starting at offset
	public void WriteHeader(byte[] output, int offset){
		output[offset] = this.biome;

		// CURRENTLY ONLY OUTPUTTING LAST VISITED TIME AS 0
		for(int i=1; i < 8; i++)
			output[offset+i] = 0;

		output[offset+8] = this.needsGeneration;

		// The next 12 bytes are size information and don't need to be presented
		for(int i=9; i < RegionFileHandler.chunkHeaderSize; i++)
			output[offset+i] = 0;
	}


	public void BuildOnVoxelData(VoxelData vd){
		this.data = vd;

		if(this.changeSet != null){
			vd.SetChangeSet(this.changeSet);
			this.changeSet.MarkAll();
		}
	}

	public void BuildVoxelMetadata(VoxelMetadata vm){
		this.metadata = vm;

		if(this.changeSet != null){
			vm.SetChangeSet(this.changeSet);
			this.changeSet.MarkAll();
		}
	}

	// Server-side: starts recording the voxels changed in this chunk, so only them are sent to clients
	public void EnableChangeTracking(){
		if(this.changeSet == null)
			this.changeSet = new ChunkChangeSet(this.pos);

		this.data.SetChangeSet(this.changeSet);
		this.metadata.SetChangeSet(this.changeSet);
	}

	// Server-side: the chunk may have changed without going through VoxelData or VoxelMetadata setters
	public void MarkChanged(){
		if(this.changeSet != null)
			this.changeSet.MarkVersion();
	}
}
