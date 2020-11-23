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
	private static int[] cachedData = new int[Chunk.chunkWidth * Chunk.chunkDepth * Chunk.chunkWidth];

	// Writes Chunk c's data using a Pallete's compression into given buffer
	// and returns the amount of bytes written
	public static int CompressBlocks(Chunk c, byte[] buffer){
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
		buff.CopyTo(buffer);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		palleteArray.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;
			
	}

	// Writes Chunk c's HP metadata into given buffer
	// and returns the amount of bytes written
	public static int CompressMetadataHP(Chunk c, byte[] buffer){
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);
		int bytes;

		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = Compression.PrepareChunkMetadata(c, true);
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
		buff.CopyTo(buffer);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		palleteArray.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;
	}

	// Writes Chunk c's state metadata into given buffer
	// and returns the amount of bytes written
	public static int CompressMetadataState(Chunk c, byte[] buffer){
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);
		int bytes;

		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = Compression.PrepareChunkMetadata(c, false);
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
		buff.CopyTo(buffer);

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

		// Buffer Variables
		ushort bufferedCount = 0;
		ushort blockCode = 0;
		int readBytes = 0;

		// Block Data
		for(int y=0; y<Chunk.chunkDepth; y++){
			for(int x=0; x<Chunk.chunkWidth; x++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					// If not buffered, read
					if(bufferedCount == 0){
						// Reads code
						blockCode = Compression.ReadShort(buffer, readBytes);
						readBytes += 2;

						// If code is contained in Pallete
						if(palleteList.Contains(blockCode)){
							bufferedCount = (ushort)(Compression.ReadShort(buffer, readBytes) - 1);
							readBytes += 2;
						}

						c.data.SetCell(x,y,z, blockCode);

					}
					// If it's filling the buffered data
					else{
						c.data.SetCell(x,y,z, blockCode);
						bufferedCount--;
					}


				}
			}
		}
	}

	// Builds Chunk's HP Metadata using Decompression algorithm
	public static void DecompressMetadataHP(Chunk c, byte[] buffer){
		// Preparation Variables
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);

		// Buffer Variables
		ushort bufferedCount = 0;
		ushort metaCode = 0;
		int readBytes = 0;

		// Block Data
		for(int y=0; y<Chunk.chunkDepth; y++){
			for(int x=0; x<Chunk.chunkWidth; x++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					// If not buffered, read
					if(bufferedCount == 0){
						// Reads code
						metaCode = Compression.ReadShort(buffer, readBytes);
						readBytes += 2;

						// If code is contained in Pallete
						if(palleteList.Contains(metaCode)){
							bufferedCount = (ushort)(Compression.ReadShort(buffer, readBytes) - 1);
							readBytes += 2;
						}

						c.metadata.SetHP(x,y,z,metaCode);
					}
					// If it's filling the buffered data
					else{
						// Creates metadata if not null
						c.metadata.SetHP(x,y,z,metaCode);
						bufferedCount--;
					}


				}
			}
		}
	}

	// Builds Chunk's State Metadata using Decompression algorithm
	public static void DecompressMetadataState(Chunk c, byte[] buffer){
		// Preparation Variables
		List<ushort> palleteList = Compression.GetPallete(Pallete.METADATA);

		// Buffer Variables
		ushort bufferedCount = 0;
		ushort metaCode = 0;
		int readBytes = 0;

		// Block Data
		for(int y=0; y<Chunk.chunkDepth; y++){
			for(int x=0; x<Chunk.chunkWidth; x++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					// If not buffered, read
					if(bufferedCount == 0){
						// Reads code
						metaCode = Compression.ReadShort(buffer, readBytes);
						readBytes += 2;

						// If code is contained in Pallete
						if(palleteList.Contains(metaCode)){
							bufferedCount = (ushort)(Compression.ReadShort(buffer, readBytes) - 1);
							readBytes += 2;
						}

						// Creates metadata if not null
						if(metaCode != ushort.MaxValue){
							c.metadata.SetState(x,y,z, metaCode);
						}

					}
					// If it's filling the buffered data
					else{
						// Creates metadata if not null
						if(metaCode != ushort.MaxValue){
							c.metadata.SetState(x,y,z,metaCode);
						}
						bufferedCount--;
					}


				}
			}
		}
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


	// Creates the NativeArray for Multithreading
	private static NativeArray<ushort> PrepareChunkData(Chunk c){
		ushort[] data = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
		
		int i = 0;

		for(int y=0; y < Chunk.chunkDepth; y++){
			for(int x=0; x < Chunk.chunkWidth; x++){
				for(int z=0; z < Chunk.chunkWidth; z++){
					data[i] = c.data.GetCell(x,y,z);
					i++;
				}
			}
		}

		return new NativeArray<ushort>(data, Allocator.TempJob);
	}

	// Creates a NativeArray for Multithreading
	private static NativeArray<ushort> PrepareChunkMetadata(Chunk c, bool hp){
		ushort[] data = new ushort[Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth];
		
		int i = 0;

		if(hp){
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int x=0; x < Chunk.chunkWidth; x++){
					for(int z=0; z < Chunk.chunkWidth; z++){
						data[i] = c.metadata.GetHP(x,y,z);								
						i++;
					}
				}
			}
		}
		else{
			for(int y=0; y < Chunk.chunkDepth; y++){
				for(int x=0; x < Chunk.chunkWidth; x++){
					for(int z=0; z < Chunk.chunkWidth; z++){
						data[i] = c.metadata.GetState(x,y,z);								
						i++;
					}
				}
			}		
		}

		return new NativeArray<ushort>(data, Allocator.TempJob);
		
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