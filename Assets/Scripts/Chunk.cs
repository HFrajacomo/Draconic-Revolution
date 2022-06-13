using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Unity.Collections.NotBurstCompatible;

public class Chunk
{
	// Chunk Settings
	public VoxelData data;
	public VoxelMetadata metadata;
	public static readonly int chunkWidth = 16;
	public static readonly int chunkDepth = 256;
	public static float chunkWidthMult = 15.99f; 
	public ChunkPos pos;
	public string biomeName;
	public byte needsGeneration;
	public float4 features;
	public string lastVisitedTime;

	// Multiplayer Settings
	private NetMessage message;

	// Draw Flags
	public bool drawMain = false;
	public bool xpDraw = false;
	public bool zpDraw = false;
	public bool xmDraw = false;
	public bool zmDraw = false;

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
    private int[] leavesTris;
    private int[] iceTris;
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
		this.meshFilter.mesh = this.mesh;
		this.meshCollider.sharedMesh = this.mesh;
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

	public void Destroy(){
		GameObject.Destroy(this.obj);
		this.obj = null;
		Object.Destroy(this.mesh);
		Object.Destroy(this.meshFilter);
		Object.Destroy(this.meshCollider);
		this.meshFilter = null;
		this.meshCollider = null; 
		this.loader = null;
		this.blockBook = null;

		this.data.Destroy();
		this.metadata.Destroy();

		this.data = null;
		this.metadata = null;
		this.mesh = null;
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

	private void ClearMesh(){
		this.mesh.Clear();
	}

	// Checks if a Chunk can be redrawn
    private bool CheckRedraw(){
        ChunkPos position;

        position = new ChunkPos(pos.x-1, pos.z);

        if(!xmDraw && loader.chunks.ContainsKey(position))
            return true;
            
        position = new ChunkPos(pos.x+1, pos.z);

        if(!xpDraw && loader.chunks.ContainsKey(position))
            return true;

        position = new ChunkPos(pos.x, pos.z-1);

        if(!zmDraw && loader.chunks.ContainsKey(position))
            return true;

        position = new ChunkPos(pos.x, pos.z+1);

        if(!zpDraw && loader.chunks.ContainsKey(position))
            return true;

        return false;
    }

	// Draws Chunk Borders. Returns true if all borders have been drawn, otherwise, return false.
	public bool BuildSideBorder(bool reload=false, bool loadBUD=false){
		bool changed = false; // Flag is set if any change has been made that requires a redraw
		bool doneRendering = true;

		if(reload){
			xmDraw = false;
			zmDraw = false;
			xpDraw = false;
			zpDraw = false;
		}

		if(!CheckRedraw())
			return false;

		int3[] coordArray;
		int3[] budArray;

		NativeArray<ushort> blockdata = NativeTools.CopyToNative(this.data.GetData());
		NativeArray<ushort> metadata = NativeTools.CopyToNative(this.metadata.GetStateData());
		NativeArray<byte> lightdata = NativeTools.CopyToNative(this.data.GetLightMap());

		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> uvs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> lightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<int> tris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> specularTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> liquidTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> leavesTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> iceTris = new NativeList<int>(0, Allocator.TempJob);
	
		NativeList<int3> toLoadEvent = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> toBUD = new NativeList<int3>(0, Allocator.TempJob);

		// Cached
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheUVVerts = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);

		// For Init
		this.meshFilter.mesh.GetVertices(vertexAux);
		NativeArray<Vector3> disposableVerts = NativeTools.CopyToNative<Vector3>(vertexAux.ToArray());
		vertexAux.Clear();

		this.meshFilter.mesh.GetUVs(0, UVaux);
		NativeArray<Vector2> disposableUVS = NativeTools.CopyToNative<Vector2>(UVaux.ToArray());
		UVaux.Clear();

		this.meshFilter.mesh.GetUVs(3, UVaux);
		NativeArray<Vector2> disposableLight = NativeTools.CopyToNative<Vector2>(UVaux.ToArray());
		UVaux.Clear();

		this.meshFilter.mesh.GetNormals(normalAux);
		NativeArray<Vector3> disposableNormals = NativeTools.CopyToNative<Vector3>(normalAux.ToArray());
		normalAux.Clear();

		NativeArray<int> disposableTris = new NativeArray<int>(this.meshFilter.mesh.GetTriangles(0), Allocator.TempJob);
		NativeArray<int> disposableSpecTris = new NativeArray<int>(this.meshFilter.mesh.GetTriangles(1), Allocator.TempJob);
		NativeArray<int> disposableLiquidTris = new NativeArray<int>(this.meshFilter.mesh.GetTriangles(2), Allocator.TempJob);
		NativeArray<int> disposableLeavesTris = new NativeArray<int>(this.meshFilter.mesh.GetTriangles(4), Allocator.TempJob);
		NativeArray<int> disposableIceTris = new NativeArray<int>(this.meshFilter.mesh.GetTriangles(5), Allocator.TempJob);


		JobHandle job;


		// Initialize Data
		verts.AddRange(disposableVerts);
		uvs.AddRange(disposableUVS);
		lightUV.AddRange(disposableLight);
		tris.AddRange(disposableTris);
		specularTris.AddRange(disposableSpecTris);
		liquidTris.AddRange(disposableLiquidTris);
		leavesTris.AddRange(disposableLeavesTris);
		iceTris.AddRange(disposableIceTris);
		normals.AddRange(disposableNormals);


		// Dispose Init
		disposableVerts.Dispose();
		disposableUVS.Dispose();
		disposableTris.Dispose();
		disposableSpecTris.Dispose();
		disposableLiquidTris.Dispose();
		disposableNormals.Dispose();
		disposableLight.Dispose();
		disposableLeavesTris.Dispose();
		disposableIceTris.Dispose();

		// X- Analysis
		ChunkPos targetChunk = new ChunkPos(this.pos.x-1, this.pos.z); 
		if(loader.chunks.ContainsKey(targetChunk)){
			xmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap());
			
			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborlight = neighborlight,
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
				leavesTris = leavesTris,
				iceTris = iceTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				blockSeamless = BlockEncyclopediaECS.blockSeamless,
				objectSeamless = BlockEncyclopediaECS.objectSeamless,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockMaterial = BlockEncyclopediaECS.blockMaterial,
				objectMaterial = BlockEncyclopediaECS.objectMaterial,
				blockWashable = BlockEncyclopediaECS.blockWashable,
				objectWashable = BlockEncyclopediaECS.objectWashable,
				blockTiles = BlockEncyclopediaECS.blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();
			
			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);

			if(loadBUD){
				foreach(int3 coord in coordArray){
					this.message.AddBatchLoad(coord.x, coord.y, coord.z, 0, 0, 0, 0);
				}			
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}
			toLoadEvent.Clear();
		}
		else{
			doneRendering = false;
		}


		// X+ Analysis
		targetChunk = new ChunkPos(this.pos.x+1, this.pos.z); 
		if(loader.chunks.ContainsKey(targetChunk)){
			xpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap());

			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborlight = neighborlight,
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
				leavesTris = leavesTris,
				iceTris = iceTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				blockSeamless = BlockEncyclopediaECS.blockSeamless,
				objectSeamless = BlockEncyclopediaECS.objectSeamless,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockMaterial = BlockEncyclopediaECS.blockMaterial,
				objectMaterial = BlockEncyclopediaECS.objectMaterial,
				blockWashable = BlockEncyclopediaECS.blockWashable,
				objectWashable = BlockEncyclopediaECS.objectWashable,
				blockTiles = BlockEncyclopediaECS.blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);

			if(loadBUD){
				foreach(int3 coord in coordArray){
					this.message.AddBatchLoad(coord.x, coord.y, coord.z, 0, 0, 0, 0);
				}

				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}
			toLoadEvent.Clear();
		}
		else{
			doneRendering = false;
		}

		// Z- Analysis
		targetChunk = new ChunkPos(this.pos.x, this.pos.z-1); 
		if(loader.chunks.ContainsKey(targetChunk)){
			zmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap());
			
			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborlight = neighborlight,
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
				leavesTris = leavesTris,
				iceTris = iceTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				blockSeamless = BlockEncyclopediaECS.blockSeamless,
				objectSeamless = BlockEncyclopediaECS.objectSeamless,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockMaterial = BlockEncyclopediaECS.blockMaterial,
				objectMaterial = BlockEncyclopediaECS.objectMaterial,
				blockWashable = BlockEncyclopediaECS.blockWashable,
				objectWashable = BlockEncyclopediaECS.objectWashable,
				blockTiles = BlockEncyclopediaECS.blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD){
				foreach(int3 coord in coordArray){
					this.message.AddBatchLoad(coord.x, coord.y, coord.z, 0, 0, 0, 0);
				}
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}
			toLoadEvent.Clear();
		}
		else{
			doneRendering = false;
		}

		// Z+ Analysis
		targetChunk = new ChunkPos(this.pos.x, this.pos.z+1); 
		if(loader.chunks.ContainsKey(targetChunk)){
			zpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap());
			
			BuildBorderJob bbJob = new BuildBorderJob{
				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborlight = neighborlight,
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
				leavesTris = leavesTris,
				iceTris = iceTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				blockSeamless = BlockEncyclopediaECS.blockSeamless,
				objectSeamless = BlockEncyclopediaECS.objectSeamless,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockMaterial = BlockEncyclopediaECS.blockMaterial,
				objectMaterial = BlockEncyclopediaECS.objectMaterial,
				blockWashable = BlockEncyclopediaECS.blockWashable,
				objectWashable = BlockEncyclopediaECS.objectWashable,
				blockTiles = BlockEncyclopediaECS.blockTiles
			};
			job = bbJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD){
				foreach(int3 coord in coordArray){
					this.message.AddBatchLoad(coord.x, coord.y, coord.z, 0, 0, 0, 0);
				}
				this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
			}
			toLoadEvent.Clear();
		}
		else{
			doneRendering = false;
		}

		this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());

		// Runs BUD in neighbor chunks
		if(loadBUD){
			budArray = toBUD.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);

			foreach(int3 bu in budArray){
				this.message.AddBatchLoad(bu.x, bu.y, bu.z, 0, 0, 0, ushort.MaxValue);
			}

			this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
		}
		
		// If mesh wasn't redrawn
		if(changed){
			this.triangles = tris.ToArray();
			this.specularTris = specularTris.ToArray();
			this.liquidTris = liquidTris.ToArray();
			this.leavesTris = leavesTris.ToArray();
			this.iceTris = iceTris.ToArray();
			assetTris = this.meshFilter.mesh.GetTriangles(3);

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
		leavesTris.Dispose();
		iceTris.Dispose();
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
    	this.iceTris = null;
    	this.assetTris = null;
    	this.UVs.Clear();
    	this.lightUVMain.Clear();

		return doneRendering;
	}


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(bool load=false, bool pregenReload=false){
		NativeArray<ushort> blockdata = NativeTools.CopyToNative<ushort>(this.data.GetData());
		NativeArray<ushort> statedata = NativeTools.CopyToNative<ushort>(this.metadata.GetStateData());
		NativeArray<byte> lightdata = NativeTools.CopyToNative<byte>(this.data.GetLightMap());
		
		NativeList<int3> loadCoordList = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<ushort> loadCodeList = new NativeList<ushort>(0, Allocator.TempJob);
		NativeList<int3> loadAssetList = new NativeList<int3>(0, Allocator.TempJob);

		NativeList<int> normalTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> specularTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> liquidTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> leavesTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> iceTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> lightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheCubeUV = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);

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
			leavesTris = leavesTris,
			iceTris = iceTris,
			cacheCubeVert = cacheCubeVert,
			cacheCubeUV = cacheCubeUV,
			cacheCubeNormal = cacheCubeNormal,
			blockTransparent = BlockEncyclopediaECS.blockTransparent,
			objectTransparent = BlockEncyclopediaECS.objectTransparent,
			blockSeamless = BlockEncyclopediaECS.blockSeamless,
			objectSeamless = BlockEncyclopediaECS.objectSeamless,
			blockLoad = BlockEncyclopediaECS.blockLoad,
			objectLoad = BlockEncyclopediaECS.objectLoad,
			blockInvisible = BlockEncyclopediaECS.blockInvisible,
			objectInvisible = BlockEncyclopediaECS.objectInvisible,
			blockMaterial = BlockEncyclopediaECS.blockMaterial,
			objectMaterial = BlockEncyclopediaECS.objectMaterial,
			blockWashable = BlockEncyclopediaECS.blockWashable,
			objectWashable = BlockEncyclopediaECS.objectWashable,
			blockTiles = BlockEncyclopediaECS.blockTiles,
			blockDrawTop = BlockEncyclopediaECS.blockDrawTopRegardless
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
		this.message = new NetMessage(NetCode.BATCHLOADBUD);
		this.message.BatchLoadBUD(this.pos);

		if(load){
			coordArray = loadCoordList.AsArray().ToArray();
			foreach(int3 coord in coordArray){
				this.message.AddBatchLoad(coord.x, coord.y, coord.z, 0, 0, 0, 0);
			}	
			this.loader.client.Send(this.message.GetMessage(), this.message.GetSize());
		}
		loadCoordList.Clear();
		

		NativeList<Vector3> meshVerts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> meshUVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> meshLightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> meshNormals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<int> meshTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<ushort> blockCodes = new NativeList<ushort>(0, Allocator.TempJob);
		blockCodes.CopyFromNBC(this.cacheCodes.ToArray());
		NativeList<int> vertsOffset = new NativeList<int>(0, Allocator.TempJob);
		vertsOffset.CopyFromNBC(this.indexVert.ToArray());
		NativeList<int> trisOffset = new NativeList<int>(0, Allocator.TempJob);
		trisOffset.CopyFromNBC(this.indexTris.ToArray());
		NativeList<int> UVOffset = new NativeList<int>(0, Allocator.TempJob);
		UVOffset.CopyFromNBC(this.indexUV.ToArray());
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
			needRotation = BlockEncyclopediaECS.objectNeedRotation,
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
		this.leavesTris = leavesTris.ToArray();
		this.iceTris = iceTris.ToArray();

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
		leavesTris.Dispose();
		iceTris.Dispose();
		blockdata.Dispose();
		statedata.Dispose();
		loadCoordList.Dispose();
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
    	this.leavesTris = null;
    	this.iceTris = null;
    	this.UVs.Clear();
    	this.normals.Clear();

		this.drawMain = true;
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
    	this.meshCollider.sharedMesh.Clear();
    	this.meshFilter.mesh.Clear();

    	if(this.vertices.Count >= ushort.MaxValue){
    		this.meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilter.mesh.subMeshCount = 6;

    	this.meshFilter.mesh.SetVertices(this.vertices.ToArray());
    	this.meshFilter.mesh.SetTriangles(triangles, 0);
 	    this.meshFilter.mesh.SetTriangles(this.iceTris, 5);

    	this.meshCollider.sharedMesh = null;
    	this.meshCollider.sharedMesh = this.meshFilter.mesh;

    	this.meshFilter.mesh.SetTriangles(this.specularTris, 1);
    	this.meshFilter.mesh.SetTriangles(this.liquidTris, 2);
    	this.meshFilter.mesh.SetTriangles(this.assetTris, 3);
 	    this.meshFilter.mesh.SetTriangles(this.leavesTris, 4);

    	this.meshFilter.mesh.SetUVs(0, this.UVs.ToArray());
    	this.meshFilter.mesh.SetUVs(3, this.lightUVMain.ToArray());

    	this.meshFilter.mesh.SetNormals(this.normals.ToArray());
    }

    // Builds meshes from verts, UVs and tris from different layers
    private void BuildMeshSide(Vector3[] verts, Vector2[] UV, Vector2[] lightUV, Vector3[] normals){
    	this.meshCollider.sharedMesh.Clear();
    	this.meshFilter.mesh.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		this.meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilter.mesh.subMeshCount = 6;

    	this.meshFilter.mesh.vertices = verts;
    	this.meshFilter.mesh.SetTriangles(triangles, 0);
    	this.meshFilter.mesh.SetTriangles(this.iceTris, 5);

    	this.meshCollider.sharedMesh = null;
    	this.meshCollider.sharedMesh = this.meshFilter.mesh;;

    	this.meshFilter.mesh.SetTriangles(this.specularTris, 1);
    	this.meshFilter.mesh.SetTriangles(this.liquidTris, 2);
    	this.meshFilter.mesh.SetTriangles(this.assetTris, 3);
    	this.meshFilter.mesh.SetTriangles(this.leavesTris, 4);

    	this.meshFilter.mesh.uv = UV;
    	this.meshFilter.mesh.uv4 = lightUV;
    	this.meshFilter.mesh.SetNormals(normals);
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
	public NativeList<int> leavesTris;
	public NativeList<int> iceTris;

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
	public NativeArray<bool> blockSeamless;
	[ReadOnly]
	public NativeArray<bool> objectSeamless;
	[ReadOnly]
	public NativeArray<bool> blockLoad;
	[ReadOnly]
	public NativeArray<bool> objectLoad;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<ShaderIndex> blockMaterial;
	[ReadOnly]
	public NativeArray<ShaderIndex> objectMaterial;
	[ReadOnly]
	public NativeArray<int3> blockTiles;
	[ReadOnly]
	public NativeArray<bool> blockWashable;
	[ReadOnly]
	public NativeArray<bool> objectWashable;
	[ReadOnly]
	public NativeArray<bool> blockDrawTop;


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		ushort thisState;

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
		    				if(blockLoad[thisBlock] && !blockSeamless[thisBlock]){
		    					loadOutList.Add(new int3(x,y,z));
		    				}
		    			}
		    			// If Asset
		    			else{
		    				if(objectLoad[ushort.MaxValue-thisBlock] && !objectSeamless[ushort.MaxValue-thisBlock]){
		    					loadOutList.Add(new int3(x,y,z));
		    				}
		    			}

	    			// --------------------------------

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
			    			if(blockSeamless[thisBlock]){
				    			if(CheckSeams(thisBlock, neighborBlock, thisState, GetNeighborState(x,y,z,i))){
				    				continue;
				    			}
				    			else if(neighborBlock <= ushort.MaxValue/2 && i != 4){
				    				if(neighborBlock == 0 || blockWashable[neighborBlock]){
				    				}

				    			}
				    			else if(neighborBlock > ushort.MaxValue/2 && i != 4){
				    				if(objectWashable[ushort.MaxValue-neighborBlock]){
				    				}
				    			}
			    			}

			    		}
			    		else{
			    			if(objectSeamless[ushort.MaxValue-thisBlock]){
				    			if(CheckSeams(thisBlock, neighborBlock, thisState, GetNeighborState(x,y,z,i))){
				    				continue;
				    			}
				    			else if(neighborBlock <= ushort.MaxValue/2 && i != 4){
				    				if(neighborBlock == 0 || blockWashable[neighborBlock]){
				    				}

				    			}
				    			else if(neighborBlock > ushort.MaxValue/2 && i != 4){
				    				if(objectWashable[ushort.MaxValue-neighborBlock]){
				    				}
				    			}	    				
			    			}
			    		}

		    			// Main Drawing Handling
			    		if(CheckPlacement(neighborBlock)){
					    	if(!LoadMesh(x, y, z, i, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal)){
					    		break;
					    	}
			    		}
			    		else if(thisBlock <= ushort.MaxValue/2){
			    			if(blockDrawTop[thisBlock] && i == 4){
			    				if(neighborBlock <= ushort.MaxValue/2){
			    					if(blockTransparent[neighborBlock] == 0){
			    						if(!LoadMesh(x, y, z, i, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal)){
						    				break;
						    			}
			    					}
			    				}
			    				else{
			    					if(objectTransparent[neighborBlock] == 0){
			    						if(!LoadMesh(x, y, z, i, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal)){
						    				break;
						    			}
			    					}			    					
			    				}

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
    		return (Boolean(blockTransparent[neighborBlock]) || blockInvisible[neighborBlock]);
    	else
			return (Boolean(objectTransparent[ushort.MaxValue-neighborBlock]) || objectInvisible[ushort.MaxValue-neighborBlock]);
    }

    // Checks if seamlesses are side by side
    private bool CheckSeams(int thisBlock, int neighborBlock, ushort thisState, ushort neighborState){
    	bool thisSeamless;
    	bool neighborSeamless;


    	if(thisBlock <= ushort.MaxValue/2)
    		thisSeamless = blockSeamless[thisBlock];
    	else
    		thisSeamless = objectSeamless[ushort.MaxValue-thisBlock];

    	if(neighborBlock <= ushort.MaxValue/2)
    		neighborSeamless = blockSeamless[neighborBlock];
    	else
    		neighborSeamless = objectSeamless[ushort.MaxValue-neighborBlock];

    	return thisSeamless && neighborSeamless && (thisState == neighborState) && (thisBlock == neighborBlock);
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
    	ShaderIndex renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == ShaderIndex.OPAQUE){
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
    	else if(renderThread == ShaderIndex.SPECULAR){
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
    	else if(renderThread == ShaderIndex.WATER){
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

    	// If object is Leaves
    	else if(renderThread == ShaderIndex.LEAVES){
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

	    	leavesTris.Add(vCount -4);
	    	leavesTris.Add(vCount -4 +1);
	    	leavesTris.Add(vCount -4 +2);
	    	leavesTris.Add(vCount -4);
	    	leavesTris.Add(vCount -4 +2);
	    	leavesTris.Add(vCount -4 +3);

	    	return true;
    	}

    	// If object is Ice
    	else if(renderThread == ShaderIndex.ICE){
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

	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +1);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4 +3);

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

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(x > 1 || (x == 1 && dir != 3) || (x == 0 && dir == 1))
    		xm = false;
    	if(x < Chunk.chunkWidth-2 || (x == Chunk.chunkWidth-2 && dir != 1) || (x == Chunk.chunkWidth-1 && dir == 3))
    		xp = false;
    	if(z > 1 || (z == 1 && dir != 2) || (z == 0 && dir == 0))
    		zm = false;
    	if(z < Chunk.chunkWidth-2 || (z == Chunk.chunkWidth-2 && dir != 0) || (z == Chunk.chunkWidth-1 && dir == 2))
    		zp = false;
    	if(y > 1 || (y == 1 && dir != 5) || (y == 0 && dir == 4))
    		ym = false;
    	if(y < Chunk.chunkDepth-2 || (y == Chunk.chunkDepth-2 && dir != 4) || (y == Chunk.chunkDepth-1 && dir == 5))
    		yp = false;


    	// If there's no light
    	if(currentLightLevel <= 1){
    		bool found = false;

    		if(x > 0 && !xm)
    			if(GetNeighborLight(x-1, y, z, dir) > 0)
    				found = true;
    		if(x < Chunk.chunkWidth-1 && !xp)
    			if(GetNeighborLight(x+1, y, z, dir) > 0)
    				found = true;
    		if(z > 0 && !zm)
    			if(GetNeighborLight(x, y, z-1, dir) > 0)
    				found = true;    		
    		if(z < Chunk.chunkWidth-1 && !zp)
    			if(GetNeighborLight(x, y, z+1, dir) > 0)
    				found = true;
    		if(y > 0 && !ym)
    			if(GetNeighborLight(x, y-1, z, dir) > 0)
    				found = true;
    		if(y < Chunk.chunkDepth-1 && !yp)
    			if(GetNeighborLight(x, y+1, z, dir) > 0)
    				found = true;

    		if(!found){
		    	array[0] = Vector2.zero;
		    	array[1] = Vector2.zero;
		    	array[2] = Vector2.zero;
		    	array[3] = Vector2.zero;
		    	return;
		    }
    	}

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

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(x > 1 || (x == 1 && dir != 3) || (x == 0 && dir == 1))
    		xm = false;
    	if(x < Chunk.chunkWidth-2 || (x == Chunk.chunkWidth-2 && dir != 1) || (x == Chunk.chunkWidth-1 && dir == 3))
    		xp = false;
    	if(z > 1 || (z == 1 && dir != 2) || (z == 0 && dir == 0))
    		zm = false;
    	if(z < Chunk.chunkWidth-2 || (z == Chunk.chunkWidth-2 && dir != 0) || (z == Chunk.chunkWidth-1 && dir == 2))
    		zp = false;
    	if(y > 1 || (y == 1 && dir != 5) || (y == 0 && dir == 4))
    		ym = false;
    	if(y < Chunk.chunkDepth-2 || (y == Chunk.chunkDepth-2 && dir != 4) || (y == Chunk.chunkDepth-1 && dir == 5))
    		yp = false;


    	// If there's no light
    	if(currentLightLevel <= 1){
    		bool found = false;

    		if(x > 0 && !xm)
    			if(GetNeighborLight(x-1, y, z, dir) > 0)
    				found = true;
    		if(x < Chunk.chunkWidth-1 && !xp)
    			if(GetNeighborLight(x+1, y, z, dir) > 0)
    				found = true;
    		if(z > 0 && !zm)
    			if(GetNeighborLight(x, y, z-1, dir) > 0)
    				found = true;    		
    		if(z < Chunk.chunkWidth-1 && !zp)
    			if(GetNeighborLight(x, y, z+1, dir) > 0)
    				found = true;
    		if(y > 0 && !ym)
    			if(GetNeighborLight(x, y-1, z, dir) > 0)
    				found = true;
    		if(y < Chunk.chunkDepth-1 && !yp)
    			if(GetNeighborLight(x, y+1, z, dir) > 0)
    				found = true;

    		if(!found){
		    	array[0] = new Vector2(array[0].x, 0);
		    	array[1] = new Vector2(array[1].x, 0);
		    	array[2] = new Vector2(array[2].x, 0);
		    	array[3] = new Vector2(array[3].x, 0);
		    	return;
		    }
    	}

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCornersExtra(auxPos, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp, 4);
    	// Down
     	else
     		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp, 5);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	// North
    	if(dir == 0)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0);
    	// East
    	else if(dir == 1)
    		SetCornerExtra(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1);
    	// South
     	else if(dir == 2)
    		SetCornerExtra(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2);
    	// West
      	else if(dir == 3)
    		SetCornerExtra(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3);
      	// Up
    	else if(dir == 4)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp, 4);
    	// Down
     	else
    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp, 5);
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

    private bool CheckTransient(int facing, bool xm, bool zm, bool xp, bool zp){
    	if((facing == 0 || facing == 2) && (xm || xp))
    		return true;
    	if((facing == 1 || facing == 3) && (zm || zp))
    		return true;
    	if((facing == 4 || facing == 5) && (xm || zm || xp || zp))
    		return true;
    	return false;
    }

    private int GetVertexLight(int current, int n, int e, int s, int ne, int se, ref bool lightFromBorder, ref bool flipDirection){
    	int val = 0;

    	// Light from inside scenario
    	if(current < e && e - current == 1){
    		if(current > n)
    			val = current << 24;
    		else
    			val = n << 24;

    		if(current > s)
    			val += current;
    		else
    			val += s;

    		if(s > current || se > current || current > n)
    			flipDirection = true;
    	}
    	// Light from outside scenario
    	else if(current > e && current - e == 1){
    		if(current > n)
    			val = (current+1) << 24;
    		else
    			val = (n+1) << 24;

    		if(current > s)
    			val += current+1;
    		else
    			val += s+1;

    		lightFromBorder = true;
    		if(s > current || se > current || current > n)
    			flipDirection = true;
    	}
    	// If everything around is the same light level
    	else if((current == e && current == n && current == ne) || (current == e && current == se && current == s)){
    		val = current;
    		val <<= 8;
    		val += current;
    		val <<= 8;
    		val += current;
    		val <<= 8;
    		val += current;
    		return val;
    	}
    	// Light from above or bottom
    	else if(current == e){
    		// hotfix
    		if(current > 1){
    			if(n == 0)
    				n = current;
    			if(s == 0)
    				s = current;
    		}

    		if(current > n)
    			val = n << 24;
    		else
    			val = current << 24;

    		if(current > s)
    			val += s;
    		else
    			val += current;
    	}

    	// Enclosed space
    	else if(n == 0 && s == 0 && e == 0){
    		val = (current+1) << 24;
    		val += current+1;
    	}

    	// No recorded case
    	else{
    		val = current << 24;
    		val += current;
    	}


    	// Populate outer values
    	val += (Max(current, n, e, ne) << 16);
    	val += (Max(current, e, s, se) << 8);

    	return val;
    }

    private int ProcessTransient(int facing, bool xm, bool zm, bool xp, bool zp, int currentLight, int l1, int l2, int l3, int l4, int l5, int l6, int l7, int l8){
    	bool fromOutsideBorder = false;
    	bool flipDirection = false;

    	if(facing == 0 && xm)
    		return GetVertexLight(currentLight, l1, l4, l3, l8, l7, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 0 && xp)
    		return GetVertexLight(currentLight, l1, l2, l3, l5, l6, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 1 && zm)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 1 && zp)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 2 && xm)
    		return GetVertexLight(currentLight, l1, l4, l3, l8, l7, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 2 && xp)
    		return GetVertexLight(currentLight, l1, l2, l3, l5, l6, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 3 && zm)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 3 && zp)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 4 && xm){
    		int transientValue = GetVertexLight(currentLight, l1, l2, l3, l5, l6, ref fromOutsideBorder, ref flipDirection);
    		return (transientValue << 16) + (transientValue >> 16);
    	}
    	if(facing == 4 && xp)
    		return GetVertexLight(currentLight, l3, l4, l1, l7, l8, ref fromOutsideBorder, ref flipDirection);


	   	if(facing == 4 && zm){
    		int transientValue = GetVertexLight(currentLight, l2, l1, l4, l5, l8, ref fromOutsideBorder, ref flipDirection);
	   		if(fromOutsideBorder)
	   			if(!flipDirection)
	   				return transientValue;
	   			else
    				return (transientValue << 16) + (transientValue >> 16);
    		else
    			if(!flipDirection)
    				return (transientValue << 16) + (transientValue >> 16);
    			else
    				return transientValue;
	   	}
	   	if(facing == 4 && zp){
	   		int transientValue = GetVertexLight(currentLight, l4, l3, l2, l7, l6, ref fromOutsideBorder, ref flipDirection);
	   		if(fromOutsideBorder)
	   			if(flipDirection)
	   				return transientValue;
	   			else
    				return (transientValue << 16) + (transientValue >> 16);
    		else
    			if(flipDirection)
    				return (transientValue << 16) + (transientValue >> 16);
    			else
    				return transientValue;
	   	}


    	if(facing == 5 && xm)
    		return GetVertexLight(currentLight, l1, l4, l3, l8, l7, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 5 && xp)
    		return GetVertexLight(currentLight, l1, l2, l3, l5, l6, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 5 && zm)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7, ref fromOutsideBorder, ref flipDirection);
    	if(facing == 5 && zp)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8, ref fromOutsideBorder, ref flipDirection);
    	return 0;
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(0, xm, xp, zm, zp, ym, yp))
	    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, 0, isNatural:true);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(1, xm, xp, zm, zp, ym, yp))
	    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, 1, isNatural:true);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(2, xm, xp, zm, zp, ym, yp))
	    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, 2, isNatural:true);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(3, xm, xp, zm, zp, ym, yp))
	    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, 3, isNatural:true);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(0, xm, xp, zm, zp, ym, yp) && CheckBorder(1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[0] + VoxelData.offsets[1];
	    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(1, xm, xp, zm, zp, ym, yp) && CheckBorder(2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[1] + VoxelData.offsets[2];
	    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(2, xm, xp, zm, zp, ym, yp) && CheckBorder(3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[2] + VoxelData.offsets[3];
	    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(3, xm, xp, zm, zp, ym, yp) && CheckBorder(0, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[3] + VoxelData.offsets[0];
	    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:true);

    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}

		if(CheckTransient(facing, xm, zm, xp, zp)){
			transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
			array[0] = new Vector2(transientValue >> 24, 1);
			array[1] = new Vector2(((transientValue >> 16) & 0x000000FF), 1);
			array[2] = new Vector2(((transientValue >> 8) & 0x000000FF), 1);
			array[3] = new Vector2((transientValue & 0x000000FF), 1);
			return;
		}

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(0, xm, xp, zm, zp, ym, yp))
	    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, 0, isNatural:false);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(1, xm, xp, zm, zp, ym, yp))
	    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, 1, isNatural:false);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(2, xm, xp, zm, zp, ym, yp))
	    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, 2, isNatural:false);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(3, xm, xp, zm, zp, ym, yp))
	    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, 3, isNatural:false);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(0, xm, xp, zm, zp, ym, yp) && CheckBorder(1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[0] + VoxelData.offsets[1];
	    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(1, xm, xp, zm, zp, ym, yp) && CheckBorder(2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[1] + VoxelData.offsets[2];
	    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(2, xm, xp, zm, zp, ym, yp) && CheckBorder(3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[2] + VoxelData.offsets[3];
	    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(3, xm, xp, zm, zp, ym, yp) && CheckBorder(0, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[3] + VoxelData.offsets[0];
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

		if(CheckTransient(facing, xm, zm, xp, zp)){
			transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
			array[0] = new Vector2(array[0].x, transientValue >> 24);
			array[1] = new Vector2(array[1].x, ((transientValue >> 16) & 0x000000FF));
			array[2] = new Vector2(array[2].x, ((transientValue >> 8) & 0x000000FF));
			array[3] = new Vector2(array[3].x, (transientValue & 0x000000FF));
			return;
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
		if(blockMaterial[blockCode] == ShaderIndex.OPAQUE){
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
		else if(blockMaterial[blockCode] == ShaderIndex.SPECULAR){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeY);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			array[0] = new Vector2(0,1);
			array[1] = new Vector2(1,1);
			array[2] = new Vector2(1,0);
			array[3] = new Vector2(0,0);
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

//[BurstCompile]
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
	public NativeArray<byte> neighborlight;
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
	public NativeList<int> leavesTris;
	public NativeList<int> iceTris;

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
	public NativeArray<bool> blockSeamless;
	[ReadOnly]
	public NativeArray<bool> objectSeamless;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<ShaderIndex> blockMaterial;
	[ReadOnly]
	public NativeArray<ShaderIndex> objectMaterial;
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

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x-1)*Chunk.chunkWidth+Chunk.chunkWidth-1, y, pos.z*Chunk.chunkWidth+z));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(0,y,z));
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
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
						LoadMesh(0, y, z, 3, new int3(Chunk.chunkWidth-1, y, z), thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
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

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x+1)*Chunk.chunkWidth, y, pos.z*Chunk.chunkWidth+z));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(Chunk.chunkWidth-1,y,z));
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
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
						LoadMesh(Chunk.chunkWidth-1, y, z, 1, new int3(0, y, z), thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
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

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3(pos.x*Chunk.chunkWidth+x, y, (pos.z-1)*Chunk.chunkWidth+Chunk.chunkWidth-1));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(x, y, 0));
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
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
						LoadMesh(x, y, 0, 2, new int3(x, y, Chunk.chunkWidth-1), thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
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

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3(pos.x*Chunk.chunkWidth+x, y, (pos.z+1)*Chunk.chunkWidth));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(new int3(x, y, Chunk.chunkWidth-1));
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
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
						LoadMesh(x, y, Chunk.chunkWidth-1, 0, new int3(x, y, 0), thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal);
					}
				}
			}
			return;
		}
	}

	// Checks if other chunk border block is a liquid and puts it on Border Update List
	private void CheckBorderUpdate(int x, int y, int z, ushort blockCode){

		if(blockCode <= ushort.MaxValue/2){
			if(blockSeamless[blockCode]){
				toLoadEvent.Add(new int3(x,y,z));
			}
		}
		else{
			if(objectSeamless[ushort.MaxValue-blockCode]){
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
    private bool CheckSeams(int thisBlock, int neighborBlock){
    	bool thisSeamless;
    	bool neighborSeamless;

    	if(thisBlock <= ushort.MaxValue/2)
    		thisSeamless = blockSeamless[thisBlock];
    	else
    		thisSeamless = objectSeamless[ushort.MaxValue-thisBlock];

    	if(neighborBlock <= ushort.MaxValue/2)
    		neighborSeamless = blockSeamless[neighborBlock];
    	else
    		neighborSeamless = objectSeamless[ushort.MaxValue-neighborBlock];

    	return thisSeamless && neighborSeamless && (thisBlock == neighborBlock);
    }

    private bool Boolean(byte a){
    	if(a == 0)
    		return false;
    	return true;
    }

    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Load is true when Chunk is being loaded and not reloaded
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, int3 neighborIndex, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, int lookahead=0){
    	ShaderIndex renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == ShaderIndex.OPAQUE){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
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
    	else if(renderThread == ShaderIndex.SPECULAR){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
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
    	else if(renderThread == ShaderIndex.WATER){
    		VertsByState(cacheCubeVert, dir, metadata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
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

    	// If object is Leaves
    	else if(renderThread == ShaderIndex.LEAVES){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	leavesTris.Add(vCount -4);
	    	leavesTris.Add(vCount -4 +1);
	    	leavesTris.Add(vCount -4 +2);
	    	leavesTris.Add(vCount -4);
	    	leavesTris.Add(vCount -4 +2);
	    	leavesTris.Add(vCount -4 +3);

	    	return true;
    	}

    	// If object is Ice
    	else if(renderThread == ShaderIndex.ICE){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y,z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +1);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4 +3);

	    	return true;
    	}

    	return false;
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(neighborIndex.x > 1 || (neighborIndex.x == 1 && dir != 3) || (neighborIndex.x == 0 && dir == 1))
    		xm = false;
    	if(neighborIndex.x < Chunk.chunkWidth-2 || (neighborIndex.x == Chunk.chunkWidth-2 && dir != 1) || (neighborIndex.x == Chunk.chunkWidth-1 && dir == 3))
    		xp = false;
    	if(neighborIndex.z > 1 || (neighborIndex.z == 1 && dir != 2) || (neighborIndex.z == 0 && dir == 0))
    		zm = false;
    	if(neighborIndex.z < Chunk.chunkWidth-2 || (neighborIndex.z == Chunk.chunkWidth-2 && dir != 0) || (neighborIndex.z == Chunk.chunkWidth-1 && dir == 2))
    		zp = false;
    	if(neighborIndex.y > 1 || (neighborIndex.y == 1 && dir != 5) || (neighborIndex.y == 0 && dir == 4))
    		ym = false;
    	if(neighborIndex.y < Chunk.chunkDepth-2 || (neighborIndex.y == Chunk.chunkDepth-2 && dir != 4) || (neighborIndex.y == Chunk.chunkDepth-1 && dir == 5))
    		yp = false;


    	// If there's no light
    	if(currentLightLevel <= 1){
    		bool found = false;

    		if(neighborIndex.x > 0 && !xm)
    			if(GetNeighborLight(neighborIndex.x-1, neighborIndex.y, neighborIndex.z, dir) > 0)
    				found = true;
    		if(neighborIndex.x < Chunk.chunkWidth-1 && !xp)
    			if(GetNeighborLight(neighborIndex.x+1, neighborIndex.y, neighborIndex.z, dir) > 0)
    				found = true;
    		if(neighborIndex.z > 0 && !zm)
    			if(GetNeighborLight(neighborIndex.x, neighborIndex.y, neighborIndex.z-1, dir) > 0)
    				found = true;    		
    		if(neighborIndex.z < Chunk.chunkWidth-1 && !zp)
    			if(GetNeighborLight(neighborIndex.x, neighborIndex.y, neighborIndex.z+1, dir) > 0)
    				found = true;
    		if(neighborIndex.y > 0 && !ym)
    			if(GetNeighborLight(neighborIndex.x, neighborIndex.y-1, neighborIndex.z, dir) > 0)
    				found = true;
    		if(neighborIndex.y < Chunk.chunkDepth-1 && !yp)
    			if(GetNeighborLight(neighborIndex.x, neighborIndex.y+1, neighborIndex.z, dir) > 0)
    				found = true;

    		if(!found){
		    	array[0] = Vector2.zero;
		    	array[1] = Vector2.zero;
		    	array[2] = Vector2.zero;
		    	array[3] = Vector2.zero;
		    	return;
		    }
    	}

    	CalculateLightCorners(neighborIndex, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp);
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(array[0].x, maxLightLevel);
	    	array[1] = new Vector2(array[1].x, maxLightLevel);
	    	array[2] = new Vector2(array[2].x, maxLightLevel);
	    	array[3] = new Vector2(array[3].x, maxLightLevel);
	    	return;
    	}

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(neighborIndex.x > 0)
    		xm = false;
    	if(neighborIndex.x < Chunk.chunkWidth-1)
    		xp = false;
    	if(neighborIndex.z > 0)
    		zm = false;
    	if(neighborIndex.z < Chunk.chunkWidth-1)
    		zp = false;
    	if(neighborIndex.y > 0 || (neighborIndex.y == 0 && dir == 4))
    		ym = false;
    	if(neighborIndex.y < Chunk.chunkDepth-1)
    		yp = false;

    	// If there's no light
    	if(currentLightLevel <= 1){
    		bool found = false;

    		if(dir != 1 && neighborIndex.x != 0)
    			if(GetOtherLight(neighborIndex.x-1, neighborIndex.y, neighborIndex.z, dir, isNatural:false) > 0)
    				found = true;
    		if(dir != 3 && neighborIndex.x != Chunk.chunkWidth-1)
    			if(GetOtherLight(neighborIndex.x+1, neighborIndex.y, neighborIndex.z, dir, isNatural:false) > 0)
    				found = true;
    		if(dir != 0 && neighborIndex.z != 0)
    			if(GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z-1, dir, isNatural:false) > 0)
    				found = true;    		
    		if(dir != 2 && neighborIndex.z != Chunk.chunkWidth)
    			if(GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z+1, dir, isNatural:false) > 0)
    				found = true;
    		if(dir != 4 && neighborIndex.y != 0)
    			if(GetOtherLight(neighborIndex.x, neighborIndex.y-1, neighborIndex.z, dir, isNatural:false) > 0)
    				found = true;
    		if(dir != 5 && neighborIndex.y != Chunk.chunkDepth-1)
    			if(GetOtherLight(neighborIndex.x, neighborIndex.y+1, neighborIndex.z, dir, isNatural:false) > 0)
    				found = true;

    		if(!found){
		    	array[0] = new Vector2(array[0].x, 0);
		    	array[1] = new Vector2(array[1].x, 0);
		    	array[2] = new Vector2(array[2].x, 0);
		    	array[3] = new Vector2(array[3].x, 0);
		    	return;
		    }
    	}

    	CalculateLightCornersExtra(neighborIndex, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp, 4);
    	// Down
     	else
    		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp, 5);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	// North
    	if(dir == 0)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0);
    	// East
    	else if(dir == 1)
    		SetCornerExtra(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1);
    	// South
     	else if(dir == 2)
    		SetCornerExtra(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2);
    	// West
      	else if(dir == 3)
    		SetCornerExtra(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3);
      	// Up
    	else if(dir == 4)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp, 4);
    	// Down
     	else
    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp, 5);
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

    private bool CheckTransient(int facing, bool xm, bool zm, bool xp, bool zp){
    	if((facing == 0 || facing == 2) && (xm || xp))
    		return true;
    	if((facing == 1 || facing == 3) && (zm || zp))
    		return true;
    	if((facing == 4 || facing == 5) && (xm || zm || xp || zp))
    		return true;
    	return false;
    }

    private int GetVertexLight(int current, int n, int e, int s, int ne, int se){
    	int val = 0;

    	// Light from left scenario
    	if(current < e && e - current == 1){
    		if(current > n)
    			val = current << 24;
    		else
    			val = n << 24;

    		if(current > s)
    			val += current;
    		else
    			val += s;
    	}
    	// Light from right scenario
    	else if(current > e && current - e == 1){
    		if(current > n)
    			val = (current+1) << 24;
    		else
    			val = (n+1) << 24;

    		if(current > s)
    			val += current+1;
    		else
    			val += s+1;
    	}
    	// Light from above or bottom
    	else if(current == e){
    		if(current > n)
    			val = n << 24;
    		else
    			val = current << 24;

    		if(current > s)
    			val += s;
    		else
    			val += current;
    	}

    	// Enclosed space
    	else if(n == 0 && s == 0 && e == 0){
    		val = (current+1) << 24;
    		val += current+1;
    	}

    	// No recorded case
    	else{
    		val = current << 24;
    		val += current;
    	}


    	// Populate outer values
    	val += (Max(current, n, e, ne) << 16);
    	val += (Max(current, e, s, se) << 8);

    	return val;
    }

    private int ProcessTransient(int facing, bool xm, bool zm, bool xp, bool zp, int currentLight, int l1, int l2, int l3, int l4, int l5, int l6, int l7, int l8){
    	if(facing == 0 && xm)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8);
    	if(facing == 0 && xp)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7);
    	if(facing == 1 && zm)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7);
    	if(facing == 1 && zp)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8);
    	if(facing == 2 && xm)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7);
    	if(facing == 2 && xp)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8);
    	if(facing == 3 && zm)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8);
    	if(facing == 3 && zp)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7);
    	if(facing == 4 && xm){
    		int transientValue = GetVertexLight(currentLight, l1, l2, l3, l5, l6);
    		return (transientValue << 16) + (transientValue >> 16);
    	}
    	if(facing == 4 && xp)
    		return GetVertexLight(currentLight, l3, l4, l1, l7, l8);
	   	if(facing == 4 && zm){
    		int transientValue = GetVertexLight(currentLight, l2, l1, l4, l5, l8);
    		return (transientValue << 16) + (transientValue >> 16);
	   	}
	   	if(facing == 4 && zp)
    		return GetVertexLight(currentLight, l4, l3, l2, l7, l6);
    	if(facing == 5 && xm)
    		return GetVertexLight(currentLight, l2, l1, l4, l5, l8);
    	if(facing == 5 && xp)
    		return GetVertexLight(currentLight, l2, l3, l4, l6, l7);
    	if(facing == 5 && zm)
    		return GetVertexLight(currentLight, l1, l4, l3, l8, l7);
    	if(facing == 5 && zp)
    		return GetVertexLight(currentLight, l1, l2, l3, l5, l6);
    	return 0;
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
	    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
	    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
	    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
	    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:true);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:true);

    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}

		if(CheckTransient(facing, xm, zm, xp, zp)){
			transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
			array[0] = new Vector2(transientValue >> 24, 1);
			array[1] = new Vector2(((transientValue >> 16) & 0x000000FF), 1);
			array[2] = new Vector2(((transientValue >> 8) & 0x000000FF), 1);
			array[3] = new Vector2((transientValue & 0x000000FF), 1);
			return;
		}

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(0, xm, xp, zm, zp, ym, yp))
	    		light1 = GetOtherLight(pos.x, pos.y, pos.z, 0, isNatural:false);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(1, xm, xp, zm, zp, ym, yp))
	    		light2 = GetOtherLight(pos.x, pos.y, pos.z, 1, isNatural:false);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(2, xm, xp, zm, zp, ym, yp))
	    		light3 = GetOtherLight(pos.x, pos.y, pos.z, 2, isNatural:false);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(3, xm, xp, zm, zp, ym, yp))
	    		light4 = GetOtherLight(pos.x, pos.y, pos.z, 3, isNatural:false);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(0, xm, xp, zm, zp, ym, yp) && CheckBorder(1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[0] + VoxelData.offsets[1];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(1, xm, xp, zm, zp, ym, yp) && CheckBorder(2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[1] + VoxelData.offsets[2];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(2, xm, xp, zm, zp, ym, yp) && CheckBorder(3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[2] + VoxelData.offsets[3];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(3, xm, xp, zm, zp, ym, yp) && CheckBorder(0, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[3] + VoxelData.offsets[0];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:false);

    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}

		if(CheckTransient(facing, xm, zm, xp, zp)){
			transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
			array[0] = new Vector2(array[0].x, transientValue >> 24);
			array[1] = new Vector2(array[1].x, ((transientValue >> 16) & 0x000000FF));
			array[2] = new Vector2(array[2].x, ((transientValue >> 8) & 0x000000FF));
			array[3] = new Vector2(array[3].x, (transientValue & 0x000000FF));
			return;
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
		if(blockMaterial[blockCode] == ShaderIndex.OPAQUE){
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
		else if(blockMaterial[blockCode] == ShaderIndex.SPECULAR){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeY);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			array[0] = new Vector2(0,1);
			array[1] = new Vector2(1,1);
			array[2] = new Vector2(1,0);
			array[3] = new Vector2(0,0);
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

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;
		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, int dir, bool isNatural=true){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(isNatural)
			return neighborlight[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] & 0x0F;
		else
			return neighborlight[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return neighborlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return neighborlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;

		if(isNatural)
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int3 coord, bool isNatural=true){
		if(isNatural)
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
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