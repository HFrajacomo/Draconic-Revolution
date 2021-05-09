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
	private string worldName;
	private int seed;
	private static float chunkLength = 32f;
	public TimeOfDay globalTime;

	// Unity Reference
	private ChunkLoader_Server cl;

	// Data
	private string saveDir;
	private string worldDir;

	// File Data
	public Stream worldFile;
	public Stream playerFile;

	// Cache Information
	private byte[] nullMetadata = new byte[]{255,255};

	// Region Pool
	private Dictionary<ChunkPos, RegionFile> pool = new Dictionary<ChunkPos, RegionFile>();
	private int maxPoolSize = 4;

	// Cache
	private byte[] byteArray = new byte[1];
	private byte[] intArray = new byte[4];
	private byte[] timeArray = new byte[7];
	private byte[] indexArray = new byte[16];
	private byte[] headerBuffer = new byte[21];
	private byte[] nameBuffer = new byte[256];
	private byte[] blockBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer (roughly 0,1 MB)
	private byte[] hpBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer 
	private byte[] stateBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer
	private byte[] playerBuffer = new byte[12];

	// Sizes
	public static int chunkHeaderSize = 21; // Size (in bytes) of header
	public static int chunkSize = Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 8; // Size in bytes of Chunk payload

	public RegionFileHandler(ChunkLoader_Server cl){
		InitWorldFiles(cl);
	}

	// Initializes all files after received first player
	public void InitDataFiles(ChunkPos pos){
		LoadRegionFile(pos, init:true);
	}

	// Initializes World Files
	public void InitWorldFiles(ChunkLoader_Server cl){
		this.cl = cl;
		this.worldName = World.worldName;
		this.seed = World.worldSeed;

		this.globalTime = GameObject.Find("/Time Counter").GetComponent<TimeOfDay>();

		this.saveDir = "Worlds/";
		this.worldDir = "Worlds/" + this.worldName + "/";


		// If "Worlds/" dir doesn't exist
		if(!Directory.Exists(this.saveDir)){
			Directory.CreateDirectory(this.saveDir);
		}

		// If current world doesn't exist
		if(!Directory.Exists(this.worldDir)){
			Directory.CreateDirectory(this.worldDir);
		}

		// If already has a world data file
		if(File.Exists(this.worldDir + "world.wdat")){
			this.worldFile = File.Open(this.worldDir + "world.wdat", FileMode.Open);
			LoadWorld();
		}
		else{
			this.worldFile = File.Open(this.worldDir + "world.wdat", FileMode.Create);	
			SaveWorld();			
		}

		// If already has a player data file
		if(File.Exists(this.worldDir + "player.pdat")){
			this.playerFile = File.Open(this.worldDir + "player.pdat", FileMode.Open);
		}
		else{
			this.playerFile = File.Open(this.worldDir + "player.pdat", FileMode.Create);	
		}		
	}

	/*
	// Start RegionFileHandler on a given ChunkPos
	public void Start(ChunkPos pos){
		this.worldName = World.worldName;
		this.seed = World.worldSeed;
		this.globalTime = GameObject.Find("/Time Counter").GetComponent<TimeOfDay>();

		this.saveDir = "Worlds/";
		this.worldDir = "Worlds/" + this.worldName + "/";


		// If "Worlds/" dir doesn't exist
		if(!Directory.Exists(this.saveDir)){
			Directory.CreateDirectory(this.saveDir);
		}

		// If current world doesn't exist
		if(!Directory.Exists(this.worldDir)){
			Directory.CreateDirectory(this.worldDir);
		}

		// If already has a world data file
		if(File.Exists(this.worldDir + "world.wdat")){
			this.worldFile = File.Open(this.worldDir + "world.wdat", FileMode.Open);
			LoadWorld();
		}
		else{
			this.worldFile = File.Open(this.worldDir + "world.wdat", FileMode.Create);	
			SaveWorld();			
		}

		// If already has a player data file
		if(File.Exists(this.worldDir + "player.pdat")){
			this.playerFile = File.Open(this.worldDir + "player.pdat", FileMode.Open);
		}
		else{
			this.playerFile = File.Open(this.worldDir + "player.pdat", FileMode.Create);	
		}

		LoadRegionFile(pos, init:true);
	}
	*/

	// Returns initialized seed if world was just generated or returns saved seed if world exists
	public int GetRealSeed(){
		return this.seed;
	}

	// Checks if RegionFile represents ChunkPos, and loads correct RegionFile if not
	public void GetCorrectRegion(ChunkPos pos){
		if(!CheckUsage(pos)){
			LoadRegionFile(pos);
		}
	}

	// Saves World Data to wdat File
	/*
	WDAT FORMAT
	SEED(4): int representing seed
	TIME(7): time data day(4)/hour(1)/minute(1)/tick(1)
	*/
	public void SaveWorld(){
		intArray[0] = (byte)(this.seed >> 24);
		intArray[1] = (byte)(this.seed >> 16);
		intArray[2] = (byte)(this.seed >> 8);
		intArray[3] = (byte)this.seed;

		this.worldFile.SetLength(0);
		this.worldFile.Write(intArray, 0, 4);

		globalTime.TimeHeader(timeArray);

		this.worldFile.Write(timeArray, 0, 7);
	}

	// Loads data from WDAT file
	public void LoadWorld(){
		int seed = 0;

		this.worldFile.Read(intArray, 0, 4);

		seed += intArray[0];
		seed = seed << 8;
		seed += intArray[1];
		seed = seed << 8;
		seed += intArray[2];
		seed = seed << 8;
		seed += intArray[3];

		if(seed == 0)
			this.seed = 1;
		else
			this.seed = seed;


		this.worldFile.Read(timeArray, 0, 7);

		globalTime.SetTime(timeArray);

	}

	// Saved data to pdat file
	// DESIGNED FOR SINGLEPLAYER
	/*
	POSITION(12): 3 floats containing x, y, z
	*/
	public void SavePlayer(Vector3 t){
		playerBuffer[0] = (byte)((int)t.x >> 24);
		playerBuffer[1] = (byte)((int)t.x >> 16);
		playerBuffer[2] = (byte)((int)t.x >> 8);
		playerBuffer[3] = (byte)((int)t.x);
		playerBuffer[4] = (byte)((int)t.y >> 24);
		playerBuffer[5] = (byte)((int)t.y >> 16);
		playerBuffer[6] = (byte)((int)t.y >> 8);
		playerBuffer[7] = (byte)((int)t.y);
		playerBuffer[8] = (byte)((int)t.z >> 24);
		playerBuffer[9] = (byte)((int)t.z >> 16);
		playerBuffer[10] = (byte)((int)t.z >> 8);
		playerBuffer[11] = (byte)((int)t.z);

		this.playerFile.SetLength(0);
		this.playerFile.Write(playerBuffer, 0, 12);
	}

	// Loads data from PDAT file
	public Vector3 LoadPlayer(){
		if(this.playerFile.Length == 0){
			return new Vector3(0, this.cl.GetBlockHeight(new ChunkPos(0,0),0, 0), 0);
		}

		this.playerFile.Read(playerBuffer, 0, 12);
		return new Vector3(ReadPos(playerBuffer, 0), ReadPos(playerBuffer, 4) + 1, ReadPos(playerBuffer, 8));
	}

	private float ReadPos(byte[] buffer, int pos){
		int f = 0;

		f = f ^ buffer[pos];
		f = f << 8;
		f = f ^ buffer[pos+1];
		f = f << 8;
		f = f ^ buffer[pos+2];
		f = f << 8;
		f = f ^ buffer[pos+3];

		return f;
	}


	// Loads RegionFile related to given Chunk
	public void LoadRegionFile(ChunkPos pos, bool init=false){
		int rfx;
		int rfz;
		string name;

		rfx = Mathf.FloorToInt(pos.x / RegionFileHandler.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / RegionFileHandler.chunkLength);
		name = "r" + rfx.ToString() + "x" + rfz.ToString();

		ChunkPos newPos = new ChunkPos(rfx, rfz);

		// If Pool already has that region
		if(this.pool.ContainsKey(newPos))
			return;

		// If Pool is not full
		if(this.pool.Count < this.maxPoolSize){
			this.pool.Add(newPos, new RegionFile(name, newPos, RegionFileHandler.chunkLength));
		}
		// If Pool is full
		else{
			FreePool(newPos); // Takes a RegionFile away from Pool
			this.pool.Add(newPos, new RegionFile(name, newPos, RegionFileHandler.chunkLength));			
		}
	}

	// Finds which RegionPos to take away from the pool if full
	private void FreePool(ChunkPos pos){
		foreach(ChunkPos p in this.pool.Keys){
			if(Mathf.Abs(p.x - pos.x) + Mathf.Abs(p.z - pos.z) >= 2){
				this.pool[p].Close();
				this.pool.Remove(p);
				return;
			}
		}
	}

	// Closes all streams in pool
	public void CloseAll(){
		foreach(ChunkPos pos in this.pool.Keys){
			this.pool[pos].Close();
		}
	}

	// Loads a chunk information from RDF file using Pallete-based Decompression
	public void LoadChunk(Chunk c){
		byte biome=0;
		byte gen=0;
		int blockdata=0;
		int hpdata=0;
		int statedata=0;

		GetCorrectRegion(c.pos);

		ReadHeader(c.pos);
		InterpretHeader(ref biome, ref gen, ref blockdata, ref hpdata, ref statedata);

		c.biomeName = BiomeHandler.ByteToBiome(biome);
		c.lastVisitedTime = globalTime.DateBytes(timeArray);
		c.needsGeneration = gen;

		this.pool[ConvertToRegion(c.pos)].file.Read(blockBuffer, 0, blockdata);
		this.pool[ConvertToRegion(c.pos)].file.Read(hpBuffer, 0, hpdata);
		this.pool[ConvertToRegion(c.pos)].file.Read(stateBuffer, 0, statedata);

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
		int lastKnownSize=0;

		GetCorrectRegion(c.pos);

		// Reads pre-save size if is already indexed
		if(IsIndexed(c.pos)){
			byte biome=0;
			byte gen=0;
			int blockdata=0;
			int hpdata=0;
			int statedata=0;

			ReadHeader(c.pos);
			InterpretHeader(ref biome, ref gen, ref blockdata, ref hpdata, ref statedata);
			lastKnownSize = chunkHeaderSize + blockdata + hpdata + statedata;
		}


		// Saves data to buffers and gets total size
		blockSize = Compression.CompressBlocks(c, blockBuffer);
		hpSize = Compression.CompressMetadataHP(c, hpBuffer);
		stateSize = Compression.CompressMetadataState(c, stateBuffer);

		InitializeHeader(c, blockSize, hpSize, stateSize);

		totalSize = chunkHeaderSize + blockSize + hpSize + stateSize;

		// If Chunk was already saved
		if(IsIndexed(c.pos)){
			ChunkPos regionPos = ConvertToRegion(c.pos);

			this.pool[regionPos].AddHole(this.pool[regionPos].index[chunkCode], lastKnownSize);
			seekPosition = this.pool[regionPos].FindPosition(totalSize);
			this.pool[regionPos].SaveHoles();

			// If position in RegionFile has changed
			if(seekPosition != this.pool[regionPos].index[chunkCode]){
				this.pool[regionPos].index[chunkCode] = seekPosition;
				this.pool[regionPos].UnloadIndex();
			}

			// Saves Chunk
			this.pool[regionPos].Write(seekPosition, headerBuffer, chunkHeaderSize);
			this.pool[regionPos].Write(seekPosition+chunkHeaderSize, blockBuffer, blockSize);
			this.pool[regionPos].Write(seekPosition+chunkHeaderSize+blockSize, hpBuffer, hpSize);
			this.pool[regionPos].Write(seekPosition+chunkHeaderSize+blockSize+hpSize, stateBuffer, stateSize);
		}
		// If it's a new Chunk
		else{
			ChunkPos regionPos = ConvertToRegion(c.pos);

			seekPosition = this.pool[regionPos].FindPosition(totalSize);
			this.pool[regionPos].SaveHoles();

			// Adds new chunk to Index
			this.pool[regionPos].index.Add(chunkCode, seekPosition);
			AddEntryIndex(chunkCode, seekPosition);
			this.pool[regionPos].indexFile.Write(indexArray, 0, 16);
			this.pool[regionPos].indexFile.Flush();

			// Saves Chunk
			this.pool[regionPos].Write(seekPosition, headerBuffer, chunkHeaderSize);
			this.pool[regionPos].Write(seekPosition+chunkHeaderSize, blockBuffer, blockSize);
			this.pool[regionPos].Write(seekPosition+chunkHeaderSize+blockSize, hpBuffer, hpSize);
			this.pool[regionPos].Write(seekPosition+chunkHeaderSize+blockSize+hpSize, stateBuffer, stateSize);
		}

	}

	// Reads header from a chunk
	// leaves rdf file at Seek Position ready to read blockdata
	private void ReadHeader(ChunkPos pos){
		long code = GetLinearRegionCoords(pos);

		this.pool[ConvertToRegion(pos)].file.Seek(this.pool[ConvertToRegion(pos)].index[code], SeekOrigin.Begin);
		this.pool[ConvertToRegion(pos)].file.Read(headerBuffer, 0, chunkHeaderSize);
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

	// Checks if current chunk is in index already
	// Assumes that correct region has been loaded into pool
	public bool IsIndexed(ChunkPos pos){
		if(this.pool[ConvertToRegion(pos)].index.ContainsKey(GetLinearRegionCoords(pos)))
			return true;
		return false;
	}

	// Translates current ChunkPos to RegionPos
	public ChunkPos ConvertToRegion(ChunkPos pos){
		int rfx;
		int rfz;

		rfx = Mathf.FloorToInt(pos.x / RegionFileHandler.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / RegionFileHandler.chunkLength);

		return new ChunkPos(rfx, rfz);		
	}

	// Gets NeedGeneration byte from Chunk
	public bool GetsNeedGeneration(ChunkPos pos){
		ReadHeader(pos);

		if(headerBuffer[8] == 0)
			return false;
		return true;
	}

	// Checks if current chunk should be housed in current RegionFile
	public bool CheckUsage(ChunkPos pos){
		int rfx;
		int rfz;

		rfx = Mathf.FloorToInt(pos.x / RegionFileHandler.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / RegionFileHandler.chunkLength);

		ChunkPos check = new ChunkPos(rfx, rfz);

		if(this.pool.ContainsKey(check))
			return true;
		return false;
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
	private string worldDir;

	// File Data
	public Stream file;
	public Stream indexFile;
	public Stream holeFile;
	public Dictionary<long, long> index;
	public FragmentationHandler fragHandler;

	// Cached Data
	private byte[] cachedIndex;
	private byte[] longArray;
	private byte[] cachedHoles;

	// Opens the file and adds ".rdf" at the end (Region Data File)
	public RegionFile(string name, ChunkPos pos, float chunkLen){
		bool isLoaded = true;

		this.name = name;
		this.regionPos = pos;
		this.chunkLength = chunkLen;
		this.index = new Dictionary<long, long>();
		this.worldDir = "Worlds/" + World.worldName + "/";

		this.cachedIndex = new byte[16384];
		this.cachedHoles = new byte[16384];
		this.longArray = new byte[8];

		try{
			this.file = File.Open(this.worldDir + name + ".rdf", FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.file = File.Open(this.worldDir + name + ".rdf", FileMode.Create);
		}

		try{
			this.indexFile = File.Open(this.worldDir + name + ".ind", FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.indexFile = File.Open(this.worldDir + name + ".ind", FileMode.Create);
		}

		try{
			this.holeFile = File.Open(this.worldDir + name + ".hle", FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.holeFile = File.Open(this.worldDir + name + ".hle", FileMode.Create);
		}

		this.fragHandler = new FragmentationHandler(isLoaded);
		
		if(isLoaded){
			LoadIndex();
			LoadHoles();
		}
	}


	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*chunkLength + pos.x);
	}

	// Overload?
	public void AddHole(long pos, int size, bool infinite=false){
		this.fragHandler.AddHole(pos, size, infinite:infinite);
	}

	// Overload?
	public long FindPosition(int size){
		return this.fragHandler.FindPosition(size);
	}

	// Writes buffer stream to file
	public void Write(long position, byte[] buffer, int size){
		this.file.Seek(position, SeekOrigin.Begin);
		this.file.Write(buffer, 0, size);
		this.file.Flush();
	}

	// Reads index data com IND file
	public void LoadIndex(){
		this.indexFile.Seek(0, SeekOrigin.Begin);

		ReadIndexEntry();
		long a;
		long b;

		this.index.Clear();

		for(int i=0; i<this.indexFile.Length; i+=16){
			a = ReadLong(i);
			b = ReadLong(i+8);

			this.index[a] = b;
		}
	}

	// Save hole data to the HLE file
	public void SaveHoles(){
		bool done = false;
		int offset = 0;
		int writtenBytes = 0;

		this.holeFile.SetLength(0);
		writtenBytes = this.fragHandler.CacheHoles(offset, ref done);
		this.holeFile.Write(this.fragHandler.cachedHoles, 0, writtenBytes);
		this.holeFile.Flush();

		while(!done){
			offset++;
			writtenBytes = this.fragHandler.CacheHoles(offset, ref done);
			this.holeFile.Write(this.fragHandler.cachedHoles, 0, writtenBytes);
			this.holeFile.Flush();		
		}
	}

	// Loads all DataHole data to Fragment Handlers list
	public void LoadHoles(){
		this.holeFile.Seek(0, SeekOrigin.Begin);

		if(this.holeFile.Length <= 16380){
			this.holeFile.Read(this.cachedHoles, 0, (int)holeFile.Length);
			AddHolesFromBuffer((int)holeFile.Length);
		}
		else{
			int times = 0;

			this.holeFile.Read(this.cachedHoles, 0, 16380);
			AddHolesFromBuffer(16380);
			times++;

			// While there is still data to be read
			while(holeFile.Length - times*16380 >= 16380){
				this.holeFile.Read(this.cachedHoles, 0, 16380);
				AddHolesFromBuffer(16380);
				times++;
			}

			// Reads remnants
			if(holeFile.Length - times*16380 > 0){
				this.holeFile.Read(this.cachedHoles, 0, (int)(holeFile.Length - times*16380));
				AddHolesFromBuffer((int)(holeFile.Length - times*16380));
			}

		}
	}

	// Adds holes read from buffer data
	private void AddHolesFromBuffer(int readBytes){
		long a;
		int b;

		for(int i=0; i < readBytes; i+= 12){
			a = ReadLongHole(i);
			b = ReadIntHole(i+8);

			if(b > 0){
				AddHole(a, b);
			}
			else{
				AddHole(a, -1, infinite:true);
			}
		}
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

		this.indexFile.SetLength(0);
		this.indexFile.Write(this.cachedIndex, 0, position);
		this.indexFile.Flush();
	}

	// Reads all index file and sends it to cachedIndex
	private void ReadIndexEntry(){
		this.indexFile.Read(this.cachedIndex, 0, (int)this.indexFile.Length);
	}

	// Reads a long in byte[] cachedIndex at position n
	private long ReadLong(int pos){
		long a;

		a = cachedIndex[pos];
		a = a << 8;
		a += cachedIndex[pos+1];
		a = a << 8;
		a += cachedIndex[pos+2];
		a = a << 8;
		a += cachedIndex[pos+3];
		a = a << 8;
		a += cachedIndex[pos+4];
		a = a << 8;
		a += cachedIndex[pos+5];
		a = a << 8;
		a += cachedIndex[pos+6];
		a = a << 8;
		a += cachedIndex[pos+7];

		return a;
	}

	// Reads a long in byte[] cachedHoles at position n
	private long ReadLongHole(int pos){
		long a;

		a = this.cachedHoles[pos];
		a = a << 8;
		a += this.cachedHoles[pos+1];
		a = a << 8;
		a += this.cachedHoles[pos+2];
		a = a << 8;
		a += this.cachedHoles[pos+3];
		a = a << 8;
		a += this.cachedHoles[pos+4];
		a = a << 8;
		a += this.cachedHoles[pos+5];
		a = a << 8;
		a += this.cachedHoles[pos+6];
		a = a << 8;
		a += this.cachedHoles[pos+7];

		return a;
	}

	// Reads an int in byte[] cachedIndex at position n
	private int ReadInt(int pos){
		int a;

		a = cachedIndex[pos];
		a = a << 8;
		a += cachedIndex[pos+1];
		a = a << 8;
		a += cachedIndex[pos+2];
		a = a << 8;
		a += cachedIndex[pos+3];

		return a;
	}

	// Reads an int in byte[] cachedHoles at position n
	private int ReadIntHole(int pos){
		int a;

		a = this.cachedHoles[pos];
		a = a << 8;
		a += this.cachedHoles[pos+1];
		a = a << 8;
		a += this.cachedHoles[pos+2];
		a = a << 8;
		a += this.cachedHoles[pos+3];

		return a;
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
		UnloadIndex();
		SaveHoles();

		this.file.Close();
		this.indexFile.Close();
		this.holeFile.Close();
	}

}


/*
Handles DataHoles and makes sure there's little fragmentation to disk
*/
public class FragmentationHandler{
	private List<DataHole> data;
	public byte[] cachedHoles = new byte[384]; // 32 Holes per Read

	public FragmentationHandler(bool loaded){
		this.data = new List<DataHole>(){};

		if(!loaded)
			this.data.Add(new DataHole(0, -1, infinite:true));
	}

	// Finds a position in RegionFile that fits
	// a chunk with given size
	public long FindPosition(int size){
		long output;

		for(int i=0; i < this.data.Count; i++){
			if(data[i].size > size){
				output = data[i].position;
				data.Insert(i+1, new DataHole(data[i].position + size, (int)data[i].size - size));
				data.RemoveAt(i);
				return output;
			}
			else if(data[i].size == size){
				output = data[i].position;
				data.RemoveAt(i);
				return output;				
			}
		}

		output = data[data.Count-1].position;
		data.Add(new DataHole(data[data.Count-1].position + size, -1, infinite:true));
		data.RemoveAt(data.Count-2);
		return output;
	}

	// Puts hole data in CachedHoles
	// Returns the amount of bytes written and a reference bool that serves as a flag
	// When the flag is true, caching has been completed. If false, more CacheHoles need to be called
	// Offset is a multiplier of 384 indices
	public int CacheHoles(int offset, ref bool done){
		done = this.data.Count - offset*32 <= 32;
		int index=0;

		if(done){
			for(int i=offset*32; i < this.data.Count; i++){
				data[i].Bytefy(this.cachedHoles, index);
				index += 12;
			}
		}
		else{
			for(int i=offset*32; i < (offset+1)*32; i++){
				data[i].Bytefy(this.cachedHoles, index);
				index += 12;
			}			
		}
		return index;
	}

	// Adds a DataHole to list in a priority list fashion
	public void AddHole(long pos, int size, bool infinite=false){
		if(infinite){
			this.data.Add(new DataHole(pos, -1, infinite:true));
			return;
		}

		for(int i=0; i<this.data.Count;i++){
			if(this.data[i].position > pos){
				this.data.Insert(i, new DataHole(pos, size));
				MergeHoles(i);
				return;
			}
		}

		// Adds a hole if there isn't any
		this.data.Add(new DataHole(pos, size));
		return;
	}

	// Removes if hole has no size
	private bool RemoveZero(DataHole dh){
		if(dh.size == 0){
			this.data.Remove(dh);
			return true;
		}
		return false;
	}

	public int Count(){
		return this.data.Count;
	}

	// Merges DataHoles starting from pos in data list if there's any
	// ONLY USE WHEN JUST ADDED A HOLE IN POS
	private void MergeHoles(int index){
		if(this.data[index].position + this.data[index].size == this.data[index+1].position){
			
			// If neighbor hole is infinite
			if(this.data[index+1].infinite){
				this.data.RemoveAt(index+1);
				this.data[index] = new DataHole(this.data[index].position, -1, infinite:true);
			}
			// If neighbor is a normal hole
			else{
				this.data[index] = new DataHole(this.data[index].position, this.data[index].size + this.data[index+1].size);
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

	// Turns DataHole into byte format for HLE files
	public void Bytefy(byte[] b, int offset){
		b[offset] = (byte)(this.position >> 56);
		b[offset+1] = (byte)(this.position >> 48);
		b[offset+2] = (byte)(this.position >> 40);
		b[offset+3] = (byte)(this.position >> 32);
		b[offset+4] = (byte)(this.position >> 24);
		b[offset+5] = (byte)(this.position >> 16);
		b[offset+6] = (byte)(this.position >> 8);
		b[offset+7] = (byte)this.position;
		b[offset+8] = (byte)(this.size >> 24);
		b[offset+9] = (byte)(this.size >> 16);
		b[offset+10] = (byte)(this.size >> 8);
		b[offset+11] = (byte)this.size;
	}
}