using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

/*
Calculates the shadow and light maps of a chunk in worker threads.
Jobs work on copies of the chunk data, which are written back to the chunk in the main thread once done
*/
public class LightSlot{
	private static readonly int VOXEL_COUNT = Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth;
	private static readonly int MAP_SIZE = Chunk.chunkWidth*Chunk.chunkWidth;
	private static readonly int INITIAL_CAPACITY = 1024;

	public ChunkPos pos;

	private Chunk chunk;
	private bool busy = false;
	private JobHandle handle;
	private bool isDisposed = false;

	// Working copies
	private NativeArray<byte> light;
	private NativeArray<byte> shadow;
	private NativeArray<ushort> blocks;
	private NativeArray<ushort> states;
	private NativeArray<byte> heightMap;
	private NativeArray<bool> ceilingMap;
	private NativeArray<bool> skyFromAbove;

	// Scratch
	private NativeList<int4> lightSources;
	private NativeList<int4> auxLightSources;
	private NativeList<int3> bfsq;
	private NativeList<int4> bfsqExtra;
	private NativeList<byte5> bfsqDir;
	private NativeList<byte5> directionalList;
	private NativeList<byte5> auxDirectionals;
	private NativeBitArray visited;


	public LightSlot(){
		this.light = new NativeArray<byte>(VOXEL_COUNT, Allocator.Persistent);
		this.shadow = new NativeArray<byte>(VOXEL_COUNT, Allocator.Persistent);
		this.blocks = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.states = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent);
		this.heightMap = new NativeArray<byte>(MAP_SIZE, Allocator.Persistent);
		this.ceilingMap = new NativeArray<bool>(MAP_SIZE, Allocator.Persistent);
		this.skyFromAbove = new NativeArray<bool>(MAP_SIZE, Allocator.Persistent);

		this.lightSources = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		this.auxLightSources = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		this.bfsq = new NativeList<int3>(INITIAL_CAPACITY, Allocator.Persistent);
		this.bfsqExtra = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		this.bfsqDir = new NativeList<byte5>(INITIAL_CAPACITY, Allocator.Persistent);
		this.directionalList = new NativeList<byte5>(INITIAL_CAPACITY, Allocator.Persistent);
		this.auxDirectionals = new NativeList<byte5>(INITIAL_CAPACITY, Allocator.Persistent);
		this.visited = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);
	}

	public bool IsBusy(){return this.busy;}
	public bool IsDone(){return this.handle.IsCompleted;}

	public void Complete(){
		this.handle.Complete();
	}

	// Starts calculating the light of a chunk, reading its data now. Returns false if the chunk isn't loaded
	// skyFromAbove holds the columns where direct sunlight enters the top of the chunk
	public bool Start(ChunkPos pos, SunExitMask skyFromAbove, ChunkLoader loader){
		if(!loader.Contains(pos))
			return false;

		Chunk c = loader.Get(pos);
		VoxelData data = c.data;

		data.EnsureHeightMaps();
		skyFromAbove.CopyTo(this.skyFromAbove);

		this.pos = pos;
		this.chunk = c;
		this.busy = true;

		this.blocks.CopyFrom(data.GetData());
		c.metadata.CopyStateTo(this.states);
		this.heightMap.CopyFrom(data.GetHeightMap());
		this.ceilingMap.CopyFrom(data.GetCeilingMap());

		if(data.GetLightMap() != null)
			this.light.CopyFrom(data.GetLightMap());
		else
			Clear(this.light);

		if(data.GetShadowMap() != null)
			this.shadow.CopyFrom(data.GetShadowMap());
		else
			Clear(this.shadow);

		this.lightSources.Clear();
		this.auxLightSources.Clear();
		this.bfsq.Clear();
		this.bfsqExtra.Clear();
		this.bfsqDir.Clear();
		this.directionalList.Clear();
		this.auxDirectionals.Clear();
		this.visited.Clear();

		CalculateShadowMapJob csmJob = new CalculateShadowMapJob{
			shadowMap = this.shadow,
			lightMap = this.light,
			lightSources = this.lightSources,
			heightMap = this.heightMap,
			data = this.blocks,
			states = this.states,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			isTransparentObj = BlockEncyclopediaECS.objectTransparent,
			isTransparentBlock = BlockEncyclopediaECS.blockTransparent,
			blockLuminosity = BlockEncyclopediaECS.blockLuminosity,
			objectLuminosity = BlockEncyclopediaECS.objectLuminosity,
			cpos = pos,
			ceilingMap = this.ceilingMap,
			skyFromAbove = this.skyFromAbove
		};

		CalculateLightMapJob clmJob = new CalculateLightMapJob{
			lightMap = this.light,
			shadowMap = this.shadow,
			lightSources = this.lightSources,
			heightMap = this.heightMap,
			ceilingMap = this.ceilingMap,
			chunkWidth = Chunk.chunkWidth,
			chunkDepth = Chunk.chunkDepth,
			bfsq = this.bfsq,
			bfsqExtra = this.bfsqExtra,
			bfsqDir = this.bfsqDir,
			visited = this.visited,
			directionalList = this.directionalList,
			auxLightSources = this.auxLightSources,
			auxDirectionals = this.auxDirectionals,
			cpos = pos
		};

		this.handle = clmJob.Schedule(csmJob.Schedule());
		return true;
	}

	// Writes the calculated maps back to the chunk. Returns false if the chunk was unloaded in the meantime
	public bool ApplyResults(ChunkLoader loader){
		this.handle.Complete();

		if(this.chunk == null || this.chunk.data == null)
			return false;
		if(!loader.Contains(this.pos) || loader.Get(this.pos) != this.chunk)
			return false;

		VoxelData data = this.chunk.data;

		data.EnsureLightMaps();
		this.light.CopyTo(data.GetLightMap());
		this.shadow.CopyTo(data.GetShadowMap());

		return true;
	}

	public void Release(){
		this.chunk = null;
		this.busy = false;
		this.handle = default(JobHandle);
	}

	public void Dispose(){
		if(this.isDisposed)
			return;

		this.handle.Complete();
		Release();

		this.light.Dispose();
		this.shadow.Dispose();
		this.blocks.Dispose();
		this.states.Dispose();
		this.heightMap.Dispose();
		this.ceilingMap.Dispose();
		this.skyFromAbove.Dispose();

		this.lightSources.Dispose();
		this.auxLightSources.Dispose();
		this.bfsq.Dispose();
		this.bfsqExtra.Dispose();
		this.bfsqDir.Dispose();
		this.directionalList.Dispose();
		this.auxDirectionals.Dispose();
		this.visited.Dispose();

		this.isDisposed = true;
	}

	private static unsafe void Clear(NativeArray<byte> array){
		UnsafeUtility.MemClear(array.GetUnsafePtr(), array.Length);
	}
}
