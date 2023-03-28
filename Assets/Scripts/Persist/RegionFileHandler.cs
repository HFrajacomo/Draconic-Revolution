using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = System.Random;

/*
Region Data File Format (.rdf)

| RDF File                                                                      |
| Chunk Header (21 bytes)  || Chunk Data (ChunkDimensions*8 bytes)                |
-> | Biome (1 byte) LastDay(4 bytes) LastHour(1 byte) LastMinute(1 byte) LastTick(1 byte) NeedGeneration (1 byte) |     -> | BlockData  (ChunkDimensions*4 bytes) || Metadata (ChunkDimensions*4 bytes) | 
-> | BlockDataSize (4 bytes) | HPDataSize (4 bytes) | StateDataSize (4 bytes)
*/

public class RegionFileHandler{
	private string worldName;
	private static readonly string fileFormat = ".rdf";
	private int seed;
	public TimeOfDay globalTime;

	// Unity Reference
	private ChunkLoader_Server cl;

	// Data
	private string saveDir;
	private string worldDir;

	// File Data
	public Stream worldFile;
	public Stream playerFile;

	// Player Data
	public Dictionary<ulong, PlayerData> allPlayerData = new Dictionary<ulong, PlayerData>();

	// Cache Information
	private byte[] nullMetadata = new byte[]{255,255};

	// Region Pool
	private Dictionary<ChunkPos, RegionFile> pool = new Dictionary<ChunkPos, RegionFile>();

	// Sizes
	public static int chunkHeaderSize = 21; // Size (in bytes) of header
	public static int chunkSize = Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 8; // Size in bytes of Chunk payload
	public static int pdatEntrySize = 32;

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
	private byte[] playerBuffer = new byte[RegionFileHandler.pdatEntrySize];

	public RegionFileHandler(ChunkLoader_Server cl){
		InitWorldFiles(cl);
	}

	// Initializes all files after received first player
	public void InitDataFiles(ChunkPos pos){
		LoadRegionFile(pos);
	}

	// Initializes World Files
	public void InitWorldFiles(ChunkLoader_Server cl){
		this.cl = cl;
		this.worldName = World.worldName;

		this.globalTime = GameObject.Find("/Time Counter").GetComponent<TimeOfDay>();

		#if UNITY_EDITOR
			this.saveDir = "Worlds/";
			this.worldDir = this.saveDir + this.worldName + "/";
		#else
			// If is in Dedicated Server
			if(!World.isClient){
				this.saveDir = "Worlds/";
				this.worldDir = this.saveDir + this.worldName + "/";
			}
			// If it's a Local Server
			else{
				this.saveDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";
				this.worldDir = this.saveDir + this.worldName + "\\";			
			}
		#endif

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
			World.SetWorldSeed(this.seed);

		}
		else{
			Random rnd = new Random((int)(DateTime.Now.Ticks % 999999));
			this.seed = rnd.Next(1,999999);

			this.worldFile = File.Open(this.worldDir + "world.wdat", FileMode.Create);	
			SaveWorld();
			World.SetWorldSeed(this.seed);		
		}

		// If already has a player data file
		if(File.Exists(this.worldDir + "player.pdat")){
			this.playerFile = File.Open(this.worldDir + "player.pdat", FileMode.Open);
			LoadAllPlayers();
		}
		else{
			this.playerFile = File.Open(this.worldDir + "player.pdat", FileMode.Create);	
		}	
	}

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
	/*
	ACCOUNTID(8): The account's ulong ID
	POSITION(12): 3 floats containing x, y, z
	DIRECTION(12): 3 floats containing x, y, z
	*/
	public void SavePlayers(){
		int count = 0;
		this.playerFile.SetLength(0);

		foreach(PlayerData pdat in this.allPlayerData.Values){
			this.playerFile.Write(pdat.ToByteArray(), 0, RegionFileHandler.pdatEntrySize);
			count++;
		}
	}

	// Loads data from PDAT file
	public PlayerData LoadPlayer(ulong ID, bool fromServer=false){
		if(!this.allPlayerData.ContainsKey(ID)){
			if(!fromServer)
				this.allPlayerData.Add(ID, new PlayerData(ID, new float3(0, this.cl.GetBlockHeight(new ChunkPos(0,0,3),0, 0), 0), new float3(0, 0, 0)));
			else
				this.allPlayerData.Add(ID, new PlayerData(ID, new float3(0, this.cl.GetBlockHeight(new ChunkPos(0,0,3),0, 0)+(3*Chunk.chunkDepth)+1, 0), new float3(0, 0, 0)));				
		}

		return this.allPlayerData[ID];
	}

	// Loads all data from PDAT file and puts into the allPlayerData dict
	private void LoadAllPlayers(){
		for(int i=0; i < this.playerFile.Length / RegionFileHandler.pdatEntrySize; i++){
			this.playerFile.Seek(i*RegionFileHandler.pdatEntrySize, SeekOrigin.Begin);
			this.playerFile.Read(playerBuffer, 0, RegionFileHandler.pdatEntrySize);

			PlayerData pdata = new PlayerData(playerBuffer);
			this.allPlayerData.Add(pdata.ID, pdata);
		}
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
	public void LoadRegionFile(ChunkPos pos){
		int rfx;
		int rfz;
		string name;

		rfx = Mathf.FloorToInt(pos.x / Constants.CHUNKS_IN_REGION_FILE);
		rfz = Mathf.FloorToInt(pos.z / Constants.CHUNKS_IN_REGION_FILE);
		name = "r" + rfx.ToString() + "x" + rfz.ToString() + "_" + pos.y;

		ChunkPos newPos = new ChunkPos(rfx, rfz, pos.y);

		// If Pool already has that region
		if(this.pool.ContainsKey(newPos))
			return;

		// If Pool is not full
		if(this.pool.Count < Constants.MAXIMUM_REGION_FILE_POOL){
			this.pool.Add(newPos, new RegionFile(name, fileFormat, newPos, Constants.CHUNKS_IN_REGION_FILE));
		}
		// If Pool is full
		else{
			FreePool(newPos); // Takes a RegionFile away from Pool
			this.pool.Add(newPos, new RegionFile(name, fileFormat, newPos, Constants.CHUNKS_IN_REGION_FILE));			
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

	// Creates a world folder with only a .wdat file inside
	public static bool CreateWorldFile(string worldName, int seed){
		string worldFolder = EnvironmentVariablesCentral.clientExeDir + "\\Worlds\\" + worldName + "\\";
		Stream file;
		byte[] byteArray = new byte[11];

		if(Directory.Exists(worldFolder))
			return false;

		byteArray[0] = (byte)(seed >> 24);
		byteArray[1] = (byte)(seed >> 16);
		byteArray[2] = (byte)(seed >> 8);
		byteArray[3] = (byte)seed;

		byteArray[4] = 0;
		byteArray[5] = 0;
		byteArray[6] = 0;
		byteArray[7] = 0;
		byteArray[8] = 6;
		byteArray[9] = 0;
		byteArray[10] = 0;

		Directory.CreateDirectory(worldFolder);
		file = File.Open(worldFolder + "world.wdat", FileMode.Create);
		file.Write(byteArray, 0, 11);
		file.Close();

		return true;
	}

	// Closes all streams in pool
	public void CloseAll(){
		foreach(ChunkPos pos in this.pool.Keys){
			this.pool[pos].Close();
		}

		SavePlayers();
		this.playerFile.Close();
		this.worldFile.Close();
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

		rfx = Mathf.FloorToInt(pos.x / Constants.CHUNKS_IN_REGION_FILE);
		rfz = Mathf.FloorToInt(pos.z / Constants.CHUNKS_IN_REGION_FILE);

		return new ChunkPos(rfx, rfz, pos.y);		
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

		rfx = Mathf.FloorToInt(pos.x / Constants.CHUNKS_IN_REGION_FILE);
		rfz = Mathf.FloorToInt(pos.z / Constants.CHUNKS_IN_REGION_FILE);

		ChunkPos check = new ChunkPos(rfx, rfz, pos.y);

		if(this.pool.ContainsKey(check))
			return true;
		return false;
	}


	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*Constants.CHUNKS_IN_REGION_FILE + pos.x);
	}
}