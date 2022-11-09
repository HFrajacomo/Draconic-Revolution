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
	public static readonly int chunkMaxY = 3;
	public static float chunkWidthMult = 15.99f; 
	public ChunkPos pos;
	public string biomeName;
	public byte needsGeneration;
	public float4 features;
	public string lastVisitedTime;

	// Multiplayer Settings
	private NetMessage message;

	// Debug Settings
	private static bool showHitbox = false;
	private MeshFilter hitboxFilter;

	// Draw Flags
	public bool drawMain = false;
	public bool xpDraw = false;
	public bool zpDraw = false;
	public bool xmDraw = false;
	public bool zmDraw = false;
	public bool xmzm = false;
	public bool xpzm = false;
	public bool xmzp = false;
	public bool xpzp = false;

	// Bud Flags
	public bool xpBUD = false;
	public bool xmBUD = false;
	public bool zpBUD = false;
	public bool zmBUD = false;
	public bool xpzmBUD = false;
	public bool xpzpBUD = false;
	public bool xmzmBUD = false;
	public bool xmzpBUD = false;

	/*
		Unity Settings
	*/
	// Block Chunks 
	public ChunkRenderer renderer;
	public MeshFilter meshFilter;
	public MeshCollider meshCollider;
	public GameObject obj;
	public BlockEncyclopedia blockBook;
	public ChunkLoader loader;

	// Decal Chunk
	public MeshFilter meshFilterDecal;
	public GameObject objDecal;

	// Raycast Collider Chunk
	public MeshCollider meshColliderRaycast;
	public GameObject objRaycast;

	// Main Mesh Information
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

  	// Decal Mesh Information
  	private List<Vector3> decalVertices = new List<Vector3>();
  	private List<Vector2> decalUV = new List<Vector2>();
  	private int[] decalTris;

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
    private List<Vector3> cacheHitboxVerts = new List<Vector3>();
    private List<Vector3> cacheHitboxNormals = new List<Vector3>();
    private List<int> cacheHitboxTriangles = new List<int>();
    private List<int> indexHitboxVert = new List<int>();
    private List<int> indexHitboxTris = new List<int>();
    private List<Vector3> hitboxScaling = new List<Vector3>();

    // Decals Cache
    private Mesh mesh;
    private Mesh meshDecal;
    private Mesh meshRaycast;
    private Mesh cacheMesh = new Mesh();

    // General Cache
    private ChunkPos[] surroundingChunks = new ChunkPos[8];

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
		this.objDecal = new GameObject();
		this.objDecal.name = "Decals " + pos.x + ", " + pos.z;
		this.objDecal.transform.SetParent(this.renderer.transform);
		this.objDecal.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.objRaycast = new GameObject();
		this.objRaycast.name = "RaycastCollider " + pos.x + ", " + pos.z;
		this.objRaycast.transform.SetParent(this.renderer.transform);
		this.objRaycast.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.objRaycast.layer = 11;

		this.data = new VoxelData();
		this.metadata = new VoxelMetadata();

		this.obj.AddComponent<MeshFilter>();
		MeshRenderer msr = this.obj.AddComponent<MeshRenderer>() as MeshRenderer;
		this.obj.AddComponent<MeshCollider>();
		this.objDecal.AddComponent<MeshFilter>();
		MeshRenderer msrDecal = this.objDecal.AddComponent<MeshRenderer>() as MeshRenderer;
		msrDecal.shadowCastingMode = ShadowCastingMode.Off;
		this.meshColliderRaycast = this.objRaycast.AddComponent<MeshCollider>() as MeshCollider;

		this.meshFilter = this.obj.GetComponent<MeshFilter>();
		this.meshCollider = this.obj.GetComponent<MeshCollider>();
		this.meshFilterDecal = this.objDecal.GetComponent<MeshFilter>();
		this.obj.GetComponent<MeshRenderer>().sharedMaterials = this.renderer.GetComponent<MeshRenderer>().sharedMaterials;
		this.objDecal.GetComponent<MeshRenderer>().material = this.renderer.decalMaterial;
		this.blockBook = be;
		this.obj.layer = 8;

		this.mesh = new Mesh();
		this.mesh.MarkDynamic();
		this.meshFilter.mesh = this.mesh;
		this.meshCollider.sharedMesh = this.mesh;
		this.meshDecal = new Mesh();
		this.meshDecal.MarkDynamic();
		this.meshFilterDecal.mesh = this.meshDecal;
		this.meshRaycast = new Mesh();
		this.meshRaycast.MarkDynamic();
		this.meshRaycast.name = this.objRaycast.name;
		this.meshColliderRaycast.sharedMesh = this.meshRaycast;

		this.surroundingChunks[0] = new ChunkPos(pos.x, pos.z+1, pos.y);
		this.surroundingChunks[1] = new ChunkPos(pos.x+1, pos.z, pos.y);
		this.surroundingChunks[2] = new ChunkPos(pos.x, pos.z-1, pos.y);
		this.surroundingChunks[3] = new ChunkPos(pos.x-1, pos.z, pos.y);
		this.surroundingChunks[4] = new ChunkPos(pos.x+1, pos.z-1, pos.y);
		this.surroundingChunks[5] = new ChunkPos(pos.x-1, pos.z-1, pos.y);
		this.surroundingChunks[6] = new ChunkPos(pos.x-1, pos.z+1, pos.y);
		this.surroundingChunks[7] = new ChunkPos(pos.x+1, pos.z+1, pos.y);

		if(showHitbox){
			this.hitboxFilter = this.objRaycast.AddComponent<MeshFilter>() as MeshFilter;
			this.objRaycast.AddComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		}
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
		GameObject.Destroy(this.objDecal);
		GameObject.Destroy(this.objRaycast);
		this.obj = null;
		Object.Destroy(this.mesh);
		Object.Destroy(this.meshFilter);
		Object.Destroy(this.meshCollider);
		Object.Destroy(this.meshColliderRaycast);
		this.meshFilter = null;
		this.meshCollider = null; 
		this.loader = null;
		this.blockBook = null;

		this.data.Destroy();
		this.metadata.Destroy();

		this.data = null;
		this.metadata = null;
		this.mesh = null;
		this.meshDecal = null;
		this.meshRaycast = null;
		this.meshFilterDecal = null;
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
        if(!xmDraw && loader.chunks.ContainsKey(this.surroundingChunks[3]))
            return true;
        if(!xpDraw && loader.chunks.ContainsKey(this.surroundingChunks[1]))
            return true;
        if(!zmDraw && loader.chunks.ContainsKey(this.surroundingChunks[2]))
            return true;
        if(!zpDraw && loader.chunks.ContainsKey(this.surroundingChunks[0]))
            return true;

        if(!xmzm && loader.chunks.ContainsKey(this.surroundingChunks[2]) && loader.chunks.ContainsKey(this.surroundingChunks[3]) && loader.chunks.ContainsKey(this.surroundingChunks[5]))
        	return true;
        if(!xpzm && loader.chunks.ContainsKey(this.surroundingChunks[1]) && loader.chunks.ContainsKey(this.surroundingChunks[2]) && loader.chunks.ContainsKey(this.surroundingChunks[4]))
        	return true;
        if(!xmzp && loader.chunks.ContainsKey(this.surroundingChunks[0]) && loader.chunks.ContainsKey(this.surroundingChunks[3]) && loader.chunks.ContainsKey(this.surroundingChunks[6]))
        	return true;
        if(!xpzp && loader.chunks.ContainsKey(this.surroundingChunks[0]) && loader.chunks.ContainsKey(this.surroundingChunks[1]) && loader.chunks.ContainsKey(this.surroundingChunks[7]))
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
			xmzm = false;
			xmzp = false;
			xpzm = false;
			xpzp = false;
		}

		if(!CheckRedraw())
			return false;

		int3[] coordArray;

		NativeArray<ushort> blockdata = NativeTools.CopyToNative(this.data.GetData());
		NativeArray<ushort> hpdata = NativeTools.CopyToNative(this.metadata.GetHPData());
		NativeArray<ushort> metadata = NativeTools.CopyToNative(this.metadata.GetStateData());
		NativeArray<byte> lightdata = NativeTools.CopyToNative(this.data.GetLightMap(this.metadata));

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

		// Decals
		NativeList<Vector3> vertsDecal = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVDecal = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<int> trisDecal = new NativeList<int>(0, Allocator.TempJob); 
		NativeArray<Vector3> cacheCubeVertsDecal = new NativeArray<Vector3>(4, Allocator.TempJob);


		// For Init
		NativeArray<Vector3> disposableVerts = NativeTools.CopyToNative<Vector3>(this.vertices.ToArray());
		NativeArray<Vector2> disposableUVS = NativeTools.CopyToNative<Vector2>(this.UVs.ToArray());
		NativeArray<Vector2> disposableLight = NativeTools.CopyToNative<Vector2>(this.lightUVMain.ToArray());
		NativeArray<Vector3> disposableNormals = NativeTools.CopyToNative<Vector3>(this.normals.ToArray());

		// Decals
		NativeArray<Vector3> disposableVertsDecal = NativeTools.CopyToNative<Vector3>(this.decalVertices.ToArray());
		NativeArray<Vector2> disposableUVSDecal = NativeTools.CopyToNative<Vector2>(this.decalUV.ToArray());


		NativeArray<int> disposableTris = new NativeArray<int>(this.triangles, Allocator.TempJob);
		NativeArray<int> disposableSpecTris = new NativeArray<int>(this.specularTris, Allocator.TempJob);
		NativeArray<int> disposableLiquidTris = new NativeArray<int>(this.liquidTris, Allocator.TempJob);
		NativeArray<int> disposableLeavesTris = new NativeArray<int>(this.leavesTris, Allocator.TempJob);
		NativeArray<int> disposableIceTris = new NativeArray<int>(this.iceTris, Allocator.TempJob);
		NativeArray<int> disposableDecalTris = new NativeArray<int>(this.decalTris, Allocator.TempJob);


		JobHandle job;
		JobHandle jobDecal;


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
		vertsDecal.AddRange(disposableVertsDecal);
		UVDecal.AddRange(disposableUVSDecal);
		trisDecal.AddRange(disposableDecalTris);


		// Dispose Init
		disposableVerts.Dispose();
		disposableVertsDecal.Dispose();
		disposableUVS.Dispose();
		disposableUVSDecal.Dispose();
		disposableTris.Dispose();
		disposableSpecTris.Dispose();
		disposableLiquidTris.Dispose();
		disposableNormals.Dispose();
		disposableLight.Dispose();
		disposableLeavesTris.Dispose();
		disposableIceTris.Dispose();
		disposableDecalTris.Dispose();

		// X- Analysis
		ChunkPos targetChunk = new ChunkPos(this.pos.x-1, this.pos.z, this.pos.y);
		if(loader.chunks.ContainsKey(targetChunk) && !xmDraw){
			xmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap(loader.chunks[targetChunk].metadata));
			
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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless
			};

			BuildDecalSideJob bdsj = new BuildDecalSideJob{
				pos = this.pos,
				blockdata = blockdata,
				neighbordata = neighbordata,
				hpdata = hpdata,
				xm = true,
				zm = false,
				xp = false,
				zp = false,
				verts = vertsDecal,
				UV = UVDecal,
				triangles = trisDecal,
				blockHP = BlockEncyclopediaECS.blockHP,
				objectHP = BlockEncyclopediaECS.objectHP,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				cacheCubeVerts = cacheCubeVertsDecal
			};

			job = bbJob.Schedule();
			jobDecal = bdsj.Schedule();

			job.Complete();
			jobDecal.Complete();
			
			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);

			if(loadBUD && !xmBUD){
				xmBUD = true;
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
		targetChunk = new ChunkPos(this.pos.x+1, this.pos.z, this.pos.y); 
		if(loader.chunks.ContainsKey(targetChunk) && !xpDraw){
			xpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap(loader.chunks[targetChunk].metadata));

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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless
			};

			BuildDecalSideJob bdsj = new BuildDecalSideJob{
				pos = this.pos,
				blockdata = blockdata,
				neighbordata = neighbordata,
				hpdata = hpdata,
				xm = false,
				zm = false,
				xp = true,
				zp = false,
				verts = vertsDecal,
				UV = UVDecal,
				triangles = trisDecal,
				blockHP = BlockEncyclopediaECS.blockHP,
				objectHP = BlockEncyclopediaECS.objectHP,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				cacheCubeVerts = cacheCubeVertsDecal
			};

			job = bbJob.Schedule();
			jobDecal = bdsj.Schedule();

			job.Complete();
			jobDecal.Complete();

			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);

			if(loadBUD && !xpBUD){
				xpBUD = true;
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
		targetChunk = new ChunkPos(this.pos.x, this.pos.z-1, this.pos.y); 
		if(loader.chunks.ContainsKey(targetChunk) && !zmDraw){
			zmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap(loader.chunks[targetChunk].metadata));

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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless
			};

			BuildDecalSideJob bdsj = new BuildDecalSideJob{
				pos = this.pos,
				blockdata = blockdata,
				neighbordata = neighbordata,
				hpdata = hpdata,
				xm = false,
				zm = true,
				xp = false,
				zp = false,
				verts = vertsDecal,
				UV = UVDecal,
				triangles = trisDecal,
				blockHP = BlockEncyclopediaECS.blockHP,
				objectHP = BlockEncyclopediaECS.objectHP,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				cacheCubeVerts = cacheCubeVertsDecal
			};

			job = bbJob.Schedule();
			jobDecal = bdsj.Schedule();

			job.Complete();
			jobDecal.Complete();

			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD && !zmBUD){
				zmBUD = true;
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
		targetChunk = new ChunkPos(this.pos.x, this.pos.z+1, this.pos.y); 
		if(loader.chunks.ContainsKey(targetChunk) && !zpDraw){
			zpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[targetChunk].data.GetData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[targetChunk].data.GetLightMap(loader.chunks[targetChunk].metadata));

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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless
			};

			BuildDecalSideJob bdsj = new BuildDecalSideJob{
				pos = this.pos,
				blockdata = blockdata,
				neighbordata = neighbordata,
				hpdata = hpdata,
				xm = false,
				zm = false,
				xp = false,
				zp = true,
				verts = vertsDecal,
				UV = UVDecal,
				triangles = trisDecal,
				blockHP = BlockEncyclopediaECS.blockHP,
				objectHP = BlockEncyclopediaECS.objectHP,
				blockInvisible = BlockEncyclopediaECS.blockInvisible,
				objectInvisible = BlockEncyclopediaECS.objectInvisible,
				blockTransparent = BlockEncyclopediaECS.blockTransparent,
				objectTransparent = BlockEncyclopediaECS.objectTransparent,
				cacheCubeVerts = cacheCubeVertsDecal
			};

			job = bbJob.Schedule();
			jobDecal = bdsj.Schedule();

			job.Complete();
			jobDecal.Complete();

			neighbordata.Dispose();
			neighborlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD && !zpBUD){
				zpBUD = true;
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

		// XPZM Corner
		if(loader.chunks.ContainsKey(this.surroundingChunks[1]) && loader.chunks.ContainsKey(this.surroundingChunks[2]) && loader.chunks.ContainsKey(this.surroundingChunks[4]) && !xpzm){
			xpzm = true;
			changed = true;

			NativeArray<ushort> xsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[1]].data.GetData());
			NativeArray<byte> xsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[1]].data.GetLightMap(loader.chunks[this.surroundingChunks[1]].metadata));	
			NativeArray<ushort> zsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[2]].data.GetData());
			NativeArray<byte> zsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[2]].data.GetLightMap(loader.chunks[this.surroundingChunks[2]].metadata));
			NativeArray<ushort> cornerdata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[4]].data.GetData());
			NativeArray<byte> cornerlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[4]].data.GetLightMap(loader.chunks[this.surroundingChunks[4]].metadata));

			BuildCornerJob bcj = new BuildCornerJob{
				xsidedata = xsidedata,
				xsidelight = xsidelight,
				zsidedata = zsidedata,
				zsidelight = zsidelight,
				cornerdata = cornerdata,
				cornerlight = cornerlight,

				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				toLoadEvent = toLoadEvent,
				xmzm = false,
				xpzm = true,
				xmzp = false,
				xpzp = false,
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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless				
			};

			job = bcj.Schedule();
			job.Complete();

			xsidedata.Dispose();
			xsidelight.Dispose();
			zsidedata.Dispose();
			zsidelight.Dispose();
			cornerdata.Dispose();
			cornerlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD && !xpzmBUD){
				xpzmBUD = true;
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

		// XMZM Corner
		if(loader.chunks.ContainsKey(this.surroundingChunks[2]) && loader.chunks.ContainsKey(this.surroundingChunks[3]) && loader.chunks.ContainsKey(this.surroundingChunks[5]) && !xmzm){
			xmzm = true;
			changed = true;

			NativeArray<ushort> xsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[3]].data.GetData());
			NativeArray<byte> xsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[3]].data.GetLightMap(loader.chunks[this.surroundingChunks[3]].metadata));	
			NativeArray<ushort> zsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[2]].data.GetData());
			NativeArray<byte> zsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[2]].data.GetLightMap(loader.chunks[this.surroundingChunks[2]].metadata));
			NativeArray<ushort> cornerdata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[5]].data.GetData());
			NativeArray<byte> cornerlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[5]].data.GetLightMap(loader.chunks[this.surroundingChunks[5]].metadata));

			BuildCornerJob bcj = new BuildCornerJob{
				xsidedata = xsidedata,
				xsidelight = xsidelight,
				zsidedata = zsidedata,
				zsidelight = zsidelight,
				cornerdata = cornerdata,
				cornerlight = cornerlight,

				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				toLoadEvent = toLoadEvent,
				xmzm = true,
				xpzm = false,
				xmzp = false,
				xpzp = false,
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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless				
			};

			job = bcj.Schedule();
			job.Complete();

			xsidedata.Dispose();
			xsidelight.Dispose();
			zsidedata.Dispose();
			zsidelight.Dispose();
			cornerdata.Dispose();
			cornerlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD && !xmzmBUD){
				xmzmBUD = true;
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

		// XMZP Corner
		if(loader.chunks.ContainsKey(this.surroundingChunks[3]) && loader.chunks.ContainsKey(this.surroundingChunks[0]) && loader.chunks.ContainsKey(this.surroundingChunks[6]) && !xmzp){
			xmzp = true;
			changed = true;

			NativeArray<ushort> xsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[3]].data.GetData());
			NativeArray<byte> xsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[3]].data.GetLightMap(loader.chunks[this.surroundingChunks[3]].metadata));	
			NativeArray<ushort> zsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[0]].data.GetData());
			NativeArray<byte> zsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[0]].data.GetLightMap(loader.chunks[this.surroundingChunks[0]].metadata));
			NativeArray<ushort> cornerdata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[6]].data.GetData());
			NativeArray<byte> cornerlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[6]].data.GetLightMap(loader.chunks[this.surroundingChunks[6]].metadata));

			BuildCornerJob bcj = new BuildCornerJob{
				xsidedata = xsidedata,
				xsidelight = xsidelight,
				zsidedata = zsidedata,
				zsidelight = zsidelight,
				cornerdata = cornerdata,
				cornerlight = cornerlight,

				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				toLoadEvent = toLoadEvent,
				xmzm = false,
				xpzm = false,
				xmzp = true,
				xpzp = false,
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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless				
			};

			job = bcj.Schedule();
			job.Complete();

			xsidedata.Dispose();
			xsidelight.Dispose();
			zsidedata.Dispose();
			zsidelight.Dispose();
			cornerdata.Dispose();
			cornerlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD && !xmzpBUD){
				xmzpBUD = true;
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

		// XPZP Corner
		if(loader.chunks.ContainsKey(this.surroundingChunks[0]) && loader.chunks.ContainsKey(this.surroundingChunks[1]) && loader.chunks.ContainsKey(this.surroundingChunks[7]) && !xpzp){
			xpzp = true;
			changed = true;

			NativeArray<ushort> xsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[1]].data.GetData());
			NativeArray<byte> xsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[1]].data.GetLightMap(loader.chunks[this.surroundingChunks[1]].metadata));	
			NativeArray<ushort> zsidedata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[0]].data.GetData());
			NativeArray<byte> zsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[0]].data.GetLightMap(loader.chunks[this.surroundingChunks[0]].metadata));
			NativeArray<ushort> cornerdata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingChunks[7]].data.GetData());
			NativeArray<byte> cornerlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[7]].data.GetLightMap(loader.chunks[this.surroundingChunks[7]].metadata));

			BuildCornerJob bcj = new BuildCornerJob{
				xsidedata = xsidedata,
				xsidelight = xsidelight,
				zsidedata = zsidedata,
				zsidelight = zsidelight,
				cornerdata = cornerdata,
				cornerlight = cornerlight,

				pos = this.pos,
				toBUD = toBUD,
				reload = loadBUD,
				data = blockdata,
				metadata = metadata,
				lightdata = lightdata,
				toLoadEvent = toLoadEvent,
				xmzm = false,
				xpzm = false,
				xmzp = false,
				xpzp = true,
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
				blockTiles = BlockEncyclopediaECS.blockTiles,
				blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless				
			};

			job = bcj.Schedule();
			job.Complete();

			xsidedata.Dispose();
			xsidelight.Dispose();
			zsidedata.Dispose();
			zsidelight.Dispose();
			cornerdata.Dispose();
			cornerlight.Dispose();

			// ToLoad() Event Trigger
			coordArray = toLoadEvent.AsArray().ToArray();
			this.message = new NetMessage(NetCode.BATCHLOADBUD);
			this.message.BatchLoadBUD(this.pos);
			if(loadBUD && !xpzpBUD){
				xpzpBUD = true;
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
		
		// If mesh was redrawn
		if(changed){
			NativeTris triangleStructure = new NativeTris(tris, specularTris, liquidTris, leavesTris, iceTris);

			BuildMeshSide(verts.ToArray(), uvs.ToArray(), lightUV.ToArray(), normals.ToArray(), triangleStructure);
			BuildDecalMesh(vertsDecal.ToArray(), UVDecal.ToArray(), trisDecal.ToArray());
		}

		tris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		leavesTris.Dispose();
		iceTris.Dispose();

		blockdata.Dispose();
		metadata.Dispose();
		verts.Dispose();
		uvs.Dispose();
		normals.Dispose();
		cacheCubeVert.Dispose();
		cacheUVVerts.Dispose();
		cacheCubeNormal.Dispose();
		toLoadEvent.Dispose();
		toBUD.Dispose();
		lightUV.Dispose();
		lightdata.Dispose();
		hpdata.Dispose();
		vertsDecal.Dispose();
		UVDecal.Dispose();
		trisDecal.Dispose();
		cacheCubeVertsDecal.Dispose();

		return doneRendering;
	}


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(bool load=false, bool pregenReload=false){
		/*
		Reset Chunk side rebuilding
		*/

		// DEBUG
		/*
		drawMain = true;
		this.triangles = new int[0];
    	this.specularTris = new int[0];
    	this.liquidTris = new int[0];
    	this.iceTris = new int[0];
    	this.assetTris = new int[0];
    	this.decalTris = new int[0];
    	this.leavesTris = new int[0];
    	return;
    	*/
    	
    	this.vertices.Clear();
    	this.normals.Clear();
    	this.triangles = null;
    	this.specularTris = null;
    	this.liquidTris = null;
    	this.iceTris = null;
    	this.assetTris = null;
    	this.UVs.Clear();
    	this.lightUVMain.Clear();
    	this.decalVertices.Clear();
    	this.decalUV.Clear();
    	this.decalTris = null;

		NativeArray<ushort> blockdata = NativeTools.CopyToNative<ushort>(this.data.GetData());
		NativeArray<ushort> statedata = NativeTools.CopyToNative<ushort>(this.metadata.GetStateData());
		NativeArray<byte> lightdata = NativeTools.CopyToNative<byte>(this.data.GetLightMap(this.metadata));
		NativeArray<byte> renderMap = NativeTools.CopyToNative<byte>(this.data.GetRenderMap());
		
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
			pos = pos,
			load = load,
			data = blockdata,
			state = statedata,
			lightdata = lightdata,
			loadOutList = loadCoordList,
			loadAssetList = loadAssetList,
			renderMap = renderMap,
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
			blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless
		};
		JobHandle job = bcJob.Schedule();

		BuildAllDecals(blockdata);

		job.Complete();

		this.indexVert.Add(0);
		this.indexUV.Add(0);
		this.indexTris.Add(0);
		this.indexHitboxVert.Add(0);
		this.indexHitboxTris.Add(0);

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
				this.hitboxScaling.Add(BlockEncyclopediaECS.hitboxScaling[ushort.MaxValue-assetCode]);

				vertexAux.Clear();
				UVaux.Clear();
				normalAux.Clear();

				blockBook.objects[ushort.MaxValue-assetCode].hitboxMesh.GetVertices(vertexAux);
				this.cacheHitboxVerts.AddRange(vertexAux.ToArray());
				this.cacheHitboxTriangles.AddRange(blockBook.objects[ushort.MaxValue-assetCode].hitboxMesh.GetTriangles(0));
				blockBook.objects[ushort.MaxValue-assetCode].mesh.GetNormals(normalAux);
				this.cacheHitboxNormals.AddRange(normalAux);

				this.indexHitboxVert.Add(this.indexHitboxVert[indexHitboxVert.Count-1] + vertexAux.Count);
				this.indexHitboxTris.Add(this.indexHitboxTris[indexHitboxTris.Count-1] + blockBook.objects[ushort.MaxValue-assetCode].hitboxMesh.GetTriangles(0).Length);

				vertexAux.Clear();
				normalAux.Clear();



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
		NativeList<Vector3> hitboxVerts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector3> hitboxNormals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<int> hitboxTriangles = new NativeList<int>(0, Allocator.TempJob);
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
		NativeArray<Vector3> hitboxScaling = new NativeArray<Vector3>(this.hitboxScaling.ToArray(), Allocator.TempJob);

		NativeArray<Vector3> loadedHitboxVerts = new NativeArray<Vector3>(this.cacheHitboxVerts.ToArray(), Allocator.TempJob);
		NativeArray<Vector3> loadedHitboxNormals = new NativeArray<Vector3>(this.cacheHitboxNormals.ToArray(), Allocator.TempJob);
		NativeArray<int> loadedHitboxTriangles = new NativeArray<int>(this.cacheHitboxTriangles.ToArray(), Allocator.TempJob);
		NativeArray<int> hitboxVertsOffset = new NativeArray<int>(this.indexHitboxVert.ToArray(), Allocator.TempJob);
		NativeArray<int> hitboxTrisOffset = new NativeArray<int>(this.indexHitboxTris.ToArray(), Allocator.TempJob);

		PrepareAssetsJob paJob = new PrepareAssetsJob{
			pos = pos,
			vCount = verts.Length,

			meshVerts = meshVerts,
			meshTris = meshTris,
			meshUVs = meshUVs,
			meshLightUV = meshLightUV,
			meshNormals = meshNormals,
			hitboxVerts = hitboxVerts,
			hitboxNormals = hitboxNormals,
			hitboxTriangles = hitboxTriangles,
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
			loadedNormals = loadedNormals,

			loadedHitboxVerts = loadedHitboxVerts,
			loadedHitboxNormals = loadedHitboxNormals,
			loadedHitboxTriangles = loadedHitboxTriangles,
			hitboxVertsOffset = hitboxVertsOffset,
			hitboxTrisOffset = hitboxTrisOffset,
			hitboxScaling = hitboxScaling
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

		BuildHitboxMesh(hitboxVerts.ToArray(), hitboxNormals.ToArray(), hitboxTriangles.ToArray());

		// Dispose Bin
		verts.Dispose();
		normalTris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		leavesTris.Dispose();
		iceTris.Dispose();
		blockdata.Dispose();
		statedata.Dispose();
		renderMap.Dispose();
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
		loadedHitboxVerts.Dispose();
		loadedHitboxNormals.Dispose();
		loadedHitboxTriangles.Dispose();
		hitboxVerts.Dispose();
		hitboxNormals.Dispose();
		hitboxTriangles.Dispose();
		hitboxVertsOffset.Dispose();
		hitboxTrisOffset.Dispose();
		hitboxScaling.Dispose();


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
		this.indexHitboxVert.Clear();
		this.indexHitboxTris.Clear();
		this.hitboxScaling.Clear();
		this.cacheLightUV.Clear();
		this.drawMain = true;
    }

    public void BuildAllDecals(NativeArray<ushort> blockdata){
    	NativeArray<ushort> hpdata = NativeTools.CopyToNative<ushort>(this.metadata.GetHPData());

		NativeList<int> triangles = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheVerts = new NativeArray<Vector3>(4, Allocator.TempJob);

		BuildDecalJob bdj = new BuildDecalJob{
			pos = pos,
			blockdata = blockdata,
			verts = verts,
			UV = UVs,
			triangles = triangles,
			hpdata = hpdata,
			blockHP = BlockEncyclopediaECS.blockHP,
			objectHP = BlockEncyclopediaECS.objectHP,
			blockInvisible = BlockEncyclopediaECS.blockInvisible,
			objectInvisible = BlockEncyclopediaECS.objectInvisible,
			blockTransparent = BlockEncyclopediaECS.blockTransparent,
			objectTransparent = BlockEncyclopediaECS.objectTransparent,
			cacheCubeVerts = cacheVerts
		};
		JobHandle job = bdj.Schedule();
		job.Complete();

		this.decalVertices.AddRange(verts.ToArray());
		this.decalUV.AddRange(UVs.ToArray());
		this.decalTris = triangles.ToArray();

		BuildDecalMesh(verts.ToArray(), UVs.ToArray(), this.decalTris);

		// Dispose Bin
		hpdata.Dispose();
		triangles.Dispose();
		verts.Dispose();
		UVs.Dispose();
		cacheVerts.Dispose();
    }

    public void BuildAllDecals(){
    	NativeArray<ushort> blockdata = NativeTools.CopyToNative<ushort>(this.data.GetData());
    	NativeArray<ushort> hpdata = NativeTools.CopyToNative<ushort>(this.metadata.GetHPData());

		NativeList<int> triangles = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheVerts = new NativeArray<Vector3>(4, Allocator.TempJob);

		BuildDecalJob bdj = new BuildDecalJob{
			pos = pos,
			blockdata = blockdata,
			verts = verts,
			UV = UVs,
			triangles = triangles,
			hpdata = hpdata,
			blockHP = BlockEncyclopediaECS.blockHP,
			objectHP = BlockEncyclopediaECS.objectHP,
			blockInvisible = BlockEncyclopediaECS.blockInvisible,
			objectInvisible = BlockEncyclopediaECS.objectInvisible,
			blockTransparent = BlockEncyclopediaECS.blockTransparent,
			objectTransparent = BlockEncyclopediaECS.objectTransparent,
			cacheCubeVerts = cacheVerts
		};
		JobHandle job = bdj.Schedule();
		job.Complete();

		BuildDecalMesh(verts.ToArray(), UVs.ToArray(), triangles.ToArray());

		// Dispose Bin
		hpdata.Dispose();
		triangles.Dispose();
		verts.Dispose();
		UVs.Dispose();
		cacheVerts.Dispose();
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
    	this.meshFilter.mesh.SetTriangles(this.triangles, 0);
 	    this.meshFilter.mesh.SetTriangles(this.iceTris, 5);

    	// Fix for a stupid Unity Bug
    	if(this.vertices.Count > 0){
	    	this.meshCollider.sharedMesh = null;
	    	this.meshCollider.sharedMesh = this.meshFilter.mesh;
    	}

    	this.meshFilter.mesh.SetTriangles(this.specularTris, 1);
    	this.meshFilter.mesh.SetTriangles(this.liquidTris, 2);
    	this.meshFilter.mesh.SetTriangles(this.assetTris, 3);
 	    this.meshFilter.mesh.SetTriangles(this.leavesTris, 4);

    	this.meshFilter.mesh.SetUVs(0, this.UVs.ToArray());
    	this.meshFilter.mesh.SetUVs(3, this.lightUVMain.ToArray());

    	this.meshFilter.mesh.SetNormals(this.normals.ToArray());

    	this.meshFilter.mesh.Optimize();
    }

    // Builds meshes from verts, UVs and tris from different layers
    private void BuildMeshSide(Vector3[] verts, Vector2[] UV, Vector2[] lightUV, Vector3[] normals, NativeTris triStruct){
    	this.meshCollider.sharedMesh.Clear();
    	this.meshFilter.mesh.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		this.meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilter.mesh.subMeshCount = 6;

    	this.meshFilter.mesh.vertices = verts;
    	this.meshFilter.mesh.SetTriangles(triStruct.tris.ToArray(), 0);
    	this.meshFilter.mesh.SetTriangles(triStruct.iceTris.ToArray(), 5);

    	// Fix for a stupid Unity Bug
    	if(verts.Length > 0){
	    	this.meshCollider.sharedMesh = null;
	    	this.meshCollider.sharedMesh = this.meshFilter.mesh;
    	}

    	this.meshFilter.mesh.SetTriangles(triStruct.specularTris.ToArray(), 1);
    	this.meshFilter.mesh.SetTriangles(triStruct.liquidTris.ToArray(), 2);
    	this.meshFilter.mesh.SetTriangles(this.assetTris, 3);
    	this.meshFilter.mesh.SetTriangles(triStruct.leavesTris.ToArray(), 4);

    	this.meshFilter.mesh.uv = UV;
    	this.meshFilter.mesh.uv4 = lightUV;
    	this.meshFilter.mesh.SetNormals(normals);
    }

    // Builds the decal mesh
    private void BuildDecalMesh(Vector3[] verts, Vector2[] UV, int[] triangles){
    	this.meshFilterDecal.mesh.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		this.meshFilterDecal.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilterDecal.mesh.vertices = verts;
    	this.meshFilterDecal.mesh.SetTriangles(triangles, 0);
    	this.meshFilterDecal.mesh.uv = UV;
    	this.meshFilterDecal.mesh.RecalculateNormals();
    }

    // Builds the hitbox mesh onto the Hitbox MeshCollider
    private void BuildHitboxMesh(Vector3[] verts, Vector3[] normals, int[] triangles){
    	this.meshRaycast.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		this.meshColliderRaycast.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	// Fix for a stupid Unity Bug
    	if(verts.Length == 0){
    		this.meshColliderRaycast.sharedMesh = null;

    		if(showHitbox)
    			this.hitboxFilter.mesh = null;
    		return;
    	}

    	this.meshRaycast.vertices = verts;
    	this.meshRaycast.SetTriangles(triangles, 0);
    	this.meshRaycast.normals = normals;
    	this.meshColliderRaycast.sharedMesh = this.meshRaycast;

    	if(showHitbox){
    		this.hitboxFilter.mesh = this.meshColliderRaycast.sharedMesh;
    	}
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
	public ChunkPos pos;

	[ReadOnly]
	public NativeArray<ushort> data; // Voxeldata
	[ReadOnly]
	public NativeArray<ushort> state; // VoxelMetadata.state
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<byte> renderMap;

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
	public NativeArray<bool> blockDrawRegardless;


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		ushort neighborState;
		ushort thisState;
		bool isBlock;
		bool isTransparent;
		int ii;
		int3 c;

		for(int x=0; x<Chunk.chunkWidth; x++){
			for(int z=0; z<Chunk.chunkWidth; z++){
				for(int y=renderMap[x*Chunk.chunkWidth+z]; y >= 0; y--){
					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisState = state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					isBlock = thisBlock <= ushort.MaxValue/2;

	    			// Runs OnLoad event
	    			if(load){
	    				// If is a block
		    			if(isBlock){
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
		    		}

		    		// Object
		    		if(!isBlock){
		    			LoadMesh(x, y, z, -1, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal);
		    		}

		    		// Transparency
		    		if(isBlock){
		    			isTransparent = blockTransparent[thisBlock] == 1 || blockDrawRegardless[thisBlock];
		    		}
		    		else{
		    			isTransparent = objectTransparent[ushort.MaxValue - thisBlock] == 1;
		    		}

		    		if(isTransparent){
				    	for(int i=0; i<6; i++){
				    		neighborBlock = GetNeighbor(x, y, z, i);
				    		neighborState = GetNeighborState(x, y, z, i);
				    		c = GetCoords(x, y, z, i);
				    		isBlock = neighborBlock <= ushort.MaxValue/2;
				    		ii = InvertDir(i);

				    		if(neighborBlock == 0)
				    			continue;
				    		
				    		// Chunk Border and floor culling here! ----------	
			    			// If Corner
				    		if(c.x >= Chunk.chunkWidth || c.x < 0 || c.z >= Chunk.chunkWidth || c.z < 0)
				    			break;

			    			if((c.x == 0 || c.x == Chunk.chunkWidth-1) && (c.z == 0 || c.z == Chunk.chunkWidth-1))
			    				continue;

				    		if((c.x == 0 && (ii != 1)) || (c.z == 0 && (ii != 0)))
				    			continue;

				    		if((c.x == Chunk.chunkWidth-1 && (ii != 3)) || (c.z == Chunk.chunkWidth-1 && (ii != 2)))
				    			continue;

				    		if(c.y == 0 && ii == 5)
				    			continue;

				    		if(c.y == Chunk.chunkDepth-1 && ii != 5){
				    			continue;
				    		}

				    		if(!isBlock)
				    			continue;


				    		////////// -----------------------------------

							// Handles Liquid chunks
				    		if(isBlock){
				    			if(blockSeamless[neighborBlock]){
					    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
					    				continue;
					    			}
				    			}

				    		}
				    		else{
				    			if(objectSeamless[ushort.MaxValue-neighborBlock]){
					    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
					    				continue;
					    			}    				
				    			}
				    		}

						    LoadMesh(c.x, c.y, c.z, ii, neighborBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal);
					    } // faces loop
					}
	    		} // y loop
	    	} // z loop
	    } // x loop
    }

    private int InvertDir(int i){
    	if(i == 0)
    		return 2;
    	if(i == 1)
    		return 3;
    	if(i == 2)
    		return 0;
    	if(i == 3)
    		return 1;
    	if(i == 4)
    		return 5;
    	if(i == 5)
    		return 4;
    	return 0;
    }

    // Gets neighbor element
	private ushort GetNeighbor(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		}

		return data[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	private int3 GetCoords(int x, int y, int z, int dir){
		return new int3(x, y, z) + VoxelData.offsets[dir];
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

		if(coord.y >= Chunk.chunkDepth)
			return 15;

		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;

		if(coord.y >= Chunk.chunkDepth)
			return 15;

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

    	return thisSeamless && neighborSeamless && (thisBlock == neighborBlock) && thisState == neighborState;
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
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
    		VertsByState(cacheCubeVert, dir, state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
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
			loadAssetList.Add(new int3(x,y+(Chunk.chunkDepth*pos.y),z));
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

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCorners(auxPos, dir, array, currentLightLevel);

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

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCornersExtra(auxPos, dir, array, currentLightLevel);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, 0);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, 1);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, 2);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, 3);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, 4);
    	// Down
     	else
     		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, 5);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel){
    	// North
    	if(dir == 0)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 4, 3, 5, 0);
    	// East
    	else if(dir == 1)
    		SetCornerExtra(array, pos, currentLightLevel, 2, 4, 0, 5, 1);
    	// South
     	else if(dir == 2)
    		SetCornerExtra(array, pos, currentLightLevel, 3, 4, 1, 5, 2);
    	// West
      	else if(dir == 3)
    		SetCornerExtra(array, pos, currentLightLevel, 0, 4, 2, 5, 3);
      	// Up
    	else if(dir == 4)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, 4);
    	// Down
     	else
    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, 5);
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

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

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

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
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
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
	[ReadOnly]
	public ChunkPos pos;

	// Output
	public NativeList<Vector3> meshVerts;
	public NativeList<Vector2> meshUVs;
	public NativeList<int> meshTris;
	public NativeList<Vector3> meshNormals;
	public NativeList<Vector2> meshLightUV;

	// Hitbox
	public NativeList<Vector3> hitboxVerts;
	public NativeList<Vector3> hitboxNormals;
	public NativeList<int> hitboxTriangles;

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

	// Loaded Hitbox Data
	[ReadOnly]
	public NativeArray<Vector3> loadedHitboxVerts;
	[ReadOnly]
	public NativeArray<Vector3> loadedHitboxNormals;
	[ReadOnly]
	public NativeArray<int> loadedHitboxTriangles;
	[ReadOnly]
	public NativeArray<int> hitboxVertsOffset;
	[ReadOnly]
	public NativeArray<int> hitboxTrisOffset;
	[ReadOnly]
	public NativeArray<Vector3> hitboxScaling;

	public void Execute(){
		int i;
		int currentVertAmount = vCount;
		int hitboxVertAmount = 0;

		for(int j=0; j < coords.Length; j++){
			i = GetIndex(blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z]);

			if(i == -1)
				continue;

			// If has special offset or rotation
			if(needRotation[ushort.MaxValue - blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z]]){
				int code = blockdata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];
				int state = metadata[coords[j].x*Chunk.chunkWidth*Chunk.chunkDepth+coords[j].y*Chunk.chunkWidth+coords[j].z];

				// Normal Vertices
				Vector3 vertPos = new Vector3(coords[j].x, coords[j].y, coords[j].z);
				for(int vertIndex=vertsOffset[i]; vertIndex < vertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3MultOffsetRotate(loadedVerts[vertIndex], scaling[i], vertPos, inplaceOffset[code*256+state], inplaceRotation[code*256+state]);
					meshVerts.Add(resultVert);
					meshNormals.Add(GetNormalRotation(loadedNormals[vertIndex], inplaceRotation[code*256+state]));
					meshLightUV.Add(new Vector2(GetLight(coords[j].x, coords[j].y, coords[j].z), GetLight(coords[j].x, coords[j].y, coords[j].z, isNatural:false)));
				}

				// Hitbox Vertices
				for(int vertIndex=hitboxVertsOffset[i]; vertIndex < hitboxVertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3MultOffsetRotate(loadedHitboxVerts[vertIndex], hitboxScaling[i], vertPos, inplaceOffset[code*256+state], inplaceRotation[code*256+state]);
					hitboxVerts.Add(resultVert);
					hitboxNormals.Add(GetNormalRotation(loadedHitboxNormals[vertIndex], inplaceRotation[code*256+state]));
				}

			}
			// If doesn't have special rotation
			else{
				// Normal Vertices
				Vector3 vertPos = new Vector3(coords[j].x, coords[j].y, coords[j].z);
				for(int vertIndex=vertsOffset[i]; vertIndex < vertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3Mult(loadedVerts[vertIndex], scaling[i], vertPos);
					meshVerts.Add(resultVert);
					meshNormals.Add(loadedNormals[vertIndex]);
					meshLightUV.Add(new Vector2(GetLight(coords[j].x, coords[j].y, coords[j].z), GetLight(coords[j].x, coords[j].y, coords[j].z, isNatural:false)));
				}

				// Hitbox Vertices
				for(int vertIndex=hitboxVertsOffset[i]; vertIndex < hitboxVertsOffset[i+1]; vertIndex++){
					Vector3 resultVert = Vector3Mult(loadedHitboxVerts[vertIndex], hitboxScaling[i], vertPos);
					hitboxVerts.Add(resultVert);
					hitboxNormals.Add(loadedHitboxNormals[vertIndex]);
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

			// Hitbox Triangles
			for(int triIndex=hitboxTrisOffset[i]; triIndex < hitboxTrisOffset[i+1]; triIndex++){
				hitboxTriangles.Add(loadedHitboxTriangles[triIndex] + hitboxVertAmount);
			}	
			hitboxVertAmount += (hitboxVertsOffset[i+1] - hitboxVertsOffset[i]);	
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
	[ReadOnly]
	public NativeArray<bool> blockDrawRegardless;


	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		byte skipDir;
		int3 thisCoord;
		int3 neighborCoord;
		int3 newChunkPos;
		bool isFacingBorder;
		byte chunkDir; // 0 = Z+, 1 = X+, 2 = Z-, 3 = X-

		// X- Side
		if(xM){
			skipDir = 1;
			chunkDir = 3;
				
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((z == 0 || z == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 3))
							continue;

						if((z == 0 && i == 2) || (z == Chunk.chunkWidth-1 && i == 0))
							continue;

						thisBlock = data[y*Chunk.chunkWidth+z];
						thisCoord = new int3(0, y, z);

						if(i == 3){
							neighborBlock = neighbordata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
							neighborCoord = new int3(Chunk.chunkWidth-1, y, z);
							newChunkPos = new int3(-1, 0, Chunk.chunkWidth-1);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1){
							neighborBlock = data[(y+1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(0, y+1, z);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0){
							neighborBlock = data[(y-1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(0, y-1, z);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 0 && z < Chunk.chunkWidth-1){
							neighborBlock = data[y*Chunk.chunkWidth+(z+1)];
							neighborCoord = new int3(0, y, z+1);
							newChunkPos = new int3(0,0,0);
							isFacingBorder = false;
						}
						else if(i == 2 && z > 0){
							neighborBlock = data[y*Chunk.chunkWidth+(z-1)];
							neighborCoord = new int3(0, y, z-1);
							newChunkPos = new int3(0,0,0);
							isFacingBorder = false;
						}
						else{
							continue;
						}

						if(thisBlock == 0 && neighborBlock == 0)
							continue;

						if(CheckSeams(thisBlock, neighborBlock)){
							continue;
						}
						else{
							if(neighborBlock <= ushort.MaxValue/2){
								if(blockSeamless[neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(0, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
					}
				}
			}
			return;
		}
		// X+ Side
		else if(xP){
			skipDir = 3;
			chunkDir = 1;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((z == 0 || z == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 1))
							continue;

						if((z == 0 && i == 2) || (z == Chunk.chunkWidth-1 && i == 0))
							continue;

						thisBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
						thisCoord = new int3(Chunk.chunkWidth-1, y, z);

						if(i == 1){
							neighborBlock = neighbordata[y*Chunk.chunkWidth+z];
							neighborCoord = new int3(0, y, z);
							newChunkPos = new int3(1, 0, 0);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(Chunk.chunkWidth-1, y+1, z);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(Chunk.chunkWidth-1, y-1, z);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 0 && z < Chunk.chunkWidth-1){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(z+1)];
							neighborCoord = new int3(Chunk.chunkWidth-1, y, z+1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 2 && z > 0){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(z-1)];
							neighborCoord = new int3(Chunk.chunkWidth-1, y, z-1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else{
							continue;
						}

						if(thisBlock == 0 && neighborBlock == 0)
							continue;

						if(CheckSeams(thisBlock, neighborBlock)){
							continue;
						}
						else{
							if(neighborBlock <= ushort.MaxValue/2){
								if(blockSeamless[neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(Chunk.chunkWidth-1, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
					}
				}
			}
			return;
		}
		// Z- Side
		else if(zM){
			skipDir = 0;
			chunkDir = 2;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((x == 0 || x == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 2))
							continue;

						if((x == 0 && i == 3) || (x == Chunk.chunkWidth-1 && i == 1))
							continue;

						thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
						thisCoord = new int3(x, y, 0);

						if(i == 2){
							neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x, y, Chunk.chunkWidth-1);
							newChunkPos = new int3(0, -1, Chunk.chunkWidth-1);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth];
							neighborCoord = new int3(x, y+1, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth];
							neighborCoord = new int3(x, y-1, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 1 && x < Chunk.chunkWidth-1){
							neighborBlock = data[(x+1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
							neighborCoord = new int3(x+1, y, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 3 && x > 0){
							neighborBlock = data[(x-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
							neighborCoord = new int3(x-1, y, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else{
							continue;
						}

						if(thisBlock == 0 && neighborBlock == 0)
							continue;

						if(CheckSeams(thisBlock, neighborBlock)){
							continue;
						}
						else{
							if(neighborBlock <= ushort.MaxValue/2){
								if(blockSeamless[neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.z)));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.z)));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(x, y, 0, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
					}
				}
			}
			return;
		}
		// Z+ Side
		else if(zP){
			skipDir = 2;
			chunkDir = 0;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((x == 0 || x == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 0))
							continue;

						if((x == 0 && i == 3) || (x == Chunk.chunkWidth-1 && i == 1))
							continue;

						thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						thisCoord = new int3(x, y, Chunk.chunkWidth-1);

						if(i == 0){
							neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
							neighborCoord = new int3(x, y, 0);
							newChunkPos = new int3(0, 1, 0);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x, y+1, Chunk.chunkWidth-1);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x, y-1, Chunk.chunkWidth-1);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 1 && x < Chunk.chunkWidth-1){
							neighborBlock = data[(x+1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x+1, y, Chunk.chunkWidth-1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 3 && x > 0){
							neighborBlock = data[(x-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x-1, y, Chunk.chunkWidth-1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else{
							continue;
						}

						if(thisBlock == 0 && neighborBlock == 0)
							continue;

						if(CheckSeams(thisBlock, neighborBlock)){
							continue;
						}
						else{
							if(neighborBlock <= ushort.MaxValue/2){
								if(blockSeamless[neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+newChunkPos.z));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+newChunkPos.z));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(x, y, Chunk.chunkWidth-1, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
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
    private bool LoadMesh(int x, int y, int z, int dir, int3 neighborIndex, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, byte chunkDir, int lookahead=0, bool isFacingBorder=true){
    	ShaderIndex renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == ShaderIndex.OPAQUE){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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
    		VertsByState(cacheCubeVert, dir, metadata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir, bool isFacingBorder=true){
    	int maxLightLevel = 15;
    	int currentLightLevel;

    	if(isFacingBorder)
    		currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z);
    	else
    		currentLightLevel = GetNeighborLight(neighborIndex.x, neighborIndex.y, neighborIndex.z);

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

    	CalculateLightCorners(neighborIndex, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp, chunkDir, isFacingBorder);
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir, bool isFacingBorder=true){
    	int maxLightLevel = 15;
    	int currentLightLevel;

    	if(isFacingBorder)
    		currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);
    	else
    		currentLightLevel = GetNeighborLight(neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);

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

    	CalculateLightCornersExtra(neighborIndex, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp, chunkDir, isFacingBorder);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, byte chunkDir, bool isFacingBorder){
    	if(isFacingBorder){
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
	    else{
	    	if(dir == 0)
	    		SetCornerBorder(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0, chunkDir);
	    	// East
	    	else if(dir == 1)
	    		SetCornerBorder(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1, chunkDir);
	    	// South
	     	else if(dir == 2)
	    		SetCornerBorder(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2, chunkDir);
	    	// West
	      	else if(dir == 3)
	    		SetCornerBorder(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3, chunkDir);
	    	else if(dir == 4)
	    		SetCornerBorder(array, pos, currentLightLevel, 1,2,3,0, xm, xp, zm, zp, ym, yp, 4, chunkDir);
	    	else if(dir == 5)
	    		SetCornerBorder(array, pos, currentLightLevel, 1,0,3,2, xm, xp, zm, zp, ym, yp, 5, chunkDir);
	    }
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, byte chunkDir, bool isFacingBorder){
    	if(isFacingBorder){
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
	    else{
	    	if(dir == 0)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0, chunkDir);
	    	// East
	    	else if(dir == 1)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1, chunkDir);
	    	// South
	     	else if(dir == 2)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2, chunkDir);
	    	// West
	      	else if(dir == 3)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3, chunkDir);
	    	else if(dir == 4)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 1,2,3,0, xm, xp, zm, zp, ym, yp, 4, chunkDir);
	    	else if(dir == 5)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 1,0,3,2, xm, xp, zm, zp, ym, yp, 5, chunkDir);
	    }
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

    private int GetVertexLight(int current, int l1, int l2, int l3, int l4, int l5, int l6, int l7, int l8){
    	int val = 0;

    	// Populate outer values
    	val += (Max(current, l1, l2, l5) << 24);
    	val += (Max(current, l2, l3, l6) << 16);
    	val += (Max(current, l3, l4, l7) << 8);
    	val += (Max(current, l4, l1, l8));

    	return val;
    }

    private int ProcessTransient(int facing, bool xm, bool zm, bool xp, bool zp, int currentLight, int l1, int l2, int l3, int l4, int l5, int l6, int l7, int l8){
    	return GetVertexLight(currentLight, l1, l2, l3, l4, l5, l6, l7, l8);
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

    private void SetCornerBorder(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing, byte chunkDir){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
    	else
    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
    	else
    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
    	else
    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:true);
    	else
    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) || CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir) || (!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light5 = currentLightLevel;
    	}
    	else{
    		light5 = currentLightLevel;
    	}

    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) || CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir) || (!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light6 = currentLightLevel;
    	}
    	else{
    		light6 = currentLightLevel;
    	}

    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) || CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir) || (!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light7 = currentLightLevel;
    	}
    	else{
    		light7 = currentLightLevel;
    	}

    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) || CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir) || (!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light8 = currentLightLevel;
    	}
    	else{
    		light8 = currentLightLevel;
    	}  	

		transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
		array[0] = new Vector2(transientValue >> 24, 1);
		array[1] = new Vector2(((transientValue >> 16) & 0x000000FF), 1);
		array[2] = new Vector2(((transientValue >> 8) & 0x000000FF), 1);
		array[3] = new Vector2((transientValue & 0x000000FF), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
	    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
	    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
	    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
	    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:false);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
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

    private void SetCornerBorderExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing, byte chunkDir){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
    	else
    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
    	else
    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
    	else
    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:false);
    	else
    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) || CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir) || (!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light5 = currentLightLevel;
    	}
    	else{
    		light5 = currentLightLevel;
    	}

    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) || CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir) || (!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light6 = currentLightLevel;
    	}
    	else{
    		light6 = currentLightLevel;
    	}

    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) || CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir) || (!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light7 = currentLightLevel;
    	}
    	else{
    		light7 = currentLightLevel;
    	}

    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) || CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir) || (!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light8 = currentLightLevel;
    	}
    	else{
    		light8 = currentLightLevel;
    	}  	

		transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
		array[0] = new Vector2(array[0].x, transientValue >> 24);
		array[1] = new Vector2(array[1].x, ((transientValue >> 16) & 0x000000FF));
		array[2] = new Vector2(array[2].x, ((transientValue >> 8) & 0x000000FF));
		array[3] = new Vector2(array[3].x, (transientValue & 0x000000FF));
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
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
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

		if(neighborCoord.x >= 16)
			neighborCoord.x -= 16;
		if(neighborCoord.x < 0)
			neighborCoord.x += 16;
		if(neighborCoord.z >= 16)
			neighborCoord.z -= 16;
		if(neighborCoord.z < 0)
			neighborCoord.z += 16;

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
	private int GetNeighborLight(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, int dir, bool isNatural=true, byte chunkDir=5, int currentLight=0){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(neighborCoord.x >= 16)
			if(chunkDir == 1)
				neighborCoord.x -= 16;
			else
				return currentLight;

		if(neighborCoord.x < 0)
			if(chunkDir == 3)
				neighborCoord.x += 16;
			else
				return currentLight;

		if(neighborCoord.z >= 16)
			if(chunkDir == 0)
				neighborCoord.z -= 16;
			else
				return currentLight;
		if(neighborCoord.z < 0)
			if(chunkDir == 2)
				neighborCoord.z += 16;
			else
				return currentLight;

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

		if(coord.x >= 16)
			coord.x -= 16;
		if(coord.x < 0)
			coord.x += 16;
		if(coord.z >= 16)
			coord.z -= 16;
		if(coord.z < 0)
			coord.z += 16;

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

[BurstCompile]
public struct BuildDecalJob : IJob{
	[ReadOnly]
	public ChunkPos pos;

	[ReadOnly]
	public NativeArray<ushort> blockdata;
	public NativeList<Vector3> verts;
	public NativeList<Vector2> UV; 
	public NativeList<int> triangles;
	[ReadOnly]
	public NativeArray<ushort> hpdata;
	[ReadOnly]
	public NativeArray<ushort> blockHP;
	[ReadOnly]
	public NativeArray<ushort> objectHP;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<byte> blockTransparent;
	[ReadOnly]
	public NativeArray<byte> objectTransparent;
	public NativeArray<Vector3> cacheCubeVerts;

	public void Execute(){
		ushort block;
		ushort hp;
		int decalCode;
		ushort neighborBlock;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int z=0; z < Chunk.chunkWidth; z++){

			    	for(int i=0; i<6; i++){
			    		block = blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
			    		neighborBlock = GetNeighbor(x, y, z, i);
			    		hp = hpdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
			    		
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

			    		if(block <= ushort.MaxValue/2){
			    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[block])
			    				continue;
			    		}
			    		else{
			    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[block])
			    				continue;			    			
			    		}

			    		if(CheckTransparentOrInvisible(neighborBlock)){
			    			decalCode = GetDecalStage(block, hp);

			    			if(decalCode >= 0)
			    				BuildDecal(x, y, z, i, decalCode);
			    		}

			    	}
				}
			}
		}
	}

	public void BuildDecal(int x, int y, int z, int dir, int decal){
		FaceVertices(cacheCubeVerts, dir, 0.5f, GetDecalPosition(x, y, z, dir));
		verts.AddRange(cacheCubeVerts);
		int vCount = verts.Length;

		FillUV(decal);
		
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +1);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4 +3); 
	}

	public void FillUV(int decal){
		float xSize = 1 / (float)Constants.DECAL_STAGE_SIZE;
		float xMin = (float)decal * xSize;

		UV.Add(new Vector2(xMin, 0));
		UV.Add(new Vector2(xMin, 1));
		UV.Add(new Vector2(xMin + xSize, 1));
		UV.Add(new Vector2(xMin + xSize, 0));
	}

	// Cube Mesh Data get verts
	public void FaceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	public bool CheckTransparentOrInvisible(ushort block){
		if(block <= ushort.MaxValue/2)
			return Boolean(blockTransparent[block]) || blockInvisible[block];
		else
			return objectInvisible[ushort.MaxValue - block] || Boolean(objectTransparent[ushort.MaxValue - block]);
	}

	public int GetDecalStage(ushort block, ushort hp){
		float hpPercentage;

		if(block <= ushort.MaxValue/2)
			hpPercentage = (float)hp / (float)blockHP[block];
		else
			hpPercentage = (float)hp / (float)objectHP[ushort.MaxValue - block];

	    for(int i=0; i < Constants.DECAL_STAGE_SIZE; i++){
			if(hpPercentage <= Constants.DECAL_STAGE_PERCENTAGE[i])
				return (Constants.DECAL_STAGE_SIZE - 1) - i;
		}

		return -1;
	}

	// Gets neighbor element
	private ushort GetNeighbor(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		}

		return blockdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	private bool Boolean(byte b){
		if(b == 0)
			return false;
		return true;
	}

	public Vector3 GetDecalPosition(float x, float y, float z, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, Constants.DECAL_OFFSET);
		else if(dir == 1)
			normal = new Vector3(Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -Constants.DECAL_OFFSET);
		else if(dir == 3)
			normal = new Vector3(-Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, Constants.DECAL_OFFSET, 0);
		else
			normal = new Vector3(0, -Constants.DECAL_OFFSET, 0);

		return new Vector3(x + normal.x, y + normal.y, z + normal.z);
	}
}

[BurstCompile]
public struct BuildDecalSideJob : IJob{
	[ReadOnly]
	public ChunkPos pos;
	[ReadOnly]
	public NativeArray<ushort> blockdata;
	[ReadOnly]
	public NativeArray<ushort> neighbordata;

	public NativeList<Vector3> verts;
	public NativeList<Vector2> UV; 
	public NativeList<int> triangles;
	[ReadOnly]
	public NativeArray<ushort> hpdata;
	[ReadOnly]
	public NativeArray<ushort> blockHP;
	[ReadOnly]
	public NativeArray<ushort> objectHP;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<byte> blockTransparent;
	[ReadOnly]
	public NativeArray<byte> objectTransparent;
	public NativeArray<Vector3> cacheCubeVerts;

	public bool xp, xm, zp, zm;

	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		ushort hp;
		int dir;
		int decalCode;

		if(xm){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){

					thisBlock = blockdata[y*Chunk.chunkWidth+z];
					neighborBlock = neighbordata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
		    		hp = hpdata[y*Chunk.chunkWidth+z];
		    		dir = 3;

		    		if(thisBlock <= ushort.MaxValue/2){
		    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[thisBlock])
		    				continue;
		    		}
		    		else{
		    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[ushort.MaxValue - thisBlock])
		    				continue;			    			
		    		}

		    		if(CheckTransparentOrInvisible(neighborBlock)){
		    			decalCode = GetDecalStage(thisBlock, hp);

		    			if(decalCode >= 0)
		    				BuildDecal(0, y, z, dir, decalCode);
		    		}

				}
			}
			return;
		}
		// X+ Side
		else if(xp){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					thisBlock = blockdata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					neighborBlock = neighbordata[y*Chunk.chunkWidth+z];
		    		hp = hpdata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
		    		dir = 1;

		    		if(thisBlock <= ushort.MaxValue/2){
		    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[thisBlock])
		    				continue;
		    		}
		    		else{
		    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[ushort.MaxValue - thisBlock])
		    				continue;			    			
		    		}

		    		if(CheckTransparentOrInvisible(neighborBlock)){
		    			decalCode = GetDecalStage(thisBlock, hp);

		    			if(decalCode >= 0)
		    				BuildDecal(Chunk.chunkWidth-1, y, z, dir, decalCode);
		    		}

				}
			}
			return;
		}
		// Z- Side
		else if(zm){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					thisBlock = blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
					neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
		    		hp = hpdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
		    		dir = 2;

		    		if(thisBlock <= ushort.MaxValue/2){
		    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[thisBlock])
		    				continue;
		    		}
		    		else{
		    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[ushort.MaxValue - thisBlock])
		    				continue;			    			
		    		}

		    		if(CheckTransparentOrInvisible(neighborBlock)){
		    			decalCode = GetDecalStage(thisBlock, hp);

		    			if(decalCode >= 0)
		    				BuildDecal(x, y, 0, dir, decalCode);
		    		}

				}
			}
			return;
		}
		// Z+ Side
		else if(zp){
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					thisBlock = blockdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
					neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
		    		hp = hpdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
		    		dir = 0;

		    		if(thisBlock <= ushort.MaxValue/2){
		    			if(hp == 0 || hp == ushort.MaxValue || hp == blockHP[thisBlock])
		    				continue;
		    		}
		    		else{
		    			if(hp == 0 || hp == ushort.MaxValue || hp == objectHP[ushort.MaxValue - thisBlock])
		    				continue;			    			
		    		}

		    		if(CheckTransparentOrInvisible(neighborBlock)){
		    			decalCode = GetDecalStage(thisBlock, hp);

		    			if(decalCode >= 0)
		    				BuildDecal(x, y, Chunk.chunkWidth-1, dir, decalCode);
		    		}

				}
			}
		}
	}

	public void BuildDecal(int x, int y, int z, int dir, int decal){
		FaceVertices(cacheCubeVerts, dir, 0.5f, GetDecalPosition(x, y, z, dir));
		verts.AddRange(cacheCubeVerts);
		int vCount = verts.Length;

		FillUV(decal);
		
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +1);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4 +3); 
	}

	public void FillUV(int decal){
		float xSize = 1 / (float)Constants.DECAL_STAGE_SIZE;
		float xMin = (float)decal * xSize;

		UV.Add(new Vector2(xMin, 0));
		UV.Add(new Vector2(xMin, 1));
		UV.Add(new Vector2(xMin + xSize, 1));
		UV.Add(new Vector2(xMin + xSize, 0));
	}

	// Cube Mesh Data get verts
	public void FaceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	public bool CheckTransparentOrInvisible(ushort block){
		if(block <= ushort.MaxValue/2)
			return Boolean(blockTransparent[block]) || blockInvisible[block];
		else
			return objectInvisible[ushort.MaxValue - block] || Boolean(objectTransparent[ushort.MaxValue - block]);
	}

	public int GetDecalStage(ushort block, ushort hp){
		float hpPercentage;

		if(block <= ushort.MaxValue/2)
			hpPercentage = (float)hp / (float)blockHP[block];
		else
			hpPercentage = (float)hp / (float)objectHP[ushort.MaxValue - block];
		


	    for(int i=0; i < Constants.DECAL_STAGE_SIZE; i++){
			if(hpPercentage <= Constants.DECAL_STAGE_PERCENTAGE[i])
				return (Constants.DECAL_STAGE_SIZE - 1) - i;
		}

		return -1;
	}

	// Gets neighbor element
	private ushort GetNeighbor(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		}

		return blockdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	private bool Boolean(byte b){
		if(b == 0)
			return false;
		return true;
	}

	public Vector3 GetDecalPosition(int x, int y, int z, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, Constants.DECAL_OFFSET);
		else if(dir == 1)
			normal = new Vector3(Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -Constants.DECAL_OFFSET);
		else if(dir == 3)
			normal = new Vector3(-Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, Constants.DECAL_OFFSET, 0);
		else
			normal = new Vector3(0, -Constants.DECAL_OFFSET, 0);

		return new Vector3(x + normal.x, y + normal.y, z + normal.z);
	}
}


[BurstCompile]
public struct BuildCornerJob : IJob{
	[ReadOnly]
	public bool reload;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<ushort> metadata;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<ushort> xsidedata;
	[ReadOnly]
	public NativeArray<byte> xsidelight;
	[ReadOnly]
	public NativeArray<ushort> zsidedata;
	[ReadOnly]
	public NativeArray<byte> zsidelight;
	[ReadOnly]
	public NativeArray<ushort> cornerdata;
	[ReadOnly]
	public NativeArray<byte> cornerlight;
	[ReadOnly]
	public bool xmzm, xmzp, xpzm, xpzp;
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
	[ReadOnly]
	public NativeArray<bool> blockDrawRegardless;


	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		byte chunkDir;
		int3 thisCoord;
		int3 neighborCoord;
		int4 newChunkPos; // 1st and 2nd are ChunkPos offset and 3rd and 4th are in-Chunk coords

		// XMZM
		if(xmzm){
			chunkDir = 5;
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int i=0; i < 6; i++){
					int x = 0;
					int z = 0;

					if(i == 0 || i == 1)
						continue;

					thisBlock = data[y*Chunk.chunkWidth];
					thisCoord = new int3(0, y, 0);

					if(i == 3){
						neighborBlock = xsidedata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
						neighborCoord = new int3(Chunk.chunkWidth-1, y, z);
						newChunkPos = new int4(-1, 0, Chunk.chunkWidth-1, z);
					}
					else if(i == 2){
						neighborBlock = zsidedata[y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, -1, x, Chunk.chunkWidth-1);
					}
					else if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[(y+1)*Chunk.chunkWidth];
						neighborCoord = new int3(0, y+1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[(y-1)*Chunk.chunkWidth];
						neighborCoord = new int3(0, y-1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
		// XPZM
		else if(xpzm){
			chunkDir = 4;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int i=0; i < 6; i++){
					int x = Chunk.chunkWidth-1;
					int z = 0;

					if(i == 0 || i == 3)
						continue;

					thisBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisCoord = new int3(Chunk.chunkWidth-1, y, z);

					if(i == 1){
						neighborBlock = xsidedata[y*Chunk.chunkWidth+z];
						neighborCoord = new int3(0, y, z);
						newChunkPos = new int4(1, 0, 0, z);
					}
					else if(i == 2){
						neighborBlock = zsidedata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, -1, x, Chunk.chunkWidth-1);
					}
					else if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y+1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y-1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
		// XMZP Side
		else if(xmzp){
			chunkDir = 6;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int i=0; i < 6; i++){
					int x = 0;
					int z = Chunk.chunkWidth-1;

					if(i == 1 || i == 2)
						continue;

					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisCoord = new int3(x, y, z);

					if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y+1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y-1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 3){
						neighborBlock = xsidedata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
						neighborCoord = new int3(Chunk.chunkWidth-1, y, z);
						newChunkPos = new int4(-1, 0, Chunk.chunkWidth-1, z);
					}
					else if(i == 0){
						neighborBlock = zsidedata[y*Chunk.chunkWidth];
						neighborCoord = new int3(0, y, 0);
						newChunkPos = new int4(0, 1, 0, 0);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
		// XPZP Side
		else if(xpzp){
			chunkDir = 7;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int i=0; i < 6; i++){
					int x = Chunk.chunkWidth-1;
					int z = Chunk.chunkWidth-1;

					if(i == 2 || i == 3)
						continue;

					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisCoord = new int3(x, y, z);

					if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y+1, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y-1, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 1){
						neighborBlock = xsidedata[y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(0, y, Chunk.chunkWidth-1);
						newChunkPos = new int4(1,0,0,z);
					}
					else if(i == 0){
						neighborBlock = zsidedata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
						neighborCoord = new int3(x, y, 0);
						newChunkPos = new int4(0,1,x,0);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
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
    private bool LoadMesh(int x, int y, int z, int dir, int3 neighborIndex, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, byte chunkDir, int lookahead=0){
    	ShaderIndex renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == ShaderIndex.OPAQUE){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    		VertsByState(cacheCubeVert, dir, metadata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir){
    	int maxLightLevel = 15;
    	int currentLightLevel;

		currentLightLevel = GetLightBasedOnDir(dir, neighborIndex.x, neighborIndex.y, neighborIndex.z);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	SetCorner(array, neighborIndex, currentLightLevel, chunkDir, dir);
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir){
    	int maxLightLevel = 15;
    	int currentLightLevel;

		currentLightLevel = GetLightBasedOnDir(dir, neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	SetCornerExtra(array, neighborIndex, currentLightLevel, chunkDir, dir);
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, byte chunkDir, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;

    	// XPZM Corner
    	if(chunkDir == 4){
    		if(facing == 4){
	    		light1 = GetLightOnX(0, pos.y, pos.z);
	    		light2 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
	    		light4 = GetLightOnCurrent(pos.x, pos.y, pos.z+1);
	    		light5 = GetLightOnCorner(chunkDir, pos.y);
	    		light6 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1);
	    		light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1);
	    		light8 = GetLightOnX(0, pos.y, pos.z+1);
	    	}
	    	else if(facing == 5){
	    		light1 = GetLightOnX(0, pos.y, pos.z);
	    		light2 = GetLightOnCurrent(pos.x, pos.y, pos.z+1);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
	    		light4 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1);
	    		light5 = GetLightOnX(0, pos.y, pos.z+1);
	    		light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1);
	    		light7 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1);
	    		light8 = GetLightOnCorner(chunkDir, pos.y);
	    	}
	    	else if(facing == 1){
	    		light1 = GetLightOnCorner(chunkDir, pos.y);
	    		light2 = GetLightOnX(0, pos.y+1, pos.z);
	    		light3 = GetLightOnX(0, pos.y, pos.z+1);
	    		light4 = GetLightOnX(0, pos.y-1, pos.z);
	    		light5 = GetLightOnCorner(chunkDir, pos.y+1);
	    		light6 = GetLightOnX(0, pos.y+1, pos.z+1);
	    		light7 = GetLightOnX(0, pos.y-1, pos.z+1);
	    		light8 = GetLightOnCorner(chunkDir, pos.y-1);
	    	}
	    	else{ // if(facing == 2)
	    		light1 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1);
	    		light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1);
	    		light3 = GetLightOnCorner(chunkDir, pos.y);
	    		light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1);
	    		light5 = GetLightOnZ(pos.x-1, pos.y+1, Chunk.chunkWidth-1);
	    		light6 = GetLightOnCorner(chunkDir, pos.y+1);
	    		light7 = GetLightOnCorner(chunkDir, pos.y-1);
	    		light8 = GetLightOnZ(pos.x-1, pos.y-1, Chunk.chunkWidth-1);
	    	}
    	}
    	// XMZM Corner
    	else if(chunkDir == 5){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z);
    			light2 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnCurrent(0, pos.y, pos.z+1);
    			light5 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1);
    			light6 = GetLightOnCorner(chunkDir, pos.y);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1);
    			light8 = GetLightOnCurrent(1, pos.y, 1);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z);
    			light2 = GetLightOnCurrent(0, pos.y, pos.z+1);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1);
    			light5 = GetLightOnCurrent(1, pos.y, 1);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y);
    			light8 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1);
    		}
    		else if(facing == 2){
    			light1 = GetLightOnCorner(chunkDir, pos.y);
    			light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1);
    			light3 = GetLightOnZ(pos.x+1, pos.y, Chunk.chunkWidth-1);
    			light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1);
    			light6 = GetLightOnZ(pos.x+1, pos.y+1, Chunk.chunkWidth-1);
    			light7 = GetLightOnZ(pos.x+1, pos.y-1, Chunk.chunkWidth-1);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z);
    			light3 = GetLightOnCorner(chunkDir, pos.y);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z);
    			light5 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z+1);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1);
    			light8 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z+1);
    		}
    	}
    	// XMZP Corner
    	else if(chunkDir == 6){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnZ(pos.x, pos.y, 0);
    			light5 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1);
    			light7 = GetLightOnCorner(chunkDir, pos.y);
    			light8 = GetLightOnZ(pos.x+1, pos.y, 0);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z);
    			light2 = GetLightOnZ(pos.x, pos.y, 0);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light5 = GetLightOnZ(pos.x+1, pos.y, 0);
    			light6 = GetLightOnCorner(chunkDir, pos.y);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1);
    			light8 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnZ(pos.x+1, pos.y, 0);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0);
    			light3 = GetLightOnCorner(chunkDir, pos.y);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0);
    			light5 = GetLightOnZ(pos.x+1, pos.y+1, 0);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1);
    			light8 = GetLightOnZ(pos.x+1, pos.y-1, 0);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnCorner(chunkDir, pos.y);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z-1);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z-1);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1);
    		}
    	}
    	// XPZP Corner
    	else{
    		if(facing == 4){
    			light1 = GetLightOnX(0, pos.y, pos.z);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
    			light4 = GetLightOnZ(pos.x, pos.y, 0);
    			light5 = GetLightOnX(0, pos.y, pos.z-1);
    			light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1);
    			light7 = GetLightOnZ(pos.x-1, pos.y, 0);
    			light8 = GetLightOnCorner(chunkDir, pos.y);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnX(0, pos.y, pos.z);
    			light2 = GetLightOnZ(pos.x, pos.y, 0);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light5 = GetLightOnCorner(chunkDir, pos.y);
    			light6 = GetLightOnZ(pos.x-1, pos.y, 0);
    			light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1);
    			light8 = GetLightOnX(0, pos.y, pos.z-1);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnCorner(chunkDir, pos.y);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0);
    			light3 = GetLightOnZ(pos.x-1, pos.y, 0);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1);
    			light6 = GetLightOnZ(pos.x-1, pos.y+1, 0);
    			light7 = GetLightOnZ(pos.x-1, pos.y-1, 0);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1);
    		}
    		else{ // if(facing == 1)
    			light1 = GetLightOnX(0, pos.y, pos.z-1);
    			light2 = GetLightOnX(0, pos.y+1, pos.z);
    			light3 = GetLightOnCorner(chunkDir, pos.y);
    			light4 = GetLightOnX(0, pos.y-1, pos.z);
    			light5 = GetLightOnX(0, pos.y+1, pos.z-1);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1);
    			light8 = GetLightOnX(0, pos.y-1, pos.z-1);
    		}
    	}


		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, byte chunkDir, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;

    	// XPZM Corner
    	if(chunkDir == 4){
    		if(facing == 4){
	    		light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
	    		light2 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
	    		light4 = GetLightOnCurrent(pos.x, pos.y, pos.z+1, isNatural:false);
	    		light5 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    		light6 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1, isNatural:false);
	    		light8 = GetLightOnX(0, pos.y, pos.z+1, isNatural:false);
	    	}
	    	else if(facing == 5){
	    		light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
	    		light2 = GetLightOnCurrent(pos.x, pos.y, pos.z+1, isNatural:false);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
	    		light4 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light5 = GetLightOnX(0, pos.y, pos.z+1, isNatural:false);
	    		light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1, isNatural:false);
	    		light7 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light8 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    	}
	    	else if(facing == 1){
	    		light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    		light2 = GetLightOnX(0, pos.y+1, pos.z, isNatural:false);
	    		light3 = GetLightOnX(0, pos.y, pos.z+1, isNatural:false);
	    		light4 = GetLightOnX(0, pos.y-1, pos.z, isNatural:false);
	    		light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
	    		light6 = GetLightOnX(0, pos.y+1, pos.z+1, isNatural:false);
	    		light7 = GetLightOnX(0, pos.y-1, pos.z+1, isNatural:false);
	    		light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
	    	}
	    	else{ // if(facing == 2)
	    		light1 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
	    		light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    		light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
	    		light5 = GetLightOnZ(pos.x-1, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
	    		light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
	    		light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
	    		light8 = GetLightOnZ(pos.x-1, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
	    	}
    	}
    	// XMZM Corner
    	else if(chunkDir == 5){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnCurrent(0, pos.y, pos.z+1, isNatural:false);
    			light5 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1, isNatural:false);
    			light8 = GetLightOnCurrent(1, pos.y, 1, isNatural:false);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnCurrent(0, pos.y, pos.z+1, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light5 = GetLightOnCurrent(1, pos.y, 1, isNatural:false);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light8 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1, isNatural:false);
    		}
    		else if(facing == 2){
    			light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
    			light3 = GetLightOnZ(pos.x+1, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light6 = GetLightOnZ(pos.x+1, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
    			light7 = GetLightOnZ(pos.x+1, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1, isNatural:false);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z, isNatural:false);
    			light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z, isNatural:false);
    			light5 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z+1, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    			light8 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z+1, isNatural:false);
    		}
    	}
    	// XMZP Corner
    	else if(chunkDir == 6){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light5 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1, isNatural:false);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light8 = GetLightOnZ(pos.x+1, pos.y, 0, isNatural:false);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light5 = GetLightOnZ(pos.x+1, pos.y, 0, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1, isNatural:false);
    			light8 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1, isNatural:false);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnZ(pos.x+1, pos.y, 0, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0, isNatural:false);
    			light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0, isNatural:false);
    			light5 = GetLightOnZ(pos.x+1, pos.y+1, 0, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    			light8 = GetLightOnZ(pos.x+1, pos.y-1, 0, isNatural:false);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1, isNatural:false);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z-1, isNatural:false);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z-1, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    		}
    	}
    	// XPZP Corner
    	else{
    		if(facing == 4){
    			light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light5 = GetLightOnX(0, pos.y, pos.z-1, isNatural:false);
    			light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1, isNatural:false);
    			light7 = GetLightOnZ(pos.x-1, pos.y, 0, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light6 = GetLightOnZ(pos.x-1, pos.y, 0, isNatural:false);
    			light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1, isNatural:false);
    			light8 = GetLightOnX(0, pos.y, pos.z-1, isNatural:false);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0, isNatural:false);
    			light3 = GetLightOnZ(pos.x-1, pos.y, 0, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light6 = GetLightOnZ(pos.x-1, pos.y+1, 0, isNatural:false);
    			light7 = GetLightOnZ(pos.x-1, pos.y-1, 0, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    		}
    		else{ // if(facing == 1)
    			light1 = GetLightOnX(0, pos.y, pos.z-1, isNatural:false);
    			light2 = GetLightOnX(0, pos.y+1, pos.z, isNatural:false);
    			light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light4 = GetLightOnX(0, pos.y-1, pos.z, isNatural:false);
    			light5 = GetLightOnX(0, pos.y+1, pos.z-1, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    			light8 = GetLightOnX(0, pos.y-1, pos.z-1, isNatural:false);
    		}
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
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
	}

	// Gets the light of maybe neighbors by looking into the dir used to get current XYZ
	private int GetLightBasedOnDir(int dir, int x, int y, int z, bool isNatural=true){
		if(isNatural){
			if(dir == 4 || dir == 5)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else if(dir == 0 || dir == 2)
				return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else if(dir == 1 || dir == 3)
				return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		}
		else{
			if(dir == 4 || dir == 5)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
			else if(dir == 0 || dir == 2)
				return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
			else if(dir == 1 || dir == 3)
				return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;			
		}

		return 0;
	}

	private int GetLightOnCurrent(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetLightOnX(int x, int y, int z, bool isNatural=true){
		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 0;

		if(isNatural)
			return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetLightOnZ(int x, int y, int z, bool isNatural=true){
		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 0;

		if(isNatural)
			return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetLightOnCorner(byte chunkDir, int y, bool isNatural=true){
		if(y < 0)
			return 0;

		if(isNatural){
			if(chunkDir == 4)
				return cornerlight[(y)*Chunk.chunkWidth+(Chunk.chunkWidth-1)] & 0x0F;
			else if(chunkDir == 5)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y)*Chunk.chunkWidth+(Chunk.chunkWidth-1)] & 0x0F;
			else if(chunkDir == 6)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y)*Chunk.chunkWidth] & 0x0F;
			else if(chunkDir == 7)
				return cornerlight[(y)*Chunk.chunkWidth] & 0x0F;
		}
		else{
			if(chunkDir == 4)
				return cornerlight[y*Chunk.chunkWidth+(Chunk.chunkWidth-1)] >> 4;
			else if(chunkDir == 5)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)] >> 4;
			else if(chunkDir == 6)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth] >> 4;
			else if(chunkDir == 7)
				return cornerlight[y*Chunk.chunkWidth] >> 4;
		}

		return 0;
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



public struct NativeTris {
	public NativeArray<int> tris;
	public NativeArray<int> specularTris;
	public NativeArray<int> liquidTris; 
	public NativeArray<int> leavesTris; 
	public NativeArray<int> iceTris; 

	public NativeTris(NativeArray<int> t, NativeArray<int> spec, NativeArray<int> liquid, NativeArray<int> leav, NativeArray<int> ice){
		this.tris = t;
		this.specularTris = spec;
		this.liquidTris = liquid;
		this.leavesTris = leav;
		this.iceTris = ice;
	}

	public void Dispose(){
		this.tris.Dispose();
		this.specularTris.Dispose();
		this.liquidTris.Dispose();
		this.leavesTris.Dispose();
		this.iceTris.Dispose();
	}
}