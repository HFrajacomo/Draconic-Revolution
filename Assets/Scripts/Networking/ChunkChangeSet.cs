using System.Collections.Generic;

/*
Server-side record of the voxels of a loaded chunk that changed since they were last sent to clients
	Every tracked change also increases the version of the chunk, so compressed copies of it (ChunkPayloadCache) know when they are outdated
	Changes made by replacing whole arrays can't be tracked per voxel, so they overflow the record and the whole chunk is sent instead
*/
public class ChunkChangeSet
{
	public readonly ChunkPos pos;

	private int version = 0;
	private HashSet<int> changed; // Voxel indexes, created on the first change
	private bool overflow = false;


	public ChunkChangeSet(ChunkPos pos){
		this.pos = pos;
	}

	public int GetVersion(){return this.version;}
	public bool IsOverflowed(){return this.overflow;}
	public int Count(){return this.changed == null ? 0 : this.changed.Count;}
	public HashSet<int> GetChanges(){return this.changed;}

	// A single voxel changed. Index follows VoxelData: x*chunkWidth*chunkDepth + y*chunkWidth + z
	public void Record(int index){
		this.version++;

		if(this.overflow)
			return;

		if(this.changed == null)
			this.changed = new HashSet<int>();

		if(this.changed.Add(index) && this.changed.Count > Constants.BLOCKBATCH_MAX_CHANGES)
			MarkAll();
	}

	// The chunk changed in a way that can't be tracked per voxel
	public void MarkAll(){
		this.version++;
		this.overflow = true;

		if(this.changed != null)
			this.changed.Clear();
	}

	// Only invalidates compressed copies of the chunk
	public void MarkVersion(){
		this.version++;
	}

	// The voxel was already sent to every client with its current data
	public void Forget(int index){
		if(this.changed != null)
			this.changed.Remove(index);
	}

	// Every change was sent to clients
	public void Clear(){
		this.overflow = false;

		if(this.changed != null)
			this.changed.Clear();
	}

	public static int GetIndex(int x, int y, int z){
		return x*Chunk.chunkWidth*Chunk.chunkDepth + y*Chunk.chunkWidth + z;
	}

	public static void GetCoords(int index, out int x, out int y, out int z){
		x = index / (Chunk.chunkWidth*Chunk.chunkDepth);
		index -= x * Chunk.chunkWidth*Chunk.chunkDepth;
		y = index / Chunk.chunkWidth;
		z = index - y * Chunk.chunkWidth;
	}
}
