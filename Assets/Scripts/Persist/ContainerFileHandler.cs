using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/*
Saves the items inside container voxels (chests and other VX_ContainerBehaviour voxels)

Uses the same Region logic as chunks and entities: every Region (32x32 chunks of a depth layer) has its own files
	Container Data File (.cdf): entries of [Size 4][InvType 1][Slot]... with slots written by InventorySlotCodec
	Container Index File (.cind): pairs of [Key 8][Position 8]
	Container Hole File (.chle): free spaces of the .cdf file (see FragmentationHandler)

The key of a container is ((localChunkZ * 32 + localChunkX) << 16) | voxelIndex, so every voxel of the Region has its own entry
and can be found with its ChunkPos and its position inside the chunk
*/
public class ContainerFileHandler{
	private static readonly string DATA_FORMAT = ".cdf";
	private static readonly string INDEX_FORMAT = ".cind";
	private static readonly string HOLE_FORMAT = ".chle";
	private static readonly int HEADER_SIZE = 4;

	// Region Pool
	private Dictionary<ChunkPos, RegionFile> pool = new Dictionary<ChunkPos, RegionFile>();

	// Cache
	private byte[] intBuffer = new byte[4];
	private byte[] indexArray = new byte[16];


	public ContainerFileHandler(){
		string worldDir = EnvironmentVariablesCentral.saveDir + World.worldName + "\\";

		if(!Directory.Exists(worldDir))
			Directory.CreateDirectory(worldDir);
	}

	// Whether the container has a saved entry
	public bool Contains(ContainerKey key){
		GetCorrectRegion(key.pos);
		return this.pool[ConvertToRegion(key.pos)].index.ContainsKey(GetCode(key));
	}

	// Reads the saved entry of a container into buffer. Returns false if it has none
	public bool TryLoad(ContainerKey key, byte[] buffer, out int size){
		RegionFile region;
		long position;

		size = 0;
		GetCorrectRegion(key.pos);
		region = this.pool[ConvertToRegion(key.pos)];

		if(!region.index.TryGetValue(GetCode(key), out position))
			return false;

		region.Read(position, this.intBuffer, 0, HEADER_SIZE);
		size = NetDecoder.ReadInt(this.intBuffer, 0);

		if(size <= 0 || size > buffer.Length){
			Debug.LogWarning($"[ContainerFileHandler] Container {key} has an invalid entry size: {size}");
			size = 0;
			return false;
		}

		region.Read(position + HEADER_SIZE, buffer, 0, size);
		return true;
	}

	// Writes the entry of a container, reusing its previous space if it still fits
	public void Save(ContainerKey key, byte[] data, int size){
		RegionFile region;
		long code = GetCode(key);
		long previousPosition, position;
		int previousSize;

		GetCorrectRegion(key.pos);
		region = this.pool[ConvertToRegion(key.pos)];

		// If container was already saved
		if(region.index.TryGetValue(code, out previousPosition)){
			region.Read(previousPosition, this.intBuffer, 0, HEADER_SIZE);
			previousSize = NetDecoder.ReadInt(this.intBuffer, 0);

			region.AddHole(previousPosition, previousSize + HEADER_SIZE);
			position = region.FindPosition(size + HEADER_SIZE);
			region.SaveHoles();

			// If position in the file has changed
			if(position != previousPosition){
				region.index[code] = position;
				region.UnloadIndex();
			}
		}
		// If container is new
		else{
			position = region.FindPosition(size + HEADER_SIZE);
			region.SaveHoles();

			region.index.Add(code, position);
			WriteIndexEntry(code, position);
			region.indexFile.Seek(0, SeekOrigin.End);
			region.indexFile.Write(this.indexArray, 0, 16);
			region.indexFile.Flush();
		}

		NetDecoder.WriteInt(size, this.intBuffer, 0);
		region.Write(position, this.intBuffer, HEADER_SIZE);
		region.Write(position + HEADER_SIZE, data, size);
	}

	// Removes the entry of a container, turning its space into a hole
	public void Delete(ContainerKey key){
		RegionFile region;
		long code = GetCode(key);
		long position;
		int size;

		GetCorrectRegion(key.pos);
		region = this.pool[ConvertToRegion(key.pos)];

		if(!region.index.TryGetValue(code, out position))
			return;

		region.Read(position, this.intBuffer, 0, HEADER_SIZE);
		size = NetDecoder.ReadInt(this.intBuffer, 0);

		region.AddHole(position, size + HEADER_SIZE);
		region.SaveHoles();

		region.index.Remove(code);
		region.UnloadIndex();
	}

	// Closes all streams in pool
	public void CloseAll(){
		foreach(ChunkPos pos in this.pool.Keys){
			this.pool[pos].Close();
		}

		this.pool.Clear();
	}

	// Loads the RegionFile of a chunk if it isn't in the pool
	private void GetCorrectRegion(ChunkPos pos){
		ChunkPos regionPos = ConvertToRegion(pos);
		string name;

		if(this.pool.ContainsKey(regionPos))
			return;

		if(this.pool.Count >= Constants.MAXIMUM_REGION_FILE_POOL)
			FreePool(regionPos);

		name = "c" + regionPos.x.ToString() + "x" + regionPos.z.ToString() + "_" + pos.y;
		this.pool.Add(regionPos, new RegionFile(name, DATA_FORMAT, regionPos, Constants.CHUNKS_IN_REGION_FILE, indexFormat:INDEX_FORMAT, holeFormat:HOLE_FORMAT));
	}

	// Takes a RegionFile away from the pool, preferring the ones far from the region being loaded
	private void FreePool(ChunkPos regionPos){
		ChunkPos toRemove = regionPos;
		bool found = false;

		foreach(ChunkPos p in this.pool.Keys){
			toRemove = p;
			found = true;

			if(Mathf.Abs(p.x - regionPos.x) + Mathf.Abs(p.z - regionPos.z) >= 2)
				break;
		}

		if(!found)
			return;

		this.pool[toRemove].Close();
		this.pool.Remove(toRemove);
	}

	// Translates a ChunkPos to the position of its Region
	private ChunkPos ConvertToRegion(ChunkPos pos){
		return new ChunkPos(Mathf.FloorToInt(pos.x / Constants.CHUNKS_IN_REGION_FILE), Mathf.FloorToInt(pos.z / Constants.CHUNKS_IN_REGION_FILE), pos.y);
	}

	// Key of a container inside its Region
	private long GetCode(ContainerKey key){
		ChunkPos regionPos = ConvertToRegion(key.pos);
		int regionSize = (int)Constants.CHUNKS_IN_REGION_FILE;
		long localX = key.pos.x - regionPos.x * regionSize;
		long localZ = key.pos.z - regionPos.z * regionSize;

		return ((localZ * regionSize + localX) << 16) | (long)key.GetVoxelIndex();
	}

	private void WriteIndexEntry(long key, long val){
		NetDecoder.WriteLong(key, this.indexArray, 0);
		NetDecoder.WriteLong(val, this.indexArray, 8);
	}
}
