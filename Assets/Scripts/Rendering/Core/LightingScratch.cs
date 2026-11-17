using Unity.Collections;
using Unity.Mathematics;

/*
Reusable native buffers of the Voxel Lighting jobs that run synchronously in the main thread
(light calculation of updated chunks, verification and light propagation).
Asynchronous light calculations use the buffers of each LightSlot instead
*/
public static class LightingScratch{
	private static bool isCreated = false;
	private static readonly int VOXEL_COUNT = Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth;
	private static readonly int INITIAL_CAPACITY = 1024;

	// CalculateLightMap
	public static NativeList<int4> lightSources;
	public static NativeList<int4> auxLightSources;
	public static NativeList<int3> bfsq;
	public static NativeList<int4> bfsqExtra;
	public static NativeList<byte5> bfsqDir;
	public static NativeList<byte5> directionalList;
	public static NativeList<byte5> auxDirectionals;
	public static NativeBitArray visited;

	// PropagateLight
	public static NativeList<int3> bfsq1;
	public static NativeList<int3> bfsq2;
	public static NativeList<int3> bfsqe1;
	public static NativeList<int3> bfsqe2;
	public static NativeBitArray visited1;
	public static NativeBitArray visited2;
	public static NativeBitArray visitede1;
	public static NativeBitArray visitede2;
	public static NativeList<int4> aux;
	public static NativeParallelHashSet<int4> hashAux;

	// Shared
	public static NativeArray<ushort> states; // States of the chunk being lit, so uniform states don't need an array
	public static NativeArray<bool> skyFromAbove; // Columns where direct sunlight enters the chunk being lit
	public static NativeArray<byte> changed;
	public static NativeArray<byte> emptyByte;


	public static void Init(){
		if(isCreated)
			return;

		lightSources = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		auxLightSources = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		bfsq = new NativeList<int3>(INITIAL_CAPACITY, Allocator.Persistent);
		bfsqExtra = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		bfsqDir = new NativeList<byte5>(INITIAL_CAPACITY, Allocator.Persistent);
		directionalList = new NativeList<byte5>(INITIAL_CAPACITY, Allocator.Persistent);
		auxDirectionals = new NativeList<byte5>(INITIAL_CAPACITY, Allocator.Persistent);
		visited = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);

		bfsq1 = new NativeList<int3>(INITIAL_CAPACITY, Allocator.Persistent);
		bfsq2 = new NativeList<int3>(INITIAL_CAPACITY, Allocator.Persistent);
		bfsqe1 = new NativeList<int3>(INITIAL_CAPACITY, Allocator.Persistent);
		bfsqe2 = new NativeList<int3>(INITIAL_CAPACITY, Allocator.Persistent);
		visited1 = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);
		visited2 = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);
		visitede1 = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);
		visitede2 = new NativeBitArray(VOXEL_COUNT, Allocator.Persistent);
		aux = new NativeList<int4>(INITIAL_CAPACITY, Allocator.Persistent);
		hashAux = new NativeParallelHashSet<int4>(INITIAL_CAPACITY, Allocator.Persistent);

		states = new NativeArray<ushort>(VOXEL_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		skyFromAbove = new NativeArray<bool>(Chunk.chunkWidth*Chunk.chunkWidth, Allocator.Persistent);
		changed = new NativeArray<byte>(4, Allocator.Persistent);
		emptyByte = new NativeArray<byte>(0, Allocator.Persistent);

		isCreated = true;
	}

	// Resets the buffers used by CalculateLightMap
	public static void PrepareLightMap(){
		Init();

		lightSources.Clear();
		auxLightSources.Clear();
		bfsq.Clear();
		bfsqExtra.Clear();
		bfsqDir.Clear();
		directionalList.Clear();
		auxDirectionals.Clear();
		visited.Clear();
	}

	// Resets the buffers used by PropagateLight
	public static void PreparePropagation(){
		Init();

		bfsq1.Clear();
		bfsq2.Clear();
		bfsqe1.Clear();
		bfsqe2.Clear();
		visited1.Clear();
		visited2.Clear();
		visitede1.Clear();
		visitede2.Clear();
		aux.Clear();
		hashAux.Clear();
		ClearChanged();
	}

	// Resets the buffers used by VerifyPropagation
	public static void PrepareVerification(){
		Init();
		ClearChanged();
	}

	public static void Destroy(){
		if(!isCreated)
			return;

		lightSources.Dispose();
		auxLightSources.Dispose();
		bfsq.Dispose();
		bfsqExtra.Dispose();
		bfsqDir.Dispose();
		directionalList.Dispose();
		auxDirectionals.Dispose();
		visited.Dispose();

		bfsq1.Dispose();
		bfsq2.Dispose();
		bfsqe1.Dispose();
		bfsqe2.Dispose();
		visited1.Dispose();
		visited2.Dispose();
		visitede1.Dispose();
		visitede2.Dispose();
		aux.Dispose();
		hashAux.Dispose();

		states.Dispose();
		skyFromAbove.Dispose();
		changed.Dispose();
		emptyByte.Dispose();

		isCreated = false;
	}

	private static void ClearChanged(){
		for(int i=0; i < changed.Length; i++)
			changed[i] = 0;
	}
}
