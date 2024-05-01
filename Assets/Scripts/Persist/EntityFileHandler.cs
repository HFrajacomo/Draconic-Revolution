using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityFileHandler{
	private string worldName;
	private static readonly string fileFormat = ".edf";
	private static readonly int headerSize = 4;
	private int seed;
	public TimeOfDay globalTime;

	// Unity Reference
	private ChunkLoader_Server cl;
	private EntityHandler_Server entityHandler;

	// Data
	private string saveDir;
	private string worldDir;

	// Region Pool
	private Dictionary<ChunkPos, RegionFile> pool = new Dictionary<ChunkPos, RegionFile>();

	// Buffer
	private byte[] mainBuffer = new byte[5242880]; // 5MB of space
	private int mainIndex = 0;
	private byte[] intBuffer = new byte[4];
	private byte[] indexArray = new byte[16];

	// Cache
	private Item cachedItem;
	private Weapon cachedWeapon;
	private ItemStack cachedIts;
	private ChunkPos cachedRegionPos;
	private AbstractAI cachedAI;
	private DroppedItemAI cachedDropItem;

	////  Drop Item type Item -> 21
	// EntityType [1]
	// MemoryStorageType [1]
	// ItemId [2]
	// Amount [1] 
	// CurrentTick [4]
	// Position [12]

	//// Drop Item type Weapon -> 26
	// EntityType [1]
	// MemoryStorageType [1]
	// ItemID [2]
	// Current Durability [4]
	// Refine Level [1]
	// Enchantment [1]
	// CurrentTick [4]
	// Position [12]


	public EntityFileHandler(ChunkLoader_Server cl){
		this.cl = cl;
		this.entityHandler = this.cl.server.entityHandler;
		this.worldName = World.worldName;
		this.globalTime = GameObject.Find("/Time Counter").GetComponent<TimeOfDay>();

		InitDirectories();
	}

	private void InitDirectories(){
		this.saveDir = EnvironmentVariablesCentral.saveDir;
		this.worldDir = this.saveDir + this.worldName + "\\";	

		// If "Worlds/" dir doesn't exist
		if(!Directory.Exists(this.saveDir)){
			Directory.CreateDirectory(this.saveDir);
		}

		// If current world doesn't exist
		if(!Directory.Exists(this.worldDir)){
			Directory.CreateDirectory(this.worldDir);
		}
	}

	// Initializes all files after received first player
	public void InitDataFiles(ChunkPos pos){
		LoadRegionFile(pos);
	}

	// Saves an entire chunk-worth of entities
	public void SaveChunkEntities(ChunkPos pos, List<AbstractAI> entityList){
		long chunkCode = GetLinearRegionCoords(pos);
		long seekPosition = 0;

		GetCorrectRegion(pos);
		this.cachedRegionPos = ConvertToRegion(pos);
		mainIndex = 0;


		EncodeEntityInformation(entityList);

		// If chunk data exists
		if(IsIndexed(pos)){
			ReadHeader(pos);
			int lastChunkSize = NetDecoder.ReadInt(intBuffer, 0);

			ChunkPos regionPos = ConvertToRegion(pos);

			this.pool[regionPos].AddHole(this.pool[regionPos].index[chunkCode], lastChunkSize + headerSize);
			seekPosition = this.pool[regionPos].FindPosition(headerSize + mainIndex);
			this.pool[regionPos].SaveHoles();

			// If position in RegionFile has changed
			if(seekPosition != this.pool[regionPos].index[chunkCode]){
				this.pool[regionPos].index[chunkCode] = seekPosition;
				this.pool[regionPos].UnloadIndex();
			}

			NetDecoder.WriteInt(mainIndex, intBuffer, 0);

			// Saves Chunk
			this.pool[regionPos].Write(seekPosition, intBuffer, 4);
			this.pool[regionPos].Write(seekPosition+4, mainBuffer, mainIndex);
		}
		// If chunk is new
		else{
			ChunkPos regionPos = ConvertToRegion(pos);

			seekPosition = this.pool[regionPos].FindPosition(headerSize + mainIndex);
			this.pool[regionPos].SaveHoles();

			// Adds new chunk to Index
			this.pool[regionPos].index.Add(chunkCode, seekPosition);
			AddEntryIndex(chunkCode, seekPosition);
			this.pool[regionPos].indexFile.Write(indexArray, 0, 16);
			this.pool[regionPos].indexFile.Flush();

			NetDecoder.WriteInt(mainIndex, intBuffer, 0);

			// Saves Chunk
			this.pool[regionPos].Write(seekPosition, intBuffer, 4);
			this.pool[regionPos].Write(seekPosition+4, mainBuffer, mainIndex);
		}

		mainIndex = 0;
	}

	// Loads a chunk-worth of entities
	public void LoadChunkEntities(ChunkPos pos){
		GetCorrectRegion(pos);

		if(!IsIndexed(pos))
			return;

		int ticksPassed = this.cl.time.TicksPassedFrom(this.cl.chunks[pos].lastVisitedTime);

		ReadHeader(pos);
		int chunkSize = NetDecoder.ReadInt(intBuffer, 0);

		this.pool[ConvertToRegion(pos)].file.Read(mainBuffer, 0, chunkSize);

		RecreateEntities(chunkSize, ticksPassed);
	}

	private void RecreateEntities(int chunkSize, int ticksPassed){
		int i=0;
		EntityType type;
		MemoryStorageType storageType;
		ushort itemID;
		byte amount, refineLevel, enchantment, state;
		int currentTick;
		float3 position;
		uint currentDurability;
		Item item = Item.GenerateItem(ItemID.STONEBLOCK);
		Weapon weapon = item as Weapon;

		while(i < chunkSize){
			type = (EntityType)mainBuffer[i];
			i++;

			if(type == EntityType.DROP){
				storageType = (MemoryStorageType)mainBuffer[i];
				i++;

				switch(storageType){
					case MemoryStorageType.ITEM:
						state = mainBuffer[i];
						i++;
						itemID = NetDecoder.ReadUshort(mainBuffer, i);
						i += 2;
						amount = mainBuffer[i];
						i++;
						currentTick = NetDecoder.ReadInt(mainBuffer, i);
						i += 4;
						position = NetDecoder.ReadFloat3(mainBuffer, i);
						i += 12;
						item = Item.GenerateItem(itemID);
						break;
					case MemoryStorageType.WEAPON:
						state = mainBuffer[i];
						i++;
						itemID = NetDecoder.ReadUshort(mainBuffer, i);
						i += 2;
						currentDurability = NetDecoder.ReadUint(mainBuffer, i);
						i += 4;
						refineLevel = mainBuffer[i];
						i++;
						enchantment = mainBuffer[i];
						i++;
						currentTick = NetDecoder.ReadInt(mainBuffer, i);
						i += 4;
						position = NetDecoder.ReadFloat3(mainBuffer, i);
						i += 12;
						weapon = (Weapon)Item.GenerateItem(itemID);
						weapon.currentDurability = currentDurability;
						weapon.refineLevel = refineLevel;
						weapon.extraEffect = (EnchantmentType)enchantment;
						amount = 1;
						break;
					default:
						currentTick = 0;
						position = new float3(0,0,0);
						refineLevel = 0;
						enchantment = 0;
						amount = 0;
						state = 0;
						break;					
				}

				if(HasPassedItemLifespan(ticksPassed, currentTick))
					continue;

				if(storageType == MemoryStorageType.ITEM){
					this.entityHandler.AddItem(position, item, amount, state, this.cl);
				}
				else if(storageType == MemoryStorageType.WEAPON){
					this.entityHandler.AddItem(position, weapon, amount, state, this.cl);
				}

			}
			else{
				// NOT IMPLEMENTED
				break;
			}
		}
	}

	private bool HasPassedItemLifespan(int ticksPassed, int currentTick){
		return ticksPassed + currentTick >= Constants.ITEM_ENTITY_LIFE_SPAN_TICKS;
	}

	private void EncodeEntityInformation(List<AbstractAI> entityList){
		// Encodes Entity information
		for(int i=0; i < entityList.Count; i++){
			this.cachedAI = entityList[i];

			if(this.cachedAI.markedForDelete)
				continue;

			switch(this.cachedAI.GetID().type){
				case EntityType.DROP:
					this.cachedDropItem = (DroppedItemAI)this.cachedAI;
					WriteDroppedItem(this.cachedDropItem);
					break;
				default:
					break;
			}
		}
	}

	// Adds Dropped Item entity information to MainBuffer
	private int WriteDroppedItem(DroppedItemAI ai){
		this.cachedIts = ai.GetItemStack();
		this.cachedItem = this.cachedIts.GetItem();

		mainBuffer[mainIndex] = (byte)EntityType.DROP;
		mainIndex++;

		switch(this.cachedItem.memoryStorageType){
			case MemoryStorageType.ITEM:
				mainBuffer[mainIndex] = (byte)MemoryStorageType.ITEM;
				mainIndex++;
				mainBuffer[mainIndex] = (byte)ai.GetState();
				mainIndex++;
				NetDecoder.WriteUshort((ushort)this.cachedItem.id, mainBuffer, mainIndex);
				mainIndex += 2;
				mainBuffer[mainIndex] = this.cachedIts.GetAmount();
				mainIndex++;
				NetDecoder.WriteInt(ai.GetLifespan(), mainBuffer, mainIndex);
				mainIndex += 4;
				NetDecoder.WriteFloat3(ai.GetPosition(), mainBuffer, mainIndex);
				mainIndex += 12;
				break;
			case MemoryStorageType.WEAPON:
				this.cachedWeapon = (Weapon)this.cachedItem;

				mainBuffer[mainIndex] = (byte)MemoryStorageType.WEAPON;
				mainIndex++;
				mainBuffer[mainIndex] = (byte)ai.GetState();
				mainIndex++;
				NetDecoder.WriteUshort((ushort)this.cachedWeapon.id, mainBuffer, mainIndex);
				mainIndex += 2;
				NetDecoder.WriteUint(this.cachedWeapon.currentDurability, mainBuffer, mainIndex);
				mainIndex += 4;
				mainBuffer[mainIndex] = this.cachedWeapon.refineLevel;
				mainIndex++;
				mainBuffer[mainIndex] = (byte)this.cachedWeapon.extraEffect;
				NetDecoder.WriteInt(ai.GetLifespan(), mainBuffer, mainIndex);
				mainIndex += 4;
				NetDecoder.WriteFloat3(ai.GetPosition(), mainBuffer, mainIndex);
				mainIndex += 12;
				break;
			default:
				break;
		}

		return mainIndex;
	}

	// Loads RegionFile related to given Chunk
	public void LoadRegionFile(ChunkPos pos){
		int rfx;
		int rfz;
		string name;

		rfx = Mathf.FloorToInt(pos.x / Constants.CHUNKS_IN_REGION_FILE);
		rfz = Mathf.FloorToInt(pos.z / Constants.CHUNKS_IN_REGION_FILE);
		name = "e" + rfx.ToString() + "x" + rfz.ToString() + "_" + pos.y;

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

	// Reads header from a chunk
	// leaves rdf file at Seek Position ready to read blockdata
	private void ReadHeader(ChunkPos pos){
		long code = GetLinearRegionCoords(pos);

		this.pool[ConvertToRegion(pos)].file.Seek(this.pool[ConvertToRegion(pos)].index[code], SeekOrigin.Begin);
		this.pool[ConvertToRegion(pos)].file.Read(intBuffer, 0, 4);
	}

	// Closes all streams in pool
	public void CloseAll(){
		foreach(ChunkPos pos in this.pool.Keys){
			this.pool[pos].Close();
		}
	}

	// Checks if RegionFile represents ChunkPos, and loads correct RegionFile if not
	public void GetCorrectRegion(ChunkPos pos){
		if(!CheckUsage(pos)){
			LoadRegionFile(pos);
		}
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

	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*Constants.CHUNKS_IN_REGION_FILE + pos.x);
	}
}