using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

/*
Class for the compression algorithm of the RDF files to be applied
*/

public static class Compression{
	private static byte[] cachedData = new byte[Chunk.chunkWidth * Chunk.chunkDepth * Chunk.chunkWidth * 5];

	// Writes Chunk c's data using a Pallete's compression into given buffer
	// and returns the amount of bytes written
	public static int CompressBlocks(Chunk c, byte[] buffer, int targetPos=0){
		int bytes;
		Pallete p = Compression.BiomeToPallete(c.biomeName);
		List<ushort> palleteList = Compression.GetPallete(p);
		
		
		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = new NativeArray<ushort>(c.data.GetData(), Allocator.TempJob);
		NativeArray<byte> buff = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> palleteArray = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		CompressionJob cbJob = new CompressionJob{
			chunkData = chunkData,
			buffer = buff,
			palleteArray = palleteArray,
			writtenBytes = writtenBytes	
		};

		JobHandle handle = cbJob.Schedule();
		handle.Complete();

		// NativeArray to Array convertion
		//buff.CopyTo(cachedData);
		//cachedData.CopyTo(buffer, targetPos);
		NativeArray<byte>.Copy(buff, 0, buffer, targetPos, writtenBytes[0]);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		palleteArray.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;		
	}

	// Writes Chunk c's HP metadata into given buffer
	// and returns the amount of bytes written
	public static int CompressMetadataHP(Chunk c, byte[] buffer, int targetPos=0){
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);
		int bytes;

		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = new NativeArray<ushort>(c.metadata.GetHPData(), Allocator.TempJob);
		NativeArray<byte> buff = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> palleteArray = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		CompressionJob cmdJob = new CompressionJob{
			chunkData = chunkData,
			buffer = buff,
			palleteArray = palleteArray,
			writtenBytes = writtenBytes		
		};

		JobHandle handle = cmdJob.Schedule();
		handle.Complete();

		// NativeArray to Array convertion
		//buff.CopyTo(cachedData);
		//cachedData.CopyTo(buffer, targetPos);
		NativeArray<byte>.Copy(buff, 0, buffer, targetPos, writtenBytes[0]);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		palleteArray.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;
	}

	// Writes Chunk c's state metadata into given buffer
	// and returns the amount of bytes written
	public static int CompressMetadataState(Chunk c, byte[] buffer, int targetPos=0){
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);
		int bytes;

		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = new NativeArray<ushort>(c.metadata.GetStateData(), Allocator.TempJob);
		NativeArray<byte> buff = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> palleteArray = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		CompressionJob cmdJob = new CompressionJob{
			chunkData = chunkData,
			buffer = buff,
			palleteArray = palleteArray,
			writtenBytes = writtenBytes		
		};

		JobHandle handle = cmdJob.Schedule();
		handle.Complete();

		// NativeArray to Array convertion
		//buff.CopyTo(cachedData);
		//cachedData.CopyTo(buffer, targetPos);
		NativeArray<byte>.Copy(buff, 0, buffer, targetPos, writtenBytes[0]);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		palleteArray.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;
	}


	// Builds Chunk's VoxelData using Decompression algorithm
	public static void DecompressBlocks(Chunk c, byte[] buffer){
		// Preparation Variables
		Pallete p = Compression.BiomeToPallete(c.biomeName);
		List<ushort> palleteList = Compression.GetPallete(p);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> pallete = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = pallete,
			initialPos = 0
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.data.SetData(data.ToArray());


		data.Dispose();
		readData.Dispose();
		pallete.Dispose();
	}

	// Builds byte message from Server using Decompression algorithm
	public static void DecompressBlocksClient(Chunk c, byte[] buffer, int initialPos){
		// Preparation Variables
		Pallete p = Compression.BiomeToPallete(c.biomeName);
		List<ushort> palleteList = Compression.GetPallete(p);


		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> pallete = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = pallete,
			initialPos = initialPos
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.data.SetData(data.ToArray());

		data.Dispose();
		readData.Dispose();
		pallete.Dispose();
	}

	// Builds Chunk's HP Metadata using Decompression algorithm
	public static void DecompressMetadataHP(Chunk c, byte[] buffer){
		// Preparation Variables
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> pallete = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = pallete,
			initialPos = 0
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetHPData(data.ToArray());

		data.Dispose();
		readData.Dispose();
		pallete.Dispose();
	} 

	// Builds Chunk's HP Metadata using Decompression algorithm
	public static void DecompressMetadataHPClient(Chunk c, byte[] buffer, int initialPos){
		// Preparation Variables
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> pallete = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = pallete,
			initialPos = initialPos
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetHPData(data.ToArray());

		data.Dispose();
		readData.Dispose();
		pallete.Dispose();
	} 

	// Builds Chunk's State Metadata using Decompression algorithm
	public static void DecompressMetadataState(Chunk c, byte[] buffer){
		// Preparation Variables
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> pallete = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = pallete,
			initialPos = 0
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetStateData(data.ToArray());
		
		data.Dispose();
		readData.Dispose();
		pallete.Dispose();
	}

	// Builds Chunk's State Metadata using Decompression algorithm
	public static void DecompressMetadataStateClient(Chunk c, byte[] buffer, int initialPos){
		// Preparation Variables
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = new NativeArray<byte>(buffer, Allocator.TempJob);
		NativeArray<ushort> pallete = new NativeArray<ushort>(palleteList.ToArray(), Allocator.TempJob);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = pallete,
			initialPos = initialPos
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetStateData(data.ToArray());
		
		data.Dispose();
		readData.Dispose();
		pallete.Dispose();
	}

	// Converts Chunk Biome to Pallete
	private static Pallete BiomeToPallete(string biome){
		switch(biome){
			case "Plains":
				return Pallete.GRASSLANDS;
			case "Grassy Highlands":
				return Pallete.GRASSLANDS;
			case "Ocean":
				return Pallete.OCEAN;
			case "Forest":
				return Pallete.FOREST;
			default:
				return Pallete.BASIC;
		}
	}

	// Returns a Palleted list
	private static List<ushort> GetPallete(Pallete p){
		switch(p){
			case Pallete.BASIC:
				return new List<ushort>{0,3,6, (ushort)(ushort.MaxValue/2)}; // Air, Stone and Water (and pregen air)

			case Pallete.GRASSLANDS:
				return new List<ushort>{0,1,2,3,6,ushort.MaxValue-2, (ushort)(ushort.MaxValue/2)}; // Air, Grass, Dirt, Stone, Water and Leaves (and pregen air)

			case Pallete.OCEAN:
				return new List<ushort>{0,2,3,6, (ushort)(ushort.MaxValue/2)}; // Air, Dirt, Stone and Water (and pregen air)

			case Pallete.FOREST:
				return new List<ushort>{0,2,3,6,65534, (ushort)(ushort.MaxValue/2)}; // Air, Dirt, Stone, Water and Leaves (and pregen air)

			// Special Pallete used for Metadata Compression
			case Pallete.METADATA:
				return new List<ushort>{0,1,ushort.MaxValue};

			default:
				return new List<ushort>{0,3,6,ushort.MaxValue-2, (ushort)(ushort.MaxValue/2)}; // Returns Pallete.BASIC
		}
	}

	// Writes a short block data to a buffer in a certain position
	private static void WriteShort(ushort data, byte[] buffer, int pos){
		buffer[pos] = (byte)(data >> 8);
		buffer[pos+1] = (byte)(data);		
	}

	// Reads ushort from a buffer
	private static ushort ReadShort(byte[] buffer, int pos){
		int a;
		a = buffer[pos];
		a = a << 8;
		a = a ^ buffer[pos+1];

		return (ushort)a;
	}

}

/*
Palletes for Region File data compression 
*/
public enum Pallete
{
	BASIC,
	GRASSLANDS,
	OCEAN,
	FOREST,
	METADATA
}



/*
MULTITHREADING
*/
[BurstCompile]
public struct CompressionJob : IJob{
	public NativeArray<ushort> chunkData;
	public NativeArray<byte> buffer;
	public NativeArray<ushort> palleteArray;
	public NativeArray<int> writtenBytes;

	public void Execute(){
		ushort blockCode;
		ushort bufferedBlock = 0;
		ushort bufferedCount = 0;
		bool contains;

		// Block Data
		for(int y=0; y<Chunk.chunkDepth; y++){
			for(int x=0; x<Chunk.chunkWidth; x++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					blockCode = chunkData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					contains = Contains(blockCode);

					// Case found block is not in Pallete and not buffered
					if(!contains && bufferedCount == 0){
						WriteShort(blockCode, writtenBytes[0]);
						writtenBytes[0] += 2;
					}
					// Case found block is not in Pallete and is buffered
					else if(!contains){
						WriteShort(bufferedBlock, writtenBytes[0]);
						writtenBytes[0] += 2;
						WriteShort(bufferedCount, writtenBytes[0]);
						writtenBytes[0] += 2;
						WriteShort(blockCode, writtenBytes[0]);
						writtenBytes[0] += 2;
						bufferedCount = 0;
					}
					// Case found block is in Pallete and is the buffered block
					else if(contains && bufferedBlock == blockCode && bufferedCount > 0){
						bufferedCount++;
					}
					// Case found block is in Pallete and is buffered by another block
					else if(contains && bufferedBlock != blockCode && bufferedCount > 0){
						WriteShort(bufferedBlock, writtenBytes[0]);
						writtenBytes[0] += 2;
						WriteShort(bufferedCount, writtenBytes[0]);
						writtenBytes[0] += 2;	
						bufferedBlock = blockCode;
						bufferedCount = 1;					
					}
					// General case of finding a palleted block that is not buffered
					else{
						bufferedBlock = blockCode;
						bufferedCount = 1;
					}

				}
			}
		}
		// Writes to buffer if chunk ends on a buffered stream
		if(bufferedCount > 0){
			WriteShort(bufferedBlock, writtenBytes[0]);
			writtenBytes[0] += 2;
			WriteShort(bufferedCount, writtenBytes[0]);
			writtenBytes[0] += 2;
		}
	}

	// Writes a short block data to a buffer in a certain position
	private void WriteShort(ushort data, int pos){
		buffer[pos] = (byte)(data >> 8);
		buffer[pos+1] = (byte)data;
	}

	// Checks if a blockCode is in palleteArray
	private bool Contains(ushort data){
		for(int i=0; i < palleteArray.Length; i++){
			if(palleteArray[i] == data)
				return true;
		}
		return false;
	}
}


[BurstCompile]
public struct DecompressJob : IJob{
	[ReadOnly]
	public NativeArray<ushort> pallete;
	[ReadOnly]
	public NativeArray<byte> readData;

	public NativeArray<ushort> data;

	[ReadOnly]
	public int initialPos; 


	public void Execute(){
		// Buffer Variables
		ushort bufferedCount = 0;
		ushort blockCode = 0;
		int readBytes = initialPos;

		// Block Data
		for(int y=0; y<Chunk.chunkDepth; y++){
			for(int x=0; x<Chunk.chunkWidth; x++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					// If not buffered, read
					if(bufferedCount == 0){
						// Reads code
						blockCode = ReadShort(readBytes);
						readBytes += 2;

						// If code is contained in Pallete
						if(Contains(blockCode)){
							bufferedCount = (ushort)(ReadShort(readBytes) - 1);
							readBytes += 2;
						}

						data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;

					}
					// If it's filling the buffered data
					else{
						data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] = blockCode;
						bufferedCount--;
					}
				}
			}
		}	
	}

	// Reads ushort from a buffer
	private ushort ReadShort(int pos){
		int a;
		a = readData[pos];
		a = a << 8;
		a = a ^ readData[pos+1];

		return (ushort)a;
	}

	// Checks if a element is in pallete
	private bool Contains(ushort u){
		for(int i=0; i < pallete.Length; i++){
			if(pallete[i] == u)
				return true;
		}
		return false;
	}
}