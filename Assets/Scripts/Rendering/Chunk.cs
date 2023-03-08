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

	public bool topDraw = false;
	public bool bottomDraw = false;

	public bool txpDraw = false;
	public bool txmDraw = false;
	public bool tzpDraw = false;
	public bool tzmDraw = false;
	public bool txpzmDraw = false;
	public bool txmzmDraw = false;
	public bool txmzpDraw = false;
	public bool txpzpDraw = false;
	public bool bxpDraw = false;
	public bool bxmDraw = false;
	public bool bzpDraw = false;
	public bool bzmDraw = false;
	public bool bxpzmDraw = false;
	public bool bxmzmDraw = false;
	public bool bxpzpDraw = false;
	public bool bxmzpDraw = false;

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
    private int[] lavaTris;
  	private List<Vector2> UVs = new List<Vector2>();
  	private List<Vector2> lightUVMain = new List<Vector2>();
  	private List<Vector3> normals = new List<Vector3>();
    private List<Vector4> tangents = new List<Vector4>();

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
    private List<Vector4> cacheTangents = new List<Vector4>();
    private List<int> indexVert = new List<int>();
    private List<int> indexUV = new List<int>();
    private List<int> indexTris = new List<int>();
    private List<Vector3> scalingFactor = new List<Vector3>();
    private List<Vector2> UVaux = new List<Vector2>();
    private List<Vector3> vertexAux = new List<Vector3>();
    private List<Vector3> normalAux = new List<Vector3>();
    private List<Vector4> tangentAux = new List<Vector4>();
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
    private ChunkPos[] surroundingTopChunks = new ChunkPos[8];
    private ChunkPos[] surroundingBotChunks = new ChunkPos[8];
    private ChunkPos[] surroundingVerticalChunks = new ChunkPos[2];


	public Chunk(ChunkPos pos, ChunkRenderer r, BlockEncyclopedia be, ChunkLoader loader){
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

		this.surroundingTopChunks[0] = new ChunkPos(pos.x, pos.z+1, pos.y+1);
		this.surroundingTopChunks[1] = new ChunkPos(pos.x+1, pos.z, pos.y+1);
		this.surroundingTopChunks[2] = new ChunkPos(pos.x, pos.z-1, pos.y+1);
		this.surroundingTopChunks[3] = new ChunkPos(pos.x-1, pos.z, pos.y+1);
		this.surroundingTopChunks[4] = new ChunkPos(pos.x+1, pos.z-1, pos.y+1);
		this.surroundingTopChunks[5] = new ChunkPos(pos.x-1, pos.z-1, pos.y+1);
		this.surroundingTopChunks[6] = new ChunkPos(pos.x-1, pos.z+1, pos.y+1);
		this.surroundingTopChunks[7] = new ChunkPos(pos.x+1, pos.z+1, pos.y+1);

		this.surroundingBotChunks[0] = new ChunkPos(pos.x, pos.z+1, pos.y-1);
		this.surroundingBotChunks[1] = new ChunkPos(pos.x+1, pos.z, pos.y-1);
		this.surroundingBotChunks[2] = new ChunkPos(pos.x, pos.z-1, pos.y-1);
		this.surroundingBotChunks[3] = new ChunkPos(pos.x-1, pos.z, pos.y-1);
		this.surroundingBotChunks[4] = new ChunkPos(pos.x+1, pos.z-1, pos.y-1);
		this.surroundingBotChunks[5] = new ChunkPos(pos.x-1, pos.z-1, pos.y-1);
		this.surroundingBotChunks[6] = new ChunkPos(pos.x-1, pos.z+1, pos.y-1);
		this.surroundingBotChunks[7] = new ChunkPos(pos.x+1, pos.z+1, pos.y-1);

		this.surroundingVerticalChunks[0] = new ChunkPos(pos.x, pos.z, pos.y+1);
		this.surroundingVerticalChunks[1] = new ChunkPos(pos.x, pos.z, pos.y-1);

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
			this.data = new VoxelData(new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth], pos);
		else
			this.data = new VoxelData();

		this.metadata = new VoxelMetadata();
	}

	// Clone
	public Chunk Clone(){
		Chunk c = new Chunk(this.pos, this.renderer, this.blockBook, this.loader);

		c.biomeName = this.biomeName;
		c.data = new VoxelData(this.data.GetData(), this.pos);
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

        if(!topDraw && loader.chunks.ContainsKey(this.surroundingVerticalChunks[0]))
        	return true;
        if(!bottomDraw && loader.chunks.ContainsKey(this.surroundingVerticalChunks[1]))
        	return true;

        if(!tzpDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[0]))
        	return true;
        if(!txpDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[1]))
        	return true;
        if(!tzmDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[2]))
        	return true;
        if(!txmDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[3]))
        	return true;

        if(!txpzmDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[1]) && loader.chunks.ContainsKey(this.surroundingTopChunks[2]) && loader.chunks.ContainsKey(this.surroundingTopChunks[4]))
        	return true;
        if(!txmzmDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[2]) && loader.chunks.ContainsKey(this.surroundingTopChunks[3]) && loader.chunks.ContainsKey(this.surroundingTopChunks[5]))
        	return true;
        if(!txmzpDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[3]) && loader.chunks.ContainsKey(this.surroundingTopChunks[0]) && loader.chunks.ContainsKey(this.surroundingTopChunks[6]))
        	return true;
        if(!txpzpDraw && loader.chunks.ContainsKey(this.surroundingTopChunks[0]) && loader.chunks.ContainsKey(this.surroundingTopChunks[1]) && loader.chunks.ContainsKey(this.surroundingTopChunks[7]))
        	return true;

        if(!bzpDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[0]))
        	return true;
        if(!bxpDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[1]))
        	return true;
        if(!bzmDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[2]))
        	return true;
        if(!bxmDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[3]))
        	return true;
        	
        if(!bxpzmDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[1]) && loader.chunks.ContainsKey(this.surroundingBotChunks[2]) && loader.chunks.ContainsKey(this.surroundingBotChunks[4]))
        	return true;
        if(!bxmzmDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[2]) && loader.chunks.ContainsKey(this.surroundingBotChunks[3]) && loader.chunks.ContainsKey(this.surroundingBotChunks[5]))
        	return true;
        if(!bxmzpDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[3]) && loader.chunks.ContainsKey(this.surroundingBotChunks[0]) && loader.chunks.ContainsKey(this.surroundingBotChunks[6]))
        	return true;
        if(!bxpzpDraw && loader.chunks.ContainsKey(this.surroundingBotChunks[0]) && loader.chunks.ContainsKey(this.surroundingBotChunks[1]) && loader.chunks.ContainsKey(this.surroundingBotChunks[7]))
        	return true;


        return false;
    }

	// Draws Chunk Borders
	public void BuildSideBorder(bool reload=false, bool loadBUD=false){
		bool changed = false; // Flag is set if any change has been made that requires a redraw

		if(reload){
			xmDraw = false;
			zmDraw = false;
			xpDraw = false;
			zpDraw = false;
			xmzm = false;
			xmzp = false;
			xpzm = false;
			xpzp = false;

			topDraw = false;
			bottomDraw = false;
			
			txpDraw = false;
			txmDraw = false;
			tzmDraw = false;
			tzpDraw = false;
			txmzmDraw = false;
			txpzmDraw = false;
			txmzpDraw = false;
			txpzpDraw = false;

			bxpDraw = false;
			bxmDraw = false;
			bzmDraw = false;
			bzpDraw = false;
			bxmzmDraw = false;
			bxpzmDraw = false;
			bxmzpDraw = false;
			bxpzpDraw = false;
		}

		if(!CheckRedraw())
			return;

		int3[] coordArray;

		NativeArray<ushort> blockdata = NativeTools.CopyToNative(this.data.GetData());
		NativeArray<ushort> hpdata = NativeTools.CopyToNative(this.metadata.GetHPData());
		NativeArray<ushort> metadata = NativeTools.CopyToNative(this.metadata.GetStateData());
		NativeArray<byte> lightdata = NativeTools.CopyToNative(this.data.GetLightMap(this.metadata));
		NativeArray<byte> renderMap = NativeTools.CopyToNative<byte>(this.data.GetRenderMap());

		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> uvs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> lightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector4> tangents = new NativeList<Vector4>(0, Allocator.TempJob);
		NativeList<int> tris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> specularTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> liquidTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> leavesTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> iceTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> lavaTris = new NativeList<int>(0, Allocator.TempJob);
	
		NativeList<int3> toLoadEvent = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<int3> toBUD = new NativeList<int3>(0, Allocator.TempJob);

		// Cached
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheUVVerts = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector4> cacheCubeTangent = new NativeArray<Vector4>(4, Allocator.TempJob);

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
		NativeArray<Vector4> disposableTangents = NativeTools.CopyToNative<Vector4>(this.tangents.ToArray());

		// Decals
		NativeArray<Vector3> disposableVertsDecal = NativeTools.CopyToNative<Vector3>(this.decalVertices.ToArray());
		NativeArray<Vector2> disposableUVSDecal = NativeTools.CopyToNative<Vector2>(this.decalUV.ToArray());


		NativeArray<int> disposableTris = new NativeArray<int>(this.triangles, Allocator.TempJob);
		NativeArray<int> disposableSpecTris = new NativeArray<int>(this.specularTris, Allocator.TempJob);
		NativeArray<int> disposableLiquidTris = new NativeArray<int>(this.liquidTris, Allocator.TempJob);
		NativeArray<int> disposableLeavesTris = new NativeArray<int>(this.leavesTris, Allocator.TempJob);
		NativeArray<int> disposableIceTris = new NativeArray<int>(this.iceTris, Allocator.TempJob);
		NativeArray<int> disposableLavaTris = new NativeArray<int>(this.lavaTris, Allocator.TempJob);
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
		lavaTris.AddRange(disposableLavaTris);
		leavesTris.AddRange(disposableLeavesTris);
		iceTris.AddRange(disposableIceTris);
		normals.AddRange(disposableNormals);
		tangents.AddRange(disposableTangents);
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
		disposableLavaTris.Dispose();
		disposableNormals.Dispose();
		disposableTangents.Dispose();
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,

				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

		// Top Side
		if(loader.chunks.ContainsKey(this.surroundingVerticalChunks[0]) && !topDraw){
			topDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[0]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[0]].metadata.GetStateData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[0]].metadata));

			BuildVerticalChunkJob bvcj = new BuildVerticalChunkJob {
				pos = this.pos,
				isBottom = false,
				isTop = true,
				data = blockdata,
				state = metadata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				neighborlight = neighborlight,
				lightdata = lightdata,
				renderMap = renderMap,
				verts = verts,
				UVs = uvs,
				lightUV = lightUV,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcj.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			neighborlight.Dispose();
		}

		// Bottom Side
		if(loader.chunks.ContainsKey(this.surroundingVerticalChunks[1]) && !bottomDraw){
			bottomDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[1]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[1]].metadata.GetStateData());
			NativeArray<byte> neighborlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[1]].metadata));

			BuildVerticalChunkJob bvcj = new BuildVerticalChunkJob {
				pos = this.pos,
				isBottom = true,
				isTop = false,
				data = blockdata,
				state = metadata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				neighborlight = neighborlight,
				lightdata = lightdata,
				renderMap = renderMap,
				verts = verts,
				UVs = uvs,
				lightUV = lightUV,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcj.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			neighborlight.Dispose();
		}

		// TopBot Sides
		
		// XM Top
		if(loader.chunks.ContainsKey(this.surroundingTopChunks[3]) && loader.chunks.ContainsKey(surroundingVerticalChunks[0]) && !txmDraw){
			txmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[0]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[0]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[3]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[3]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				dsidelight = dsidelight,
				ysidelight = ysidelight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xm = true,
				xp = false,
				zp = false,
				zm = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// XP Top
		if(loader.chunks.ContainsKey(this.surroundingTopChunks[1]) && loader.chunks.ContainsKey(surroundingVerticalChunks[0]) && !txpDraw){
			txpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[0]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[0]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[1]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[1]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xm = false,
				xp = true,
				zp = false,
				zm = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// ZM Top
		if(loader.chunks.ContainsKey(this.surroundingTopChunks[2]) && loader.chunks.ContainsKey(surroundingVerticalChunks[0]) && !tzmDraw){
			tzmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[0]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[0]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[2]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[2]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xm = false,
				xp = false,
				zp = false,
				zm = true,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// ZP Top
		if(loader.chunks.ContainsKey(this.surroundingTopChunks[0]) && loader.chunks.ContainsKey(surroundingVerticalChunks[0]) && !tzpDraw){
			tzpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[0]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[0]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[0]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[0]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xm = false,
				xp = false,
				zp = true,
				zm = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// XM Bot
		if(loader.chunks.ContainsKey(this.surroundingBotChunks[3]) && loader.chunks.ContainsKey(surroundingVerticalChunks[1]) && !bxmDraw){
			bxmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[1]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[1]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[3]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[3]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xm = true,
				xp = false,
				zp = false,
				zm = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// XP Bot
		if(loader.chunks.ContainsKey(this.surroundingBotChunks[1]) && loader.chunks.ContainsKey(surroundingVerticalChunks[1]) && !bxpDraw){
			bxpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[1]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[1]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[1]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[1]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xm = false,
				xp = true,
				zp = false,
				zm = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// ZM Bot
		if(loader.chunks.ContainsKey(this.surroundingBotChunks[2]) && loader.chunks.ContainsKey(surroundingVerticalChunks[1]) && !bzmDraw){
			bzmDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[1]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[1]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[2]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[2]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xm = false,
				xp = false,
				zp = false,
				zm = true,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
		}

		// ZP Bot
		if(loader.chunks.ContainsKey(this.surroundingBotChunks[0]) && loader.chunks.ContainsKey(surroundingVerticalChunks[1]) && !bzpDraw){
			bzpDraw = true;
			changed = true;

			NativeArray<ushort> neighbordata = NativeTools.CopyToNative<ushort>(loader.chunks[surroundingVerticalChunks[1]].data.GetData());
			NativeArray<ushort> neighborstate = NativeTools.CopyToNative<ushort>(loader.chunks[this.surroundingVerticalChunks[1]].metadata.GetStateData());
			NativeArray<byte> ysidelight = NativeTools.CopyToNative<byte>(loader.chunks[surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> dsidelight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[0]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[0]].metadata));

			BuildVerticalSideJob bvsJob = new BuildVerticalSideJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				lightdata = lightdata,
				neighbordata = neighbordata,
				neighborStates = neighborstate,
				ysidelight = ysidelight,
				dsidelight = dsidelight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xm = false,
				xp = false,
				zp = true,
				zm = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvsJob.Schedule();
			job.Complete();

			neighbordata.Dispose();
			neighborstate.Dispose();
			ysidelight.Dispose();
			dsidelight.Dispose();
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cachedCubeVerts = cacheCubeVert,
				cachedUVVerts = cacheUVVerts,
				cachedCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

		// Vertical Corners
		// 
		// XMZM Top
		if(CornerIsReady(top:true, xmzm:true) && !txmzmDraw){
			txmzmDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[3]].data.GetLightMap(loader.chunks[this.surroundingChunks[3]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[2]].data.GetLightMap(loader.chunks[this.surroundingChunks[2]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[3]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[3]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[5]].data.GetLightMap(loader.chunks[this.surroundingChunks[5]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[2]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[2]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[5]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[5]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xmzm = true,
				xpzm = false,
				xmzp = false,
				xpzp = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XPZM Top
		if(CornerIsReady(top:true, xpzm:true) && !txpzmDraw){
			txpzmDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[1]].data.GetLightMap(loader.chunks[this.surroundingChunks[1]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[2]].data.GetLightMap(loader.chunks[this.surroundingChunks[2]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[1]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[1]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[4]].data.GetLightMap(loader.chunks[this.surroundingChunks[4]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[2]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[2]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[4]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[4]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xmzm = false,
				xpzm = true,
				xmzp = false,
				xpzp = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XMZP Top
		if(CornerIsReady(top:true, xmzp:true)&& !txmzpDraw){
			txmzpDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[3]].data.GetLightMap(loader.chunks[this.surroundingChunks[3]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[0]].data.GetLightMap(loader.chunks[this.surroundingChunks[0]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[3]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[3]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[6]].data.GetLightMap(loader.chunks[this.surroundingChunks[6]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[0]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[0]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[6]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[6]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xmzm = false,
				xpzm = false,
				xmzp = true,
				xpzp = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XPZP Top
		if(CornerIsReady(top:true, xpzp:true) && !txpzpDraw){
			txpzpDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[1]].data.GetLightMap(loader.chunks[this.surroundingChunks[1]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[0]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[0]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[0]].data.GetLightMap(loader.chunks[this.surroundingChunks[0]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[1]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[1]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[7]].data.GetLightMap(loader.chunks[this.surroundingChunks[7]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[0]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[0]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingTopChunks[7]].data.GetLightMap(loader.chunks[this.surroundingTopChunks[7]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = false,
				isTop = true,
				xmzm = false,
				xpzm = false,
				xmzp = false,
				xpzp = true,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XMZM Bot
		if(CornerIsReady(bottom:true, xmzm:true) && !bxmzmDraw){
			bxmzmDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[3]].data.GetLightMap(loader.chunks[this.surroundingChunks[3]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[2]].data.GetLightMap(loader.chunks[this.surroundingChunks[2]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[3]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[3]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[5]].data.GetLightMap(loader.chunks[this.surroundingChunks[5]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[2]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[2]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[5]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[5]].metadata));


			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xmzm = true,
				xpzm = false,
				xmzp = false,
				xpzp = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XPZM Bot
		if(CornerIsReady(bottom:true, xpzm:true) && !bxpzmDraw){
			bxpzmDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[1]].data.GetLightMap(loader.chunks[this.surroundingChunks[1]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[2]].data.GetLightMap(loader.chunks[this.surroundingChunks[2]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[1]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[1]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[4]].data.GetLightMap(loader.chunks[this.surroundingChunks[4]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[2]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[2]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[4]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[4]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xmzm = false,
				xpzm = true,
				xmzp = false,
				xpzp = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XMZP Bot
		if(CornerIsReady(bottom:true, xmzp:true) && !bxmzpDraw){
			bxmzpDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[3]].data.GetLightMap(loader.chunks[this.surroundingChunks[3]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[0]].data.GetLightMap(loader.chunks[this.surroundingChunks[0]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[3]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[3]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[6]].data.GetLightMap(loader.chunks[this.surroundingChunks[6]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[0]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[0]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[6]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[6]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xmzm = false,
				xpzm = false,
				xmzp = true,
				xpzp = false,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		// XPZP Bot
		if(CornerIsReady(bottom:true, xpzp:true) && !bxpzpDraw){
			bxpzpDraw = true;
			changed = true;

			NativeArray<byte> xlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[1]].data.GetLightMap(loader.chunks[this.surroundingChunks[1]].metadata));
			NativeArray<byte> ylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingVerticalChunks[1]].data.GetLightMap(loader.chunks[this.surroundingVerticalChunks[1]].metadata));
			NativeArray<byte> zlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[0]].data.GetLightMap(loader.chunks[this.surroundingChunks[0]].metadata));
			NativeArray<byte> xylight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[1]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[1]].metadata));
			NativeArray<byte> xzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingChunks[7]].data.GetLightMap(loader.chunks[this.surroundingChunks[7]].metadata));
			NativeArray<byte> yzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[0]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[0]].metadata));
			NativeArray<byte> xyzlight = NativeTools.CopyToNative<byte>(loader.chunks[this.surroundingBotChunks[7]].data.GetLightMap(loader.chunks[this.surroundingBotChunks[7]].metadata));

			BuildVerticalCornerJob bvcJob = new BuildVerticalCornerJob{
				pos = this.pos,
				data = blockdata,
				state = metadata,
				xlight = xlight,
				ylight = ylight,
				zlight = zlight,
				xylight = xylight,
				xzlight = xzlight,
				yzlight = yzlight,
				xyzlight = xyzlight,
				renderMap = renderMap,
				isBottom = true,
				isTop = false,
				xmzm = false,
				xpzm = false,
				xmzp = false,
				xpzp = true,
				verts = verts,
				UVs = uvs,
				normals = normals,
				tangents = tangents,
				normalTris = tris,
				specularTris = specularTris,
				liquidTris = liquidTris,
				leavesTris = leavesTris,
				iceTris = iceTris,
				lavaTris = lavaTris,
				lightUV = lightUV,
				cacheCubeVert = cacheCubeVert,
				cacheCubeUV = cacheUVVerts,
				cacheCubeNormal = cacheCubeNormal,
				cacheCubeTangent = cacheCubeTangent,
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

			job = bvcJob.Schedule();
			job.Complete();

			xlight.Dispose();
			ylight.Dispose();
			zlight.Dispose();
			xylight.Dispose();
			xzlight.Dispose();
			yzlight.Dispose();
			xyzlight.Dispose();
		}

		
		// If mesh was redrawn
		if(changed){
			NativeTris triangleStructure = new NativeTris(tris, specularTris, liquidTris, leavesTris, iceTris, lavaTris);

			BuildMeshSide(verts.ToArray(), uvs.ToArray(), lightUV.ToArray(), normals.ToArray(), tangents.ToArray(), triangleStructure);
			BuildDecalMesh(vertsDecal.ToArray(), UVDecal.ToArray(), trisDecal.ToArray());
		}

		tris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		leavesTris.Dispose();
		iceTris.Dispose();
		lavaTris.Dispose();

		blockdata.Dispose();
		metadata.Dispose();
		renderMap.Dispose();
		verts.Dispose();
		uvs.Dispose();
		normals.Dispose();
		tangents.Dispose();
		cacheCubeVert.Dispose();
		cacheUVVerts.Dispose();
		cacheCubeNormal.Dispose();
		cacheCubeTangent.Dispose();
		toLoadEvent.Dispose();
		toBUD.Dispose();
		lightUV.Dispose();
		lightdata.Dispose();
		hpdata.Dispose();
		vertsDecal.Dispose();
		UVDecal.Dispose();
		trisDecal.Dispose();
		cacheCubeVertsDecal.Dispose();
	}

	// Checks if needed chunks to generate a vertical corner are loaded
	private bool CornerIsReady(bool bottom=false, bool top=false, bool xmzm=false, bool xpzm=false, bool xmzp=false, bool xpzp=false){
		if(bottom){
			if(xmzm){
				if(Has(surroundingChunks[3]) && Has(surroundingVerticalChunks[1]) && Has(surroundingChunks[2]) && Has(surroundingBotChunks[3]) &&
					Has(surroundingChunks[5]) && Has(surroundingBotChunks[2]) && Has(surroundingBotChunks[5])){
					return true;
				}
			}
			else if(xmzp){
				if(Has(surroundingChunks[3]) && Has(surroundingVerticalChunks[1]) && Has(surroundingChunks[0]) &&
					Has(surroundingBotChunks[3]) && Has(surroundingChunks[6]) && Has(surroundingBotChunks[0]) && Has(surroundingBotChunks[6])){
					return true;
				}
			}
			else if(xpzm){
				if(Has(surroundingChunks[1]) && Has(surroundingVerticalChunks[1]) && Has(surroundingChunks[2]) && Has(surroundingBotChunks[1]) &&
				    Has(surroundingChunks[4]) && Has(surroundingBotChunks[2]) && Has(surroundingBotChunks[4])){
					return true;
				}
			}
			else if(xpzp){
				if(Has(surroundingChunks[1]) && Has(surroundingVerticalChunks[1]) && Has(surroundingChunks[0]) && Has(surroundingBotChunks[1]) && 
					Has(surroundingChunks[7]) && Has(surroundingBotChunks[0]) && Has(surroundingBotChunks[7])){
					return true;
				}
			}
		}
		else if(top){
			if(xmzm){
				if(Has(surroundingChunks[3]) && Has(surroundingVerticalChunks[0]) && Has(surroundingChunks[2]) && Has(surroundingTopChunks[3]) &&
					Has(surroundingChunks[5]) && Has(surroundingTopChunks[2]) && Has(surroundingTopChunks[5])){
					return true;
				}
			}
			else if(xmzp){
				if(Has(surroundingChunks[3]) && Has(surroundingVerticalChunks[0]) && Has(surroundingChunks[0]) &&
					Has(surroundingTopChunks[3]) && Has(surroundingChunks[6]) && Has(surroundingTopChunks[0]) && Has(surroundingTopChunks[6])){
					return true;
				}
			}
			else if(xpzm){
				if(Has(surroundingChunks[1]) && Has(surroundingVerticalChunks[0]) && Has(surroundingChunks[2]) && Has(surroundingTopChunks[1]) &&
				    Has(surroundingChunks[4]) && Has(surroundingTopChunks[2]) && Has(surroundingTopChunks[4])){
					return true;
				}
			}
			else if(xpzp){
				if(Has(surroundingChunks[1]) && Has(surroundingVerticalChunks[0]) && Has(surroundingChunks[0]) && Has(surroundingTopChunks[1]) && 
					Has(surroundingChunks[7]) && Has(surroundingTopChunks[0]) && Has(surroundingTopChunks[7])){
					return true;
				}
			}
		}
		return false;
	}

	// Short for "loader.chunks.ContainsKey()"
	private bool Has(ChunkPos pos){
		return this.loader.chunks.ContainsKey(pos);
	}

	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(bool load=false, bool pregenReload=false){
		/*
		Reset Chunk side rebuilding
		*/
    	
    	this.vertices.Clear();
    	this.normals.Clear();
    	this.tangents.Clear();
    	this.triangles = null;
    	this.specularTris = null;
    	this.liquidTris = null;
    	this.iceTris = null;
    	this.lavaTris = null;
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
		NativeList<int> lavaTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector2> lightUV = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector4> tangents = new NativeList<Vector4>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheCubeUV = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector4> cacheCubeTangent = new NativeArray<Vector4>(4, Allocator.TempJob);

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
			tangents = tangents,
			normalTris = normalTris,
			specularTris = specularTris,
			liquidTris = liquidTris,
			leavesTris = leavesTris,
			iceTris = iceTris,
			lavaTris = lavaTris,
			cacheCubeVert = cacheCubeVert,
			cacheCubeUV = cacheCubeUV,
			cacheCubeNormal = cacheCubeNormal,
			cacheCubeTangent = cacheCubeTangent,
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

		BuildAllDecals(blockdata, renderMap);

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
				blockBook.objects[ushort.MaxValue-assetCode].mesh.GetTangents(tangentAux);
				this.cacheTangents.AddRange(tangentAux.ToArray());
				this.scalingFactor.Add(BlockEncyclopediaECS.objectScaling[ushort.MaxValue-assetCode]);
				this.hitboxScaling.Add(BlockEncyclopediaECS.hitboxScaling[ushort.MaxValue-assetCode]);

				vertexAux.Clear();
				UVaux.Clear();
				normalAux.Clear();
				tangentAux.Clear();

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
		NativeList<Vector4> meshTangents = new NativeList<Vector4>(0, Allocator.TempJob);
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
		NativeArray<Vector4> loadedTangents = new NativeArray<Vector4>(this.cacheTangents.ToArray(), Allocator.TempJob);
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
			meshTangents = meshTangents,
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
			loadedTangents = loadedTangents,

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
		this.lavaTris = lavaTris.ToArray();

		this.UVs.AddRange(UVs.ToArray());
		this.UVs.AddRange(meshUVs.ToArray());

		this.lightUVMain.AddRange(lightUV.ToArray());
		this.lightUVMain.AddRange(meshLightUV.ToArray());

		this.normals.AddRange(normals.ToArray());
		this.normals.AddRange(meshNormals.ToArray());

		this.tangents.AddRange(tangents.ToArray());
		this.tangents.AddRange(meshTangents.ToArray());

		BuildHitboxMesh(hitboxVerts.ToArray(), hitboxNormals.ToArray(), hitboxTriangles.ToArray());

		// Dispose Bin
		verts.Dispose();
		normalTris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		leavesTris.Dispose();
		iceTris.Dispose();
		lavaTris.Dispose();
		blockdata.Dispose();
		statedata.Dispose();
		renderMap.Dispose();
		loadCoordList.Dispose();
		cacheCubeVert.Dispose();
		cacheCubeNormal.Dispose();
		cacheCubeTangent.Dispose();
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
		loadedTangents.Dispose();
		loadAssetList.Dispose();
		scaling.Dispose();
		UVs.Dispose();
		normals.Dispose();
		tangents.Dispose();
		meshUVs.Dispose();
		meshNormals.Dispose();
		meshTangents.Dispose();
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

    public void BuildAllDecals(NativeArray<ushort> blockdata, NativeArray<byte> renderMap){
    	NativeArray<ushort> hpdata = NativeTools.CopyToNative<ushort>(this.metadata.GetHPData());

		NativeList<int> triangles = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheVerts = new NativeArray<Vector3>(4, Allocator.TempJob);

		BuildDecalJob bdj = new BuildDecalJob{
			pos = pos,
			blockdata = blockdata,
			renderMap = renderMap,
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

    	this.meshFilter.mesh.subMeshCount = 7;

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
 	    this.meshFilter.mesh.SetTriangles(this.lavaTris, 6);

    	this.meshFilter.mesh.SetUVs(0, this.UVs.ToArray());
    	this.meshFilter.mesh.SetUVs(3, this.lightUVMain.ToArray());

    	this.meshFilter.mesh.SetNormals(this.normals.ToArray());
    	this.meshFilter.mesh.SetTangents(this.tangents.ToArray());
    }

    // Builds meshes from verts, UVs and tris from different layers
    private void BuildMeshSide(Vector3[] verts, Vector2[] UV, Vector2[] lightUV, Vector3[] normals, Vector4[] tangents, NativeTris triStruct){
    	this.meshCollider.sharedMesh.Clear();
    	this.meshFilter.mesh.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		this.meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilter.mesh.subMeshCount = 7;

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
    	this.meshFilter.mesh.SetTriangles(triStruct.lavaTris.ToArray(), 6);

    	this.meshFilter.mesh.uv = UV;
    	this.meshFilter.mesh.uv4 = lightUV;
    	this.meshFilter.mesh.SetNormals(normals);
    	this.meshFilter.mesh.SetTangents(tangents);
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
