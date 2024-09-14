using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NetMessage
{
	public NetCode code;
	public int size;
	public ulong id;
	private static byte[] buffer = new byte[Chunk.chunkWidth * Chunk.chunkDepth * Chunk.chunkWidth * 5]; // 2MB
	private byte[] data;

	private static bool broadcastReceived = false;
	private static bool broadcastSent = false;
	private static bool broadcastProcessed = false;

	// Constructor
	public NetMessage(NetCode code){
		this.code = code;
		this.id = 0;
		this.data = null;
		NetMessage.buffer[0] = (byte)code;
		this.size = 1;
	}

	// Constructor
	public NetMessage(NetCode code, ulong id){
		this.code = code;
		this.id = id;
		this.data = null;
		NetMessage.buffer[0] = (byte)code;
		this.size = 1;
	}

	public NetMessage(byte[] data, ulong id){
		this.code = (NetCode)data[0];
		this.data = data;
		this.size = data.Length;
		this.id = id;
	}

	// Gets the buffer
	public byte[] GetMessage(){
		return NetMessage.buffer;
	}

	// Gets size of buffer
	public int GetSize(){
		return this.size;
	}

	// Gets the ID for server communication
	public ulong GetID(){
		return this.id;
	}

	// Gets the data in Server-sided messages
	public byte[] GetData(){
		return this.data;
	}

	// Adds a new block information to BatchLoadBUD message
	public void AddBatchLoad(int x, int y, int z, int facing, ushort blockCode, ushort state, ushort hp){
		NetDecoder.WriteInt(x, NetMessage.buffer, this.size);
		NetDecoder.WriteInt(y, NetMessage.buffer, this.size+4);
		NetDecoder.WriteInt(z, NetMessage.buffer, this.size+8);
		NetDecoder.WriteInt(facing, NetMessage.buffer, this.size+12);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, this.size+16);
		NetDecoder.WriteUshort(state, NetMessage.buffer, this.size+18);
		NetDecoder.WriteUshort(hp, NetMessage.buffer, this.size+20);
		this.size += 22;
	}

	// Returns a copy of the previously instanced NetMessage but passing the info in NetMessage.buffer to this.data
	public NetMessage CopyAsInternal(NetMessage m){
		byte[] data = new byte[m.GetSize()];
		Array.Copy(NetMessage.buffer, 0, data, 0, data.Length);

		return new NetMessage(data, ulong.MaxValue);
	}

	// Broadcasts message to stdout
	public static void Broadcast(NetBroadcast bc, byte netcode, ulong id, int length){
		if(bc == NetBroadcast.RECEIVED && NetMessage.broadcastReceived)
			Debug.Log("Received: " + (NetCode)netcode + " | " + id + " > " + length);
		else if(bc == NetBroadcast.SENT && NetMessage.broadcastSent)
			Debug.Log("Sent: " + (NetCode)netcode + " | " + id + " > " + length);
		else if(bc == NetBroadcast.PROCESSED && NetMessage.broadcastProcessed)
			Debug.Log("Processed: " + (NetCode)netcode + " | " + id);
		else if(bc == NetBroadcast.TEST){
			Debug.Log("TESTING: " + (NetCode)netcode + " | " + id + " > " + length);
		}
	}

	/*
	Individual Building of NetMessages
	*/

	// Client sending initial information to server after connection was accepted
	public void SendClientInfo(ulong accountID, int playerRenderDistance, int seed, string worldName){
		// TODO: Add character name to here
		// {CODE}[AccountID][Render] [Seed] [stringSize (int)] [worldName]
		int len;
		if(World.isClient)
			len = worldName.Length;
		else
			len = 0;

		NetDecoder.WriteLong(accountID, NetMessage.buffer, 1);
		NetDecoder.WriteInt(playerRenderDistance, NetMessage.buffer, 9);
		NetDecoder.WriteInt(seed, NetMessage.buffer, 13);
		NetDecoder.WriteInt(len, NetMessage.buffer, 17);
		NetDecoder.WriteString(worldName, NetMessage.buffer, 21);
		this.size = 21 + len;
	}

	// Server sending player character position
	public void SendServerInfo(float xPos, float yPos, float zPos, float xDir, float yDir, float zDir, uint day, byte hour, byte minute){
		NetDecoder.WriteFloat(xPos, NetMessage.buffer, 1);
		NetDecoder.WriteFloat(yPos, NetMessage.buffer, 5);
		NetDecoder.WriteFloat(zPos, NetMessage.buffer, 9);
		NetDecoder.WriteFloat(xDir, NetMessage.buffer, 13);
		NetDecoder.WriteFloat(yDir, NetMessage.buffer, 17);
		NetDecoder.WriteFloat(zDir, NetMessage.buffer, 21);
		NetDecoder.WriteUint(day, NetMessage.buffer, 25);
		NetDecoder.WriteByte(hour, NetMessage.buffer, 29);
		NetDecoder.WriteByte(minute, NetMessage.buffer, 30);

		this.size = 31;
	}

	
	// Client asking for a chunk information to Server
	public void RequestChunkLoad(ChunkPos pos){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		this.size = 10;
	}

	// Client asking for Server to unload a chunk
	public void RequestChunkUnload(ChunkPos pos){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		this.size = 10;
	}

	// Server sending chunk information to Client
	public void SendChunk(Chunk c){
		// {CODE} [ChunkPos] [blockSize] [hpSize] [stateSize] | Respective data
		
		int headerSize = RegionFileHandler.chunkHeaderSize;
		int blockDataSize = Compression.CompressBlocks(c, NetMessage.buffer, 22+headerSize);
		int hpDataSize = Compression.CompressMetadataHP(c, NetMessage.buffer, 22+headerSize+blockDataSize);
		int stateDataSize = Compression.CompressMetadataState(c, NetMessage.buffer, 22+headerSize+blockDataSize+hpDataSize);
		
		NetDecoder.WriteChunkPos(c.pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(blockDataSize, NetMessage.buffer, 10);
		NetDecoder.WriteInt(hpDataSize, NetMessage.buffer, 14);
		NetDecoder.WriteInt(stateDataSize, NetMessage.buffer, 18);

		byte[] header = c.GetHeader();
		Array.Copy(header, 0, NetMessage.buffer, 22, headerSize);

		this.size = 22+headerSize+blockDataSize+hpDataSize+stateDataSize;
	}

	// Sends a BUD packet to the server
	public void SendBUD(BUDSignal bud, int timeOffset){
		NetDecoder.WriteInt((int)bud.type, NetMessage.buffer, 1);
		NetDecoder.WriteInt(bud.x, NetMessage.buffer, 5);
		NetDecoder.WriteInt(bud.y, NetMessage.buffer, 9);
		NetDecoder.WriteInt(bud.z, NetMessage.buffer, 13);
		NetDecoder.WriteInt(bud.budX, NetMessage.buffer, 17);
		NetDecoder.WriteInt(bud.budY, NetMessage.buffer, 21);
		NetDecoder.WriteInt(bud.budZ, NetMessage.buffer, 25);
		NetDecoder.WriteInt(bud.facing, NetMessage.buffer, 29);
		NetDecoder.WriteInt(timeOffset, NetMessage.buffer, 33);
		this.size = 37;
	}

	// Client or Server send a single voxel data to each other
	// Keywords "Slot" and "NewQuantity" are used only in Place operations for Client->Server messages
	public void DirectBlockUpdate(BUDCode type, ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state, ushort hp, byte slot=0, byte newQuantity=0){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 22);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 26);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 28);
		NetDecoder.WriteUshort(hp, NetMessage.buffer, 30);
		NetDecoder.WriteInt((int)type, NetMessage.buffer, 32);
		NetDecoder.WriteByte(slot, NetMessage.buffer, 36);
		NetDecoder.WriteByte(newQuantity, NetMessage.buffer, 37);
		this.size = 38;
	}

	// Clients sends their position to Server
	public void ClientPlayerPosition(float x, float y, float z, float rotX, float rotY, float rotZ){
		NetDecoder.WriteFloat(x, NetMessage.buffer, 1);
		NetDecoder.WriteFloat(y, NetMessage.buffer, 5);
		NetDecoder.WriteFloat(z, NetMessage.buffer, 9);
		NetDecoder.WriteFloat(rotX, NetMessage.buffer, 13);
		NetDecoder.WriteFloat(rotY, NetMessage.buffer, 17);
		NetDecoder.WriteFloat(rotZ, NetMessage.buffer, 21);
		this.size = 25; 
	}

	// Client sends a voxel coordinate to trigger OnInteraction in server
	public void Interact(ChunkPos pos, int x, int y, int z, int facing){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 22);
		this.size = 26;
	}

	// Server sends VFX data to Client
	public void VFXData(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 22);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 26);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 28);
		this.size = 30;
	}

	// Server sends VFX change of state to Client
	public void VFXChange(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 22);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 26);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 28);
		this.size = 30;
	}

	// Server sends VFX deletion information to Client
	public void VFXBreak(ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 22);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 24);
		this.size = 26;
	}

	// Sends time data to Client
	public void SendGameTime(uint day, byte hour, byte minute){
		NetDecoder.WriteUint(day, NetMessage.buffer, 1);
		NetDecoder.WriteByte(hour, NetMessage.buffer, 5);
		NetDecoder.WriteByte(minute, NetMessage.buffer, 6);
		this.size = 7;
	}

	// Server sends entity data to Client
	public void PlayerLocation(ulong code, float posX, float posY, float posZ, float dirX, float dirY, float dirZ){
		NetDecoder.WriteLong(code, NetMessage.buffer, 1);
		NetDecoder.WriteFloat3(posX, posY, posZ, NetMessage.buffer, 9);
		NetDecoder.WriteFloat3(dirX, dirY, dirZ, NetMessage.buffer, 21);
		this.size = 33;
	}
	public void PlayerLocation(PlayerData pdat){
		PlayerLocation(pdat.GetID(), pdat.posX, pdat.posY, pdat.posZ, pdat.dirX, pdat.dirY, pdat.dirZ);
	}

	// Client requests a player's appearance information
	public void RequestPlayerAppearance(ulong code){
		NetDecoder.WriteLong(code, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Server sends player appearance information to Client
	public void SendPlayerAppearance(ulong code, CharacterAppearance app, bool isMale){
		NetDecoder.WriteLong(code, NetMessage.buffer, 1);
		NetDecoder.WriteCharacterAppearance(app, NetMessage.buffer, 9);
		NetDecoder.WriteBool(isMale, NetMessage.buffer, 256);
		this.size = 257;
	}

	// Server sends the item in a player's hand to the Client
	public void PlayerItemHand(ulong code, Item it){
		NetDecoder.WriteLong(code, NetMessage.buffer, 1);
		NetDecoder.WriteItem(it, NetMessage.buffer, 9);
		this.size = 11;
	}

	// Server sends a deletion command to out-of-bounds entities to Client
	public void EntityDelete(EntityType type, ulong code){
		NetDecoder.WriteByte((byte)type, NetMessage.buffer, 1);
		NetDecoder.WriteLong(code, NetMessage.buffer, 2);
		this.size = 10;
	}

	// Client sends the chunk the player is currently in
	public void ClientChunk(ChunkPos lastPos, ChunkPos newPos){
		NetDecoder.WriteChunkPos(lastPos, NetMessage.buffer, 1);
		NetDecoder.WriteChunkPos(newPos, NetMessage.buffer, 10);
		this.size = 19;
	}

	// Client sends item information for server to create a Dropped Item Entity
	public void DropItem(float posX, float posY, float posZ, float moveX, float moveY, float moveZ, ushort itemCode, byte amount, byte slotId){
		NetDecoder.WriteFloat3(posX, posY, posZ, NetMessage.buffer, 1);
		NetDecoder.WriteFloat3(moveX, moveY, moveZ, NetMessage.buffer, 13);
		NetDecoder.WriteUshort(itemCode, NetMessage.buffer, 25);
		NetDecoder.WriteByte(amount, NetMessage.buffer, 27);
		NetDecoder.WriteByte(slotId, NetMessage.buffer, 28);
		this.size = 29;
	}

	// Item Entity Data
	public void ItemEntityData(float posX, float posY, float posZ, float rotX, float rotY, float rotZ, ushort itemCode, byte amount, ulong entityCode){
		NetDecoder.WriteFloat3(posX, posY, posZ, NetMessage.buffer, 1);
		NetDecoder.WriteFloat3(rotX, rotY, rotZ, NetMessage.buffer, 13);
		NetDecoder.WriteUshort(itemCode, NetMessage.buffer, 25);
		NetDecoder.WriteByte(amount, NetMessage.buffer, 27);
		NetDecoder.WriteLong(entityCode, NetMessage.buffer, 28);
		this.size = 36;
	}

	// Clients sends list of blocks that require a LOAD BUD operation
	public void BatchLoadBUD(ChunkPos pos){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		this.size = 10;	
	}

	// Client or server sends a block damage operation to server
	// TODO: Add Damage Type
	public void BlockDamage(ChunkPos pos, int x, int y, int z, ushort newHPOrDamage, bool shouldRedrawChunk){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteUshort(newHPOrDamage, NetMessage.buffer, 22);
		NetDecoder.WriteBool(shouldRedrawChunk, NetMessage.buffer, 24);
		this.size = 25;
	}

	// Server sends inventory information to client
	public void SendInventory(byte[] data, int length){
		Array.Copy(data, 0, NetMessage.buffer, 1, length);
		this.size = 1 + length;
	}

	// Server sends a client an order to register an SFX into SFXLoader
	public void SFXPlay(ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 10);
		NetDecoder.WriteInt(y, NetMessage.buffer, 14);
		NetDecoder.WriteInt(z, NetMessage.buffer, 18);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 22);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 24);
		this.size = 26;
	}

	// Server sends clients the bytes that composes a Noise (used for global weather noise)
	public void SendNoise(byte[] noise, int seed){
		NetDecoder.WriteByteArray(noise, NetMessage.buffer, 1);
		NetDecoder.WriteInt(seed, NetMessage.buffer, 1+noise.Length);
		this.size = 1+noise.Length+4;
	}

	// Client asks for existance of Character ID. Used before World load
	public void RequestCharacterExistence(ulong id){
		NetDecoder.WriteLong(id, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Client asks for CharacterSheet
	public void RequestCharacterSheet(ulong id){
		NetDecoder.WriteLong(id, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Server sends character appearance and a flag
	public void SendCharacterPreload(CharacterAppearance? app, bool isMale){
		if(app == null){
			NetDecoder.WriteBool(false, NetMessage.buffer, 1);
			NetDecoder.WriteZeros(2, 249, NetMessage.buffer);
			NetDecoder.WriteBool(isMale, NetMessage.buffer, 249);
		}
		else{
			NetDecoder.WriteBool(true, NetMessage.buffer, 1);
			NetDecoder.WriteCharacterAppearance((CharacterAppearance)app, NetMessage.buffer, 2);
			NetDecoder.WriteBool(isMale, NetMessage.buffer, 249);
		}
		this.size = 251;
	}

	// Encodes a CharacterSheet
	public void SendCharSheet(ulong charCode, CharacterSheet sheet){
		NetDecoder.WriteLong(charCode, NetMessage.buffer, 1);
		NetDecoder.WriteCharacterSheet(sheet, NetMessage.buffer, 9);

		this.size = 1230; 
	}

	// Client sends character hotbar position to Server
	public void SendHotbarPosition(byte hotbarSlot){
		NetDecoder.WriteByte(hotbarSlot, NetMessage.buffer, 1);
		this.size = 2;
	}
}

public enum NetCode{
	TEST,
	ACCEPTEDCONNECT, // No call
	SENDCLIENTINFO,
	SENDSERVERINFO,
	REQUESTCHUNKLOAD,
	REQUESTCHUNKUNLOAD,
	SENDCHUNK,
	SENDBUD,
	DIRECTBLOCKUPDATE,
	BATCHLOADBUD,
	INTERACT,
	CLIENTPLAYERPOSITION,
	VFXDATA,
	VFXCHANGE,
	VFXBREAK,
	SENDGAMETIME,
	HEARTBEAT, // No call
	PLAYERLOCATION,
	REQUESTPLAYERAPPEARANCE,
	SENDPLAYERAPPEARANCE,
	REQUESTCHARACTERSHEET,
	PLAYERITEMHAND,
	ENTITYDELETE,
	CLIENTCHUNK,
	PLACEMENTDENIED, // No call
	DROPITEM,
	ITEMENTITYDATA,
	BLOCKDAMAGE,
	SENDINVENTORY,
	SFXPLAY,
	SENDNOISE,
	REQUESTCHARACTEREXISTENCE,
	SENDCHARACTERPRELOAD,
	SENDCHARSHEET,
	SENDHOTBARPOSITION,
	DISCONNECTINFO, // No call
	DISCONNECT  // No call
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
	DROP
}