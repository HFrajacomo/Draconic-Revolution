using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Rendering;

public class Chunk
{
	// Chunk Settings
	public VoxelData data;
	public VoxelMetadata metadata;
	public static readonly int chunkWidth = 16;
	public static readonly int chunkDepth = 100;
	public static float chunkWidthMult = 15.99f; 
	public ChunkPos pos;
	public string biomeName;
	public byte needsGeneration;
	public float4 features;
	public string lastVisitedTime;

	// Multiplayer Settings
	private NetMessage message;

	// Draw Flags
	private bool xPlusDrawn = false;
	private bool zPlusDrawn = false;
	private bool xMinusDrawn = false;
	private bool zMinusDrawn = false;
	public bool drawMain = false;

	// Unity Settings
	public ChunkRenderer renderer;
	public MeshFilter meshFilter;
	public MeshCollider meshCollider;
	public GameObject obj;
	public BlockEncyclopedia blockBook;
	public ChunkLoader loader;

	// Cache Information
    private List<Vector3> vertices = new List<Vector3>();
    private int[] specularTris;
    private int[] liquidTris;
    private int[] assetTris;
    private int[] triangles;
  	private List<Vector2> UVs = new List<Vector2>();
  	private List<Vector2> lightUVMain = new List<Vector2>();
  	private List<Vector3> normals = new List<Vector3>();

    // Assets Cache
    private List<ushort> cacheCodes = new List<ushort>();
    private List<Vector3> cacheVertsv3 = new List<Vector3>();
    private List<Vector2> cacheLightUV = new List<Vector2>();
    private List<int> cacheTris = new List<int>();
    private List<Vector2> cacheUVv2 = new List<Vector2>();
    private List<Vector3> cacheNormals = new List<Vector3>();
    private List<int> indexVert = new List<int>();
    private List<int> indexUV = new List<int>();
    private List<int> indexTris = new List<int>();
    private List<int> indexNormals = new List<int>();
    private List<Vector3> scalingFactor = new List<Vector3>();
    private List<Vector2> UVaux = new List<Vector2>();
    private List<Vector3> vertexAux = new List<Vector3>();
    private List<Vector3> normalAux = new List<Vector3>();

    private Mesh mesh;

	public Chunk(ChunkPos pos, ChunkRenderer r, BlockEncyclopedia be, ChunkLoader loader){
		this.pos = pos;
		this.needsGeneration = 0;
		this.renderer = r;
		this.loader = loader;

		// Game Object Settings
		this.obj = new GameObject();
		this.obj.name = "Chunk " + pos.x + ", " + pos.z;
		this.obj.transform.SetParent(this.renderer.transform);
		this.obj.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);

		this.data = new VoxelData();
		this.metadata = new VoxelMetadata();

		this.obj.AddComponent<MeshFilter>();
		MeshRenderer msr = this.obj.AddComponent<MeshRenderer>() as MeshRenderer;
		this.obj.AddComponent<MeshCollider>();

		this.meshFilter = this.obj.GetComponent<MeshFilter>();
		this.meshCollider = this.obj.GetComponent<MeshCollider>();
		this.obj.GetComponent<MeshRenderer>().sharedMaterials = this.renderer.GetComponent<MeshRenderer>().sharedMaterials;
		this.blockBook = be;
		this.obj.layer = 8;

		this.mesh = new Mesh();
	}

	// Dummy Chunk Generation
	// CANNOT BE USED TO DRAW, ONLY TO ADD ELEMENTS AND SAVE
	public Chunk(ChunkPos pos, bool server=false){
		this.biomeName = "Plains";
		this.pos = pos;
		this.needsGeneration = 1;

		if(!server)
			this.data = new VoxelData(new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth]);
		else
			this.data = new VoxelData();

		this.metadata = new VoxelMetadata();
	}

	// Clone
	public Chunk Clone(){
		Chunk c = new Chunk(this.pos, this.renderer, this.blockBook, this.loader);

		c.biomeName = this.biomeName;
		c.data = new VoxelData(this.data.GetData());
		c.metadata = new VoxelMetadata(this.metadata);

		return c;
	}
	
	// Returns the chunk's header in byte array format
	public byte[] GetHeader(){
		byte[] output = new byte[RegionFileHandler.chunkHeaderSize];

		output[0] = BiomeHandler.BiomeToByte(this.biomeName);
		// CURRENTLY ONLY OUTPUTTING LAST VISITED TIME AS 0
		output[1] = 0;
		output[2] = 0;
		output[3] = 0;
		output[4] = 0;
		output[5] = 0;
		output[6] = 0;
		output[7] = 0;

		output[8] = this.needsGeneration;

		// The next 12 bytes are size information and don't need to be presented
		return output;
	}


	public void BuildOnVoxelData(VoxelData vd){
		this.data = vd;
	}

	public void BuildVoxelMetadata(VoxelMetadata vm){
		this.metadata = vm;
	}

	// Draws Chunk Borders. Returns true if all borders have been drawn, otherwise, return false.
	public bool BuildSideBorder(bool reload=false, bool reloadXM=false, bool reloadXP=false, bool reloadZM=false, bool reloadZP=false){
		if(reload){
			this.xMinusDrawn = false;
			this.xPlusDrawn = false;
			this.zMinusDrawn = false;
			this.zPlusDrawn = false;
		}

		if(reloadXM)
			this.xMinusDrawn = false;
		if(reloadXP)
			this.xPlusDrawn = false;
		if(reloadZM)
			this.zMinusDrawn = false;
		if(reloadZP)
			this.zPlusDrawn = false;

		// If current operation CANNOT update borders
		if(xMinusDrawn && xPlusDrawn && zMinusDrawn && zPlusDrawn){
			return true;
		}

		// Fast check if current border is at the edge of render distance
		if(!this.ShouldRun()){
			return false;
		}


		bool changed = false; // Flag is set if any change has been made that requires a redraw
		int3[] coordArray;
		int3[] budArray;

		NativeArray<ushort> blockdata = new NativeArray<ushort>(this.data.GetData(), Allocator.TempJob);
		NativeArray<ushort> metadata = new NativeArray<ushort>(this.metadata.GetStateData(), Allocator.TempJob);
		NativeArray<byte> lightdata = new NativeArray<byte>(this.data.GetLightMap(), Allocator.TempJob);

		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> uvs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> lightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<int> tris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> specularTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> liquidTris = new NativeList<int>(0, Allocator.TempJob);
	
		NativeList<int3> toLoadEvent = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> toBUD = new NativeList<int3>(0, Allocator.TempJob);

		NativeArray<byte> blockTransparent = new NativeArray<byte>(BlockEncyclopediaECS.blockTransparent, Allocator.TempJob);
		NativeArray<byte> objectTransparent = new NativeArray<byte>(BlockEncyclopediaECS.objectTransparent, Allocator.TempJob);
		NativeArray<bool> blockLiquid = new NativeArray<bool>(BlockEncyclopediaECS.blockLiquid, Allocator.TempJob);
		NativeArray<bool> objectLiquid = new NativeArray<bool>(BlockEncyclopediaECS.objectLiquid, Allocator.TempJob);
		NativeArray<bool> blockInvisible = new NativeArray<bool>(BlockEncyclopediaECS.blockInvisible, Allocator.TempJob);
		NativeArray<bool> objectInvisible = new NativeArray<bool>(BlockEncyclopediaECS.objectInvisible, Allocator.TempJob);
		NativeArray<byte> blockMaterial = new NativeArray<byte>(BlockEncyclopediaECS.blockMaterial, Allocator.TempJob);
		NativeArray<byte> objectMaterial = new NativeArray<byte>(BlockEncyclopediaECS.objectMaterial, Allocator.TempJob);
		NativeArray<int3> blockTiles = new NativeArray<int3>(BlockEncyclopediaECS.blockTiles, Allocator.TempJob);
		NativeArray<bool> blockWashable = new NativeArray<bool>(BlockEncyclopediaECS.blockWashable, Allocator.TempJob);
		NativeArray<bool> objectWashable = new NativeArray<bool>(BlockEncyclopediaECS.objectWashable, Allocator.TempJob);

		// Cached
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheUVVerts = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);

		// For Init
		this.meshFilter.sharedMesh.GetVertices(vertexAux);
		NativeArray<Vector3> disposableVerts = new NativeArray<Vector3>(vertexAux.ToArray(), Allocator.TempJob);
		vertexAux.Clear();

		this.meshFilter.sharedMesh.GetUVs(0, UVaux);
		NativeArray<Vector2> disposableUVS = new NativeArray<Vector2>(UVaux.ToArray(), Allocator.TempJob);
		UVaux.Clear();

		this.meshFilter.sharedMesh.GetUVs(3, UVaux);
		NativeArray<Vector2> disposableLight = new NativeArray<Vector2>(UVaux.ToArray(), Allocator.TempJob);
		UVaux.Clear();

		this.meshFilter.sharedMesh.GetNormals(normalAux);
		NativeArray<Vector3> disposableNormals = new NativeArray<Vector3>(normalAux.ToArray(), Allocator.TempJob);
		normalAux.Clear();

		NativeArray<int> disposableTris = new NativeArray<int>(this.meshFilter.sharedMesh.GetTriangles(0), Allocator.TempJob);
		NativeArray<int> disposableSpecTris = new NativeArray<int>(this.meshFilter.sharedMesh.GetTriangles(1), Allocator.TempJob);
		NativeArray<int> disposableLiquidTris = new NativeArray<int>(this.meshFilter.sharedMesh.GetTriangles(2), Allocator.TempJob);


		JobHandle job;


		// Initialize Data
		verts.AddRange(disposableVerts);
		uvs.AddRange(disposableUVS);
		lightUV.AddRange(disposableLight);
		tris.AddRange(disposableTris);
		specularTris.AddRange(disposableSpecTris);
		liquidTris.AddRange(disposableLiquidTris);
		normals.AddRange(disposableNormals);


		// Dispose Init
		disposableVerts.Dispose();
		disposableUVS.Dispose();
		disposableTris.Dispose();
		disposableSpecTris.Dispose();
		disposableLiquidTris.Dispose();
		disposableNormals.Dispose();
		disposableLight.Dispose();


		// X- Analysis
		ChunkPos targetChunk = new ChunkPos(this.pos.x-1, this.pos.z); 
		if(loader.chunks.ContainsKey(targetChunk) && !xMinusDrawn){
			this.xMinusDrawn = true;
			changed = true;

			NativeArray<ushort> neighbordata = new NativeArray<ushort>(loader.chunks[targetChunk].data.GetData(), Allocator.TempJob);
			
			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = reload,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				toLoadEvent = toLoadEvent,
				xM = true,
				xP = false,
				zP = false,
				zM = false,
				verts = verts,
				uvs = uvs,
				normals = normals,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = blockTransparent,
				objectTransparent = objectTransparent,
				blockLiquid = blockLiquid,
				objectLiquid = objectLiquid,
				blockInvisible = blockInvisible,
				objectInvisible = objectInvisible,
				blockMaterial = blockMaterial,
				objectMaterial = objectMaterial,
				blockWashable = blockWashable,
				objectWashable = objectWashable,
				blockTiles = blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();
			
			neighbordata.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				// SEND BUD
				this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, coord.x, coord.y, coord.z, 0, 0, 0, 0);
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}			
			toLoadEvent.Clear();
		}

		// X+ Analysis
		targetChunk = new ChunkPos(this.pos.x+1, this.pos.z); 
		if(loader.chunks.ContainsKey(targetChunk) && !xPlusDrawn){
			this.xPlusDrawn = true;
			changed = true;

			NativeArray<ushort> neighbordata = new NativeArray<ushort>(loader.chunks[targetChunk].data.GetData(), Allocator.TempJob);

			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = reload,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				toLoadEvent = toLoadEvent,
				xM = false,
				xP = true,
				zP = false,
				zM = false,
				verts = verts,
				uvs = uvs,
				normals = normals,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = blockTransparent,
				objectTransparent = objectTransparent,
				blockLiquid = blockLiquid,
				objectLiquid = objectLiquid,
				blockInvisible = blockInvisible,
				objectInvisible = objectInvisible,
				blockMaterial = blockMaterial,
				objectMaterial = objectMaterial,
				blockWashable = blockWashable,
				objectWashable = objectWashable,
				blockTiles = blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();

			neighbordata.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				// SEND BUD
				this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, coord.x, coord.y, coord.z, 0, 0, 0, 0);
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}			
			toLoadEvent.Clear();
		}

		// Z- Analysis
		targetChunk = new ChunkPos(this.pos.x, this.pos.z-1); 
		if(loader.chunks.ContainsKey(targetChunk) && !zMinusDrawn){
			this.zMinusDrawn = true;
			changed = true;

			NativeArray<ushort> neighbordata = new NativeArray<ushort>(loader.chunks[targetChunk].data.GetData(), Allocator.TempJob);
			
			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = reload,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				toLoadEvent = toLoadEvent,
				xM = false,
				xP = false,
				zP = false,
				zM = true,
				verts = verts,
				uvs = uvs,
				normals = normals,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = blockTransparent,
				objectTransparent = objectTransparent,
				blockLiquid = blockLiquid,
				objectLiquid = objectLiquid,
				blockInvisible = blockInvisible,
				objectInvisible = objectInvisible,
				blockMaterial = blockMaterial,
				objectMaterial = objectMaterial,
				blockWashable = blockWashable,
				objectWashable = objectWashable,
				blockTiles = blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();

			neighbordata.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				// SEND BUD
				this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, coord.x, coord.y, coord.z, 0, 0, 0, 0);
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}			
			toLoadEvent.Clear();
		}

		// Z+ Analysis
		targetChunk = new ChunkPos(this.pos.x, this.pos.z+1); 
		if(loader.chunks.ContainsKey(targetChunk) && !zPlusDrawn){
			this.zPlusDrawn = true;
			changed = true;

			NativeArray<ushort> neighbordata = new NativeArray<ushort>(loader.chunks[targetChunk].data.GetData(), Allocator.TempJob);
			
			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = reload,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				toLoadEvent = toLoadEvent,
				xM = false,
				xP = false,
				zP = true,
				zM = false,
				verts = verts,
				uvs = uvs,
				normals = normals,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = blockTransparent,
				objectTransparent = objectTransparent,
				blockLiquid = blockLiquid,
				objectLiquid = objectLiquid,
				blockInvisible = blockInvisible,
				objectInvisible = objectInvisible,
				blockMaterial = blockMaterial,
				objectMaterial = objectMaterial,
				blockWashable = blockWashable,
				objectWashable = objectWashable,
				blockTiles = blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();

			neighbordata.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				// SEND BUD
				this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, coord.x, coord.y, coord.z, 0, 0, 0, 0);
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}			
			toLoadEvent.Clear();
		}

		// Runs BUD in neighbor chunks
		budArray = toBUD.AsArray().ToArray();
		foreach(int3 bu in budArray){
			// SEND BUD
			this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
			this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, bu.x, bu.y, bu.z, 0, 0, 0, ushort.MaxValue);
			this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
		}
		
		// If mesh wasn't redrawn
		if(changed){
			this.triangles = tris.ToArray();
			this.specularTris = specularTris.ToArray();
			this.liquidTris = liquidTris.ToArray();
			assetTris = this.meshFilter.sharedMesh.GetTriangles(3);

			BuildMeshSide(verts.ToArray(), uvs.ToArray(), lightUV.ToArray(), normals.ToArray());
		}

		blockdata.Dispose();
		metadata.Dispose();
		verts.Dispose();
		uvs.Dispose();
		normals.Dispose();
		tris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		blockTransparent.Dispose();
		objectTransparent.Dispose();
		blockLiquid.Dispose();
		objectLiquid.Dispose();
		blockInvisible.Dispose();
		objectInvisible.Dispose();
		blockMaterial.Dispose();
		objectMaterial.Dispose();
		blockWashable.Dispose();
		objectWashable.Dispose();
		blockTiles.Dispose();
		cacheCubeVert.Dispose();
		cacheUVVerts.Dispose();
		cacheCubeNormal.Dispose();
		toLoadEvent.Dispose();
		toBUD.Dispose();
		lightUV.Dispose();
		lightdata.Dispose();

    	this.vertices.Clear();
    	this.triangles = null;
    	this.specularTris = null;
    	this.liquidTris = null;
    	this.assetTris = null;
    	this.UVs.Clear();
    	this.lightUVMain.Clear();

		// If current operation CANNOT update borders
		if(xMinusDrawn && xPlusDrawn && zMinusDrawn && zPlusDrawn)
			return true;
		else
			return false;
	}


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(bool load=false, bool pregenReload=false){
		NativeArray<ushort> blockdata = new NativeArray<ushort>(this.data.GetData(), Allocator.TempJob);
		NativeArray<ushort> statedata = new NativeArray<ushort>(this.metadata.GetStateData(), Allocator.TempJob);
		NativeArray<byte> lightdata = new NativeArray<byte>(this.data.GetLightMap(), Allocator.TempJob);
		
		NativeList<int3> loadCoordList = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<ushort> loadCodeList = new NativeList<ushort>(0, Allocator.TempJob);
		NativeList<int3> loadAssetList = new NativeList<int3>(0, Allocator.TempJob);

		NativeList<int> normalTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> specularTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> liquidTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> lightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheCubeUV = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);

		// Cached from Block Encyclopedia ECS
		NativeArray<byte> blockTransparent = new NativeArray<byte>(BlockEncyclopediaECS.blockTransparent, Allocator.TempJob);
		NativeArray<byte> objectTransparent = new NativeArray<byte>(BlockEncyclopediaECS.objectTransparent, Allocator.TempJob);
		NativeArray<bool> blockLiquid = new NativeArray<bool>(BlockEncyclopediaECS.blockLiquid, Allocator.TempJob);
		NativeArray<bool> objectLiquid = new NativeArray<bool>(BlockEncyclopediaECS.objectLiquid, Allocator.TempJob);
		NativeArray<bool> blockLoad = new NativeArray<bool>(BlockEncyclopediaECS.blockLoad, Allocator.TempJob);
		NativeArray<bool> objectLoad = new NativeArray<bool>(BlockEncyclopediaECS.objectLoad, Allocator.TempJob);
		NativeArray<bool> blockInvisible = new NativeArray<bool>(BlockEncyclopediaECS.blockInvisible, Allocator.TempJob);
		NativeArray<bool> objectInvisible = new NativeArray<bool>(BlockEncyclopediaECS.objectInvisible, Allocator.TempJob);
		NativeArray<byte> blockMaterial = new NativeArray<byte>(BlockEncyclopediaECS.blockMaterial, Allocator.TempJob);
		NativeArray<byte> objectMaterial = new NativeArray<byte>(BlockEncyclopediaECS.objectMaterial, Allocator.TempJob);
		NativeArray<int3> blockTiles = new NativeArray<int3>(BlockEncyclopediaECS.blockTiles, Allocator.TempJob);
		NativeArray<bool> objectNeedRotation = new NativeArray<bool>(BlockEncyclopediaECS.objectNeedRotation, Allocator.TempJob);
		NativeArray<bool> blockWashable = new NativeArray<bool>(BlockEncyclopediaECS.blockWashable, Allocator.TempJob);
		NativeArray<bool> objectWashable = new NativeArray<bool>(BlockEncyclopediaECS.objectWashable, Allocator.TempJob);

		// Threading Job
		BuildChunkJob bcJob = new BuildChunkJob{
			load = load,
			data = blockdata,
			state = statedata,
			lightdata = lightdata,
			loadOutList = loadCoordList,
			loadAssetList = loadAssetList,
			verts = verts,
			UVs = UVs,
			lightUV = lightUV,
			normals = normals,
			normalTris = normalTris,
			specularTris = specularTris,
			liquidTris = liquidTris,
			cacheCubeVert = cacheCubeVert,
			cacheCubeUV = cacheCubeUV,
			cacheCubeNormal = cacheCubeNormal,
			blockTransparent = blockTransparent,
			objectTransparent = objectTransparent,
			blockLiquid = blockLiquid,
			objectLiquid = objectLiquid,
			blockLoad = blockLoad,
			objectLoad = objectLoad,
			blockInvisible = blockInvisible,
			objectInvisible = objectInvisible,
			blockMaterial = blockMaterial,
			objectMaterial = objectMaterial,
			blockWashable = blockWashable,
			objectWashable = objectWashable,
			blockTiles = blockTiles
		};
		JobHandle job = bcJob.Schedule();
		job.Complete();


		this.indexVert.Add(0);
		this.indexUV.Add(0);
		this.indexTris.Add(0);

		// Offseting and Rotation shenanigans
		NativeHashMap<int, Vector3> scaleOffset = new NativeHashMap<int, Vector3>(0, Allocator.TempJob);
		NativeHashMap<int, int> rotationOffset = new NativeHashMap<int, int>(0, Allocator.TempJob);
		
		int3[] coordArray = loadAssetList.AsArray().ToArray();
		foreach(int3 coord in coordArray){
			ushort assetCode = this.data.GetCell(coord);

			if(!this.cacheCodes.Contains(assetCode)){
				this.cacheCodes.Add(assetCode);
				blockBook.objects[ushort.MaxValue-assetCode].mesh.GetVertices(vertexAux);
				this.cacheVertsv3.AddRange(vertexAux.ToArray());
				this.cacheTris.AddRange(blockBook.objects[ushort.MaxValue-assetCode].mesh.GetTriangles(0));
				blockBook.objects[ushort.MaxValue-assetCode].mesh.GetUVs(0, UVaux);
				this.cacheUVv2.AddRange(UVaux.ToArray());
				this.indexVert.Add(this.indexVert[indexVert.Count-1] + vertexAux.Count);
				this.indexTris.Add(this.indexTris[indexTris.Count-1] + blockBook.objects[ushort.MaxValue-assetCode].mesh.GetTriangles(0).Length);
				this.indexUV.Add(this.indexUV[indexUV.Count-1] + UVaux.Count);
				blockBook.objects[ushort.MaxValue-assetCode].mesh.GetNormals(normalAux);
				this.cacheNormals.AddRange(normalAux.ToArray());
				this.scalingFactor.Add(BlockEncyclopediaECS.objectScaling[ushort.MaxValue-assetCode]);
				vertexAux.Clear();
				UVaux.Clear();


				// If has special offset or rotation
				// Hash function for Dictionary is blockCode*256 + state. This leaves a maximum of 256 states for every object in the game
				if(blockBook.objects[ushort.MaxValue - assetCode].needsRotation){
					for(ushort i=0; i < blockBook.objects[ushort.MaxValue - assetCode].stateNumber; i++){
						scaleOffset.Add((int)(assetCode*256 + i), blockBook.objects[ushort.MaxValue - assetCode].GetOffsetVector(i));
						rotationOffset.Add((int)(assetCode*256 + i), blockBook.objects[ushort.MaxValue - assetCode].GetRotationValue(i));
					}
				}
			}
		}


		// ToLoad() Event Trigger
		
		if(this.biomeName == "Ocean"){
			coordArray = loadCoordList.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				if(this.data.GetCell(coord) != 6){ // Water
					this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
					this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, coord.x, coord.y, coord.z, 0, 0, 0, 0);
					this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
				}
			}
		}
		else{
		
			coordArray = loadCoordList.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				// SENDS BUD TO SERVER
				this.message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				this.message.DirectBlockUpdate(BUDCode.LOAD, this.pos, coord.x, coord.y, coord.z, 0, 0, 0, 0);
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}			
		}
		loadCoordList.Clear();
		

		NativeList<Vector3> meshVerts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> meshUVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> meshLightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> meshNormals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<int> meshTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<ushort> blockCodes = new NativeList<ushort>(0, Allocator.TempJob);
		blockCodes.CopyFrom(this.cacheCodes.ToArray());
		NativeList<int> vertsOffset = new NativeList<int>(0, Allocator.TempJob);
		vertsOffset.CopyFrom(this.indexVert.ToArray());
		NativeList<int> trisOffset = new NativeList<int>(0, Allocator.TempJob);
		trisOffset.CopyFrom(this.indexTris.ToArray());
		NativeList<int> UVOffset = new NativeList<int>(0, Allocator.TempJob);
		UVOffset.CopyFrom(this.indexUV.ToArray());
		NativeArray<Vector3> loadedVerts = new NativeArray<Vector3>(this.cacheVertsv3.ToArray(), Allocator.TempJob);
		NativeArray<Vector2> loadedUV = new NativeArray<Vector2>(this.cacheUVv2.ToArray(), Allocator.TempJob);
		NativeArray<Vector3> loadedNormals = new NativeArray<Vector3>(this.cacheNormals.ToArray(), Allocator.TempJob);
		NativeArray<int> loadedTris = new NativeArray<int>(this.cacheTris.ToArray(), Allocator.TempJob);
		NativeArray<Vector3> scaling = new NativeArray<Vector3>(this.scalingFactor.ToArray(), Allocator.TempJob);

		PrepareAssetsJob paJob = new PrepareAssetsJob{
			vCount = verts.Length,

			meshVerts = meshVerts,
			meshTris = meshTris,
			meshUVs = meshUVs,
			meshLightUV = meshLightUV,
			meshNormals = meshNormals,
			scaling = scaling,
			needRotation = objectNeedRotation,
			inplaceOffset = scaleOffset,
			inplaceRotation = rotationOffset,

			coords = loadAssetList,
			blockCodes = blockCodes,
			blockdata = blockdata,
			metadata = statedata,
			lightdata = lightdata,

			vertsOffset = vertsOffset,
			trisOffset = trisOffset,
			UVOffset = UVOffset,

			loadedVerts = loadedVerts,
			loadedUV = loadedUV,
			loadedTris = loadedTris,
			loadedNormals = loadedNormals
		};
		job = paJob.Schedule();
		job.Complete();

		// Convert data back
		this.vertices.AddRange(verts.ToArray());

		this.vertices.AddRange(meshVerts.ToArray());
		this.triangles = normalTris.ToArray();
		this.assetTris = meshTris.ToArray();

		this.specularTris = specularTris.ToArray();
		this.liquidTris = liquidTris.ToArray();

		this.UVs.AddRange(UVs.ToArray());
		this.UVs.AddRange(meshUVs.ToArray());

		this.lightUVMain.AddRange(lightUV.ToArray());
		this.lightUVMain.AddRange(meshLightUV.ToArray());

		this.normals.AddRange(normals.ToArray());
		this.normals.AddRange(meshNormals.ToArray()); 


		// Dispose Bin
		verts.Dispose();
		normalTris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		blockdata.Dispose();
		statedata.Dispose();
		loadCoordList.Dispose();
		blockTransparent.Dispose();
		objectTransparent.Dispose();
		blockLiquid.Dispose();
		objectLiquid.Dispose();
		blockLoad.Dispose();
		objectLoad.Dispose();
		blockInvisible.Dispose();
		objectInvisible.Dispose();
		blockMaterial.Dispose();
		objectMaterial.Dispose();
		blockTiles.Dispose();
		blockWashable.Dispose();
		objectWashable.Dispose();
		cacheCubeVert.Dispose();
		cacheCubeNormal.Dispose();
		cacheCubeUV.Dispose();
		loadCodeList.Dispose();
		meshVerts.Dispose();
		meshTris.Dispose();
		blockCodes.Dispose();
		vertsOffset.Dispose();
		trisOffset.Dispose();
		UVOffset.Dispose();
		loadedVerts.Dispose();
		loadedTris.Dispose();
		loadedUV.Dispose();
		loadedNormals.Dispose();
		loadAssetList.Dispose();
		scaling.Dispose();
		UVs.Dispose();
		normals.Dispose();
		meshUVs.Dispose();
		meshNormals.Dispose();
		objectNeedRotation.Dispose();
		scaleOffset.Dispose();
		rotationOffset.Dispose();
		lightUV.Dispose();
		lightdata.Dispose();
		meshLightUV.Dispose();


		BuildMesh();


		// Dispose Asset Cache
		cacheCodes.Clear();
		cacheVertsv3.Clear();
		cacheTris.Clear();
		cacheUVv2.Clear();
		cacheNormals.Clear();
		indexVert.Clear();
		indexUV.Clear();
		indexTris.Clear();
		scalingFactor.Clear();
		this.cacheLightUV.Clear();
    	this.vertices.Clear();
    	this.lightUVMain.Clear();
    	this.triangles = null;
    	this.specularTris = null;
    	this.liquidTris = null;
    	this.assetTris = null;
    	this.UVs.Clear();
    	this.normals.Clear();

		this.drawMain = true;
    }

    // Checks if current BuildChunkSide call should be calculated
    private bool ShouldRun(){
    	ChunkPos targetChunk;

    	targetChunk = new ChunkPos(this.pos.x-1, this.pos.z);

    	if(loader.chunks.ContainsKey(targetChunk) && !this.xMinusDrawn)
    		return true;

    	targetChunk = new ChunkPos(this.pos.x+1, this.pos.z);

    	if(loader.chunks.ContainsKey(targetChunk) && !this.xPlusDrawn)
    		return true;

    	targetChunk = new ChunkPos(this.pos.x, this.pos.z-1);

    	if(loader.chunks.ContainsKey(targetChunk) && !this.zMinusDrawn)
    		return true;

    	targetChunk = new ChunkPos(this.pos.x, this.pos.z+1);

    	if(loader.chunks.ContainsKey(targetChunk) && !this.zPlusDrawn)
    		return true;

    	if(loader.chunks.Count <= 2)
    		return true;

    	return false;
    }

    // Returns n ShadowUVs to a list
    private void FillShadowUV(ref List<Vector2> l, int3 coord, int n){
    	float light = this.data.GetLight(coord);

    	for(int i = 0; i < n; i++){
    		l.Add(new Vector2(light, 1));
    	}
    }

    // Builds meshes from verts, UVs and tris from different layers
    private void BuildMesh(){
    	mesh.Clear();

    	if(this.vertices.Count >= ushort.MaxValue){
    		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	mesh.subMeshCount = 4;

    	mesh.SetVertices(this.vertices.ToArray());
    	mesh.SetTriangles(triangles, 0);

    	this.meshCollider.sharedMesh = mesh;

    	mesh.SetTriangles(specularTris, 1);
    	mesh.SetTriangles(liquidTris, 2);
    	mesh.SetTriangles(assetTris, 3);

    	mesh.SetUVs(0, this.UVs.ToArray());
    	mesh.SetUVs(3, this.lightUVMain.ToArray());

    	mesh.SetNormals(this.normals.ToArray());

    	this.meshFilter.sharedMesh = mesh;
    }

    // Builds meshes from verts, UVs and tris from different layers
    private void BuildMeshSide(Vector3[] verts, Vector2[] UV, Vector2[] lightUV, Vector3[] normals){
    	mesh.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	mesh.subMeshCount = 4;

    	mesh.vertices = verts;
    	mesh.SetTriangles(triangles, 0);

    	this.meshCollider.sharedMesh = mesh;

    	mesh.SetTriangles(specularTris, 1);
    	mesh.SetTriangles(liquidTris, 2);
    	mesh.SetTriangles(assetTris, 3);

    	mesh.uv = UV;
    	mesh.uv4 = lightUV;
    	mesh.SetNormals(normals);

    	this.meshFilter.sharedMesh = mesh;
    }
}

/*
MULTITHREADING
*/
[BurstCompile]
public struct BuildChunkJob : IJob{
	[ReadOnly]
	public bool load;

	[ReadOnly]
	public NativeArray<ushort> data; // Voxeldata
	[ReadOnly]
	public NativeArray<ushort> state; // VoxelMetadata.state
	[ReadOnly]
	public NativeArray<byte> lightdata;

	// OnLoad Event Trigger List
	public NativeList<int3> loadOutList;
	public NativeList<int3> loadAssetList;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector2> UVs;
	public NativeList<Vector2> lightUV;
	public NativeList<Vector3> normals;

	// Render Thread Triangles
	public NativeList<int> normalTris;
	public NativeList<int> specularTris;
	public NativeList<int> liquidTris;

	// Cache
	public NativeArray<Vector3> cacheCubeVert;
	public NativeArray<Vector2> cacheCubeUV;
	public NativeArray<Vector3> cacheCubeNormal;

	// Block Encyclopedia Data
	[ReadOnly]
	public NativeArray<byte> blockTransparent;
	[ReadOnly]
	public NativeArray<byte> objectTransparent;
	[ReadOnly]
	public NativeArray<bool> blockLiquid;
	[ReadOnly]
	public NativeArray<bool> objectLiquid;
	[ReadOnly]
	public NativeArray<bool> blockLoad;
	[ReadOnly]
	public NativeArray<bool> objectLoad;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<byte> blockMaterial;
	[ReadOnly]
	public NativeArray<byte> objectMaterial;
	[ReadOnly]
	public NativeArray<int3> blockTiles;
	[ReadOnly]
	public NativeArray<bool> blockWashable;
	[ReadOnly]
	public NativeArray<bool> objectWashable;


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		ushort thisState;

		// Liquid Flags
		bool liquidToLoad = false;

		for(int x=0; x<Chunk.chunkWidth; x++){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisState = state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

	    			// If air
	    			if(thisBlock == 0){
	    				continue;
	    			}

	    			// Runs OnLoad event
	    			if(load)
	    				// If is a block
		    			if(thisBlock <= ushort.MaxValue/2){
		    				if(blockLoad[thisBlock] && !blockLiquid[thisBlock]){
		    					loadOutList.Add(new int3(x,y,z));
		    				}
		    			}
		    			// If Asset
		    			else{
		    				if(objectLoad[ushort.MaxValue-thisBlock] && !objectLiquid[ushort.MaxValue-thisBlock]){
		    					loadOutList.Add(new int3(x,y,z));
		    				}
		    			}

	    			// --------------------------------
		    		// Reset Liquid Count for current block
		    		liquidToLoad = false;

			    	for(int i=0; i<6; i++){
			    		neighborBlock = GetNeighbor(x, y, z, i);
			    		
			    		// Chunk Border and floor culling here! ----------
			    		
			    		if((x == 0 && 3 == i) || (z == 0 && 2 == i)){
			    			continue;
			    		}
			    		if((x == Chunk.chunkWidth-1 && 1 == i) || (z == Chunk.chunkWidth-1 && 0 == i)){
			    			continue;
			    		}
			    		if(y == 0 && 5 == i){
			    			continue;
			    		}

			    		////////// -----------------------------------

						// Handles Liquid chunks
			    		if(thisBlock <= ushort.MaxValue/2){
			    			if(blockLiquid[thisBlock]){
				    			if(CheckLiquids(thisBlock, neighborBlock, thisState, GetNeighborState(x,y,z,i))){
				    				continue;
				    			}
				    			else if(neighborBlock <= ushort.MaxValue/2 && i != 4){
				    				if(neighborBlock == 0 || blockWashable[neighborBlock]){
				    					liquidToLoad = true;
				    				}

				    			}
				    			else if(neighborBlock > ushort.MaxValue/2 && i != 4){
				    				if(objectWashable[ushort.MaxValue-neighborBlock]){
				    					liquidToLoad = true;
				    				}
				    			}
			    			}

			    		}
			    		else{
			    			if(objectLiquid[ushort.MaxValue-thisBlock]){
				    			if(CheckLiquids(thisBlock, neighborBlock, thisState, GetNeighborState(x,y,z,i))){
				    				continue;
				    			}
				    			else if(neighborBlock <= ushort.MaxValue/2 && i != 4){
				    				if(neighborBlock == 0 || blockWashable[neighborBlock]){
				    					liquidToLoad = true;
				    				}

				    			}
				    			else if(neighborBlock > ushort.MaxValue/2 && i != 4){
				    				if(objectWashable[ushort.MaxValue-neighborBlock]){
				    					liquidToLoad = true;
				    				}
				    			}	    				
			    			}
			    		}
		    			
		    			// Puts liquid into OnLoad list
		    			if(liquidToLoad && load){
		    				loadOutList.Add(new int3(x,y,z));
		    			}

		    			// Main Drawing Handling
			    		if(CheckPlacement(neighborBlock)){
					    	if(!LoadMesh(x, y, z, i, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal)){
					    		break;
					    	}
			    		}
				    } // faces loop
	    		} // z loop
	    	} // y loop
	    } // x loop
    }

    // Gets neighbor element
	private ushort GetNeighbor(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		}

		return data[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

    // Gets neighbor state
	private ushort GetNeighborState(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		} 

		return state[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + VoxelData.offsets[dir];
		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;
		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

    // Checks if neighbor is transparent or invisible
    private bool CheckPlacement(int neighborBlock){
    	if(neighborBlock <= ushort.MaxValue/2)
    		return Boolean(blockTransparent[neighborBlock]) || blockInvisible[neighborBlock];
    	else
			return Boolean(objectTransparent[ushort.MaxValue-neighborBlock]) || objectInvisible[ushort.MaxValue-neighborBlock];
    }

    // Checks if Liquids are side by side
    private bool CheckLiquids(int thisBlock, int neighborBlock, ushort thisState, ushort neighborState){
    	bool thisLiquid;
    	bool neighborLiquid;


    	if(thisBlock <= ushort.MaxValue/2)
    		thisLiquid = blockLiquid[thisBlock];
    	else
    		thisLiquid = objectLiquid[ushort.MaxValue-thisBlock];

    	if(neighborBlock <= ushort.MaxValue/2)
    		neighborLiquid = blockLiquid[neighborBlock];
    	else
    		neighborLiquid = objectLiquid[ushort.MaxValue-neighborBlock];

    	return thisLiquid && neighborLiquid && (thisState == neighborState);
    }

    private bool Boolean(byte a){
    	if(a == 0)
    		return false;
    	return true;
    }


    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Load is true when Chunk is being loaded and not reloaded
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, int lookahead=0){
    	byte renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == 0){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);
    		
	    	normalTris.Add(vCount -4);
	    	normalTris.Add(vCount -4 +1);
	    	normalTris.Add(vCount -4 +2);
	    	normalTris.Add(vCount -4);
	    	normalTris.Add(vCount -4 +2);
	    	normalTris.Add(vCount -4 +3); 
	    	
	    	return true;
    	}

    	// If object is Specular Block
    	else if(renderThread == 1){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +1);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4 +3);
	    	
	    	return true;   		
    	}

    	// If object is Liquid
    	else if(renderThread == 2){
    		VertsByState(cacheCubeVert, dir, state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);
    		
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +1);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4 +3);

	    	return true;    		
    	}

    	// If object is an Asset
    	else{
			loadAssetList.Add(new int3(x,y,z));
    		return false;
    	}
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}
    	// If there's no light
    	if(currentLightLevel <= 1){
	    	array[0] = Vector2.zero;
	    	array[1] = Vector2.zero;
	    	array[2] = Vector2.zero;
	    	array[3] = Vector2.zero;
	    	return;
    	}

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(x > 0)
    		xm = false;
    	if(x < Chunk.chunkWidth-1)
    		xp = false;
    	if(z > 0)
    		zm = false;
    	if(z < Chunk.chunkWidth-1)
    		zp = false;
    	if(y > 0)
    		ym = false;
    	if(y < Chunk.chunkDepth-1)
    		yp = false;

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCorners(auxPos, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp);

    }

    // Sets the secondary UV of ExtraLights Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(array[0].x, maxLightLevel);
	    	array[1] = new Vector2(array[1].x, maxLightLevel);
	    	array[2] = new Vector2(array[2].x, maxLightLevel);
	    	array[3] = new Vector2(array[3].x, maxLightLevel);
	    	return;
    	}
    	// If there's no light
    	if(currentLightLevel <= 1){
	    	array[0] = new Vector2(array[0].x, 0);
	    	array[1] = new Vector2(array[1].x, 0);
	    	array[2] = new Vector2(array[2].x, 0);
	    	array[3] = new Vector2(array[3].x, 0);
	    	return;
    	}

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(x > 0)
    		xm = false;
    	if(x < Chunk.chunkWidth-1)
    		xp = false;
    	if(z > 0)
    		zm = false;
    	if(z < Chunk.chunkWidth-1)
    		zp = false;
    	if(y > 0)
    		ym = false;
    	if(y < Chunk.chunkDepth-1)
    		yp = false;

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCornersExtra(auxPos, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp);

    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp);
    	// Down
     	else
    		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	// North
    	if(dir == 0)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp);
    	// East
    	else if(dir == 1)
    		SetCornerExtra(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp);
    	// South
     	else if(dir == 2)
    		SetCornerExtra(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp);
    	// West
      	else if(dir == 3)
    		SetCornerExtra(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp);
      	// Up
    	else if(dir == 4)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp);
    	// Down
     	else
    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp);
    }

    private bool CheckBorder(int dir, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	if(xm && dir == 3)
    		return false;
    	else if(xp && dir == 1)
    		return false;
    	else if(zm && dir == 2)
    		return false;
    	else if(zp && dir == 0)
    		return false;
    	else if(ym && dir == 5)
    		return false;
    	else if(yp && dir == 4)
    		return false;
    	else
    		return true;
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
	    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
	    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
	    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
	    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1);
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2);
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3);
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4);

    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal);
    	}

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
	    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
	    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
	    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
	    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:false);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:false);

    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}

		array[0] = new Vector2(array[0].x, Max(light1, light2, light5, currentLightLevel));
		array[1] = new Vector2(array[1].x, Max(light2, light3, light6, currentLightLevel));
		array[2] = new Vector2(array[2].x, Max(light3, light4, light7, currentLightLevel));
		array[3] = new Vector2(array[3].x, Max(light4, light1, light8, currentLightLevel));
    }

    /*
    Returns the maximum between light levels
    */
    private int Max(int a, int b, int c, int d){
    	int maximum = a;

    	if(maximum - b < 0)
    		maximum = b;
    	if(maximum - c < 0)
    		maximum = c;
    	if(maximum - d < 0)
    		maximum = d;
    	return maximum;
    }

    private int Clamp15(int a){
    	if(a > 15)
    		return 15;
    	else
    		return a;
    }

	// Sets UV mapping for a direction
	private void AddTexture(NativeArray<Vector2> array, int dir, ushort blockCode){
		int textureID;

		if(dir == 4)
			textureID = blockTiles[blockCode].x;
		else if(dir == 5)
			textureID = blockTiles[blockCode].y;
		else
			textureID = blockTiles[blockCode].z;

		// If should use normal atlas
		if(blockMaterial[blockCode] == 0){
			float x = textureID%Blocks.atlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.atlasSizeX);
	 
			x *= 1f / Blocks.atlasSizeX;
			y *= 1f / Blocks.atlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.atlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.atlasSizeX),y+(1f/Blocks.atlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.atlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use transparent atlas
		else if(blockMaterial[blockCode] == 1){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeY);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
	}

	// Gets UV Map for Liquid blocks
	private void LiquidTexture(NativeArray<Vector2> array, int x, int z){
		int size = Chunk.chunkWidth;
		int tileSize = 1/size;

		array[0] = new Vector2(x*tileSize,z*tileSize);
		array[1] = new Vector2(x*tileSize,(z+1)*tileSize);
		array[2] = new Vector2((x+1)*tileSize,(z+1)*tileSize);
		array[3] = new Vector2((x+1)*tileSize,z*tileSize);
	}

	// Cube Mesh Data get verts
	public static void faceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	// Gets the vertices of a given state in a liquid
	
	public static void VertsByState(NativeArray<Vector3> fv, int dir, ushort s, Vector3 pos, float scale=0.5f){
        if(s == ushort.MaxValue)
            s = 0;

		if(s == 19 || s == 20 || s == 21){
		    for (int i = 0; i < fv.Length; i++)
		    {
		    	fv[i] = (LiquidMeshData.verticesOnState[LiquidMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		    }
		}
		else{
		    for (int i = 0; i < fv.Length; i++)
		    {
		    	fv[i] = (LiquidMeshData.verticesOnState[((int)s*8)+ LiquidMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		    }
		}
	}


	public void CalculateNormal(NativeArray<Vector3> normals, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, 1);
		else if(dir == 1)
			normal = new Vector3(1, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -1);
		else if(dir == 3)
			normal = new Vector3(-1, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, 1, 0);
		else
			normal = new Vector3(0, -1, 0);

		normals[0] = normal;
		normals[1] = normal;
		normals[2] = normal;
		normals[3] = normal;
	}
}


[BurstCompile]
public struct PrepareAssetsJob : IJob{
	// Output
	public NativeList<Vector3> meshVerts;
	public NativeList<Vector2> meshUVs;
	public NativeList<int> meshTris;
	public NativeList<Vector3> meshNormals;
	public NativeList<Vector2> meshLightUV;

	[ReadOnly]
	public int vCount;

	// Input
	[ReadOnly]
	public NativeArray<ushort> blockdata;
	[ReadOnly]
	public NativeArray<ushort> metadata;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeList<int3> coords;
	[ReadOnly]
	public NativeList<ushort> blockCodes;
	[ReadOnly]
	public NativeList<int> vertsOffset;
	[ReadOnly]
	public NativeList<int> trisOffset;
	[ReadOnly]
	public NativeList<int> UVOffset;
	[ReadOnly]
	public NativeArray<Vector3> scaling;
	[ReadOnly]
	public NativeArray<bool> needRotation;
	[ReadOnly]
	public NativeHashMap<int, Vector3> inplaceOffset;
	[ReadOnly]
	public NativeHashMap<int, int> inplaceRotation;

	// Loaded Mesh Data
	[ReadOnly]
	public NativeArray<Vector3> loadedVerts;
	[ReadOnly]
	public NativeArray<Vector2> loadedUV;
	[ReadOnly]
	public NativeArray<int> loadedTris;
	[ReadOnly]
	public NativeArray<Vector3> loadedNormals;

	public void Execute(){
		int i;
		int currentVertAmount = vCount;

		for(int j=0; j < coords.Length; j++){
			i = GetIndex(blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z]);

			if(i == -1)
				continue;

			// If has special offset or rotation
			if(needRotation[ushort.MaxValue - blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z]]){
				int code = blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];
				int state = metadata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];

				// Vertices
				Vector3 vertPos = new Vector3(coords[j].x, coords[j].y, coords[j].z);
				for(int vertIndex=vertsOffset[i]; vertIndex < vertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3MultOffsetRotate(loadedVerts[vertIndex], scaling[i], vertPos, inplaceOffset[code*256+state], inplaceRotation[code*256+state]);
					meshVerts.Add(resultVert);
					meshNormals.Add(GetNormalRotation(loadedNormals[vertIndex], inplaceRotation[code*256+state]));
					meshLightUV.Add(new Vector2(GetLight(coords[j].x, coords[j].y, coords[j].z), GetLight(coords[j].x, coords[j].y, coords[j].z, isNatural:false)));
				}

			}
			// If doesn't have special rotation
			else{
				Vector3 vertPos = new Vector3(coords[j].x, coords[j].y, coords[j].z);
				for(int vertIndex=vertsOffset[i]; vertIndex < vertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3Mult(loadedVerts[vertIndex], scaling[i], vertPos);
					meshVerts.Add(resultVert);
					meshNormals.Add(loadedNormals[vertIndex]);
					meshLightUV.Add(new Vector2(GetLight(coords[j].x, coords[j].y, coords[j].z), GetLight(coords[j].x, coords[j].y, coords[j].z, isNatural:false)));
				}	
			}

			// UVs
			for(int UVIndex=UVOffset[i]; UVIndex < UVOffset[i+1]; UVIndex++){
				meshUVs.Add(loadedUV[UVIndex]);
			}

			// Triangles
			for(int triIndex=trisOffset[i]; triIndex < trisOffset[i+1]; triIndex++){
				meshTris.Add(loadedTris[triIndex] + currentVertAmount);
			}	
			currentVertAmount += (vertsOffset[i+1] - vertsOffset[i]);		
		}
	}

	// Check if a blockCode is contained in blockCodes List
	private int GetIndex(ushort code){
		for(int i=0; i < blockCodes.Length; i++){
			if(blockCodes[i] == code){
				return i;
			}
		}
		return -1;
	}

	private Vector3 Vector3Mult(Vector3 a, Vector3 b, Vector3 plus){
		return new Vector3(a.x * b.x + plus.x, a.y * b.y + plus.y, a.z * b.z + plus.z);
	}

	private Vector3 Vector3MultOffsetRotate(Vector3 a, Vector3 worldScaling, Vector3 worldOffset, Vector3 localOffset, int rotationDegree){
		a = Rotate(a, rotationDegree);
		Vector3 b = Vector3Mult(a, worldScaling, worldOffset);

		return b + localOffset;
	}

	private Vector3 GetNormalRotation(Vector3 normal, int rotation){
		return Rotate(normal, rotation);
	}

	private Vector3 Rotate(Vector3 a, int degrees){
		return new Vector3(a.x*Mathf.Cos(degrees *Mathf.Deg2Rad) - a.z*Mathf.Sin(degrees *Mathf.Deg2Rad), a.y, a.x*Mathf.Sin(degrees *Mathf.Deg2Rad) + a.z*Mathf.Cos(degrees *Mathf.Deg2Rad));
	}

	// Gets neighbor light level
	private int GetLight(int x, int y, int z, bool isNatural=true){
		int3 coord = new int3(x, y, z);

		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0xF0;
	}
}


[BurstCompile]
public struct BuildBorderJob : IJob{
	[ReadOnly]
	public bool reload;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<ushort> metadata;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<ushort> neighbordata;
	[ReadOnly]
	public bool zP, zM, xP, xM;
	[ReadOnly]
	public ChunkPos pos;

	// Border Update
	public NativeList<int3> toLoadEvent;
	public NativeList<int3> toBUD;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector2> uvs;
	public NativeList<Vector2> lightUV;
	public NativeList<Vector3> normals;

	// Render Thread Triangles
	public NativeList<int> normalTris;
	public NativeList<int> specularTris;
	public NativeList<int> liquidTris;

	// Cached
	public NativeArray<Vector3> cachedCubeVerts;
	public NativeArray<Vector2> cachedUVVerts;
	public NativeArray<Vector3> cachedCubeNormal;

	// Block Encyclopedia Data
	[ReadOnly]
	public NativeArray<byte> blockTransparent;
	[ReadOnly]
	public NativeArray<byte> objectTransparent;
	[ReadOnly]
	public NativeArray<bool> blockLiquid;
	[ReadOnly]
	public NativeArray<bool> objectLiquid;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<byte> blockMaterial;
	[ReadOnly]
	public NativeArray<byte> objectMaterial;
	[ReadOnly]
	public NativeArray<int3> blockTiles;
	[ReadOnly]
	public NativeArray<bool> blockWashable;
	[ReadOnly]
	public NativeArray<bool> objectWashable;


	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;

		// X- Side
		if(xM){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					thisBlock = data[y*Chunk.chunkWidth+z];
					neighborBlock = neighbordata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckLiquids(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockLiquid[neighborBlock]){
								toBUD.Add(new int3((pos.x-1)*Chunk.chunkWidth+Chunk.chunkWidth-1, y, pos.z*Chunk.chunkWidth+z));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(0,y,z));
							}
						}
						else{
							if(objectLiquid[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x-1)*Chunk.chunkWidth+Chunk.chunkWidth-1, y, pos.z*Chunk.chunkWidth+z));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(new int3(0,y,z));
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(0, y, z, 3, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
					}
				}
			}
			return;
		}
		// X+ Side
		else if(xP){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					thisBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					neighborBlock = neighbordata[y*Chunk.chunkWidth+z];

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckLiquids(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockLiquid[neighborBlock]){
								toBUD.Add(new int3((pos.x+1)*Chunk.chunkWidth, y, pos.z*Chunk.chunkWidth+z));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(Chunk.chunkWidth-1,y,z));
							}
						}
						else{
							if(objectLiquid[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x+1)*Chunk.chunkWidth, y, pos.z*Chunk.chunkWidth+z));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(new int3(Chunk.chunkWidth-1,y,z));
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(Chunk.chunkWidth-1, y, z, 1, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
					}
				}
			}
			return;
		}
		// Z- Side
		else if(zM){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
					neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
					
					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckLiquids(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockLiquid[neighborBlock]){
								toBUD.Add(new int3(pos.x*Chunk.chunkWidth+x, y, (pos.z-1)*Chunk.chunkWidth+Chunk.chunkWidth-1));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(x, y, 0));
							}
						}
						else{
							if(objectLiquid[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3(pos.x*Chunk.chunkWidth+x, y, (pos.z-1)*Chunk.chunkWidth+Chunk.chunkWidth-1));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(new int3(x,y,0));
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, 0, 2, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
					}
				}
			}
			return;
		}
		// Z+ Side
		else if(zP){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
					neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckLiquids(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockLiquid[neighborBlock]){
								toBUD.Add(new int3(pos.x*Chunk.chunkWidth+x, y, (pos.z+1)*Chunk.chunkWidth));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(x, y, Chunk.chunkWidth-1));
							}
						}
						else{
							if(objectLiquid[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3(pos.x*Chunk.chunkWidth+x, y, (pos.z+1)*Chunk.chunkWidth));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(new int3(x, y, Chunk.chunkWidth-1));
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, Chunk.chunkWidth-1, 0, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
					}
				}
			}
			return;
		}
	}

	// Checks if other chunk border block is a liquid and puts it on Border Update List
	private void CheckBorderUpdate(int x, int y, int z, ushort blockCode){

		if(blockCode <= ushort.MaxValue/2){
			if(blockLiquid[blockCode]){
				toLoadEvent.Add(new int3(x,y,z));
			}
		}
		else{
			if(objectLiquid[ushort.MaxValue-blockCode]){
				toLoadEvent.Add(new int3(x,y,z));
			}
		}
	} 

    // Checks if neighbor is transparent or invisible
    private bool CheckPlacement(int neighborBlock){
    	if(neighborBlock <= ushort.MaxValue/2)
    		return Boolean(blockTransparent[neighborBlock]) || blockInvisible[neighborBlock];
    	else
			return Boolean(objectTransparent[ushort.MaxValue-neighborBlock]) || objectInvisible[ushort.MaxValue-neighborBlock];
    }

    // Checks if Liquids are side by side
    private bool CheckLiquids(int thisBlock, int neighborBlock){
    	bool thisLiquid;
    	bool neighborLiquid;


    	if(thisBlock <= ushort.MaxValue/2)
    		thisLiquid = blockLiquid[thisBlock];
    	else
    		thisLiquid = objectLiquid[ushort.MaxValue-thisBlock];

    	if(neighborBlock <= ushort.MaxValue/2)
    		neighborLiquid = blockLiquid[neighborBlock];
    	else
    		neighborLiquid = objectLiquid[ushort.MaxValue-neighborBlock];

    	return thisLiquid && neighborLiquid;
    }

    private bool Boolean(byte a){
    	if(a == 0)
    		return false;
    	return true;
    }

    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Load is true when Chunk is being loaded and not reloaded
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, int lookahead=0){
    	byte renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == 0){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);
    		
	    	normalTris.Add(vCount -4);
	    	normalTris.Add(vCount -4 +1);
	    	normalTris.Add(vCount -4 +2);
	    	normalTris.Add(vCount -4);
	    	normalTris.Add(vCount -4 +2);
	    	normalTris.Add(vCount -4 +3);

	    	return true;
    	}

    	// If object is Specular Block
    	else if(renderThread == 1){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +1);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4 +3);
	    	
	    	return true;   		
    	}

    	// If object is Liquid
    	else if(renderThread == 2){
    		VertsByState(cacheCubeVert, dir, metadata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);    		

	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +1);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4 +3);

	    	return true;
    	}

    	return false;
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}
    	// If there's no light
    	if(currentLightLevel <= 1){
	    	array[0] = Vector2.zero;
	    	array[1] = Vector2.zero;
	    	array[2] = Vector2.zero;
	    	array[3] = Vector2.zero;
	    	return;
    	}

    	bool xm = false;
    	bool xp = false;
    	bool zm = false;
    	bool zp = false;
    	bool ym = false;
    	bool yp = false;

    	if(x > 0)
    		xm = true;
    	if(x < Chunk.chunkWidth-1)
    		xp = true;
    	if(z > 0)
    		zm = true;
    	if(z < Chunk.chunkWidth-1)
    		zp = true;
    	if(y > 0)
    		ym = true;
    	if(y < Chunk.chunkDepth-1)
    		yp = true;

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCorners(auxPos, dir, array, currentLightLevel, xm || xp || zm || zp || ym || yp);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool isChunkBorder){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, isChunkBorder);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, isChunkBorder);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, isChunkBorder);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, isChunkBorder);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, isChunkBorder);
    	// Down
     	else
    		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, isChunkBorder);

    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool isChunkCorner){
    	int light1, light2, light3, light4;

    	if(isChunkCorner){
	    	if(dir1 >= 0)
	    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1);
	    	else
	    		light1 = currentLightLevel;
	    	if(dir2 >= 0)
	    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2);
	    	else
	    		light2 = currentLightLevel;
	    	if(dir3 >= 0)
	    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3);
	    	else
	    		light3 = currentLightLevel;
	    	if(dir4 >= 0)
	    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4);
	    	else
	    		light4 = currentLightLevel;
    	}
    	else{
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1);
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2);
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3);
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4);
    	}

		array[0] = new Vector2(Max(light1, light2, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, currentLightLevel), 1);
    }

    /*
    Returns the maximum between light levels
    */
    private int Max(int a, int b, int c){
    	int maximum = a;

    	if(maximum - b < 0)
    		maximum = b;
    	if(maximum - c < 0)
    		maximum = c;
    	return maximum;
    }

	// Sets UV mapping for a direction
	private void AddTexture(NativeArray<Vector2> array, int dir, ushort blockCode){
		int textureID;

		if(dir == 4)
			textureID = blockTiles[blockCode].x;
		else if(dir == 5)
			textureID = blockTiles[blockCode].y;
		else
			textureID = blockTiles[blockCode].z;

		// If should use normal atlas
		if(blockMaterial[blockCode] == 0){
			float x = textureID%Blocks.atlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.atlasSizeX);
	 
			x *= 1f / Blocks.atlasSizeX;
			y *= 1f / Blocks.atlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.atlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.atlasSizeX),y+(1f/Blocks.atlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.atlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use transparent atlas
		else if(blockMaterial[blockCode] == 1){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeY);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int dir, bool isNatural=true){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		} 

		if(isNatural)
			return lightdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] & 0x0F;
		else
			return lightdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] >> 4;
	}

	// Gets UV Map for Liquid blocks
	private void LiquidTexture(NativeArray<Vector2> array, int x, int z){
		int size = Chunk.chunkWidth;
		int tileSize = 1/size;

		array[0] = new Vector2(x*tileSize,z*tileSize);
		array[1] = new Vector2(x*tileSize,(z+1)*tileSize);
		array[2] = new Vector2((x+1)*tileSize,(z+1)*tileSize);
		array[3] = new Vector2((x+1)*tileSize,z*tileSize);
	}

	// Cube Mesh Data get verts
	public void faceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	// Gets the vertices of a given state in a liquid
	public void VertsByState(NativeArray<Vector3> fv, int dir, ushort s, Vector3 pos, float scale=0.5f){
        if(s == ushort.MaxValue)
            s = 0;

		if(s == 19 || s == 20 || s == 21){
		    for (int i = 0; i < fv.Length; i++)
		    {
		      fv[i] = (LiquidMeshData.verticesOnState[LiquidMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		    }
		}
		else{
		    for (int i = 0; i < fv.Length; i++)
		    {
		      fv[i] = (LiquidMeshData.verticesOnState[((int)s*8)+ LiquidMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		    }
		}
	}

	public void CalculateNormal(NativeArray<Vector3> normals, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, 1);
		else if(dir == 1)
			normal = new Vector3(1, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -1);
		else if(dir == 3)
			normal = new Vector3(-1, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, 1, 0);
		else
			normal = new Vector3(0, -1, 0);

		normals[0] = normal;
		normals[1] = normal;
		normals[2] = normal;
		normals[3] = normal;
	}

}