using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionFileHandler{
	private RegionFile file;
	private int seed;
	private int renderDistance;
	private static float chunkLength = 32f;


	// Cache Information
	private byte[] nullMetadata = new byte[]{255,255};
	private int[,,] cachedData = new int[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	private Metadata[,,] cachedMetadata = new Metadata[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	private byte[] intArray = new byte[4];
	private byte[] ushortArray = new byte[2];
	private byte[] byteArray = new byte[1];

	// Sizes
	private static int fileHeader = 2; // Size in bytes of file header
	private static int chunkHeaderSize = 9; // Size (in bytes) of header
	//private static int metadataStep = 102400; // Amount of bytes to jump in order to reach block metadata
	private static int chunkSize = Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 8; // Size in bytes of Chunk payload

	// Builds the file handler and loads it on the ChunkPos of the first player
	public RegionFileHandler(int seed, int renderDistance, ChunkPos pos){
		this.seed = seed;
		this.renderDistance = renderDistance;

		LoadRegionFile(pos);
	}

	// Checks if RegionFile represents ChunkPos, and loads correct RegionFile if not
	public void GetCorrectRegion(ChunkPos pos){
		if(!file.CheckUsage(pos)){
			this.file.Close();
			LoadRegionFile(pos);
		}
	}

	// Getter for RegionFile
	public RegionFile GetFile(){
		return this.file;
	}

	// Loads RegionFile related to given Chunk
	public void LoadRegionFile(ChunkPos pos){
		int rfx;
		int rfz;
		string name;

		rfx = Mathf.FloorToInt(pos.x / RegionFileHandler.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / RegionFileHandler.chunkLength);
		name = "r" + rfx.ToString() + "x" + rfz.ToString();

		file = new RegionFile(name, new ChunkPos(rfx, rfz), RegionFileHandler.chunkLength);
	}

	// Loads the Chunk Data to Chunk
	public void LoadChunk(Chunk c){
		long code = GetLinearRegionCoords(c.pos);
		long chunkOffset = this.file.index[code];
		long offset = fileHeader + (chunkHeaderSize + chunkSize)*chunkOffset + chunkHeaderSize;
		ushort? hp;
		ushort? state;

		this.file.Seek(offset, SeekOrigin.Begin);

		// Reads VoxelData
		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int z=0; z < Chunk.chunkWidth; z++){
					cachedData[x,y,z] = this.file.ReadInt(intArray);
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

	// Saves chunk data to RegionFile
	/*
	NeedsGeneration in Chunks is a future implementation of Structures pre-generating in chunks
	0: No needs to generate chunk
	1: Needs to generate chunk, because it was just pre-generated
	*/
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

	// Converts an Int to Byte Array
	private byte[] IntToByte(int data){
		byte[] b = new byte[4];
		b[0] = (byte)(data >> 24);
		b[1] = (byte)(data >> 16);
		b[2] = (byte)(data >> 8);
		b[3] = (byte)(data);

		return b;
	}

	// Converts an ushort? to Byte Array
	private byte[] UshortToByte(ushort? data){
		if(data == null){
			return new byte[]{255, 255};
		}

		byte[] b = new byte[2];
		b[0] = (byte)(data >> 8);
		b[1] = (byte)(data);


		return b;
	}

	// Converts a long to byte array
	private byte[] LongToByte(long data){
		byte[] b = new byte[8];

		b[0] = (byte)(data >> 56);
		b[1] = (byte)(data >> 48);
		b[2] = (byte)(data >> 40);
		b[3] = (byte)(data >> 32);
		b[4] = (byte)(data >> 24);
		b[5] = (byte)(data >> 16);
		b[6] = (byte)(data >> 8);
		b[7] = (byte)(data);

		return b;
	}

	// Gets NeedGeneration byte from Chunk
	public bool GetsNeedGeneration(ChunkPos pos){

		this.file.Seek((long)(fileHeader + (chunkHeaderSize + chunkSize)*this.file.index[GetLinearRegionCoords(pos)] + (chunkHeaderSize-1)), SeekOrigin.Begin);
		this.file.file.Read(byteArray, 0, 1);
		this.file.Seek(0, SeekOrigin.End);

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
	public Stream file;
	private float chunkLength;
	public Stream indexFile;
	public Dictionary<long, long> index;

	// Opens the file and adds ".rdf" at the end (Region Data File)
	public RegionFile(string name, ChunkPos pos, float chunkLen){
		this.name = name;
		this.regionPos = pos;
		this.chunkLength = chunkLen;
		this.index = new Dictionary<long, long>();

		try{
			this.file = File.Open(name + ".rdf", FileMode.Open);
		} 
		catch (FileNotFoundException){
			this.file = File.Open(name + ".rdf", FileMode.Create);

			// File Header of 2 bytes containing number of written chunks
			this.file.Write(new byte[]{0,0}, 0, 2);
		}

		try{
			this.indexFile = File.Open(name + ".ind", FileMode.Open);
		} 
		catch (FileNotFoundException){
			this.indexFile = File.Open(name + ".ind", FileMode.Create);
		}

		LoadIndex();	
	}

	// Returns the amount of saved chunks specified in RegionFile
	public ushort ReadChunksSaved(){
		byte[] read = new byte[2];

		file.Seek(0, SeekOrigin.Begin);
		file.Read(read, 0, 2);
		return ByteToUshort(read);

	}

	// Increment Chunks Saved
	public ushort IncrementSaved(){
		ushort num = ReadChunksSaved();
		num++;

		file.Seek(0, SeekOrigin.Begin);
		file.Write(UshortToByte(num), 0, 2);
		return (ushort)(num-1);
	}

	// Converts an ushort? to Byte Array
	private byte[] UshortToByte(ushort? data){
		if(data == null){
			return new byte[]{255, 255};
		}

		byte[] b = new byte[2];
		b[0] = (byte)(data >> 8);
		b[1] = (byte)(data);


		return b;
	}

	// Gets the size of the file
	public long GetSize(){
		return file.Length;
	}

	// Checks if this region file is being used in current chunk
	public bool CheckUsage(ChunkPos pos){
		if(Mathf.FloorToInt(pos.x / chunkLength) == this.regionPos.x && Mathf.FloorToInt(pos.z / chunkLength) == this.regionPos.z)
			return true;
		return false;
	}

	// Writes data to file
	public void Write(byte[] b, int offset, int amount){
		this.file.Write(b, offset, amount);
	}

	// Reads long from file
	public long ReadIndexLong(byte[] buffer){
		this.indexFile.Read(buffer, 0, 8);

		return ByteToLong(buffer);
	}

	// Seeks into  file
	public void Seek(long pos, SeekOrigin so){
		file.Seek(pos, so);
	}

	// Reads an integer from RegionFile
	public int ReadInt(byte[] buffer){
		this.file.Read(buffer, 0, 4);

		return ByteToInt(buffer);
	}

	// Reads an ushort from RegionFile
	public ushort? ReadUshort(byte[] buffer){
		ushort? result;
		this.file.Read(buffer, 0, 2);

		result = ByteToUshort(buffer);

		if(result == ushort.MaxValue)
			return null;
		return result;
	}

	// Reads indexFile into index
	private void LoadIndex(){
		long code;
		byte[] buffer = new byte[8];
		
		// Loads all data into index dict
		this.indexFile.Seek(0, SeekOrigin.Begin);

		for(int i=0; i < this.indexFile.Length/8; i++){
			code = ReadIndexLong(buffer);
			this.index.Add(code, i);
		}
	}

	// Writes one entry to index file
	public void WriteEntry(long code, long offset){
		this.indexFile.Write(LongToByte(code), 0, 8);
	}

	// Writes indexFile
	private void UnloadIndex(){
		this.indexFile.Seek(0, SeekOrigin.Begin);

		// Writes all data to index file
		foreach(long l in index.Keys){
			this.indexFile.Write(LongToByte(l), 0, 8);
		}
	}

	// Checks if given ChunkPos exists in current index
	public bool ExistChunk(ChunkPos pos){
		long code = GetLinearRegionCoords(pos);

		if(this.index.ContainsKey(code))
			return true;
		return false;
	}

	// Converts a long to byte array
	private byte[] LongToByte(long data){
		byte[] b = new byte[8];

		b[0] = (byte)(data >> 56);
		b[1] = (byte)(data >> 48);
		b[2] = (byte)(data >> 40);
		b[3] = (byte)(data >> 32);
		b[4] = (byte)(data >> 24);
		b[5] = (byte)(data >> 16);
		b[6] = (byte)(data >> 8);
		b[7] = (byte)(data);

		return b;
	}

	// Convert Byte to Int
	public int ByteToInt(byte[] b){
		int a = 0;
		a = a ^ b[0];
		a = a << 8;
		a = a ^ b[1];
		a = a << 8;
		a = a ^ b[2];
		a = a << 8;
		a = a ^ b[3];

		return a;	
	}

	// Convert Byte to Long
	public long ByteToLong(byte[] b){
		long a = 0;
		a = a ^ b[0];
		a = a << 8;
		a = a ^ b[1];
		a = a << 8;
		a = a ^ b[2];
		a = a << 8;
		a = a ^ b[3];
		a = a << 8;
		a = a ^ b[4];
		a = a << 8;
		a = a ^ b[5];
		a = a << 8;
		a = a ^ b[6];
		a = a << 8;
		a = a ^ b[7];

		return a;
	}

	// Convert Byte to Ushort
	public ushort ByteToUshort(byte[] b){
		uint a = 0;
		a = a ^ b[0];
		a = a << 8;
		a = a ^ b[1];

		return (ushort)a;	
	}

	// Closes file
	public void Close(){
		this.file.Close();
		UnloadIndex();
		this.indexFile.Close();
	}

	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*chunkLength + pos.x);
	}
}
