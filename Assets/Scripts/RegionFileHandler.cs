using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Region Data File Format (.rdf)

| RDF File                                                                      |
| Chunk Header (21 bytes)  || Chunk Data (ChunkDimensions*8 bytes)                |
-> | Biome (1 byte) LastDay(4 bytes) LastHour(1 byte) LastMinute(1 byte) LastTick(1 byte) NeedGeneration (1 byte) |     -> | BlockData  (ChunkDimensions*4 bytes) || Metadata (ChunkDimensions*4 bytes) | 
-> | BlockDataSize (4 bytes) | HPDataSize (4 bytes) | StateDataSize (4 bytes)
*/

public class RegionFileHandler{
	private RegionFile region;
	private int seed;
	private int renderDistance;
	private static float chunkLength = 32f;
	public TimeOfDay globalTime;


	// Cache Information
	private byte[] nullMetadata = new byte[]{255,255};
	/*
	private ushort[,,] cachedData = new ushort[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	private Metadata[,,] cachedMetadata = new Metadata[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	private byte[] intArray = new byte[4];
	private byte[] ushortArray = new byte[2];
	*/

	private byte[] byteArray = new byte[1];
	private byte[] timeArray = new byte[7];
	private byte[] indexArray = new byte[16];
	private byte[] headerBuffer = new byte[21];
	private byte[] blockBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer (roughly 0,1 MB)
	private byte[] hpBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 2]; // Exagerated buffer (roughly 0,05 MB)
	private byte[] stateBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 2]; // Exagerated buffer (roughly 0,05 MB)

	// Sizes
	private static int chunkHeaderSize = 21; // Size (in bytes) of header
	private static int chunkSize = Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 8; // Size in bytes of Chunk payload

	// Builds the file handler and loads it on the ChunkPos of the first player
	public RegionFileHandler(int seed, int renderDistance, ChunkPos pos){
		this.seed = seed;
		this.renderDistance = renderDistance;
		this.globalTime = GameObject.Find("/Time Counter").GetComponent<TimeOfDay>();

		LoadRegionFile(pos);
	}

	// Checks if RegionFile represents ChunkPos, and loads correct RegionFile if not
	public void GetCorrectRegion(ChunkPos pos){
		if(!region.CheckUsage(pos)){
			this.region.Close();
			LoadRegionFile(pos);
		}
	}

	// Getter for RegionFile
	public RegionFile GetFile(){
		return this.region;
	}

	// Loads RegionFile related to given Chunk
	public void LoadRegionFile(ChunkPos pos){
		int rfx;
		int rfz;
		string name;

		rfx = Mathf.FloorToInt(pos.x / RegionFileHandler.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / RegionFileHandler.chunkLength);
		name = "r" + rfx.ToString() + "x" + rfz.ToString();

		region = new RegionFile(name, new ChunkPos(rfx, rfz), RegionFileHandler.chunkLength);
	}

	// New Load algorithm with Compression
	public void LoadChunk(Chunk c){
		byte biome=0;
		byte gen=0;
		int blockdata=0;
		int hpdata=0;
		int statedata=0;

		// If chunk is loaded in current RegionFile
		if(!region.CheckUsage(c.pos)){
			LoadRegionFile(c.pos);
		}

		ReadHeader(c.pos);
		InterpretHeader(ref biome, ref gen, ref blockdata, ref hpdata, ref statedata);
		
		c.biomeName = BiomeHandler.ByteToBiome(biome);
		c.lastVisitedTime = globalTime.DateBytes(timeArray);
		c.needsGeneration = gen;

		this.region.file.Read(blockBuffer, 0, blockdata);
		this.region.file.Read(hpBuffer, 0, hpdata);
		this.region.file.Read(stateBuffer, 0, statedata);

		Compression.DecompressBlocks(c, blockBuffer);
		Compression.DecompressMetadataHP(c, hpBuffer);
		Compression.DecompressMetadataState(c, stateBuffer);

	}

	// Saves a chunk to RDF file using Pallete-based Compression
	public void SaveChunk(Chunk c){
		int totalSize = 0;
		long seekPosition = 0;
		int blockSize;
		int hpSize;
		int stateSize;
		long chunkCode = GetLinearRegionCoords(c.pos);

		// Loads correct Region File
		if(!region.CheckUsage(c.pos)){
			LoadRegionFile(c.pos);
		}


		// Saves data to buffers and gets total size
		blockSize = Compression.CompressBlocks(c, blockBuffer);
		hpSize = Compression.CompressMetadataHP(c, hpBuffer);
		stateSize = Compression.CompressMetadataState(c, stateBuffer);

		InitializeHeader(c, blockSize, hpSize, stateSize);

		totalSize = chunkHeaderSize + blockSize + hpSize + stateSize; // REMINDER: Add header size too

		// If Chunk was already saved
		if(region.IsIndexed(c.pos)){
			region.AddHole(region.index[chunkCode], totalSize);
			seekPosition = region.FindPosition(totalSize);

			// If position in RegionFile has changed
			if(seekPosition != region.index[chunkCode]){
				region.index[chunkCode] = seekPosition;
				region.UnloadIndex();
			}

			// Saves Chunk
			region.Write(seekPosition, headerBuffer, chunkHeaderSize);
			region.Write(seekPosition+chunkHeaderSize, blockBuffer, blockSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize, hpBuffer, hpSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize+hpSize, stateBuffer, stateSize);
		}
		// If it's a new Chunk
		else{
			seekPosition = region.FindPosition(totalSize);

			// Adds new chunk to Index
			region.index.Add(chunkCode, seekPosition);
			AddEntryIndex(chunkCode, seekPosition);
			region.indexFile.Write(indexArray, 0, 16);

			// Saves Chunk
			region.Write(seekPosition, headerBuffer, chunkHeaderSize);
			region.Write(seekPosition+chunkHeaderSize, blockBuffer, blockSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize, hpBuffer, hpSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize+hpSize, stateBuffer, stateSize);
		

		}

	}

	// Reads header from a chunk
	// leaves rdf file at Seek Position ready to read blockdata
	private void ReadHeader(ChunkPos pos){
		long code = GetLinearRegionCoords(pos);

		this.region.file.Seek(this.region.index[code], SeekOrigin.Begin);
		this.region.file.Read(headerBuffer, 0, chunkHeaderSize);
	}

	// Interprets header data into ref variables
	private void InterpretHeader(ref byte biome, ref byte gen, ref int blockdata, ref int hpdata, ref int statedata){
		biome = headerBuffer[0];

		timeArray[0] = headerBuffer[1];
		timeArray[1] = headerBuffer[2];
		timeArray[2] = headerBuffer[3];
		timeArray[3] = headerBuffer[4];
		timeArray[4] = headerBuffer[5];
		timeArray[5] = headerBuffer[6];
		timeArray[6] = headerBuffer[7];

		gen = headerBuffer[8];

		blockdata = headerBuffer[9];
		blockdata = blockdata << 8;
		blockdata += headerBuffer[10];
		blockdata = blockdata << 8;
		blockdata += headerBuffer[11];
		blockdata = blockdata << 8;
		blockdata += headerBuffer[12];

		hpdata = headerBuffer[13];
		hpdata = hpdata << 8;
		hpdata += headerBuffer[14];
		hpdata = hpdata << 8;
		hpdata += headerBuffer[15];
		hpdata = hpdata << 8;
		hpdata += headerBuffer[16];

		statedata = headerBuffer[17];
		statedata = statedata << 8;
		statedata += headerBuffer[18];
		statedata = statedata << 8;
		statedata += headerBuffer[19];
		statedata = statedata << 8;
		statedata += headerBuffer[20];
	}

	// Writes Chunk Header to headerBuffer
	private void InitializeHeader(Chunk c, int blockSize, int hpSize, int stateSize){
		timeArray = globalTime.TimeHeader();

		headerBuffer[0] = BiomeHandler.BiomeToByte(c.biomeName);		

		for(int i=0; i<7; i++){
			headerBuffer[i+1] = timeArray[i];
		}

		headerBuffer[8] = c.needsGeneration;

		headerBuffer[9] = (byte)(blockSize >> 24);
		headerBuffer[10] = (byte)(blockSize >> 16);
		headerBuffer[11] = (byte)(blockSize >> 8);
		headerBuffer[12] = (byte)(blockSize);

		headerBuffer[13] = (byte)(hpSize >> 24);
		headerBuffer[14] = (byte)(hpSize >> 16);
		headerBuffer[15] = (byte)(hpSize >> 8);
		headerBuffer[16] = (byte)(hpSize);

		headerBuffer[17] = (byte)(stateSize >> 24);
		headerBuffer[18] = (byte)(stateSize >> 16);
		headerBuffer[19] = (byte)(stateSize >> 8);
		headerBuffer[20] = (byte)(stateSize);
	}

	// Quick Saves a new entry to index file
	private void AddEntryIndex(long key, long val){
		indexArray[0] = (byte)(key >> 56);
		indexArray[1] = (byte)(key >> 48);
		indexArray[2] = (byte)(key >> 40);
		indexArray[3] = (byte)(key >> 32);
		indexArray[4] = (byte)(key >> 24);
		indexArray[5] = (byte)(key >> 16);
		indexArray[6] = (byte)(key >> 8);
		indexArray[7] = (byte)(key);
		indexArray[8] = (byte)(val >> 56);
		indexArray[9] = (byte)(val >> 48);
		indexArray[10] = (byte)(val >> 40);
		indexArray[11] = (byte)(val >> 32);
		indexArray[12] = (byte)(val >> 24);
		indexArray[13] = (byte)(val >> 16);
		indexArray[14] = (byte)(val >> 8);
		indexArray[15] = (byte)(val);
	}

	/*
	// Loads the Chunk Data to Chunk
	public void LoadChunk(Chunk c){
		long code = GetLinearRegionCoords(c.pos);
		long chunkOffset = this.file.index[code];
		long offset = fileHeader + (chunkHeaderSize + chunkSize)*chunkOffset;
		ushort? hp;
		ushort? state;

		this.file.Seek(offset, SeekOrigin.Begin);

		// Reads Header
		file.file.Read(byteArray, 0, 1);
		c.biomeName = BiomeHandler.ByteToBiome(byteArray[0]);
		file.file.Read(timeArray, 0, 7);
		c.lastVisitedTime = globalTime.DateBytes(timeArray);
		file.file.Read(byteArray, 0, 1);
		c.needsGeneration = byteArray[0];

		// Reads VoxelData
		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int z=0; z < Chunk.chunkWidth; z++){
					cachedData[x,y,z] = this.file.ReadBlock(ushortArray);
				}
			}
		}
		c.BuildOnVoxelData(new VoxelData(cachedData));

		// Reads VoxelMetadata
		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int z=0; z < Chunk.chunkWidth; z++){
					hp = this.file.ReadUshort(ushortArray);
					state = this.file.ReadUshort(ushortArray);

					// No Metadata
					if(hp == null && state == null){
						cachedMetadata[x,y,z] = new Metadata(); 
					}
					// Only State
					else if(hp == null){
						cachedMetadata[x,y,z] = new Metadata((ushort)state);
					}
					// Only HP
					else if(state == null){
						cachedMetadata[x,y,z] = new Metadata();	
						cachedMetadata[x,y,z].SetHP(hp);	
					}
					// All metadata
					else{
						cachedMetadata[x,y,z] = new Metadata((ushort)hp, (ushort)state);
					}
				}
			}
		}
		c.BuildVoxelMetadata(new VoxelMetadata(cachedMetadata));

	}
	*/

	// Saves chunk data to RegionFile
	/*
	NeedsGeneration in Chunks is a future implementation of Structures pre-generating in chunks
	0: No needs to generate chunk
	1: Needs to generate chunk, because it was just pre-generated
	*/
	/*
	public void SaveChunk(Chunk c){
		// If chunk is loaded in current RegionFile
		if(!file.CheckUsage(c.pos)){
			LoadRegionFile(c.pos);
		}

		long chunkCode = GetLinearRegionCoords(c.pos);
		ushort entriesInIndex;
		
		// If is a newly generated chunk
		if(!file.index.ContainsKey(chunkCode)){
			file.Seek(0, SeekOrigin.End);

			// Chunk Header
			file.Write(BiomeHandler.BiomeToByte(c.biomeName), 0, 1);
			file.Write(globalTime.TimeHeader(), 0, 7);
			file.Write(new byte[]{c.needsGeneration}, 0, 1);

			// Block Data
			for(int x=0; x < Chunk.chunkWidth; x++){
				for(int y=0; y < Chunk.chunkDepth; y++){
					for(int z=0; z < Chunk.chunkWidth; z++){
						file.Write(IntToByte(c.data.GetCell(x,y,z)), 0, 4);
					}
				}
			}
			// HP and State
			for(int x=0; x < Chunk.chunkWidth; x++){
				for(int y=0; y < Chunk.chunkDepth; y++){
					for(int z=0; z < Chunk.chunkWidth; z++){

						if(c.metadata.metadata[x,y,z] == null){
							file.Write(nullMetadata, 0, 2);
							file.Write(nullMetadata, 0, 2);							
						}
						else{
							file.Write(UshortToByte(c.metadata.metadata[x,y,z].hp), 0, 2);
							file.Write(UshortToByte(c.metadata.metadata[x,y,z].state), 0, 2);
						}
					}
				}
			}

			entriesInIndex = file.IncrementSaved();
			file.WriteEntry(chunkCode, entriesInIndex);
			file.index.Add(chunkCode, entriesInIndex);
			file.Seek(0, SeekOrigin.End);
		}

		// If chunk is a pre-generated chunk
		else{
			file.Seek(fileHeader + chunkHeaderSize + (chunkSize * file.index[chunkCode]), SeekOrigin.Begin);

			// Chunk Header
			file.Write(LongToByte(chunkCode), 0, 8);
			file.Write(new byte[]{c.needsGeneration}, 0, 1);

			// Block Data
			for(int x=0; x < Chunk.chunkWidth; x++){
				for(int y=0; y < Chunk.chunkDepth; y++){
					for(int z=0; z < Chunk.chunkWidth; z++){
						file.Write(IntToByte(c.data.GetCell(x,y,z)), 0, 4);
					}
				}
			}
			// HP and State
			for(int x=0; x < Chunk.chunkWidth; x++){
				for(int y=0; y < Chunk.chunkDepth; y++){
					for(int z=0; z < Chunk.chunkWidth; z++){

						if(c.metadata.metadata[x,y,z] == null){
							file.Write(nullMetadata, 0, 2);
							file.Write(nullMetadata, 0, 2);							
						}
						else{
							file.Write(UshortToByte(c.metadata.metadata[x,y,z].hp), 0, 2);
							file.Write(UshortToByte(c.metadata.metadata[x,y,z].state), 0, 2);
						}
					}
				}
			}

			file.Seek(0, SeekOrigin.End);
		}
	}
	*/

	// Gets NeedGeneration byte from Chunk
	public bool GetsNeedGeneration(ChunkPos pos){

		this.region.file.Seek(region.index[GetLinearRegionCoords(pos)] + 8, SeekOrigin.Begin);
		this.region.file.Read(byteArray, 0, 1);

		if(byteArray[0] == 0)
			return false;

		return true;
	}


	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*chunkLength + pos.x);
	}

}

public struct RegionFile{
	public string name;
	public ChunkPos regionPos; // Variable to represent Region coordinates, and not Chunk coordinates
	private float chunkLength;

	// Minimun and Maximum valued Chunks that are contained in this RegionFile
	private ChunkPos minPos;
	private ChunkPos maxPos;

	// File Data
	public Stream file;
	public Stream indexFile;
	public Dictionary<long, long> index;
	public FragmentationHandler fragHandler;

	// Cached Data
	private byte[] cachedIndex;

	// Opens the file and adds ".rdf" at the end (Region Data File)
	public RegionFile(string name, ChunkPos pos, float chunkLen){
		this.name = name;
		this.regionPos = pos;
		this.chunkLength = chunkLen;
		this.index = new Dictionary<long, long>();

		this.cachedIndex = new byte[16384];
		this.fragHandler = new FragmentationHandler();

		// Calculates min and max pos
		this.minPos = new ChunkPos(regionPos.x*32, regionPos.z*32);
		this.maxPos = new ChunkPos(regionPos.x*32+31, regionPos.z*32+31);

		try{
			this.file = File.Open(name + ".rdf", FileMode.Open);
		} 
		catch (FileNotFoundException){
			this.file = File.Open(name + ".rdf", FileMode.Create);
		}

		try{
			this.indexFile = File.Open(name + ".ind", FileMode.Open);
		} 
		catch (FileNotFoundException){
			this.indexFile = File.Open(name + ".ind", FileMode.Create);
		}

		this.indexFile.Seek(0, SeekOrigin.End);
		//LoadIndex();	
	}

	// Checks if current chunk should be housed in current RegionFile
	public bool CheckUsage(ChunkPos pos){
		if(pos.x >= this.minPos.x && pos.x <= this.maxPos.x){
			if(pos.z >= this.minPos.z && pos.z <= this.maxPos.z){
				return true;
			}
		}
		return false;
	}

	// Checks if current chunk is in index already
	public bool IsIndexed(ChunkPos pos){
		if(index.ContainsKey(GetLinearRegionCoords(pos)))
			return true;
		return false;
	}

	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*chunkLength + pos.x);
	}

	// Overload?
	public void AddHole(long pos, int size){
		this.fragHandler.AddHole(pos, size);
	}

	// Overload?
	public long FindPosition(int size){
		return this.fragHandler.FindPosition(size);
	}

	// Writes buffer stream to file
	public void Write(long position, byte[] buffer, int size){
		this.file.Seek(position, SeekOrigin.Begin);
		this.file.Write(buffer, 0, size);
	}

	// Writes all index data to index file
	public void UnloadIndex(){
		int position = 0;

		foreach(long l in this.index.Keys){
			ReadIndexLong(l, position);
			position += 8;
			ReadIndexLong(this.index[l], position);
			position += 8;
		}

		this.indexFile.Write(this.cachedIndex, 0, position);
	}

	// Adds a long to cached byte array of index
	private void ReadIndexLong(long l, int position){
		this.cachedIndex[position] = (byte)(l >> 56);
		this.cachedIndex[position+1] = (byte)(l >> 48);
		this.cachedIndex[position+2] = (byte)(l >> 40);
		this.cachedIndex[position+3] = (byte)(l >> 32);
		this.cachedIndex[position+4] = (byte)(l >> 24);
		this.cachedIndex[position+5] = (byte)(l >> 16);
		this.cachedIndex[position+6] = (byte)(l >> 8);
		this.cachedIndex[position+7] = (byte)l;
	}

	// Closes all Streams
	public void Close(){
		this.file.Close();
		UnloadIndex();
		this.indexFile.Close();
	}

}


/*
Handles DataHoles and makes sure there's little fragmentation to disk
*/
public class FragmentationHandler{
	private List<DataHole> data;

	public FragmentationHandler(byte b=0){
		this.data = new List<DataHole>(){};
		this.data.Add(new DataHole(0, -1, infinite:true));
	}

	// Finds a position in RegionFile that fits
	// a chunk with given size
	public long FindPosition(int size){
		long output;

		for(int i=0; i < this.data.Count; i++){
			if(data[i].size >= size){
				output = data[i].position;
				data.Insert(i+1, new DataHole(data[i].position + size, (int)data[i].size - size));
				data.RemoveAt(i);
				RemoveZero(data[i]);
				return output;
			}
		}

		output = data[data.Count-1].position;
		data.Add(new DataHole(data[data.Count-1].position + size, -1, infinite:true));
		data.RemoveAt(data.Count-2);
		return output;
	}

	// Adds a DataHole to list in a priority list fashion
	public void AddHole(long pos, int size){
		for(int i=0; i<this.data.Count;i++){
			if(this.data[i].position > pos){
				this.data.Insert(i-1, new DataHole(pos, size));
				MergeHoles(i-1);
				return;
			}
		}

		this.data.Insert(this.data.Count-1, new DataHole(pos, size));
		MergeHoles(this.data.Count-2);
		return;
	}

	// Removes if hole has no size
	private void RemoveZero(DataHole dh){
		if(dh.size == 0){
			this.data.Remove(dh);
		}
	}

	// Merges DataHoles starting from pos in data list if there's any
	// ONLY USE WHEN JUST ADDED A HOLE IN POS
	private void MergeHoles(int index){
		if(this.data[index].position + this.data[index].size == this.data[index+1].position){
			
			// If neighbor hole is infinite
			if(this.data[index+1].infinite){
				this.data.RemoveAt(index+1);
				this.data[index].SetInfinite(true);
				this.data[index].AddSize(-1);
			}
			// If neighbor is a normal hole
			else{
				this.data[index].AddSize(this.data[index+1].size);
				this.data.RemoveAt(index+1);
			}
		}
	}

}

// The individual data spots that can either be dead data or free unused data
public struct DataHole{
	public long position;
	public bool infinite;
	public int size;

	public DataHole(long pos, int size, bool infinite=false){
		this.position = pos;
		this.infinite = infinite;
		this.size = size;
	}

	public void AddPos(long pos){
		this.position += pos;
	}

	public void AddSize(int size){
		this.size += size;
	}

	public void SetInfinite(bool b){
		this.infinite = b;
	}
}