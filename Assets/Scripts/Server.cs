using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Mathematics;

public class Server : MonoBehaviour
{
	public int maxPlayers = 8;
	public int port = 33000;
	private TcpListener listener;
	private bool isLocal = true;
	public Dictionary<int, TcpClient> connections;
	private int currentCode = 0;
	private const int receiveBufferSize = 4096*4096;
	private byte[] receiveBuffer = new byte[receiveBufferSize];
	public Dictionary<int, int> playerRenderDistances = new Dictionary<int, int>();

	// Unity Reference
	public ChunkLoader_Server cl;

	public void Start(){
    	connections = new Dictionary<int, TcpClient>();

    	if(!this.isLocal)
        	this.listener = new TcpListener(IPAddress.Any, this.port);
        else
        	this.listener = new TcpListener(IPAddress.Loopback, this.port);

        Debug.Log("Starting Server");
        listener.Start();
        listener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
	}

	// Sends a byte[] to the a client given it's ID
	public bool Send(byte[] data, int id){
		try{
			connections[id].GetStream().BeginWrite(data, 0, data.Length, null, null);
			return true;
		}
		catch(Exception e){
			Debug.Log("SEND ERROR: " + e.ToString());
			return false;
		}
	}


    // Callback function for TCPConnect
    private void TCPConnectCallback(IAsyncResult result){
    	TcpClient client = this.listener.EndAcceptTcpClient(result);
    	this.connections[currentCode] = client;
    	this.currentCode++;

    	Debug.Log(client.ToString() + " has connected");
    	this.Send(new byte[]{1}, this.currentCode-1);

    	client.GetStream().BeginRead(receiveBuffer, 0, receiveBufferSize, ReceiveCallback, this.currentCode-1);
    	listener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
    }

	// Receive call handling
	private void ReceiveCallback(IAsyncResult result){
		try{
			int currentID = (int)result.AsyncState;
			int bytesReceived = this.connections[currentID].GetStream().EndRead(result);

			if(bytesReceived <= 0){
				return;
			}

			byte[] data = new byte[bytesReceived];
			Array.Copy(receiveBuffer, data, bytesReceived);

			Debug.Log("Received: " + (NetCode)data[0]);
			this.HandleReceivedMessage(data, currentID);

			this.connections[currentID].GetStream().BeginRead(receiveBuffer, 0, receiveBufferSize, ReceiveCallback, currentID);
		}
		catch(Exception e){
			Debug.Log(e.ToString());
		}
	}

	// Gets the current code and advances the variable ahead
	public int GetCurrentCode(){
		int current = this.currentCode;
		this.currentCode++;
		return current;
	}

	/* ===========================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from Server
	private void HandleReceivedMessage(byte[] data, int id){
		switch((NetCode)data[0]){
			case NetCode.REQUESTCONNECT:
				RequestConnect(id);
				break;
			case NetCode.SENDCLIENTINFO:
				SendClientInfo(data, id);
				break;
			case NetCode.REQUESTCHUNKLOAD:
				RequestChunkLoad(data, id);
				break;
			case NetCode.REQUESTCHUNKUNLOAD:
				RequestChunkUnload(data, id);
				break;
			case NetCode.SENDBUD:
				SendBUD(data);
				break;
			case NetCode.DIRECTBLOCKUPDATE:
				DirectBlockUpdate(data, id);
				break;
			case NetCode.CLIENTPLAYERPOSITION:
				ClientPlayerPosition(data, id);
				break;
			case NetCode.DISCONNECT:
				Disconnect(id);
				break;
			case NetCode.INTERACT:
				Interact(data);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED");
				break;
		}
	}	

	// Detects when Client wants to connect and sends server data back
	private void RequestConnect(int id){
		NetMessage message = new NetMessage(NetCode.ACCEPTEDCONNECT);
		this.Send(message.GetMessage(), id);
	}

	// Captures client info
	private void SendClientInfo(byte[] data, int id){
		NetMessage message = new NetMessage(NetCode.SENDSERVERINFO);
		int renderDistance = NetDecoder.ReadInt(data, 1); 
		int seed = NetDecoder.ReadInt(data, 5);
		int stringSize = NetDecoder.ReadInt(data, 9);
		string worldName = NetDecoder.ReadString(data, 13, stringSize);

		playerRenderDistances[id] = renderDistance;
		this.cl.RECEIVEDWORLDDATA = true;

		// If World Seed hasn't been set yet
		if(this.cl.worldSeed == -1)
			World.worldSeed = seed;
		
		World.worldName = worldName;

		// Sends Player Info
		if(this.cl.RECEIVEDWORLDDATA){
			Vector3 playerPos = this.cl.regionHandler.LoadPlayer();
			message.SendServerInfo((int)playerPos.x, (int)playerPos.y, (int)playerPos.z);
			this.Send(message.GetMessage(), id);
		}
	}

	// Gets chunk information to player
	private void RequestChunkLoad(byte[] data, int id){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);

		// If is loaded
		if(this.cl.chunks.ContainsKey(pos)){
			this.cl.loadedChunks[pos].Add(id);

			NetMessage message = new NetMessage(NetCode.SENDCHUNK);
			message.SendChunk(this.cl.chunks[pos]);

			this.Send(message.GetMessage(), id);
		}
		else{
			// If it's not to be loaded yet
			if(!this.cl.toLoad.Contains(pos))
				this.cl.toLoad.Add(pos);

			// If was already issued a SendChunk call
			if(this.cl.loadedChunks.ContainsKey(pos)){
				this.cl.loadedChunks[pos].Add(id);
			}
			else{
				this.cl.loadedChunks.Add(pos, new List<int>(){id});
			}
		}
	}

	// Deletes the connection between a client and a chunk
	private void RequestChunkUnload(byte[] data, int id){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		this.cl.UnloadChunk(pos, id);
	}

	// Processes a simple BUD request
	private void SendBUD(byte[] data){
		BUDSignal bud;
		BUDCode code;
		int x, y, z, budX, budY, budZ, facing, offset;

		code = (BUDCode)NetDecoder.ReadInt(data, 1);
		x = NetDecoder.ReadInt(data, 5);
		y = NetDecoder.ReadInt(data, 9);
		z = NetDecoder.ReadInt(data, 13);
		budX = NetDecoder.ReadInt(data, 17);
		budY = NetDecoder.ReadInt(data, 21);
		budZ = NetDecoder.ReadInt(data, 25);
		facing = NetDecoder.ReadInt(data, 29);
		offset = NetDecoder.ReadInt(data, 33);

		bud = new BUDSignal(code, x, y, z, budX, budY, budZ, facing);

		this.cl.budscheduler.ScheduleBUD(bud, offset);
	}

	// Sends a direct action BUD to a block
	private void DirectBlockUpdate(byte[] data, int id){
		ChunkPos pos;
		int x, y, z, facing;
		ushort blockCode, state, hp;
		BUDCode type;
		NetMessage message;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 9);
		y = NetDecoder.ReadInt(data, 13);
		z = NetDecoder.ReadInt(data, 17);
		facing = NetDecoder.ReadInt(data, 21);

		blockCode = NetDecoder.ReadUshort(data, 25);
		state = NetDecoder.ReadUshort(data, 27);
		hp = NetDecoder.ReadUshort(data, 29);
		type = (BUDCode)NetDecoder.ReadInt(data, 31);

		CastCoord lastCoord = new CastCoord(new Vector3(x, y, z));

		switch(type){
			case BUDCode.PLACE:
				// if chunk is still loaded
				if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos())){

					// if it's a block
					if(blockCode <= ushort.MaxValue/2){
						// if placement rules fail
						if(!cl.blockBook.blocks[blockCode].PlacementRule(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl)){
							return;
						}
					}
					// if it's an object
					else{
						if(!cl.blockBook.objects[ushort.MaxValue-blockCode].PlacementRule(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl)){
							return;
						}
					}

					// If doesn't have special place handling
					if(!cl.blockBook.CheckCustomPlace(blockCode)){
						// Actually places block/asset into terrain
						cl.chunks[lastCoord.GetChunkPos()].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
						cl.budscheduler.ScheduleReload(lastCoord.GetChunkPos(), 0);
						EmitBlockUpdate(BUDCode.CHANGE, lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ(), 0, cl);


						// Applies OnPlace Event
						if(blockCode <= ushort.MaxValue/2)
							cl.blockBook.blocks[blockCode].OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						else{
							cl.blockBook.objects[ushort.MaxValue-blockCode].OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						}
					}

					// If has special handling
					else{
						// Actually places block/asset into terrain
						this.cl.chunks[lastCoord.GetChunkPos()].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);

						if(blockCode <= ushort.MaxValue/2){
							cl.blockBook.blocks[blockCode].OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						}
						else{
							cl.blockBook.objects[ushort.MaxValue-blockCode].OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						}

						this.cl.regionHandler.SaveChunk(this.cl.chunks[pos]);				
					}

					// Sends the updated voxel to loaded clients
					message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
					message.DirectBlockUpdate(BUDCode.PLACE, lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, blockCode, this.cl.chunks[lastCoord.GetChunkPos()].metadata.GetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ), this.cl.chunks[lastCoord.GetChunkPos()].metadata.GetHP(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ));

					SendToClients(lastCoord.GetChunkPos(), message);
				}
				break;
			case BUDCode.SETSTATE:
				if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos()))
					this.cl.chunks[lastCoord.GetChunkPos()].metadata.SetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
				break;

			case BUDCode.BREAK:
				// If doesn't has special break handling
				if(!this.cl.blockBook.CheckCustomBreak(blockCode)){

					// Actually breaks new block and updates chunk
					this.cl.chunks[pos].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, 0);
					this.cl.chunks[pos].metadata.Reset(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ);

					// Triggers OnBreak
					if(blockCode <= ushort.MaxValue/2)
						this.cl.blockBook.blocks[blockCode].OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);
					else
						this.cl.blockBook.objects[ushort.MaxValue - blockCode].OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);

					EmitBlockUpdate(BUDCode.BREAK, lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ(), 0, this.cl);
					
				}
				// If has special break handlings
				else{

					if(blockCode <= ushort.MaxValue/2){
						this.cl.blockBook.blocks[blockCode].OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);
					}
					else{
						this.cl.blockBook.objects[ushort.MaxValue - blockCode].OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);
						this.cl.regionHandler.SaveChunk(this.cl.chunks[pos]);
					}
				}

				// Sends the updated voxel to loaded clients
				message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				message.DirectBlockUpdate(BUDCode.BREAK, lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, 0, ushort.MaxValue, ushort.MaxValue);

				SendToClients(lastCoord.GetChunkPos(), message);				

				break;

			case BUDCode.LOAD:

				if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos())){
					
					if(blockCode <= ushort.MaxValue/2){
						this.cl.blockBook.blocks[blockCode].OnLoad(lastCoord, this.cl);
					}
					else{
						this.cl.blockBook.objects[blockCode].OnLoad(lastCoord, this.cl);
					}
				}
				break;

			default:
				break;
		}
	}

	// Receives player position and adds it to PlayerPositions Dict
	private void ClientPlayerPosition(byte[] data, int id){
		float x, y, z;
	
		x = NetDecoder.ReadFloat(data, 1);
		y = NetDecoder.ReadFloat(data, 5);
		z = NetDecoder.ReadFloat(data, 9);

		this.cl.playerPositions[id] = new float3(x, y, z);
	}

	// Receives a disconnect call from client
	private void Disconnect(int id){
		List<ChunkPos> toRemove = new List<ChunkPos>();

		/* TODO: Save player data to Disk */
		this.cl.playerPositions.Remove(id);

		// Captures and removes all
		foreach(KeyValuePair<ChunkPos, List<int>> item in this.cl.loadedChunks){
			if(this.cl.loadedChunks[item.Key].Contains(id)){
				if(this.cl.loadedChunks[item.Key].Count == 1){
					toRemove.Add(item.Key);
				}
				else{
					this.cl.loadedChunks.Remove(item.Key);
				}
			}

		}

		foreach(ChunkPos pos in toRemove){
			this.cl.UnloadChunk(pos, id);
		}
	}

	// Receives an Interaction command from client
	private void Interact(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x = NetDecoder.ReadInt(data, 9);
		int y = NetDecoder.ReadInt(data, 13);
		int z = NetDecoder.ReadInt(data, 17);
		int facing = NetDecoder.ReadInt(data, 21);
		int callback;

		CastCoord current = new CastCoord(pos, x, y, z);

		ushort blockCode = this.cl.GetBlock(current);

		if(blockCode <= ushort.MaxValue/2)
			callback = this.cl.blockBook.blocks[blockCode].OnInteract(pos, current.blockX, current.blockY, current.blockZ, this.cl);
		else
			callback = this.cl.blockBook.objects[ushort.MaxValue - blockCode].OnInteract(pos, current.blockX, current.blockY, current.blockZ, this.cl);

		// Actual handling of message
		CallbackHandler(callback, pos, current, facing);		
	}

	// Auxiliary Functions

	// Send input message to all Clients connected to a given Chunk
	private void SendToClients(ChunkPos pos, NetMessage message){
		foreach(int i in this.cl.loadedChunks[pos]){
			this.Send(message.GetMessage(), i);
		}
	}

	/*
	Main Callback function for block interactions
	(REFER TO THESE CODES WHENEVER ADDING NEW BLOCK INTERACTIONS)
	(MAY BE NEEDED IN ORDER TO IMPLEMENT NEW POST HANDLERS FOR NEW BLOCKS)
	*/
	private void CallbackHandler(int code, ChunkPos targetChunk, CastCoord thisPos, int facing){
		// 0: No further actions necessary
		if(code == 0)
			return;
		// 1: Interaction forces the target chunk to reload/rebuild
		else if(code == 1){
			this.cl.regionHandler.SaveChunk(this.cl.chunks[targetChunk]);
			NetMessage message = new NetMessage(NetCode.SENDCHUNK);
			message.SendChunk(this.cl.chunks[targetChunk]);
			SendToClients(targetChunk, message);
		}
		// 2: Emits BUD instantly and forces chunk reload
		else if(code == 2){
			EmitBlockUpdate(BUDCode.CHANGE, thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), 0, this.cl);
			NetMessage message = new NetMessage(NetCode.SENDCHUNK);
			message.SendChunk(this.cl.chunks[targetChunk]);
			SendToClients(targetChunk, message);
		}
		// 3: Emits BUD in next tick and forces chunk reload
		else if(code == 3){
			//Unused
		}
		// 4: Saves chunk to RDF file silently
		else if(code == 4){
			this.cl.regionHandler.SaveChunk(this.cl.chunks[targetChunk]);
		}

	}

	// Handles the emittion of BUD to neighboring blocks
	public void EmitBlockUpdate(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){
		CastCoord thisPos = GetCoordinates(x, y, z);
		BUDSignal cachedBUD;

		CastCoord[] neighbors = {
		thisPos.Add(1,0,0),
		thisPos.Add(-1,0,0),
		thisPos.Add(0,1,0),
		thisPos.Add(0,-1,0),
		thisPos.Add(0,0,1),
		thisPos.Add(0,0,-1)
		};

		int[] facings = {2,0,4,5,1,3};


		int blockCode;
		int faceCounter=0;

		foreach(CastCoord c in neighbors){
			// Ignores void updates
			if(c.blockY < 0 || c.blockY > Chunk.chunkDepth-1){
				continue;
			}

			blockCode = cl.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);

			cachedBUD = new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]);
			cl.budscheduler.ScheduleBUD(cachedBUD, tickOffset);			 
		
			faceCounter++;
		}
	}

	private CastCoord GetCoordinates(int x, int y, int z){
		return new CastCoord(new Vector3(x ,y ,z));
	}

}
