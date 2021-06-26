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
		NetDecoder.WriteLong(accountID, NetMessage.buffer, 1);
		NetDecoder.WriteInt(playerRenderDistance, NetMessage.buffer, 9);
		NetDecoder.WriteInt(seed, NetMessage.buffer, 13);
		NetDecoder.WriteInt(worldName.Length, NetMessage.buffer, 17);
		NetDecoder.WriteString(worldName, NetMessage.buffer, 21);
		this.size = 21 + worldName.Length;
	}

	// Server sending player character position
	public void SendServerInfo(float xPos, float yPos, float zPos, float xDir, float yDir, float zDir){
		NetDecoder.WriteFloat(xPos, NetMessage.buffer, 1);
		NetDecoder.WriteFloat(yPos, NetMessage.buffer, 5);
		NetDecoder.WriteFloat(zPos, NetMessage.buffer, 9);
		NetDecoder.WriteFloat(xDir, NetMessage.buffer, 13);
		NetDecoder.WriteFloat(yDir, NetMessage.buffer, 17);
		NetDecoder.WriteFloat(zDir, NetMessage.buffer, 21);
		this.size = 25;
	}

	// Client asking for a chunk information to Server
	public void RequestChunkLoad(ChunkPos pos){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Client asking for Server to unload a chunk
	public void RequestChunkUnload(ChunkPos pos){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		this.size = 9;
	}

	// Server sending chunk information to Client
	public void SendChunk(Chunk c){
		// {CODE} [ChunkPos] [blockSize] [hpSize] [stateSize] | Respective data
		
		int headerSize = RegionFileHandler.chunkHeaderSize;
		int blockDataSize = Compression.CompressBlocks(c, NetMessage.buffer, 21+headerSize);
		int hpDataSize = Compression.CompressMetadataHP(c, NetMessage.buffer, 21+headerSize+blockDataSize);
		int stateDataSize = Compression.CompressMetadataState(c, NetMessage.buffer, 21+headerSize+blockDataSize+hpDataSize);
		
		NetDecoder.WriteChunkPos(c.pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(blockDataSize, NetMessage.buffer, 9);
		NetDecoder.WriteInt(hpDataSize, NetMessage.buffer, 13);
		NetDecoder.WriteInt(stateDataSize, NetMessage.buffer, 17);

		byte[] header = c.GetHeader();
		Array.Copy(header, 0, NetMessage.buffer, 21, headerSize);

		this.size = 21+headerSize+blockDataSize+hpDataSize+stateDataSize;
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
	public void DirectBlockUpdate(BUDCode type, ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state, ushort hp){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 9);
		NetDecoder.WriteInt(y, NetMessage.buffer, 13);
		NetDecoder.WriteInt(z, NetMessage.buffer, 17);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 21);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 25);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 27);
		NetDecoder.WriteUshort(hp, NetMessage.buffer, 29);
		NetDecoder.WriteInt((int)type, NetMessage.buffer, 31);
		this.size = 35;
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
		NetDecoder.WriteInt(x, NetMessage.buffer, 9);
		NetDecoder.WriteInt(y, NetMessage.buffer, 13);
		NetDecoder.WriteInt(z, NetMessage.buffer, 17);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 21);
		this.size = 25;
	}

	// Server sends VFX data to Client
	public void VFXData(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 9);
		NetDecoder.WriteInt(y, NetMessage.buffer, 13);
		NetDecoder.WriteInt(z, NetMessage.buffer, 17);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 21);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 25);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 27);
		this.size = 29;
	}

	// Server sends VFX change of state to Client
	public void VFXChange(ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 9);
		NetDecoder.WriteInt(y, NetMessage.buffer, 13);
		NetDecoder.WriteInt(z, NetMessage.buffer, 17);
		NetDecoder.WriteInt(facing, NetMessage.buffer, 21);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 25);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 27);
		this.size = 29;
	}

	// Server sends VFX deletion information to Client
	public void VFXBreak(ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state){
		NetDecoder.WriteChunkPos(pos, NetMessage.buffer, 1);
		NetDecoder.WriteInt(x, NetMessage.buffer, 9);
		NetDecoder.WriteInt(y, NetMessage.buffer, 13);
		NetDecoder.WriteInt(z, NetMessage.buffer, 17);
		NetDecoder.WriteUshort(blockCode, NetMessage.buffer, 21);
		NetDecoder.WriteUshort(state, NetMessage.buffer, 23);
		this.size = 25;
	}

	// Sends time data to Client
	public void SendGameTime(uint day, byte hour, byte minute){
		NetDecoder.WriteUint(day, NetMessage.buffer, 1);
		NetDecoder.WriteByte(hour, NetMessage.buffer, 5);
		NetDecoder.WriteByte(minute, NetMessage.buffer, 6);
		this.size = 7;
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
	INTERACT,
	CLIENTPLAYERPOSITION,
	VFXDATA,
	VFXCHANGE,
	VFXBREAK,
	SENDGAMETIME,
	DISCONNECT  // No call
}

public enum NetBroadcast{
	TEST,
	RECEIVED,
	SENT,
	PROCESSED
}