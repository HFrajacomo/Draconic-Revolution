using Unity.Mathematics;

/*
Allocation-free voxel access by world coordinates for server-side searches (trees, leaves...)
	Caches the last chunk it used, since searches usually stay inside one or two chunks
	Begin() must be called at the start of every search, so a cached chunk never outlives an unload
*/
public class VoxelReader_Server {
	private ChunkLoader_Server cl;
	private ChunkPos cachedPos;
	private Chunk cachedChunk;

	private static readonly int WORLD_HEIGHT = Chunk.chunkDepth * (Chunk.chunkMaxY + 1);


	public void Begin(ChunkLoader_Server cl){
		this.cl = cl;
		this.cachedChunk = null;
	}

	// Returns false if the voxel is in a chunk that isn't loaded. Voxels above or below the world are air
	public bool TryRead(int3 world, out ushort block, out ushort state){
		Chunk chunk;
		int3 local;

		block = 0;
		state = 0;

		if(world.y < 0 || world.y >= WORLD_HEIGHT)
			return true;

		if(!TryGetChunk(world, out chunk, out local))
			return false;

		block = chunk.data.GetCell(local);
		state = chunk.metadata.GetState(local);
		return true;
	}

	// Turns a voxel into air. Returns false if its chunk isn't loaded
	public bool Clear(int3 world, out ChunkPos pos){
		Chunk chunk;
		int3 local;

		pos = default(ChunkPos);

		if(world.y < 0 || world.y >= WORLD_HEIGHT)
			return false;

		if(!TryGetChunk(world, out chunk, out local))
			return false;

		chunk.data.SetCell(local.x, local.y, local.z, 0);
		chunk.metadata.Reset(local.x, local.y, local.z);
		pos = this.cachedPos;
		return true;
	}

	public static int3 ToWorld(ChunkPos pos, int x, int y, int z){
		return new int3(pos.x*Chunk.chunkWidth + x, pos.y*Chunk.chunkDepth + y, pos.z*Chunk.chunkWidth + z);
	}

	// Splits a world coordinate inside the world's height into its chunk and the voxel inside it
	public static ChunkPos ToChunkPos(int3 world, out int3 local){
		int chunkX = FloorDiv(world.x, Chunk.chunkWidth);
		int chunkZ = FloorDiv(world.z, Chunk.chunkWidth);
		int chunkY = world.y / Chunk.chunkDepth;

		local = new int3(world.x - chunkX*Chunk.chunkWidth, world.y - chunkY*Chunk.chunkDepth, world.z - chunkZ*Chunk.chunkWidth);
		return new ChunkPos(chunkX, chunkZ, chunkY);
	}

	private bool TryGetChunk(int3 world, out Chunk chunk, out int3 local){
		ChunkPos pos = ToChunkPos(world, out local);

		if(this.cachedChunk != null && this.cachedPos == pos){
			chunk = this.cachedChunk;
			return true;
		}

		if(!this.cl.TryGetChunk(pos, out chunk))
			return false;

		this.cachedPos = pos;
		this.cachedChunk = chunk;
		return true;
	}

	private static int FloorDiv(int a, int b){
		int q = a / b;

		if((a % b != 0) && ((a < 0) != (b < 0)))
			q--;

		return q;
	}
}
