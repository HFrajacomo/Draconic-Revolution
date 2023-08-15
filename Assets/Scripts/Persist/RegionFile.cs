using System.Collections.Generic;
using System.IO;
using UnityEngine;

public struct RegionFile{
	public string name;
	public string fileFormat;
	public ChunkPos regionPos; // Variable to represent Region coordinates, and not Chunk coordinates
	private float chunkLength;
	private string worldDir;
	private string saveDir;
	private long fileSize;

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
	public RegionFile(string name, string fileFormat, ChunkPos pos, float chunkLen, string worldName="", bool isDefrag=false){
		bool isLoaded = true;

		this.name = name;
		this.fileFormat = fileFormat;
		this.regionPos = pos;
		this.chunkLength = chunkLen;
		this.index = new Dictionary<long, long>();

		this.cachedIndex = new byte[16384];
		this.cachedHoles = new byte[16384];
		this.longArray = new byte[8];


		#if UNITY_EDITOR
			this.saveDir = "Worlds/";

			if(!isDefrag)
				this.worldDir = this.saveDir + World.worldName + "/";
			else
				this.worldDir = this.saveDir + worldName + "/";
		#else
			// If is in Dedicated Server
			if(!World.isClient){
				this.saveDir = "Worlds/";

				if(!isDefrag)
					this.worldDir = this.saveDir + World.worldName + "/";
				else
					this.worldDir = this.saveDir + worldName + "/";
			}
			// If it's a Local Server
			else{
				this.saveDir = EnvironmentVariablesCentral.clientExeDir + "\\Worlds\\";

				if(!isDefrag)
					this.worldDir = this.saveDir + World.worldName + "\\";	
				else
					this.worldDir = this.saveDir + worldName + "\\";	
			}
		#endif

		try{
			this.file = File.Open(this.worldDir + name + this.fileFormat, FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.file = File.Open(this.worldDir + name + this.fileFormat, FileMode.Create);
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

		FileInfo info = new FileInfo(this.worldDir + this.name + this.fileFormat);

		if(info.Exists)
			this.fileSize = info.Length;
		else
			this.fileSize = 0;

		if(isLoaded){
			LoadIndex();
			LoadHoles();
		}
	}

	public long GetFileSize(){return this.fileSize;}


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

	// Reads from header to buffer stream (Not used in Game Loop)
	public void ReadHeader(long position, byte[] buffer){
		this.file.Seek(position, SeekOrigin.Begin);
		this.file.Read(buffer, 0, RegionFileHandler.chunkHeaderSize);
	}

	// Reads from file to buffer stream (Not used in Game Loop)
	public void Read(long position, byte[] buffer, int bufferOffset, int size){
		this.file.Seek(position, SeekOrigin.Begin);
		this.file.Read(buffer, bufferOffset, size);
	}

	// Reads index data with IND file
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

	// Save hole data to another stream (Not used in Game Loop)
	public void SaveHolesToFile(Stream file){
		bool done = false;
		int offset = 0;
		int writtenBytes = 0;

		this.file.SetLength(0);
		writtenBytes = this.fragHandler.CacheHoles(offset, ref done);
		this.file.Write(this.fragHandler.cachedHoles, 0, writtenBytes);
		this.file.Flush();

		while(!done){
			offset++;
			writtenBytes = this.fragHandler.CacheHoles(offset, ref done);
			this.file.Write(this.fragHandler.cachedHoles, 0, writtenBytes);
			this.file.Flush();		
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

	// Close all Streams and don't save data (Not used in Game Loop)
	public void CloseWithoutSaving(){
		this.file.Close();
		this.indexFile.Close();
		this.holeFile.Close();
	}
}