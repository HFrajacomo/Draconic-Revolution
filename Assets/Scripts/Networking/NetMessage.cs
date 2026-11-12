using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
A network message
	Outgoing messages are built in a single static buffer and must be sent right after being built, since sending copies them (main thread only)
	Received messages hold a pooled array (NetBufferPool) that can be bigger than the message, so size must be used instead of data.Length
	A received message without data is the close event of its connection

Message layouts are documented in Game Details/Networking.md
Local voxel coordinates are packed in 2 bytes (NetDecoder.WriteLocalPos), so they must be inside the chunk
*/
public struct NetMessage
{
	public NetCode code;
	public int size;
	private static byte[] buffer = new byte[Chunk.chunkWidth * Chunk.chunkDepth * Chunk.chunkWidth * 5]; // 320KB
	private byte[] data;
	private NetConnection connection;

	private static bool broadcastReceived = false;
	private static bool broadcastSent = false;
	private static bool broadcastProcessed = false;

	// Constructor of outgoing messages
	public NetMessage(NetCode code){
		this.code = code;
		this.data = null;
		this.connection = null;
		NetMessage.buffer[0] = (byte)code;
		this.size = 1;
	}

	// Constructor of received messages
	public NetMessage(byte[] data, int size, NetConnection connection){
		this.code = (NetCode)data[0];
		this.data = data;
		this.size = size;
		this.connection = connection;
	}

	// Message that informs the owner of a connection that it was closed
	public static NetMessage CloseEvent(NetConnection connection){
		NetMessage message = new NetMessage();
		message.code = NetCode.NONE;
		message.data = null;
		message.size = 0;
		message.connection = connection;
		return message;
	}

	// Local clock in milliseconds used to timestamp entity snapshots. Wraps around safely, receivers only use deltas
	public static uint GetTimestamp(){
		return (uint)((ulong)(Time.unscaledTimeAsDouble * 1000.0) & 0xFFFFFFFF);
	}

	// Gets the buffer of outgoing messages
	public byte[] GetMessage(){
		return NetMessage.buffer;
	}

	// Assigns the current NetCode to the first element of the buffer
	public void Reset(){
		NetMessage.buffer[0] = (byte)this.code;
	}

	public int GetSize(){return this.size;}
	public byte[] GetData(){return this.data;}
	public NetConnection GetConnection(){return this.connection;}
	public bool IsCloseEvent(){return this.data == null;}

	// Broadcasts message to stdout
	public static void Broadcast(NetBroadcast bc, byte netcode, ulong id, int length){
		if(bc == NetBroadcast.RECEIVED){
			if(NetMessage.broadcastReceived)
				Debug.Log("Received: " + (NetCode)netcode + " | " + id + " > " + length);
		}
		else if(bc == NetBroadcast.SENT){
			if(NetMessage.broadcastSent)
				Debug.Log("Sent: " + (NetCode)netcode + " | " + id + " > " + length);
		}
		else if(bc == NetBroadcast.PROCESSED){
			if(NetMessage.broadcastProcessed)
				Debug.Log("Processed: " + (NetCode)netcode + " | " + id + " > " + length);
		}
		else if(bc == NetBroadcast.TEST){
			Debug.Log("TESTING: " + (NetCode)netcode + " | " + id + " > " + length);
		}
	}

	/*
	Connection and World
	*/

	// Client sending initial information to server after connection was accepted
	// {CODE}[AccountID 8][Render 4][Seed 4][worldName (string16)]
	public void SendClientInfo(ulong accountID, int playerRenderDistance, int seed, string worldName){
		if(!World.isClient)
			worldName = "";

		Reset();
		NetDecoder.WriteLong(accountID, NetMessage.buffer, 1);
		NetDecoder.WriteInt(playerRenderDistance, NetMessage.buffer, 9);
		NetDecoder.WriteInt(seed, NetMessage.buffer, 13);
		this.size = 17 + NetDecoder.WriteString16(worldName, NetMessage.buffer, 17);
	}

	// Server sending player character position, the game time and the NetID of the player
	// {CODE}[Pos 12][Dir 12][Day 4][Hour][Minute][NetID 4]
	public void SendServerInfo(float xPos, float yPos, float zPos, float xDir, float yDir, float zDir, uint day, byte hour, byte minute, uint netID){
		Reset();
		NetDecoder.WriteFloat3(xPos, yPos, zPos, NetMessage.buffer, 1);
		NetDecoder.WriteFloat3(xDir, yDir, zDir, NetMessage.buffer, 13);
		NetDecoder.WriteUint(day, NetMessage.buffer, 25);
		NetDecoder.WriteByte(hour, NetMessage.buffer, 29);
		NetDecoder.WriteByte(minute, NetMessage.buffer, 30);
		NetDecoder.WriteUint(netID, NetMessage.buffer, 31);
		this.size = 35;
	}

	// Client asking for a chunk of its StreamWindow that never arrived
	public void RequestChunkLoad(ChunkPos pos){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		this.size = 10;
	}

	// Client informing the Server of the center of its StreamWindow
	// {CODE}[ChunkPos][VerticalLayer (sbyte)]
	public void StreamCenter(ChunkPos center, int verticalLayer){
		Reset();
		NetDecoder.WriteChunkPos(center, NetMessage.buffer, 1);
		NetMessage.buffer[10] = (byte)(sbyte)verticalLayer;
		this.size = 11;
	}

	// Client acknowledging the streamed chunks it consumed and informing how many are still waiting to be loaded
	// {CODE}[Consumed 2][Backlog 2]
	public void ChunkAck(int consumed, int backlog){
		Reset();
		NetDecoder.WriteUshort((ushort)Math.Min(consumed, ushort.MaxValue), NetMessage.buffer, 1);
		NetDecoder.WriteUshort((ushort)Math.Min(backlog, ushort.MaxValue), NetMessage.buffer, 3);
		this.size = 5;
	}

	// Server sending a chunk to Client
	// {CODE}[ChunkPos][Biome][BlockSize 4][HPSize 4][StateSize 4] | Compressed blocks, HP and states
	public void SendChunk(Chunk c){
		int blockDataSize, hpDataSize, stateDataSize;

		Reset();
		Compression.CompressChunk(c, NetMessage.buffer, CHUNK_HEADER_SIZE, out blockDataSize, out hpDataSize, out stateDataSize);
		WriteChunkHeader(c.pos, c.biome, blockDataSize, hpDataSize, stateDataSize, NetMessage.buffer);

		this.size = CHUNK_HEADER_SIZE + blockDataSize + hpDataSize + stateDataSize;
	}

	// Builds a SENDCHUNK message from compressed data stored in disk (blocks, HP and states one after the other)
	public static byte[] BuildChunkPayload(ChunkPos pos, byte biome, byte[] compressed, int blockSize, int hpSize, int stateSize){
		byte[] output = new byte[CHUNK_HEADER_SIZE + blockSize + hpSize + stateSize];

		output[0] = (byte)NetCode.SENDCHUNK;
		WriteChunkHeader(pos, biome, blockSize, hpSize, stateSize, output);
		Array.Copy(compressed, 0, output, CHUNK_HEADER_SIZE, blockSize + hpSize + stateSize);

		return output;
	}

	public static readonly int CHUNK_HEADER_SIZE = 23;

	private static void WriteChunkHeader(ChunkPos pos, byte biome, int blockSize, int hpSize, int stateSize, byte[] output){
		NetDecoder.WriteChunkPos(pos, output, 1);
		output[10] = biome;
		NetDecoder.WriteInt(blockSize, output, 11);
		NetDecoder.WriteInt(hpSize, output, 15);
		NetDecoder.WriteInt(stateSize, output, 19);
	}

	// Sends time data to Client
	public void SendGameTime(uint day, byte hour, byte minute){
		Reset();
		NetDecoder.WriteUint(day, NetMessage.buffer, 1);
		NetDecoder.WriteByte(hour, NetMessage.buffer, 5);
		NetDecoder.WriteByte(minute, NetMessage.buffer, 6);
		this.size = 7;
	}

	// Server sends clients the bytes that composes a Noise (used for global weather noise)
	// {CODE}[Seed 4][Noise]
	public void SendNoise(byte[] noise, int seed){
		Reset();
		NetDecoder.WriteInt(seed, NetMessage.buffer, 1);
		NetDecoder.WriteByteArray(noise, NetMessage.buffer, 5);
		this.size = 5 + noise.Length;
	}

	/*
	Voxels
	*/

	// Server sends the new data of a single voxel to Clients
	// {CODE}[ChunkPos][Local 2][BUDCode][Block 2][State 2][HP 2]
	public void BlockUpdate(BUDCode type, ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state, ushort hp){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetDecoder.WriteByte((byte)type, NetMessage.buffer, 12);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 13);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 15);
		NetDecoder.WriteUshort(hp, NetMessage.buffer, 17);
		this.size = 19;
	}

	// Server sends the new data of many voxels of a chunk to Clients. Voxels are added with AddBlockBatch()
	// {CODE}[ChunkPos][Count 2] | Count x [Local 2][Block 2][State 2][HP 2]
	public void BlockBatch(ChunkPos pos){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteUshort(0, NetMessage.buffer, 10);
		this.size = 12;
	}

	public void AddBlockBatch(int x, int y, int z, ushort blockCode, ushort state, ushort hp){
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, this.size);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, this.size+2);
		NetDecoder.WriteUshort(state, NetMessage.buffer, this.size+4);
		NetDecoder.WriteUshort(hp, NetMessage.buffer, this.size+6);
		this.size += 8;

		NetDecoder.WriteUshort((ushort)((this.size - 12)/8), NetMessage.buffer, 10);
	}

	// Client places a block or object. Origin of the placed item is sent so the server can echo it back in a PlacementDenied
	// ActionSlot is byte.MaxValue when the placement did not come from an EntityAction
	// {CODE}[ChunkPos][Local 2][Facing (sbyte)][Block 2][Slot][NewQuantity][InventoryCode][ItemID 2][ActionSlot][ActionID 2]
	public void PlaceBlock(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, byte slot, byte newQuantity, byte inventoryCode, ushort itemID, byte actionSlot=byte.MaxValue, ushort actionID=0){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetMessage.buffer[12] = (byte)(sbyte)facing;
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 13);
		NetDecoder.WriteByte(slot, NetMessage.buffer, 15);
		NetDecoder.WriteByte(newQuantity, NetMessage.buffer, 16);
		NetDecoder.WriteByte(inventoryCode, NetMessage.buffer, 17);
		NetDecoder.WriteUshort(itemID, NetMessage.buffer, 18);
		NetDecoder.WriteByte(actionSlot, NetMessage.buffer, 20);
		NetDecoder.WriteUshort(actionID, NetMessage.buffer, 21);
		this.size = 23;
	}

	// Server tells a client that its placement was denied, echoing where the placed item came from
	// {CODE}[itemID (2)][inventoryCode][slot][actionSlot][actionID (2)]
	public void PlacementDenied(ushort itemID, byte inventoryCode, byte slot, byte actionSlot, ushort actionID){
		Reset();
		NetDecoder.WriteUshort(itemID, NetMessage.buffer, 1);
		NetDecoder.WriteByte(inventoryCode, NetMessage.buffer, 3);
		NetDecoder.WriteByte(slot, NetMessage.buffer, 4);
		NetDecoder.WriteByte(actionSlot, NetMessage.buffer, 5);
		NetDecoder.WriteUshort(actionID, NetMessage.buffer, 6);
		this.size = 8;
	}

	// Clients sends the voxels of a chunk that require a LOAD BUD operation. Voxels are added with AddBatchLoad()
	// {CODE}[ChunkPos][Count 2] | Count x [Local 2]
	public void BatchLoadBUD(ChunkPos pos){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteUshort(0, NetMessage.buffer, 10);
		this.size = 12;
	}

	public void AddBatchLoad(int x, int y, int z){
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, this.size);
		this.size += 2;

		NetDecoder.WriteUshort((ushort)((this.size - 12)/2), NetMessage.buffer, 10);
	}

	// Client sends a voxel coordinate to trigger OnInteraction in server
	// {CODE}[ChunkPos][Local 2][Facing (sbyte)]
	public void Interact(ChunkPos pos, int x, int y, int z, int facing){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetMessage.buffer[12] = (byte)(sbyte)facing;
		this.size = 13;
	}

	// Client damages a voxel
	// {CODE}[ChunkPos][Local 2][Damage 2]
	public void BlockDamage(ChunkPos pos, int x, int y, int z, ushort damage){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetDecoder.WriteUshort(damage, NetMessage.buffer, 12);
		this.size = 14;
	}

	// Server sends the new HP of a damaged voxel to Clients (uses the BLOCKDAMAGE NetCode)
	// {CODE}[ChunkPos][Local 2][HP 2][ShouldRedraw]
	public void BlockHP(ChunkPos pos, int x, int y, int z, ushort newHP, bool shouldRedrawChunk){
		Reset();
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetDecoder.WriteUshort(newHP, NetMessage.buffer, 12);
		NetDecoder.WriteBool(shouldRedrawChunk, NetMessage.buffer, 14);
		this.size = 15;
	}

	// Server sends VFX data to Client
	// {CODE}[ChunkPos][Local 2][Facing (sbyte)][Block 2][State 2]
	public void VFXData(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		Reset();
		WriteVFX(pos, x, y, z, facing, blockCode, state);
	}

	// Server sends VFX change of state to Client
	// {CODE}[ChunkPos][Local 2][Facing (sbyte)][Block 2][State 2]
	public void VFXChange(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		Reset();
		WriteVFX(pos, x, y, z, facing, blockCode, state);
	}

	private void WriteVFX(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetMessage.buffer[12] = (byte)(sbyte)facing;
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 13);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 15);
		this.size = 17;
	}

	// Server sends VFX deletion information to Client
	// {CODE}[ChunkPos][Local 2][Block 2][State 2]
	public void VFXBreak(ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state){
		Reset();
		WriteVoxelEvent(pos, x, y, z, blockCode, state);
	}

	// Server sends a client an order to register an SFX into SFXLoader
	// {CODE}[ChunkPos][Local 2][Block 2][State 2]
	public void SFXPlay(ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state){
		Reset();
		WriteVoxelEvent(pos, x, y, z, blockCode, state);
	}

	private void WriteVoxelEvent(ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteLocalPos(x, y, z, NetMessage.buffer, 10);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 12);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 14);
		this.size = 16;
	}

	/*
	Entities
	*/

	// Clients sends their position to Server. Timestamp is the sender's local clock in ms, used for Snapshot Interpolation
	// {CODE}[Pos 12][Pitch 2][Yaw 2][Timestamp 4]
	public void ClientPlayerPosition(float x, float y, float z, float pitch, float yaw, uint timestamp){
		Reset();
		NetDecoder.WriteFloat3(x, y, z, NetMessage.buffer, 1);
		NetDecoder.WriteAngle(pitch, NetMessage.buffer, 13);
		NetDecoder.WriteAngle(yaw, NetMessage.buffer, 15);
		NetDecoder.WriteUint(timestamp, NetMessage.buffer, 17);
		this.size = 21;
	}

	// Client sends the chunk the player is currently in
	public void ClientChunk(ChunkPos lastPos, ChunkPos newPos){
		Reset();
		NetDecoder.WriteChunkPos(lastPos, NetMessage.buffer, 1);
		NetDecoder.WriteChunkPos(newPos, NetMessage.buffer, 10);
		this.size = 19;
	}

	// Server tells a Client that a player became visible to it, with everything needed to build the player
	// {CODE}[NetID 4][AccountID 8][Pos 12][Pitch 2][Yaw 2][Timestamp 4] | Network CharacterSheet with compact appearance
	public void PlayerSpawn(uint netID, ulong accountID, float3 pos, float pitch, float yaw, uint timestamp, CharacterSheet sheet){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteLong(accountID, NetMessage.buffer, 5);
		NetDecoder.WriteFloat3(pos, NetMessage.buffer, 13);
		NetDecoder.WriteAngle(pitch, NetMessage.buffer, 25);
		NetDecoder.WriteAngle(yaw, NetMessage.buffer, 27);
		NetDecoder.WriteUint(timestamp, NetMessage.buffer, 29);
		this.size = 33 + NetDecoder.WriteCharacterSheetNet(sheet, NetMessage.buffer, 33, compactAppearance:true);
	}

	// Server tells a Client that a dropped item became visible to it
	// {CODE}[ID 4][Pos 12][Standing][RotY 2][RotZ 2][ItemID 2][Amount][Timestamp 4]
	public void DropSpawn(uint id, float x, float y, float z, bool standing, float rotY, float rotZ, ushort itemID, byte amount, uint timestamp){
		Reset();
		NetDecoder.WriteUint(id, NetMessage.buffer, 1);
		NetDecoder.WriteFloat3(x, y, z, NetMessage.buffer, 5);
		NetDecoder.WriteBool(standing, NetMessage.buffer, 17);
		NetDecoder.WriteAngle(rotY, NetMessage.buffer, 18);
		NetDecoder.WriteAngle(rotZ, NetMessage.buffer, 20);
		NetDecoder.WriteUshort(itemID, NetMessage.buffer, 22);
		NetDecoder.WriteByte(amount, NetMessage.buffer, 24);
		NetDecoder.WriteUint(timestamp, NetMessage.buffer, 25);
		this.size = 29;
	}

	// Server tells a Client to delete an entity
	// {CODE}[EntityType][ID 4]
	public void EntityDelete(EntityType type, uint id){
		Reset();
		NetDecoder.WriteByte((byte)type, NetMessage.buffer, 1);
		NetDecoder.WriteUint(id, NetMessage.buffer, 2);
		this.size = 6;
	}

	// Client sends item information for server to create a Dropped Item Entity
	public void DropItem(float posX, float posY, float posZ, float moveX, float moveY, float moveZ, ushort itemCode, byte amount, byte slotId){
		Reset();
		NetDecoder.WriteFloat3(posX, posY, posZ, NetMessage.buffer, 1);
		NetDecoder.WriteFloat3(moveX, moveY, moveZ, NetMessage.buffer, 13);
		NetDecoder.WriteUshort(itemCode, NetMessage.buffer, 25);
		NetDecoder.WriteByte(amount, NetMessage.buffer, 27);
		NetDecoder.WriteByte(slotId, NetMessage.buffer, 28);
		this.size = 29;
	}

	/*
	Characters and Inventory
	*/

	// Server or Client sends inventory information
	public void SendInventory(byte[] data, int length){
		Reset();
		Array.Copy(data, 0, NetMessage.buffer, 1, length);
		this.size = 1 + length;
	}

	// Client asks for existance of Character ID. Used before World load
	public void RequestCharacterExistence(ulong id){
		Reset();
		NetDecoder.WriteLong(id, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Client asks for its own CharacterSheet
	public void RequestCharacterSheet(ulong id){
		Reset();
		NetDecoder.WriteLong(id, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Server sends character appearance and a flag
	// {CODE}[Exists] | If it exists: [CharacterAppearance 247][IsMale]
	public void SendCharacterPreload(CharacterAppearance? app, bool isMale){
		Reset();

		if(app == null){
			NetDecoder.WriteBool(false, NetMessage.buffer, 1);
			this.size = 2;
			return;
		}

		NetDecoder.WriteBool(true, NetMessage.buffer, 1);
		NetDecoder.WriteCharacterAppearance((CharacterAppearance)app, NetMessage.buffer, 2);
		NetDecoder.WriteBool(isMale, NetMessage.buffer, 249);
		this.size = 250;
	}

	// Encodes a CharacterSheet of a player to its owner
	// {CODE}[AccountID 8] | Network CharacterSheet
	public void SendCharSheet(ulong charCode, CharacterSheet sheet){
		Reset();
		NetDecoder.WriteLong(charCode, NetMessage.buffer, 1);
		this.size = 9 + NetDecoder.WriteCharacterSheetNet(sheet, NetMessage.buffer, 9, compactAppearance:false);
	}

	// Client sends character hotbar position to Server
	public void SendHotbarPosition(bool isNormalHotbar, byte hotbarSlot, byte attackHotbarSlot){
		Reset();
		NetDecoder.WriteByte(hotbarSlot, NetMessage.buffer, 1);
		NetDecoder.WriteByte(attackHotbarSlot, NetMessage.buffer, 2);
		NetDecoder.WriteBool(isNormalHotbar, NetMessage.buffer, 3);
		this.size = 4;
	}

	// Server sends character item in hand to Clients
	// {CODE}[NetID 4][IsItem][ItemID 2][ConnectedItem 2][Amount]
	public void SendItemInHand(uint netID, bool isItem, ushort itemID, byte amount){
		SendItemInHand(netID, isItem, itemID, 0, amount);
	}
	public void SendItemInHand(uint netID, bool isItem, ushort itemID, ushort connectedItem, byte amount){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteBool(isItem, NetMessage.buffer, 5);
		NetDecoder.WriteUshort(itemID, NetMessage.buffer, 6);
		NetDecoder.WriteUshort(connectedItem, NetMessage.buffer, 8);
		NetDecoder.WriteByte(amount, NetMessage.buffer, 10);
		this.size = 11;
	}

	/*
	Animation, Equipment and Entity SFX
		Client versions are about the sender's own player. Server versions relay them with the NetID of the player
	*/

	// {CODE}[Layer][StateHash 4]
	public void SendAnimationLayer(AnimationData data){
		Reset();
		NetDecoder.WriteByte((byte)data.layer, NetMessage.buffer, 1);
		NetDecoder.WriteInt(data.stateHash, NetMessage.buffer, 2);
		this.size = 6;
	}
	// {CODE}[NetID 4][Layer][StateHash 4]
	public void SendAnimationLayer(uint netID, AnimationData data){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteByte((byte)data.layer, NetMessage.buffer, 5);
		NetDecoder.WriteInt(data.stateHash, NetMessage.buffer, 6);
		this.size = 10;
	}

	// Server sends the battle style of a particular player and the items that skin its attachments
	// {CODE}[NetID 4][Style 4][RightHand 2][LeftHand 2]
	public void SendBattleStyle(uint netID, int code, AttachmentItems items){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteInt(code, NetMessage.buffer, 5);
		NetDecoder.WriteUshort(items.rightHand, NetMessage.buffer, 9);
		NetDecoder.WriteUshort(items.leftHand, NetMessage.buffer, 11);
		this.size = 13;
	}

	// Animator parameters are identified by Animator.StringToHash
	// {CODE}[ParameterHash 4][Value 4]
	public void SendAnimatorParameter(int parameterHash, float val){
		Reset();
		NetDecoder.WriteInt(parameterHash, NetMessage.buffer, 1);
		NetDecoder.WriteFloat(val, NetMessage.buffer, 5);
		this.size = 9;
	}
	// {CODE}[NetID 4][ParameterHash 4][Value 4]
	public void SendAnimatorParameter(uint netID, int parameterHash, float val){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteInt(parameterHash, NetMessage.buffer, 5);
		NetDecoder.WriteFloat(val, NetMessage.buffer, 9);
		this.size = 13;
	}

	// Newly equipped item and old item removed
	// {CODE} | Previous Item (deep) | New Item (deep)
	public void EquipItem(Item previousItem, Item newItem){
		Reset();
		this.size = 1 + WriteEquipment(previousItem, newItem, 1);
	}
	// {CODE}[NetID 4] | Previous Item (deep) | New Item (deep)
	public void EquipItem(uint netID, Item previousItem, Item newItem){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		this.size = 5 + WriteEquipment(previousItem, newItem, 5);
	}

	private int WriteEquipment(Item previousItem, Item newItem, int pos){
		int slotSize = NetDecoder.WriteItemDeep(previousItem, NetMessage.buffer, pos);
		slotSize += NetDecoder.WriteItemDeep(newItem, NetMessage.buffer, pos+slotSize);

		return slotSize;
	}

	// Creation or destruction of an SFX attached to a player
	// {CODE}[Operation] | EntitySFXData
	public void EntitySFX(EntityEffectOperation operation, EntitySFXData sfx){
		Reset();
		NetDecoder.WriteByte((byte)operation, NetMessage.buffer, 1);
		this.size = 2 + NetDecoder.WriteEntitySFXData(sfx, NetMessage.buffer, 2);
	}
	// {CODE}[NetID 4][Operation] | EntitySFXData
	public void EntitySFX(uint netID, EntityEffectOperation operation, EntitySFXData sfx){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteByte((byte)operation, NetMessage.buffer, 5);
		this.size = 6 + NetDecoder.WriteEntitySFXData(sfx, NetMessage.buffer, 6);
	}

	// Creation or destruction of a VFX attached to a player
	// {CODE}[Operation] | EntityVFXData
	public void EntityVFX(EntityEffectOperation operation, EntityVFXData vfx){
		Reset();
		NetDecoder.WriteByte((byte)operation, NetMessage.buffer, 1);
		this.size = 2 + NetDecoder.WriteEntityVFXData(vfx, NetMessage.buffer, 2);
	}
	// {CODE}[NetID 4][Operation] | EntityVFXData
	public void EntityVFX(uint netID, EntityEffectOperation operation, EntityVFXData vfx){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteByte((byte)operation, NetMessage.buffer, 5);
		this.size = 6 + NetDecoder.WriteEntityVFXData(vfx, NetMessage.buffer, 6);
	}

	// Creation or destruction of the moving voxel light of a player. Radius is ignored on destruction
	// {CODE}[Operation][Radius 4]
	public void EntityVoxelLight(EntityEffectOperation operation, float radius){
		Reset();
		NetDecoder.WriteByte((byte)operation, NetMessage.buffer, 1);
		NetDecoder.WriteFloat(radius, NetMessage.buffer, 2);
		this.size = 6;
	}
	// {CODE}[NetID 4][Operation][Radius 4]
	public void EntityVoxelLight(uint netID, EntityEffectOperation operation, float radius){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		NetDecoder.WriteByte((byte)operation, NetMessage.buffer, 5);
		NetDecoder.WriteFloat(radius, NetMessage.buffer, 6);
		this.size = 10;
	}

	// Switch of a BattleStyle attachment from one anchor to another
	// {CODE}[Origin][Target][Flip][HeightOffset 4]
	public void SendAttachmentAnchor(BoneAnchorType origin, BoneAnchorType target, bool flip, float heightOffset){
		Reset();
		WriteAttachmentAnchor(origin, target, flip, heightOffset, 1);
		this.size = 8;
	}
	// {CODE}[NetID 4][Origin][Target][Flip][HeightOffset 4]
	public void SendAttachmentAnchor(uint netID, BoneAnchorType origin, BoneAnchorType target, bool flip, float heightOffset){
		Reset();
		NetDecoder.WriteUint(netID, NetMessage.buffer, 1);
		WriteAttachmentAnchor(origin, target, flip, heightOffset, 5);
		this.size = 12;
	}

	private void WriteAttachmentAnchor(BoneAnchorType origin, BoneAnchorType target, bool flip, float heightOffset, int pos){
		NetDecoder.WriteByte((byte)origin, NetMessage.buffer, pos);
		NetDecoder.WriteByte((byte)target, NetMessage.buffer, pos+1);
		NetDecoder.WriteBool(flip, NetMessage.buffer, pos+2);
		NetDecoder.WriteFloat(heightOffset, NetMessage.buffer, pos+3);
	}
}

// Client and Server are always built together, so codes and layouts can change freely
public enum NetCode : byte{
	NONE, // Never sent. Close event of a connection
	ACCEPTEDCONNECT,
	HEARTBEAT,
	DISCONNECT,
	DISCONNECTINFO,
	SENDCLIENTINFO,
	SENDSERVERINFO,
	REQUESTCHUNKLOAD,
	SENDCHUNK,
	STREAMCHUNK, // Same layout as SENDCHUNK, but counts towards the client's streaming window
	STREAMCENTER,
	CHUNKACK,
	BLOCKUPDATE,
	BLOCKBATCH,
	PLACEBLOCK,
	PLACEMENTDENIED,
	BATCHLOADBUD,
	INTERACT,
	BLOCKDAMAGE,
	VFXDATA,
	VFXCHANGE,
	VFXBREAK,
	SFXPLAY,
	SENDGAMETIME,
	SENDNOISE,
	CLIENTPLAYERPOSITION,
	CLIENTCHUNK,
	PLAYERSPAWN,
	DROPSPAWN,
	ENTITYUPDATES,
	ENTITYDELETE,
	ENTITYDELETEBATCH,
	DROPITEM,
	SENDINVENTORY,
	REQUESTCHARACTEREXISTENCE,
	SENDCHARACTERPRELOAD,
	REQUESTCHARACTERSHEET,
	SENDCHARSHEET,
	SENDHOTBARPOSITION,
	SENDITEMINHAND,
	SENDANIMATIONLAYER,
	SENDBATTLESTYLE,
	SENDANIMATORPARAMETER,
	EQUIPITEM,
	ENTITYSFX,
	SENDATTACHMENTANCHOR,
	ENTITYVFX,
	ENTITYVOXELLIGHT
}

public enum NetBroadcast{
	TEST,
	RECEIVED,
	SENT,
	PROCESSED
}

public enum EntityType : byte{
	PLAYER,
	NPC,
	MOB,
	OBJECT,
	DROP,
	VOXEL // SPECIAL, ONLY USED TO GENERALIZE ENTITY RULES TO VOXELS
}
