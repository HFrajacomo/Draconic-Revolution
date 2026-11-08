using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/*
Persistent buffers and state of one chunk build. A build goes through:
	Snapshot (main thread) -> BuildChunkJob -> PrepareAssetsJob -> GreedyColliderJob -> WriteChunkMeshJob -> Apply (main thread) -> BakeColliderJob -> Collider swap (main thread)
Jobs only read the snapshot, so chunk data can be freely changed in the main thread while a build runs
*/
public class ChunkBuildSlot{
	private enum SlotState{
		FREE,
		BUILDING,
		BAKING
	}

	private static readonly int VOXEL_COUNT = Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth;
	private static readonly int INITIAL_CAPACITY = 4096;

	#if UNITY_EDITOR
		private static readonly MeshUpdateFlags MESH_FLAGS = MeshUpdateFlags.DontRecalculateBounds;
	#else
		private static readonly MeshUpdateFlags MESH_FLAGS = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
	#endif

	// Snapshot
	private NativeArray<ushort> blocks;
	private NativeArray<byte> light;
	private NativeArray<byte> heights;
	private NativeArray<ushort> state;
	private NativeArray<ushort> hp;
	private NativeArray<byte> renderMap;

	// Render Mesh
	private NativeList<Vector3> verts;
	private NativeList<Vector3> UVs;
	private NativeList<Vector3> lightUV;
	private NativeList<Vector3> normals;
	private NativeList<Vector4> tangents;
	private NativeList<int> normalTris;
	private NativeList<int> specularTris;
	private NativeList<int> liquidTris;
	private NativeList<int> leavesTris;
	private NativeList<int> iceTris;
	private NativeList<int> lavaTris;
	private NativeList<int> assetTris;
	private NativeList<int> assetSolidTris;

	// Decal Mesh
	private NativeList<Vector3> decalVerts;
	private NativeList<Vector3> decalNormals;
	private NativeList<Vector2> decalUVs;
	private NativeList<int> decalTris;

	// Collider Mesh
	private NativeBitArray colliderFaces;
	private NativeArray<int> blockVertexCount;
	private NativeList<Vector3> colliderVerts;
	private NativeList<int> colliderTris;

	// Raycast Mesh
	private NativeList<Vector3> hitboxVerts;
	private NativeList<Vector3> hitboxNormals;
	private NativeList<int> hitboxTris;

	// Objects and Load Events
	private NativeList<int3> loadAssetList;
	private NativeBitArray assetCoordinates;
	private NativeList<int3> loadCoords;

	// Job Cache
	private NativeArray<Vector3> cacheCubeVert;
	private NativeArray<Vector3> cacheCubeUV;
	private NativeArray<Vector3> cacheCubeNormal;
	private NativeArray<Vector4> cacheCubeTangent;
	private NativeArray<Vector3> extraUV;

	// Results
	private NativeArray<Bounds> bounds;
	private NativeArray<int> indexCounts;
	private NativeArray<int> bakeIDs;
	private Mesh.MeshDataArray meshDataArray;
	private bool hasMeshData = false;
	private bool hasCollider = false;
	private bool hasRaycast = false;

	// State
	private SlotState slotState = SlotState.FREE;
	private Chunk chunk;
	private bool isLoadBuild;
	private int loadVersion;
	private JobHandle buildHandle;
	private JobHandle bakeHandle;
	private bool isDisposed = false;


	public ChunkBuildSlot(){
		this.blocks = new NativeArray<ushort>(PaddedChunk.SIZE, Allocator.Persistent);
		this.light = new NativeArray<byte>(PaddedChunk.SIZE, Allocator.Persistent);
		this.heights = new NativeArray<byte>(PaddedChunk.MAP_SIZE, Allocator.Persistent);
		this.state = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.hp = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.renderMap = new NativeArray<byte>(Chunk.chunkWidth*Chunk.chunkWidth, Allocator.Persistent);

		this.verts = new NativeList<Vector3>(INITIAL_CAPACITY, Allocator.Persistent);
		this.UVs = new NativeList<Vector3>(INITIAL_CAPACITY, Allocator.Persistent);
		this.lightUV = new NativeList<Vector3>(INITIAL_CAPACITY, Allocator.Persistent);
		this.normals = new NativeList<Vector3>(INITIAL_CAPACITY, Allocator.Persistent);
		this.tangents = new NativeList<Vector4>(INITIAL_CAPACITY, Allocator.Persistent);
		this.normalTris = new NativeList<int>(INITIAL_CAPACITY, Allocator.Persistent);
		this.specularTris = new NativeList<int>(Allocator.Persistent);
		this.liquidTris = new NativeList<int>(Allocator.Persistent);
		this.leavesTris = new NativeList<int>(Allocator.Persistent);
		this.iceTris = new NativeList<int>(Allocator.Persistent);
		this.lavaTris = new NativeList<int>(Allocator.Persistent);
		this.assetTris = new NativeList<int>(Allocator.Persistent);
		this.assetSolidTris = new NativeList<int>(Allocator.Persistent);

		this.decalVerts = new NativeList<Vector3>(Allocator.Persistent);
		this.decalNormals = new NativeList<Vector3>(Allocator.Persistent);
		this.decalUVs = new NativeList<Vector2>(Allocator.Persistent);
		this.decalTris = new NativeList<int>(Allocator.Persistent);

		this.colliderFaces = new NativeBitArray(VOXEL_COUNT*6, Allocator.Persistent);
		this.blockVertexCount = new NativeArray<int>(1, Allocator.Persistent);
		this.colliderVerts = new NativeList<Vector3>(INITIAL_CAPACITY, Allocator.Persistent);
		this.colliderTris = new NativeList<int>(INITIAL_CAPACITY, Allocator.Persistent);

		this.hitboxVerts = new NativeList<Vector3>(Allocator.Persistent);
		this.hitboxNormals = new NativeList<Vector3>(Allocator.Persistent);
		this.hitboxTris = new NativeList<int>(Allocator.Persistent);

		this.loadAssetList = new NativeList<int3>(Allocator.Persistent);
		this.assetCoordinates = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);
		this.loadCoords = new NativeList<int3>(Allocator.Persistent);

		this.cacheCubeVert = new NativeArray<Vector3>(4, Allocator.Persistent);
		this.cacheCubeUV = new NativeArray<Vector3>(4, Allocator.Persistent);
		this.cacheCubeNormal = new NativeArray<Vector3>(4, Allocator.Persistent);
		this.cacheCubeTangent = new NativeArray<Vector4>(4, Allocator.Persistent);
		this.extraUV = new NativeArray<Vector3>(4, Allocator.Persistent);

		this.bounds = new NativeArray<Bounds>(WriteChunkMeshJob.MESH_COUNT, Allocator.Persistent);
		this.indexCounts = new NativeArray<int>(WriteChunkMeshJob.MESH_COUNT, Allocator.Persistent);
		this.bakeIDs = new NativeArray<int>(2, Allocator.Persistent);
	}

	public bool IsFree(){return this.slotState == SlotState.FREE;}
	public bool IsBuildDone(){return this.slotState == SlotState.BUILDING && this.buildHandle.IsCompleted;}
	public bool IsBakeDone(){return this.slotState == SlotState.BAKING && this.bakeHandle.IsCompleted;}
	public Chunk GetChunk(){return this.chunk;}
	public bool IsLoadBuild(){return this.isLoadBuild;}
	public int GetLoadVersion(){return this.loadVersion;}

	// Coordinates of the blocks with load events. Only valid after ApplyMeshes() of a load build
	public NativeList<int3> GetLoadCoords(){return this.loadCoords;}

	/*
	Takes a snapshot of the chunk and its neighbors and schedules the build jobs.
	All 8 horizontal neighbors must be loaded. Vertical neighbors are used when they and their 8 neighbors are loaded
	*/
	public void Schedule(Chunk target, ChunkLoader loader, bool load, int requestedLoadVersion){
		ChunkPos pos = target.pos;
		bool hasTop = HasVerticalLayer(loader, pos, 1);
		bool hasBottom = HasVerticalLayer(loader, pos, -1);
		bool canRain = BiomeHandler.BiomeToDampness(target.biomeName);

		this.chunk = target;
		this.isLoadBuild = load;
		this.loadVersion = requestedLoadVersion;

		GatherSnapshot(loader, pos, hasTop, hasBottom);
		ClearOutputs();

		BuildChunkJob bcJob = new BuildChunkJob{
			pos = pos,
			hasTop = hasTop,
			hasBottom = hasBottom,
			canRain = canRain,

			blocks = this.blocks,
			light = this.light,
			heights = this.heights,
			state = this.state,
			hp = this.hp,
			renderMap = this.renderMap,

			loadAssetList = this.loadAssetList,
			assetCoordinates = this.assetCoordinates,
			colliderFaces = this.colliderFaces,
			verts = this.verts,
			UVs = this.UVs,
			lightUV = this.lightUV,
			normals = this.normals,
			tangents = this.tangents,
			normalTris = this.normalTris,
			specularTris = this.specularTris,
			liquidTris = this.liquidTris,
			leavesTris = this.leavesTris,
			iceTris = this.iceTris,
			lavaTris = this.lavaTris,
			decalVerts = this.decalVerts,
			decalNormals = this.decalNormals,
			decalUVs = this.decalUVs,
			decalTris = this.decalTris,
			cacheCubeVert = this.cacheCubeVert,
			cacheCubeUV = this.cacheCubeUV,
			cacheCubeNormal = this.cacheCubeNormal,
			cacheCubeTangent = this.cacheCubeTangent,
			extraUV = this.extraUV,

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
			blockDrawLiquid = BlockEncyclopediaECS.blockDrawLiquid,
			blockHP = BlockEncyclopediaECS.blockHP,
			objectHP = BlockEncyclopediaECS.objectHP
		};
		JobHandle handle = bcJob.Schedule();

		PrepareAssetsJob paJob = new PrepareAssetsJob{
			pos = pos,
			canRain = canRain,

			meshVerts = this.verts,
			meshUVs = this.UVs,
			meshTris = this.assetTris,
			meshSolidTris = this.assetSolidTris,
			meshNormals = this.normals,
			meshTangents = this.tangents,
			meshLightUV = this.lightUV,
			blockVertexCount = this.blockVertexCount,
			hitboxVerts = this.hitboxVerts,
			hitboxNormals = this.hitboxNormals,
			hitboxTriangles = this.hitboxTris,

			coords = this.loadAssetList,
			blocks = this.blocks,
			light = this.light,
			heights = this.heights,
			state = this.state,

			scaling = BlockEncyclopediaECS.objectScaling,
			hitboxScaling = BlockEncyclopediaECS.hitboxScaling,
			needRotation = BlockEncyclopediaECS.objectNeedRotation,
			material = BlockEncyclopediaECS.objectMaterial,

			loadedVerts = ObjectMeshTables.verts,
			loadedUV = ObjectMeshTables.uvs,
			loadedNormals = ObjectMeshTables.normals,
			loadedTangents = ObjectMeshTables.tangents,
			loadedTris = ObjectMeshTables.tris,
			vertsOffset = ObjectMeshTables.vertOffset,
			trisOffset = ObjectMeshTables.trisOffset,
			loadedHitboxVerts = ObjectMeshTables.hitboxVerts,
			loadedHitboxNormals = ObjectMeshTables.hitboxNormals,
			loadedHitboxTriangles = ObjectMeshTables.hitboxTris,
			hitboxVertsOffset = ObjectMeshTables.hitboxVertOffset,
			hitboxTrisOffset = ObjectMeshTables.hitboxTrisOffset,
			stateOffset = ObjectMeshTables.stateOffset,
			stateTranslation = ObjectMeshTables.stateTranslation,
			stateRotation = ObjectMeshTables.stateRotation
		};
		handle = paJob.Schedule(handle);

		GreedyColliderJob gcJob = new GreedyColliderJob{
			pos = pos,
			colliderFaces = this.colliderFaces,
			verts = this.verts,
			assetSolidTris = this.assetSolidTris,
			blockVertexCount = this.blockVertexCount,
			colliderVerts = this.colliderVerts,
			colliderTris = this.colliderTris
		};
		handle = gcJob.Schedule(handle);

		this.meshDataArray = Mesh.AllocateWritableMeshData(WriteChunkMeshJob.MESH_COUNT);
		this.hasMeshData = true;

		WriteChunkMeshJob wcmJob = new WriteChunkMeshJob{
			meshes = this.meshDataArray,
			flags = MESH_FLAGS,
			renderLayout = ChunkMeshLayout.render,
			colliderLayout = ChunkMeshLayout.collider,
			decalLayout = ChunkMeshLayout.decal,
			raycastLayout = ChunkMeshLayout.raycast,

			verts = this.verts,
			UVs = this.UVs,
			lightUV = this.lightUV,
			normals = this.normals,
			tangents = this.tangents,
			normalTris = this.normalTris,
			specularTris = this.specularTris,
			liquidTris = this.liquidTris,
			assetTris = this.assetTris,
			assetSolidTris = this.assetSolidTris,
			leavesTris = this.leavesTris,
			iceTris = this.iceTris,
			lavaTris = this.lavaTris,

			colliderVerts = this.colliderVerts,
			colliderTris = this.colliderTris,

			decalVerts = this.decalVerts,
			decalNormals = this.decalNormals,
			decalUVs = this.decalUVs,
			decalTris = this.decalTris,

			hitboxVerts = this.hitboxVerts,
			hitboxNormals = this.hitboxNormals,
			hitboxTris = this.hitboxTris,

			bounds = this.bounds,
			indexCounts = this.indexCounts
		};
		handle = wcmJob.Schedule(handle);

		// Load events only read the snapshot, so they run alongside the mesh jobs
		if(load){
			CheckLoadEventJob clej = new CheckLoadEventJob{
				blocks = this.blocks,
				blockLoad = BlockEncyclopediaECS.blockLoad,
				objectLoad = BlockEncyclopediaECS.objectLoad,
				loadCoords = this.loadCoords
			};
			handle = JobHandle.CombineDependencies(handle, clej.Schedule());
		}

		this.buildHandle = handle;
		this.slotState = SlotState.BUILDING;
		target.SetBuildSlot(this);
	}

	// Applies the written meshes to the chunk and starts cooking its colliders. Must only be called when IsBuildDone()
	public void ApplyMeshes(){
		this.buildHandle.Complete();

		Mesh[] targets = this.chunk.GetBuildTargets();

		Mesh.ApplyAndDisposeWritableMeshData(this.meshDataArray, targets, MESH_FLAGS);
		this.hasMeshData = false;

		for(int i=0; i < targets.Length; i++){
			targets[i].bounds = this.bounds[i];
		}

		this.chunk.AttachRenderMeshes();

		this.hasCollider = this.indexCounts[WriteChunkMeshJob.COLLIDER_MESH] > 0;
		this.hasRaycast = this.indexCounts[WriteChunkMeshJob.RAYCAST_MESH] > 0;

		this.bakeIDs[0] = this.hasCollider ? targets[WriteChunkMeshJob.COLLIDER_MESH].GetInstanceID() : 0;
		this.bakeIDs[1] = this.hasRaycast ? targets[WriteChunkMeshJob.RAYCAST_MESH].GetInstanceID() : 0;

		BakeColliderJob bcJob = new BakeColliderJob{
			meshIDs = this.bakeIDs,
			cookingOptions = Chunk.COLLIDER_COOKING_OPTIONS
		};

		this.bakeHandle = bcJob.Schedule(this.bakeIDs.Length, 1);
		this.slotState = SlotState.BAKING;
	}

	// Swaps the chunk colliders to the cooked meshes and frees the slot. Must only be called when IsBakeDone()
	public void FinishBake(){
		this.bakeHandle.Complete();
		this.chunk.SwapColliders(this.hasCollider, this.hasRaycast);
		Release();
	}

	// Cancels the current build, waiting for its jobs to finish
	public void Abort(){
		if(this.slotState == SlotState.BUILDING)
			this.buildHandle.Complete();
		else if(this.slotState == SlotState.BAKING)
			this.bakeHandle.Complete();

		if(this.hasMeshData){
			this.meshDataArray.Dispose();
			this.hasMeshData = false;
		}

		Release();
	}

	public void Dispose(){
		if(this.isDisposed)
			return;

		Abort();

		this.blocks.Dispose();
		this.light.Dispose();
		this.heights.Dispose();
		this.state.Dispose();
		this.hp.Dispose();
		this.renderMap.Dispose();

		this.verts.Dispose();
		this.UVs.Dispose();
		this.lightUV.Dispose();
		this.normals.Dispose();
		this.tangents.Dispose();
		this.normalTris.Dispose();
		this.specularTris.Dispose();
		this.liquidTris.Dispose();
		this.leavesTris.Dispose();
		this.iceTris.Dispose();
		this.lavaTris.Dispose();
		this.assetTris.Dispose();
		this.assetSolidTris.Dispose();

		this.decalVerts.Dispose();
		this.decalNormals.Dispose();
		this.decalUVs.Dispose();
		this.decalTris.Dispose();

		this.colliderFaces.Dispose();
		this.blockVertexCount.Dispose();
		this.colliderVerts.Dispose();
		this.colliderTris.Dispose();

		this.hitboxVerts.Dispose();
		this.hitboxNormals.Dispose();
		this.hitboxTris.Dispose();

		this.loadAssetList.Dispose();
		this.assetCoordinates.Dispose();
		this.loadCoords.Dispose();

		this.cacheCubeVert.Dispose();
		this.cacheCubeUV.Dispose();
		this.cacheCubeNormal.Dispose();
		this.cacheCubeTangent.Dispose();
		this.extraUV.Dispose();

		this.bounds.Dispose();
		this.indexCounts.Dispose();
		this.bakeIDs.Dispose();

		this.isDisposed = true;
	}

	private void Release(){
		if(this.chunk != null)
			this.chunk.SetBuildSlot(null);

		this.chunk = null;
		this.slotState = SlotState.FREE;
	}

	private bool HasVerticalLayer(ChunkLoader loader, ChunkPos pos, int direction){
		ChunkPos vertical = new ChunkPos(pos.x, pos.z, pos.y+direction);

		return loader.Contains(vertical) && loader.CanBeDrawn(vertical);
	}

	// Copies the chunk data and the borders of its neighbors into the snapshot buffers
	private void GatherSnapshot(ChunkLoader loader, ChunkPos pos, bool hasTop, bool hasBottom){
		this.chunk.metadata.CopyStateTo(this.state);
		this.chunk.metadata.CopyHPTo(this.hp);
		this.renderMap.CopyFrom(this.chunk.data.GetRenderMap());

		if(!hasTop)
			ClearLayer(Chunk.chunkDepth);
		if(!hasBottom)
			ClearLayer(-1);

		for(int y=-1; y <= 1; y++){
			if(y == 1 && !hasTop)
				continue;
			if(y == -1 && !hasBottom)
				continue;

			for(int x=-1; x <= 1; x++){
				for(int z=-1; z <= 1; z++){
					CopyRegion(loader.Get(new ChunkPos(pos.x+x, pos.z+z, pos.y+y)), new int3(x, y, z));
				}
			}
		}
	}

	private unsafe void CopyRegion(Chunk source, int3 offset){
		ushort[] sourceBlocks = source.data.GetData();
		byte[] sourceLight = source.data.GetLightMap(source.metadata);
		byte[] sourceHeight = source.data.GetHeightMap();

		fixed(ushort* blockPtr = sourceBlocks)
		fixed(byte* lightPtr = sourceLight)
		fixed(byte* heightPtr = sourceHeight){
			CopyPaddedRegionJob job = new CopyPaddedRegionJob{
				srcBlocks = blockPtr,
				srcLight = lightPtr,
				srcHeight = heightPtr,
				offset = offset,
				blocks = this.blocks,
				light = this.light,
				heights = this.heights
			};

			job.Run();
		}
	}

	// Fills a horizontal layer of the snapshot with air and no light
	private void ClearLayer(int y){
		int index;

		for(int x=-1; x <= Chunk.chunkWidth; x++){
			for(int z=-1; z <= Chunk.chunkWidth; z++){
				index = PaddedChunk.Index(x, y, z);
				this.blocks[index] = 0;
				this.light[index] = 0;
			}
		}
	}

	private void ClearOutputs(){
		this.verts.Clear();
		this.UVs.Clear();
		this.lightUV.Clear();
		this.normals.Clear();
		this.tangents.Clear();
		this.normalTris.Clear();
		this.specularTris.Clear();
		this.liquidTris.Clear();
		this.leavesTris.Clear();
		this.iceTris.Clear();
		this.lavaTris.Clear();
		this.assetTris.Clear();
		this.assetSolidTris.Clear();

		this.decalVerts.Clear();
		this.decalNormals.Clear();
		this.decalUVs.Clear();
		this.decalTris.Clear();

		this.colliderFaces.Clear();
		this.colliderVerts.Clear();
		this.colliderTris.Clear();

		this.hitboxVerts.Clear();
		this.hitboxNormals.Clear();
		this.hitboxTris.Clear();

		this.loadAssetList.Clear();
		this.assetCoordinates.Clear();
		this.loadCoords.Clear();
	}
}
