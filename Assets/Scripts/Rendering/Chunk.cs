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
	private static bool showHitbox = true;
	private MeshFilter hitboxFilter;

	// Draw Flags
	public bool drawMain = false;
	public bool topDraw = false;
	public bool bottomDraw = false;

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
    private ChunkPos[] surroundingVerticalChunks = new ChunkPos[2];


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
		Chunk c = new Chunk(this.pos, this.renderer, this.loader);

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

	// Short for "loader.chunks.ContainsKey()"
	private bool Has(ChunkPos pos){
		return this.loader.chunks.ContainsKey(pos);
	}

	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(bool load=false){
    	ChunkPos auxPos;
    	int verticalCode = 0;


		NativeArray<ushort> blockdata = NativeTools.CopyToNative<ushort>(this.data.GetData());
		NativeArray<ushort> statedata = NativeTools.CopyToNative<ushort>(this.metadata.GetStateData());
    	NativeArray<ushort> hpdata = NativeTools.CopyToNative<ushort>(this.metadata.GetHPData());
		NativeArray<byte> lightdata = NativeTools.CopyToNative<byte>(this.data.GetLightMap(this.metadata));
		NativeArray<byte> renderMap = NativeTools.CopyToNative<byte>(this.data.GetRenderMap());		
		NativeList<int3> loadCoordList = new NativeList<int3>(0, Allocator.TempJob);
		NativeList<ushort> loadCodeList = new NativeList<ushort>(0, Allocator.TempJob);
		NativeList<int3> loadAssetList = new NativeList<int3>(0, Allocator.TempJob);
		NativeHashSet<int3> assetCoordinates = new NativeHashSet<int3>(0, Allocator.TempJob);

		NativeList<int> normalTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> specularTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> liquidTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> leavesTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> iceTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> lavaTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> assetTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<int> assetSolidTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> verts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> UVs = new NativeList<Vector2>(0, Allocator.TempJob);
		NativeList<Vector3> lightUV = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector3> normals = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector4> tangents = new NativeList<Vector4>(0, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeVert = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector2> cacheCubeUV = new NativeArray<Vector2>(4, Allocator.TempJob);
		NativeArray<Vector3> cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.TempJob);
		NativeArray<Vector4> cacheCubeTangent = new NativeArray<Vector4>(4, Allocator.TempJob);

		NativeList<int> decalTris = new NativeList<int>(0, Allocator.TempJob);
		NativeList<Vector3> decalVerts = new NativeList<Vector3>(0, Allocator.TempJob);
		NativeList<Vector2> decalUVs = new NativeList<Vector2>(0, Allocator.TempJob);

		auxPos = new ChunkPos(pos.x-1, pos.z, pos.y);
		NativeArray<ushort> xmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> xmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x+1, pos.z, pos.y);
		NativeArray<ushort> xpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> xplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x, pos.z-1, pos.y);
		NativeArray<ushort> zmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> zmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x, pos.z+1, pos.y);
		NativeArray<ushort> zpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> zplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x-1, pos.z-1, pos.y);
		NativeArray<ushort> xmzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> xmzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x-1, pos.z+1, pos.y);
		NativeArray<ushort> xmzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> xmzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x+1, pos.z-1, pos.y);
		NativeArray<ushort> xpzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> xpzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
		auxPos = new ChunkPos(pos.x+1, pos.z+1, pos.y);
		NativeArray<ushort> xpzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
		NativeArray<byte> xpzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));

		NativeArray<ushort> vdata;
		NativeArray<ushort> vxmdata;
		NativeArray<ushort> vxpdata;
		NativeArray<ushort> vzmdata;
		NativeArray<ushort> vzpdata;
		NativeArray<ushort> vxmzmdata;
		NativeArray<ushort> vxmzpdata;
		NativeArray<ushort> vxpzmdata;
		NativeArray<ushort> vxpzpdata;
		NativeArray<byte> vlight;
		NativeArray<byte> vxmlight;
		NativeArray<byte> vxplight;
		NativeArray<byte> vzmlight;
		NativeArray<byte> vzplight;
		NativeArray<byte> vxmzmlight;
		NativeArray<byte> vxmzplight;
		NativeArray<byte> vxpzmlight;
		NativeArray<byte> vxpzplight;

		if(Has(this.surroundingVerticalChunks[0]) && this.loader.CanBeDrawn(this.surroundingVerticalChunks[0])){
			vdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[this.surroundingVerticalChunks[0]].data.GetData());
			vlight = NativeTools.CopyToNative<byte>(this.loader.chunks[this.surroundingVerticalChunks[0]].data.GetLightMap(this.loader.chunks[this.surroundingVerticalChunks[0]].metadata));

			auxPos = new ChunkPos(pos.x-1, pos.z, pos.y+1);
			vxmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x+1, pos.z, pos.y+1);
			vxpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x, pos.z-1, pos.y+1);
			vzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x, pos.z+1, pos.y+1);
			vzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x-1, pos.z-1, pos.y+1);
			vxmzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxmzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x-1, pos.z+1, pos.y+1);
			vxmzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxmzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x+1, pos.z-1, pos.y+1);
			vxpzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxpzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x+1, pos.z+1, pos.y+1);
			vxpzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxpzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			verticalCode = 1;			
		}
		else if(Has(this.surroundingVerticalChunks[1]) && this.loader.CanBeDrawn(this.surroundingVerticalChunks[1])){
			vdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[this.surroundingVerticalChunks[1]].data.GetData());
			vlight = NativeTools.CopyToNative<byte>(this.loader.chunks[this.surroundingVerticalChunks[1]].data.GetLightMap(this.loader.chunks[this.surroundingVerticalChunks[1]].metadata));

			auxPos = new ChunkPos(pos.x-1, pos.z, pos.y-1);
			vxmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x+1, pos.z, pos.y-1);
			vxpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x, pos.z-1, pos.y-1);
			vzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x, pos.z+1, pos.y-1);
			vzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x-1, pos.z-1, pos.y-1);
			vxmzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxmzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x-1, pos.z+1, pos.y-1);
			vxmzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxmzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x+1, pos.z-1, pos.y-1);
			vxpzmdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxpzmlight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));
			auxPos = new ChunkPos(pos.x+1, pos.z+1, pos.y-1);
			vxpzpdata = NativeTools.CopyToNative<ushort>(this.loader.chunks[auxPos].data.GetData());
			vxpzplight = NativeTools.CopyToNative<byte>(this.loader.chunks[auxPos].data.GetLightMap(this.loader.chunks[auxPos].metadata));	
			verticalCode = -1;			
		}
		else{
			vdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vlight = new NativeArray<byte>(0, Allocator.TempJob);

			vxmdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vxpdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vzmdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vzpdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vxmzmdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vxmzpdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vxpzmdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vxpzpdata = new NativeArray<ushort>(0, Allocator.TempJob);
			vxmlight = new NativeArray<byte>(0, Allocator.TempJob);
			vxplight = new NativeArray<byte>(0, Allocator.TempJob);
			vzmlight = new NativeArray<byte>(0, Allocator.TempJob);
			vzplight = new NativeArray<byte>(0, Allocator.TempJob);
			vxmzmlight = new NativeArray<byte>(0, Allocator.TempJob);
			vxmzplight = new NativeArray<byte>(0, Allocator.TempJob);
			vxpzmlight = new NativeArray<byte>(0, Allocator.TempJob);
			vxpzplight = new NativeArray<byte>(0, Allocator.TempJob);
		}

		NativeArray<byte> heightMap = NativeTools.CopyToNative(this.data.GetHeightMap());
		auxPos = new ChunkPos(pos.x-1, pos.z, pos.y);
		NativeArray<byte> xmheightMap = NativeTools.CopyToNative(this.loader.chunks[auxPos].data.GetHeightMap());
		auxPos = new ChunkPos(pos.x+1, pos.z, pos.y);
		NativeArray<byte> xpheightMap = NativeTools.CopyToNative(this.loader.chunks[auxPos].data.GetHeightMap());
		auxPos = new ChunkPos(pos.x, pos.z-1, pos.y);
		NativeArray<byte> zmheightMap = NativeTools.CopyToNative(this.loader.chunks[auxPos].data.GetHeightMap());
		auxPos = new ChunkPos(pos.x, pos.z+1, pos.y);
		NativeArray<byte> zpheightMap = NativeTools.CopyToNative(this.loader.chunks[auxPos].data.GetHeightMap());

		NativeArray<Vector3> extraUV = new NativeArray<Vector3>(4, Allocator.TempJob);

		// Threading Job
		BuildChunkJob bcJob = new BuildChunkJob{
			pos = pos,
			load = load,
			data = blockdata,
			state = statedata,
			hp = hpdata,
			lightdata = lightdata,

			xmdata = xmdata,
			xpdata = xpdata,
			zmdata = zmdata,
			zpdata = zpdata,
			xmzmdata = xmzmdata,
			xmzpdata = xmzpdata,
			xpzmdata = xpzmdata,
			xpzpdata = xpzpdata,

			xmlight = xmlight,
			xplight = xplight,
			zmlight = zmlight,
			zplight = zplight,
			xmzmlight = xmzmlight,
			xmzplight = xmzplight,
			xpzmlight = xpzmlight,
			xpzplight = xpzplight,

			vdata = vdata,
			vlight = vlight,

			vxmdata = vxmdata,
			vxpdata = vxpdata,
			vzmdata = vzmdata,
			vzpdata = vzpdata,
			vxmzmdata = vxmzmdata,
			vxmzpdata = vxmzpdata,
			vxpzmdata = vxpzmdata,
			vxpzpdata = vxpzpdata,

			vxmlight = vxmlight,
			vxplight = vxplight,
			vzmlight = vzmlight,
			vzplight = vzplight,
			vxmzmlight = vxmzmlight,
			vxmzplight = vxmzplight,
			vxpzmlight = vxpzmlight,
			vxpzplight = vxpzplight,

			heightMap = heightMap,
			xmheight = xmheightMap,
			xpheight = xpheightMap,
			zmheight = zmheightMap,
			zpheight = zpheightMap,

			canRain = BiomeHandler.BiomeToDampness(this.biomeName),
			verticalCode = verticalCode,

			loadOutList = loadCoordList,
			loadAssetList = loadAssetList,
			assetCoordinates = assetCoordinates,
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
			decalTris = decalTris,
			decalUVs = decalUVs,
			decalVerts = decalVerts,
			cacheCubeVert = cacheCubeVert,
			cacheCubeUV = cacheCubeUV,
			cacheCubeNormal = cacheCubeNormal,
			cacheCubeTangent = cacheCubeTangent,
			extraUV = extraUV,
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
			blockDrawRegardless = BlockEncyclopediaECS.blockDrawRegardless,
			blockHP = BlockEncyclopediaECS.blockHP,
			objectHP = BlockEncyclopediaECS.objectHP,

			atlasSize = BlockEncyclopediaECS.atlasSize
		};
		JobHandle job = bcJob.Schedule();

		job.Complete();

		this.indexVert.Add(0);
		this.indexUV.Add(0);
		this.indexTris.Add(0);
		this.indexHitboxVert.Add(0);
		this.indexHitboxTris.Add(0);

		// Offseting and Rotation shenanigans
		NativeParallelHashMap<int, Vector3> scaleOffset = new NativeParallelHashMap<int, Vector3>(0, Allocator.TempJob);
		NativeParallelHashMap<int, int2> rotationOffset = new NativeParallelHashMap<int, int2>(0, Allocator.TempJob);
		
		int3[] coordArray = loadAssetList.AsArray().ToArray();
		foreach(int3 coord in coordArray){
			ushort assetCode = this.data.GetCell(coord);

			if(!this.cacheCodes.Contains(assetCode)){
				this.cacheCodes.Add(assetCode);

				VoxelLoader.GetObject(assetCode).GetMesh().GetVertices(vertexAux);
				this.cacheVertsv3.AddRange(vertexAux.ToArray());
				this.cacheTris.AddRange(VoxelLoader.GetObject(assetCode).GetMesh().GetTriangles(0));
				VoxelLoader.GetObject(assetCode).GetMesh().GetUVs(0, UVaux);
				this.cacheUVv2.AddRange(UVaux.ToArray());
				this.indexVert.Add(this.indexVert[indexVert.Count-1] + vertexAux.Count);
				this.indexTris.Add(this.indexTris[indexTris.Count-1] + VoxelLoader.GetObject(assetCode).GetMesh().GetTriangles(0).Length);
				this.indexUV.Add(this.indexUV[indexUV.Count-1] + UVaux.Count);
				VoxelLoader.GetObject(assetCode).GetMesh().GetNormals(normalAux);
				this.cacheNormals.AddRange(normalAux.ToArray());
				VoxelLoader.GetObject(assetCode).GetMesh().GetTangents(tangentAux);
				this.cacheTangents.AddRange(tangentAux.ToArray());
				this.scalingFactor.Add(BlockEncyclopediaECS.objectScaling[ushort.MaxValue-assetCode]);
				this.hitboxScaling.Add(BlockEncyclopediaECS.hitboxScaling[ushort.MaxValue-assetCode]);

				vertexAux.Clear();
				UVaux.Clear();
				normalAux.Clear();
				tangentAux.Clear();

				VoxelLoader.GetObject(assetCode).GetHitboxMesh().GetVertices(vertexAux);
				this.cacheHitboxVerts.AddRange(vertexAux.ToArray());
				this.cacheHitboxTriangles.AddRange(VoxelLoader.GetObject(assetCode).GetHitboxMesh().GetTriangles(0));
				VoxelLoader.GetObject(assetCode).GetMesh().GetNormals(normalAux);
				this.cacheHitboxNormals.AddRange(normalAux);

				this.indexHitboxVert.Add(this.indexHitboxVert[indexHitboxVert.Count-1] + vertexAux.Count);
				this.indexHitboxTris.Add(this.indexHitboxTris[indexHitboxTris.Count-1] + VoxelLoader.GetObject(assetCode).GetHitboxMesh().GetTriangles(0).Length);

				vertexAux.Clear();
				normalAux.Clear();



				// If has special offset or rotation
				// Hash function for Dictionary is blockCode*256 + state. This leaves a maximum of 256 states for every object in the game
				if(VoxelLoader.GetObject(assetCode).needsRotation){
					for(ushort i=0; i < VoxelLoader.GetObject(assetCode).maximumRotationScaleState; i++){
						scaleOffset.Add((int)(assetCode*256 + i), VoxelLoader.GetObject(assetCode).GetOffsetVector(i));
						rotationOffset.Add((int)(assetCode*256 + i), VoxelLoader.GetObject(assetCode).GetRotationValue(i));
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
			this.loader.client.Send(this.message);
		}
		loadCoordList.Clear();
		
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
			canRain = BiomeHandler.BiomeToDampness(this.biomeName),
			heightMap = heightMap,

			meshVerts = verts,
			meshTris = assetTris,
			meshSolidTris = assetSolidTris,
			meshUVs = UVs,
			meshLightUV = lightUV,
			meshNormals = normals,
			meshTangents = tangents,
			hitboxVerts = hitboxVerts,
			hitboxNormals = hitboxNormals,
			hitboxTriangles = hitboxTriangles,
			scaling = scaling,
			needRotation = BlockEncyclopediaECS.objectNeedRotation,
			material = BlockEncyclopediaECS.objectMaterial,
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


		BuildDecalMesh(decalVerts.AsArray(), decalUVs.AsArray(), decalTris);
		BuildHitboxMesh(hitboxVerts.AsArray(), hitboxNormals.AsArray(), hitboxTriangles);
		BuildMesh(verts, normalTris, specularTris, liquidTris, assetTris, assetSolidTris, leavesTris, iceTris, lavaTris, UVs, lightUV, normals, tangents);


		// Dispose Bin
		verts.Dispose();
		normalTris.Dispose();
		specularTris.Dispose();
		liquidTris.Dispose();
		leavesTris.Dispose();
		iceTris.Dispose();
		lavaTris.Dispose();
		assetTris.Dispose();
		assetSolidTris.Dispose();
		blockdata.Dispose();
		statedata.Dispose();
		renderMap.Dispose();
		loadCoordList.Dispose();
		assetCoordinates.Dispose();
		cacheCubeVert.Dispose();
		cacheCubeNormal.Dispose();
		cacheCubeTangent.Dispose();
		cacheCubeUV.Dispose();
		extraUV.Dispose();
		loadCodeList.Dispose();
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
		scaleOffset.Dispose();
		rotationOffset.Dispose();
		lightUV.Dispose();
		lightdata.Dispose();
		loadedHitboxVerts.Dispose();
		loadedHitboxNormals.Dispose();
		loadedHitboxTriangles.Dispose();
		hitboxVerts.Dispose();
		hitboxNormals.Dispose();
		hitboxTriangles.Dispose();
		hitboxVertsOffset.Dispose();
		hitboxTrisOffset.Dispose();
		hitboxScaling.Dispose();
		hpdata.Dispose();
		decalTris.Dispose();
		decalVerts.Dispose();
		decalUVs.Dispose();

		vdata.Dispose();
		vlight.Dispose();

		xmdata.Dispose();
		xpdata.Dispose();
		zmdata.Dispose();
		zpdata.Dispose();
		xmzmdata.Dispose();
		xmzpdata.Dispose();
		xpzmdata.Dispose();
		xpzpdata.Dispose();

		heightMap.Dispose();
		xmheightMap.Dispose();
		xpheightMap.Dispose();
		zmheightMap.Dispose();
		zpheightMap.Dispose();

		xmlight.Dispose();
		xplight.Dispose();
		zmlight.Dispose();
		zplight.Dispose();
		xmzmlight.Dispose();
		xmzplight.Dispose();
		xpzmlight.Dispose();
		xpzplight.Dispose();

		vxmdata.Dispose();
		vxpdata.Dispose();
		vzmdata.Dispose();
		vzpdata.Dispose();
		vxmzmdata.Dispose();
		vxmzpdata.Dispose();
		vxpzmdata.Dispose();
		vxpzpdata.Dispose();

		vxmlight.Dispose();
		vxplight.Dispose();
		vzmlight.Dispose();
		vzplight.Dispose();
		vxmzmlight.Dispose();
		vxmzplight.Dispose();
		vxpzmlight.Dispose();
		vxpzplight.Dispose();

		// Dispose Asset Cache
		this.cacheCodes.Clear();
		this.cacheVertsv3.Clear();
		this.cacheLightUV.Clear();
		this.cacheTris.Clear();
		this.cacheUVv2.Clear();
		this.cacheNormals.Clear();
		this.cacheTangents.Clear();
		this.indexVert.Clear();
		this.indexUV.Clear();
		this.indexTris.Clear();
		this.scalingFactor.Clear();
		this.UVaux.Clear();
		this.vertexAux.Clear();
		this.normalAux.Clear();
		this.tangentAux.Clear();
		this.cacheHitboxVerts.Clear();
		this.cacheHitboxNormals.Clear();
		this.cacheHitboxTriangles.Clear();
		this.indexHitboxVert.Clear();
		this.indexHitboxTris.Clear();
		this.hitboxScaling.Clear();

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
    private void BuildMesh(NativeList<Vector3> vertices, NativeList<int> tris, NativeList<int> specularTris, NativeList<int> liquidTris, NativeList<int> assetTris, NativeList<int> assetSolidTris, NativeList<int> leavesTris, NativeList<int> iceTris, NativeList<int> lavaTris, NativeList<Vector2> UVs, NativeList<Vector3> lightUV, NativeList<Vector3> normals, NativeList<Vector4> tangents){
    	this.meshCollider.sharedMesh.Clear();
    	this.meshFilter.mesh.Clear();

    	if(vertices.Length >= ushort.MaxValue){
    		this.meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilter.mesh.subMeshCount = 8;

    	this.meshFilter.mesh.SetVertices(vertices.AsArray());
    	this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(tris.AsArray()), 0);
 	    this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(iceTris.AsArray()), 6);
 	    this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(assetSolidTris.AsArray()), 4);

    	// Fix for a stupid Unity Bug
    	if(vertices.Length > 0){
	    	this.meshCollider.sharedMesh = null;
	    	this.meshCollider.sharedMesh = this.meshFilter.mesh;
    	}

    	this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(specularTris.AsArray()), 1);
    	this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(liquidTris.AsArray()), 2);
    	this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(assetTris.AsArray()), 3);
 	    this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(leavesTris.AsArray()), 5);
 	    this.meshFilter.mesh.SetTriangles(NativeTools.CopyToManaged(lavaTris.AsArray()), 7);

    	this.meshFilter.mesh.SetUVs(0, UVs.AsArray());
    	this.meshFilter.mesh.SetUVs(3, lightUV.AsArray());

    	this.meshFilter.mesh.SetNormals(normals.AsArray());
    	this.meshFilter.mesh.SetTangents(tangents.AsArray());
    }

    // Builds the decal mesh
    private void BuildDecalMesh(NativeArray<Vector3> verts, NativeArray<Vector2> UV, NativeList<int> triangles){
    	this.meshFilterDecal.mesh.Clear();

    	if(verts.Length >= ushort.MaxValue){
    		this.meshFilterDecal.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    	}

    	this.meshFilterDecal.mesh.SetVertices(verts);
    	this.meshFilterDecal.mesh.SetTriangles(NativeTools.CopyToManaged(triangles.AsArray()), 0);
    	this.meshFilterDecal.mesh.SetUVs(0, UV);
    	this.meshFilterDecal.mesh.RecalculateNormals();
    }

    // Builds the hitbox mesh onto the Hitbox MeshCollider
    private void BuildHitboxMesh(NativeArray<Vector3> verts, NativeArray<Vector3> normals, NativeList<int> triangles){
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

    	this.meshRaycast.SetVertices(verts);
    	this.meshRaycast.SetTriangles(NativeTools.CopyToManaged(triangles.AsArray()), 0);
    	this.meshRaycast.SetNormals(normals);
    	this.meshColliderRaycast.sharedMesh = this.meshRaycast;

    	if(showHitbox){
    		this.hitboxFilter.mesh = this.meshColliderRaycast.sharedMesh;
    	}
    }
}
