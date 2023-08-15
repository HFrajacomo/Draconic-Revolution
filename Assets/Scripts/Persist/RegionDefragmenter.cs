using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class RegionDefragmenter {
	// File data information
	private RegionFile region;
	private long totalSize;
	private long newSize;

	// Streams
	private Stream defragRegionFile;
	private Stream defragIndexFile;
	private Stream defragHoleFile;

	// String Constants
	private static readonly string REGION_DEFAULT_NAME = "DEFRAGMENTED_REGION";
	private static readonly string INDEX_DEFAULT_NAME = "DEFRAGMENTED_INDEX";

	// Directories
	private string worldDir;

	// Buffers
	private static byte[] BUFFER; // Exaggerated buffer size (~0.75MB)
	private static byte[] INDEX_ARRAY;

	// Index information
	private Dictionary<ulong, ulong> newIndex = new Dictionary<ulong, ulong>();
	private long currentFreeIndex = 0;
	
	// Static members
	private static readonly string FORMAT = ".rdf";
	private static readonly ChunkPos CACHED_POS = new ChunkPos(0,0,0);

	public RegionDefragmenter(string name, string worldName, string worldDir){
		if(BUFFER == null){
			BUFFER = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 12];
			INDEX_ARRAY = new byte[16];
		}

		this.worldDir = worldDir;
		this.region = new RegionFile(name, FORMAT, CACHED_POS, Constants.CHUNKS_IN_REGION_FILE, worldName:worldName, isDefrag:true);
	}

	// Defragments and returns an int2 representing (totalSize, newSize)
	public void Defragment(){
		this.totalSize = this.region.GetFileSize();

		// If region is already defragged
		if(this.region.fragHandler.IsDefragged()){
			this.newSize = this.totalSize;
			return;
		}

		// Open new temp files
		defragRegionFile = File.Open(this.worldDir + REGION_DEFAULT_NAME, FileMode.Create);
		defragIndexFile = File.Open(this.worldDir + INDEX_DEFAULT_NAME, FileMode.Create);

		// Load and Save chunk data into new files
		foreach(long key in this.region.index.Keys){
			SaveChunk(LoadChunk(this.region.index[key]), key);
		}

		this.region.CloseWithoutSaving();

		// Remakes the .HLE file
		this.region.fragHandler = new FragmentationHandler(false);
		defragHoleFile = File.Open(this.worldDir + this.region.name + ".hle", FileMode.Create);
		this.region.SaveHolesToFile(defragHoleFile);

		// Moves temp files to main
		File.Move(this.worldDir + REGION_DEFAULT_NAME, this.worldDir + this.region.name + FORMAT);
		File.Move(this.worldDir + INDEX_DEFAULT_NAME, this.worldDir + this.region.name + ".ind");

		this.newSize = CalculateNewRegionSize();

		defragRegionFile.Close();
		defragIndexFile.Close();
		defragHoleFile.Close();
	}

	public long GetDefragSize(){return this.newSize;}
	public long GetPreviousSize(){return this.totalSize;}

	// Should NOT be used when file is temporary
	private long CalculateNewRegionSize(){
		FileInfo info = new FileInfo(this.worldDir + this.region.name + FORMAT);

		if(info.Exists)
			return info.Length;
		return 0;
	} 

	// Loads a chunk positioned in memory as a byte array and returns the total size of the chunk
	private int LoadChunk(long initialPosition){
		int chunkCompressedSize;

		this.region.ReadHeader(initialPosition, BUFFER);
		chunkCompressedSize = GetChunkDataSize();
		this.region.Read(initialPosition+RegionFileHandler.chunkHeaderSize, BUFFER, RegionFileHandler.chunkHeaderSize, chunkCompressedSize);

		return chunkCompressedSize + RegionFileHandler.chunkHeaderSize;
	}

	// Saves RegionFile and IndexFile entries for a chunk
	private void SaveChunk(int totalSize, long chunkCode){
		defragRegionFile.Write(BUFFER, 0, totalSize);

		this.currentFreeIndex += totalSize;
		NetDecoder.WriteLong(chunkCode, INDEX_ARRAY, 0);
		NetDecoder.WriteLong(this.currentFreeIndex, INDEX_ARRAY, 8);

		defragIndexFile.Write(INDEX_ARRAY, 0, 16);
	}

	// Interprets header data and returns the total size of the compressed chunk data
	private int GetChunkDataSize(){
		int blockdata, hpdata, statedata;

		blockdata = BUFFER[9];
		blockdata = blockdata << 8;
		blockdata += BUFFER[10];
		blockdata = blockdata << 8;
		blockdata += BUFFER[11];
		blockdata = blockdata << 8;
		blockdata += BUFFER[12];

		hpdata = BUFFER[13];
		hpdata = hpdata << 8;
		hpdata += BUFFER[14];
		hpdata = hpdata << 8;
		hpdata += BUFFER[15];
		hpdata = hpdata << 8;
		hpdata += BUFFER[16];

		statedata = BUFFER[17];
		statedata = statedata << 8;
		statedata += BUFFER[18];
		statedata = statedata << 8;
		statedata += BUFFER[19];
		statedata = statedata << 8;
		statedata += BUFFER[20];

		return blockdata + hpdata + statedata;
	}
}