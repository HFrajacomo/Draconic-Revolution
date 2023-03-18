using System;
using System.IO;
using System.Text;
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


	// Pallete Initializers
	private static BlockID[] basicArray = new BlockID[]{BlockID.AIR, BlockID.STONE, BlockID.WATER, BlockID.GRAVEL, BlockID.PREGEN_AIR};
	private static BlockID[] grasslandsArray = new BlockID[]{BlockID.AIR, BlockID.GRASS, BlockID.DIRT, BlockID.STONE, BlockID.WATER, BlockID.LEAF, BlockID.GRAVEL, BlockID.PREGEN_AIR};
	private static BlockID[] oceanArray = new BlockID[]{BlockID.AIR, BlockID.DIRT, BlockID.STONE, BlockID.WATER, BlockID.SAND, BlockID.GRAVEL, BlockID.PREGEN_AIR};
	private static BlockID[] forestArray = new BlockID[]{BlockID.AIR, BlockID.GRASS, BlockID.DIRT, BlockID.STONE, BlockID.WATER, BlockID.SAND, BlockID.GRAVEL, BlockID.PREGEN_AIR};
	private static BlockID[] icelandsArray = new BlockID[]{BlockID.AIR, BlockID.STONE, BlockID.WATER, BlockID.PINE_LEAF, BlockID.PINE_WOOD, BlockID.SNOW, BlockID.ICE, BlockID.GRAVEL, BlockID.PREGEN_AIR};
	private static BlockID[] sandlandsArray = new BlockID[]{BlockID.AIR, BlockID.STONE, BlockID.WATER, BlockID.SAND, BlockID.SANDSTONE, BlockID.GRAVEL, BlockID.PREGEN_AIR};
	private static BlockID[] hellArray = new BlockID[]{BlockID.AIR, BlockID.HELL_MARBLE, BlockID.LAVA, BlockID.BASALT, BlockID.BONE, BlockID.PREGEN_AIR};
	private static BlockID[] coreArray = new BlockID[]{BlockID.AIR, BlockID.MOONSTONE, BlockID.PREGEN_AIR};

	private static BlockID[] structureArray =
						 new BlockID[]{BlockID.AIR, BlockID.GRASS, BlockID.DIRT, BlockID.STONE, BlockID.IRON_ORE, BlockID.WATER, BlockID.LEAF,
										BlockID.PINE_WOOD, BlockID.PINE_LEAF, BlockID.COAL_ORE, BlockID.COPPER_ORE, BlockID.TIN_ORE, BlockID.ALUMINIUM_ORE,
										BlockID.MAGNETITE_ORE, BlockID.EMERIUM_ORE, BlockID.GOLD_ORE, BlockID.GRAVEL, BlockID.BONE}; 

	private static BlockID[] metadataArray = new BlockID[]{(BlockID)0, (BlockID)1, (BlockID)(ushort.MaxValue)};

	// Palletes
	public static readonly NativeHashSet<ushort> basicPallete;
	public static readonly NativeHashSet<ushort> grasslandsPallete;
	public static readonly NativeHashSet<ushort> oceanPallete;
	public static readonly NativeHashSet<ushort> forestPallete;
	public static readonly NativeHashSet<ushort> icelandsPallete;
	public static readonly NativeHashSet<ushort> sandlandsPallete;
	public static readonly NativeHashSet<ushort> hellPallete;
	public static readonly NativeHashSet<ushort> corePallete;
	public static readonly NativeHashSet<ushort> structurePallete;
	public static readonly NativeHashSet<ushort> metadataPallete;

	// Static Constructor
	static Compression() {
		basicPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < basicArray.Length; i++){
			basicPallete.Add((ushort)basicArray[i]);
		}

		grasslandsPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < grasslandsArray.Length; i++){
			grasslandsPallete.Add((ushort)grasslandsArray[i]);
		}

		oceanPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < oceanArray.Length; i++){
			oceanPallete.Add((ushort)oceanArray[i]);
		}

		forestPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < forestArray.Length; i++){
			forestPallete.Add((ushort)forestArray[i]);
		}

		icelandsPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < icelandsArray.Length; i++){
			icelandsPallete.Add((ushort)icelandsArray[i]);
		}

		sandlandsPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < sandlandsArray.Length; i++){
			sandlandsPallete.Add((ushort)sandlandsArray[i]);
		}

		hellPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < hellArray.Length; i++){
			hellPallete.Add((ushort)hellArray[i]);
		}

		corePallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < coreArray.Length; i++){
			corePallete.Add((ushort)coreArray[i]);
		}

		structurePallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < structureArray.Length; i++){
			structurePallete.Add((ushort)structureArray[i]);
		}

		metadataPallete = new NativeHashSet<ushort>(0, Allocator.Persistent);
		for(int i = 0; i < metadataArray.Length; i++){
			metadataPallete.Add((ushort)metadataArray[i]);
		}
	}

	public static void Destroy(){
		if(!basicPallete.IsCreated)
			return;

		basicPallete.Dispose();
		grasslandsPallete.Dispose();
		oceanPallete.Dispose();
		forestPallete.Dispose();
		icelandsPallete.Dispose();
		sandlandsPallete.Dispose();
		hellPallete.Dispose();
		corePallete.Dispose();
		structurePallete.Dispose();
		metadataPallete.Dispose();
	}


	// Writes Chunk c's data using a Pallete's compression into given buffer
	// and returns the amount of bytes written
	public static int CompressBlocks(Chunk c, byte[] buffer, int targetPos=0){
		int bytes;
		Pallete p = Compression.BiomeToPallete(c.biomeName);
		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = NativeTools.CopyToNative(c.data.GetData());
		NativeArray<byte> buff = NativeTools.CopyToNative(buffer);

		CompressionJob cbJob = new CompressionJob{
			chunkData = chunkData,
			buffer = buff,
			pallete = Compression.GetPallete(p),
			writtenBytes = writtenBytes
		};

		JobHandle handle = cbJob.Schedule();
		handle.Complete();

		NativeArray<byte>.Copy(buff, 0, buffer, targetPos, writtenBytes[0]);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;		
	}

	// Writes Chunk c's HP metadata into given buffer
	// and returns the amount of bytes written
	public static int CompressMetadataHP(Chunk c, byte[] buffer, int targetPos=0){
		Pallete p = Pallete.METADATA;
		int bytes;

		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = NativeTools.CopyToNative(c.metadata.GetHPData());
		NativeArray<byte> buff = NativeTools.CopyToNative(buffer);

		CompressionJob cmdJob = new CompressionJob{
			chunkData = chunkData,
			buffer = buff,
			pallete = Compression.GetPallete(p),
			writtenBytes = writtenBytes		
		};

		JobHandle handle = cmdJob.Schedule();
		handle.Complete();

		NativeArray<byte>.Copy(buff, 0, buffer, targetPos, writtenBytes[0]);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;
	}

	// Writes Chunk c's state metadata into given buffer
	// and returns the amount of bytes written
	public static int CompressMetadataState(Chunk c, byte[] buffer, int targetPos=0){
		Pallete p = Pallete.METADATA;
		int bytes;

		NativeArray<int> writtenBytes = new NativeArray<int>(new int[1]{0}, Allocator.TempJob);
		NativeArray<ushort> chunkData = NativeTools.CopyToNative(c.metadata.GetStateData());
		NativeArray<byte> buff = NativeTools.CopyToNative(buffer);

		CompressionJob cmdJob = new CompressionJob{
			chunkData = chunkData,
			buffer = buff,
			pallete = Compression.GetPallete(p),
			writtenBytes = writtenBytes		
		};

		JobHandle handle = cmdJob.Schedule();
		handle.Complete();

		NativeArray<byte>.Copy(buff, 0, buffer, targetPos, writtenBytes[0]);

		bytes = writtenBytes[0];

		chunkData.Dispose();
		buff.Dispose();
		writtenBytes.Dispose();

		return bytes;
	}


	// Builds Chunk's VoxelData using Decompression algorithm
	public static void DecompressBlocks(Chunk c, byte[] buffer){
		// Preparation Variables
		Pallete p = Compression.BiomeToPallete(c.biomeName);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = NativeTools.CopyToNative(buffer);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = Compression.GetPallete(p),
			initialPos = 0
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.data.SetData(data.ToArray(), true);


		data.Dispose();
		readData.Dispose();
	}

	// Builds byte message from Server using Decompression algorithm
	public static void DecompressBlocksClient(Chunk c, byte[] buffer, int initialPos){
		// Preparation Variables
		Pallete p = Compression.BiomeToPallete(c.biomeName);

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = NativeTools.CopyToNative(buffer);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = Compression.GetPallete(p),
			initialPos = initialPos
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.data.SetData(data.ToArray(), false);

		data.Dispose();
		readData.Dispose();
	}

	// Builds Chunk's HP Metadata using Decompression algorithm
	public static void DecompressMetadataHP(Chunk c, byte[] buffer){
		Pallete p = Pallete.METADATA;

		// Preparation Variables
		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = NativeTools.CopyToNative(buffer);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = Compression.GetPallete(p),
			initialPos = 0
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetHPData(data.ToArray());

		data.Dispose();
		readData.Dispose();
	} 

	// Builds Chunk's HP Metadata using Decompression algorithm
	public static void DecompressMetadataHPClient(Chunk c, byte[] buffer, int initialPos){
		// Preparation Variables
		Pallete p = Pallete.METADATA;

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = NativeTools.CopyToNative(buffer);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = Compression.GetPallete(p),
			initialPos = initialPos
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetHPData(data.ToArray());

		data.Dispose();
		readData.Dispose();
	} 

	// Builds Chunk's State Metadata using Decompression algorithm
	public static void DecompressMetadataState(Chunk c, byte[] buffer){
		// Preparation Variables
		Pallete p = Pallete.METADATA;

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = NativeTools.CopyToNative(buffer);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = Compression.GetPallete(p),
			initialPos = 0
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetStateData(data.ToArray());
		
		data.Dispose();
		readData.Dispose();
	}

	// Builds Chunk's State Metadata using Decompression algorithm
	public static void DecompressMetadataStateClient(Chunk c, byte[] buffer, int initialPos){
		// Preparation Variables
		Pallete p = Pallete.METADATA;

		NativeArray<ushort> data = new NativeArray<ushort>(Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth, Allocator.TempJob);
		NativeArray<byte> readData = NativeTools.CopyToNative(buffer);

		DecompressJob dbJob = new DecompressJob{
			data = data,
			readData = readData,
			pallete = Compression.GetPallete(p),
			initialPos = initialPos
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		c.metadata.SetStateData(data.ToArray());
		
		data.Dispose();
		readData.Dispose();
	}

	// Compresses blocks in Structures array
	public static ushort[] CompressStructureBlocks(ushort[] uncompressedBlocks, bool printOut=false){
		StringBuilder sb = new StringBuilder();

		Pallete p = Pallete.STRUCTUREBLOCKS;
		NativeList<ushort> outputData = new NativeList<ushort>(0, Allocator.TempJob);
		NativeArray<ushort> inputData = NativeTools.CopyToNative(uncompressedBlocks);

		CompressStructJob cbJob = new CompressStructJob{
			inputData = inputData,
			outputData = outputData,
			pallete = Compression.GetPallete(p)
		};

		JobHandle handle = cbJob.Schedule();
		handle.Complete();

		ushort[] output = NativeTools.CopyToManaged<ushort>(outputData);

		inputData.Dispose();
		outputData.Dispose();

		if(printOut){
			for(int i=0; i < output.Length; i++){
				sb.Append(output[i]);
				if(i != output.Length-1)
					sb.Append(",");
			}

			Debug.Log(sb.ToString());
		}

		return output;
	}

	// Decompresses blocks in Structures array
	public static ushort[] DecompressStructureBlocks(ushort[] compressedBlocks, bool printOut=false){
		StringBuilder sb = new StringBuilder();

		Pallete p = Pallete.STRUCTUREBLOCKS;

		NativeList<ushort> outputData = new NativeList<ushort>(0, Allocator.TempJob);
		NativeArray<ushort> inputData = NativeTools.CopyToNative(compressedBlocks);

		DecompressStructJob dbJob = new DecompressStructJob{
			outputData = outputData,
			inputData = inputData,
			pallete = Compression.GetPallete(p)
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		ushort[] output = NativeTools.CopyToManaged<ushort>(outputData);

		outputData.Dispose();
		inputData.Dispose();

		if(printOut){
			for(int i=0; i < output.Length; i++){
				sb.Append(output[i]);
				if(i != output.Length-1)
					sb.Append(",");
			}

			Debug.Log(sb.ToString());
		}

		return output;
	}

	// Compresses either hp or state in Structures array
	public static ushort[] CompressStructureMetadata(ushort[] uncompressedMeta, bool printOut=false){
		StringBuilder sb = new StringBuilder();

		Pallete p = Pallete.METADATA;

		NativeList<ushort> outputData = new NativeList<ushort>(0, Allocator.TempJob);
		NativeArray<ushort> inputData = NativeTools.CopyToNative(uncompressedMeta);

		CompressStructJob cbJob = new CompressStructJob{
			inputData = inputData,
			outputData = outputData,
			pallete = Compression.GetPallete(p)
		};

		JobHandle handle = cbJob.Schedule();
		handle.Complete();

		ushort[] output = NativeTools.CopyToManaged<ushort>(outputData);

		inputData.Dispose();
		outputData.Dispose();

		if(printOut){
			for(int i=0; i < output.Length; i++){
				sb.Append(output[i]);
				if(i != output.Length-1)
					sb.Append(",");
			}

			Debug.Log(sb.ToString());
		}

		return output;
	}

	// Decompresses metadata in Structures array
	public static ushort[] DecompressStructureMetadata(ushort[] compressedMeta, bool printOut=false){
		StringBuilder sb = new StringBuilder();

		Pallete p = Pallete.METADATA;

		NativeList<ushort> outputData = new NativeList<ushort>(0, Allocator.TempJob);
		NativeArray<ushort> inputData = NativeTools.CopyToNative(compressedMeta);

		DecompressStructJob dbJob = new DecompressStructJob{
			outputData = outputData,
			inputData = inputData,
			pallete = Compression.GetPallete(p)
		};
		JobHandle job = dbJob.Schedule();
		job.Complete();

		ushort[] output = NativeTools.CopyToManaged<ushort>(outputData);

		outputData.Dispose();
		inputData.Dispose();

		if(printOut){
			for(int i=0; i < output.Length; i++){
				sb.Append(output[i]);
				if(i != output.Length-1)
					sb.Append(",");
			}

			Debug.Log(sb.ToString());
		}

		return output;
	}

	// Formats and compresses structure input from PreRead operation
	public static string FormatPrereadInformation(string st){
		StringBuilder sb = new StringBuilder();

		List<ushort> outputList = new List<ushort>();
		string[] splitted = st.Replace("{", "").Replace("}", "").Split(",");

		foreach(string s in splitted){
			if(s != "")
				outputList.Add((ushort)Convert.ToInt16(s));
		}

		ushort[] compressedOutput = Compression.CompressStructureBlocks(outputList.ToArray());

		sb.Append("{");
		for(int i=0; i < compressedOutput.Length; i++){
			sb.Append(compressedOutput[i]);
			if(i != compressedOutput.Length-1)
				sb.Append(",");
		}
		sb.Append("}");

		return sb.ToString();
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
			case "Snowy Plains":
				return Pallete.ICELANDS;
			case "Snowy Highlands":
				return Pallete.ICELANDS;
			case "Ice Ocean":
				return Pallete.ICELANDS;
			case "Snow Forest":
				return Pallete.ICELANDS;
			case "Desert":
				return Pallete.SANDLANDS;
			case "Hell Plains":
				return Pallete.HELL;
			case "Core":
				return Pallete.CORE;
			default:
				return Pallete.BASIC;
		}
	}

	public static bool PalleteContains(Pallete p, ushort x){
		return Compression.GetPallete(p).Contains(x);
	}

	// Returns a Palleted list
	private static NativeHashSet<ushort> GetPallete(Pallete p){
		switch(p){
			case Pallete.BASIC:
				return Compression.basicPallete;

			case Pallete.GRASSLANDS:
				return Compression.grasslandsPallete;

			case Pallete.OCEAN:
				return Compression.oceanPallete;

			case Pallete.FOREST:
				return Compression.forestPallete;

			case Pallete.ICELANDS:
				return Compression.icelandsPallete;

			case Pallete.SANDLANDS:
				return Compression.sandlandsPallete;

			case Pallete.HELL:
				return Compression.hellPallete;

			case Pallete.CORE:
				return Compression.corePallete;

			// Special Pallete used for Structure blocks Compression
			case Pallete.STRUCTUREBLOCKS:
				return Compression.structurePallete;

			// Special Pallete used for Metadata Compression
			case Pallete.METADATA:
				return Compression.metadataPallete;

			default:
				return Compression.basicPallete;
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
	ICELANDS,
	SANDLANDS,
	HELL,
	CORE,
	STRUCTUREBLOCKS,
	METADATA
}



/*
MULTITHREADING
*/
[BurstCompile]
public struct CompressionJob : IJob{
	public NativeArray<ushort> chunkData;
	public NativeArray<byte> buffer;
	public NativeHashSet<ushort> pallete;
	public NativeArray<int> writtenBytes;

	public void Execute(){
		ushort blockCode;
		ushort bufferedBlock = 0;
		int bufferedCount = 0;
		bool contains;

		// Block Data
		for(int y=0; y<Chunk.chunkDepth; y++){
			for(int x=0; x<Chunk.chunkWidth; x++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					blockCode = chunkData[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					contains = pallete.Contains(blockCode);

					// Case found block is not in Pallete and not buffered
					if(!contains && bufferedCount == 0){
						WriteShort(blockCode, writtenBytes[0]);
						writtenBytes[0] += 2;
					}
					// Case found block is not in Pallete and is buffered
					else if(!contains){
						WriteShort(bufferedBlock, writtenBytes[0]);
						writtenBytes[0] += 2;
						WriteShort((ushort)bufferedCount, writtenBytes[0]);
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
						WriteShort((ushort)bufferedCount, writtenBytes[0]);
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
			WriteShort((ushort)bufferedCount, writtenBytes[0]);
			writtenBytes[0] += 2;
		}
	}

	// Writes a short block data to a buffer in a certain position
	private void WriteShort(ushort data, int pos){
		buffer[pos] = (byte)(data >> 8);
		buffer[pos+1] = (byte)data;
	}
}


[BurstCompile]
public struct DecompressJob : IJob{
	[ReadOnly]
	public NativeHashSet<ushort> pallete;
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
						if(pallete.Contains(blockCode)){
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
}


[BurstCompile]
public struct CompressStructJob : IJob{
	[ReadOnly]
	public NativeArray<ushort> inputData;
	[ReadOnly]
	public NativeHashSet<ushort> pallete;
	public NativeList<ushort> outputData;

	public void Execute(){
		ushort blockCode;
		ushort bufferedBlock = 0;
		int bufferedCount = 0;
		bool contains;

		for(int i=0; i < inputData.Length; i++){
			blockCode = inputData[i];
			contains = pallete.Contains(blockCode);

			// Case found block is not in Pallete and not buffered
			if(!contains && bufferedCount == 0){
				outputData.Add(blockCode);
			}
			// Case found block is not in Pallete and is buffered
			else if(!contains){
				outputData.Add(bufferedBlock);
				outputData.Add((ushort)bufferedCount);
				outputData.Add(blockCode);
				bufferedCount = 0;
			}
			// Case found block is in Pallete and is the buffered block
			else if(contains && bufferedBlock == blockCode && bufferedCount > 0){
				bufferedCount++;
			}
			// Case found block is in Pallete and is buffered by another block
			else if(contains && bufferedBlock != blockCode && bufferedCount > 0){
				outputData.Add(bufferedBlock);
				outputData.Add((ushort)bufferedCount);	
				bufferedBlock = blockCode;
				bufferedCount = 1;					
			}
			// General case of finding a palleted block that is not buffered
			else{
				bufferedBlock = blockCode;
				bufferedCount = 1;
			}
	
		}
		// Writes to buffer if chunk ends on a buffered stream
		if(bufferedCount > 0){
			outputData.Add(bufferedBlock);
			outputData.Add((ushort)bufferedCount);
		}
	}
}


[BurstCompile]
public struct DecompressStructJob : IJob{
	[ReadOnly]
	public NativeHashSet<ushort> pallete;
	[ReadOnly]
	public NativeArray<ushort> inputData;
	public NativeList<ushort> outputData;

	public void Execute(){
		ushort bufferedCount = 0;
		ushort blockCode = 0;

		// Block Data
		for(int i=0; i < inputData.Length; i++){
			blockCode = inputData[i];

			// If code is contained in Pallete
			if(pallete.Contains(blockCode)){
				i++;
				bufferedCount = inputData[i];

				for(; bufferedCount > 0; bufferedCount--){
					outputData.Add(blockCode);
				}
			}
			else{
				outputData.Add(blockCode);
			}
		}
			
	}

}