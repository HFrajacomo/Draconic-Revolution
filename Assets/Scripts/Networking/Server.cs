using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using static System.Environment;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Mathematics;

using Random = UnityEngine.Random;

public class Server
{
	public int port = 33000;
	private bool isLocal = false;
	public Socket masterSocket;
	private IPEndPoint serverIP;

	private const int receiveBufferSize = 512*512;

	public Dictionary<ulong, Socket> connections;
	public Dictionary<ulong, Socket> temporaryConnections;
	public Dictionary<ulong, bool> lengthPacket;
	public Dictionary<ulong, int> packetIndex;
	public Dictionary<ulong, int> packetSize;
	public Dictionary<ulong, byte[]> dataBuffer;
	public Dictionary<ulong, DateTime> timeoutTimers;
	public Dictionary<ChunkPos, HashSet<ulong>> chunksRequested;
	private Dictionary<ulong, byte[]> receiveBuffer;

	private HashSet<ChunkPos> chunksToSend = new HashSet<ChunkPos>();

	public EntityHandler_Server entityHandler = new EntityHandler_Server();


	public Dictionary<ulong, HashSet<ulong>> connectionGraph;
	private Dictionary<ChunkPos, HashSet<ulong>> playersInChunk;

	public Dictionary<ulong, int> playerRenderDistances = new Dictionary<ulong, int>();
	private SocketError err = new SocketError();

	public List<NetMessage> queue = new List<NetMessage>();

	public ulong firstConnectedID = ulong.MaxValue;
	private const int timeoutSeconds = 15;

	// Unity Reference
	public ChunkLoader_Server cl;

	// Cache
	private byte[] cacheBreakData = new byte[38];
	private CharacterSheet cachedSheet;

	public Server(ChunkLoader_Server cl){
    	ParseArguments();

    	// Parses config file if is a Dedicated Server
    	if(!this.isLocal){
    		ParseConfigFile();
    	}

		// Initiates Server data
    	connections = new Dictionary<ulong, Socket>();
    	temporaryConnections = new Dictionary<ulong, Socket>();
    	timeoutTimers = new Dictionary<ulong, DateTime>();
    	lengthPacket = new Dictionary<ulong, bool>();
    	packetIndex = new Dictionary<ulong, int>();
    	packetSize = new Dictionary<ulong, int>();
    	dataBuffer = new Dictionary<ulong, byte[]>();
    	connectionGraph = new Dictionary<ulong, HashSet<ulong>>();
    	playersInChunk = new Dictionary<ChunkPos, HashSet<ulong>>();
    	receiveBuffer = new Dictionary<ulong, byte[]>();
    	chunksRequested = new Dictionary<ChunkPos, HashSet<ulong>>();

    	this.cl = cl;
    	
    	if(!this.isLocal){
        	this.masterSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    		this.serverIP = new IPEndPoint(0, this.port);
    	}
        else{
        	this.masterSocket = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        	this.serverIP = new IPEndPoint(0x0100007F, this.port);
        }


        Debug.Log("Starting Server");

        this.masterSocket.Bind(this.serverIP);
        this.masterSocket.Listen(byte.MaxValue);

        this.masterSocket.BeginAccept(new AsyncCallback(ConnectCallback), this.masterSocket);
	}

	// Receives command line args and parses them
	private void ParseArguments(){
		bool nextIsWorld = false;
		string[] args = GetCommandLineArgs();

		foreach(string arg in args){
			switch(arg){
				case "-Local":
					this.isLocal = true;
					World.SetToClient();
					Debug.Log("local");
					break;
				case "-World":
					nextIsWorld = true;
					break;
				default:
					if(nextIsWorld){
						Debug.Log("Setting world name to: " + arg);
						World.SetWorldName(arg);
						nextIsWorld = false;
					}
					break;
			}
		}
	}

	// Deals with the reading/writing of config file on Dedicated Servers
	private void ParseConfigFile(){
		Stream file;
		byte[] allBytes;

		// If there is a config file
		if(File.Exists("server.cfg")){
			string text;
			string[] temp;
			Dictionary<string, string> argsDictionary = new Dictionary<string, string>();

			// Parses all arguments
			file = File.Open("server.cfg", FileMode.Open);
			allBytes = new byte[file.Length];
			file.Read(allBytes, 0, (int)file.Length);
			text = System.Text.Encoding.Default.GetString(allBytes);

			foreach(string argument in text.Split('\n')){
				if(argument == "")
					continue;

				temp = argument.Split('=');
				argsDictionary.Add(temp[0], temp[1]);
			}

			if(argsDictionary.ContainsKey("world_name")){
				// If it's not filled, generate a new name
				if(argsDictionary["world_name"] == ""){
					argsDictionary["world_name"] = GenerateRandomName();
					file.Seek(0, SeekOrigin.End);
					allBytes = System.Text.Encoding.ASCII.GetBytes(argsDictionary["world_name"]);
					file.Write(allBytes, 0, allBytes.Length);
				}

				World.SetWorldName(argsDictionary["world_name"]);
			}

			file.Close();
		}
		// If a config file needs tto be created
		else{
			file = File.Open("server.cfg", FileMode.Create);
			allBytes = System.Text.Encoding.ASCII.GetBytes("world_name=");
			file.Write(allBytes, 0, allBytes.Length);
			file.Close();
			Debug.Log("Generated .cfg file. Please restart!");

			#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
			#else
				Application.Quit();
			#endif
		}
	}

	public bool IsLocal(){return this.isLocal;}

	// Generates a random 8 letter code
	private string GenerateRandomName(){
		StringBuilder sb = new StringBuilder();

		for(int i=0; i<8; i++){
			sb.Append((char)Random.Range(65, 122));
		}

		return sb.ToString();
	}

    // Callback for connections received
    private void ConnectCallback(IAsyncResult result){
    	Socket client = this.masterSocket.EndAccept(result);
    	ulong temporaryCode = GetCurrentCode();

    	this.temporaryConnections[temporaryCode] = client;
    	this.lengthPacket[temporaryCode] = true;
    	this.packetIndex[temporaryCode] = 0;
    	this.receiveBuffer[temporaryCode] = new byte[receiveBufferSize];

    	Debug.Log(client.RemoteEndPoint.ToString() + " has connected with temporary ID " + temporaryCode);

    	// Check if there's trash in the network then flush it
    	if(client.Available > 0){
    		Debug.Log("Removed trash from: " + temporaryCode);
    		client.Receive(this.receiveBuffer[temporaryCode], client.Available, 0);
    	}

    	NetMessage message = new NetMessage(NetCode.ACCEPTEDCONNECT);
    	this.Send(message.GetMessage(), message.size, temporaryCode, temporary:true);

    	this.temporaryConnections[temporaryCode].BeginReceive(this.receiveBuffer[temporaryCode], 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), temporaryCode);
    	this.masterSocket.BeginAccept(new AsyncCallback(ConnectCallback), null);
    }

	// Sends a byte[] to the a client given it's ID
	public void Send(byte[] data, int length, ulong id, bool temporary=false){
		try{
			if(!temporary){
				IAsyncResult lenResult = this.connections[id].BeginSend(this.LengthPacket(length), 0, 4, 0, out this.err, null, id);
				this.connections[id].EndSend(lenResult);

				IAsyncResult result = this.connections[id].BeginSend(data, 0, length, 0, out this.err, null, id);
				this.connections[id].EndSend(result);

				NetMessage.Broadcast(NetBroadcast.SENT, data[0], id, length);
			}
			else{
				IAsyncResult lenResult = this.temporaryConnections[id].BeginSend(this.LengthPacket(length), 0, 4, 0, out this.err, null, id);
				this.temporaryConnections[id].EndSend(lenResult);

				IAsyncResult result = this.temporaryConnections[id].BeginSend(data, 0, length, 0, out this.err, null, id);
				this.temporaryConnections[id].EndSend(result);

				NetMessage.Broadcast(NetBroadcast.SENT, data[0], id, length);				
			}
		}
		catch(Exception e){
			Debug.Log("SEND ERROR: " + e.ToString());
		}
	}

	public void SendList(byte[] data, int length, List<ulong> ids, bool temporary=false){
		for(int i=0; i < ids.Count; i++)
			this.Send(data, length, ids[i], temporary:temporary);
	}

	// Sends a message to all IDs connected
	public void SendAll(byte[] data, int length){
		foreach(ulong code in this.connections.Keys)
			this.Send(data, length, code);
	}

	// Receive call handling
	private void ReceiveCallback(IAsyncResult result){
		try{
			ulong currentID = (ulong)result.AsyncState;
			bool isTemporary = false;
			int bytesReceived;

			// If receives something before attributing AccountID
			if(this.temporaryConnections.ContainsKey(currentID))
				isTemporary = true;

			// Check if socket still exists and is connected
			if(!this.connections.ContainsKey(currentID) && !isTemporary){
				return;
			}
			if(!isTemporary && !this.connections[currentID].Connected){
				this.connections.Remove(currentID);
				return;
			}

			// Gets packet size
			if(isTemporary){
				bytesReceived = this.temporaryConnections[currentID].EndReceive(result);
			}
			else{
				bytesReceived = this.connections[currentID].EndReceive(result);
			}


			// If has received a size packet
			if(this.lengthPacket[currentID]){
				int size = NetDecoder.ReadInt(this.receiveBuffer[currentID], 0);

				this.dataBuffer[currentID] = new byte[size];
				this.packetSize[currentID] = size;
				this.lengthPacket[currentID] = false;
				this.packetIndex[currentID] = 0;

				if(isTemporary)
					this.temporaryConnections[currentID].BeginReceive(this.receiveBuffer[currentID], 0, size, 0, out this.err, new AsyncCallback(ReceiveCallback), currentID);
				else
					this.connections[currentID].BeginReceive(this.receiveBuffer[currentID], 0, size, 0, out this.err, new AsyncCallback(ReceiveCallback), currentID);
				return;
			}

			// If has received a segmented packet
			if(bytesReceived+this.packetIndex[currentID] < this.packetSize[currentID]){
				Array.Copy(this.receiveBuffer[currentID], 0, this.dataBuffer[currentID], this.packetIndex[currentID], bytesReceived);
				this.packetIndex[currentID] = this.packetIndex[currentID] + bytesReceived;
    			
    			if(isTemporary)
    				this.temporaryConnections[currentID].BeginReceive(this.receiveBuffer[currentID], 0, this.packetSize[currentID]-this.packetIndex[currentID], 0, out this.err, new AsyncCallback(ReceiveCallback), currentID);
    			else
    				this.connections[currentID].BeginReceive(this.receiveBuffer[currentID], 0, this.packetSize[currentID]-this.packetIndex[currentID], 0, out this.err, new AsyncCallback(ReceiveCallback), currentID);
				return;
			}

			// If has received the entire package
			Array.Copy(this.receiveBuffer[currentID], 0, this.dataBuffer[currentID], this.packetIndex[currentID], bytesReceived);

			NetMessage.Broadcast(NetBroadcast.RECEIVED, this.dataBuffer[currentID][0], currentID, this.packetSize[currentID]);

			NetMessage receivedMessage = new NetMessage(this.dataBuffer[currentID], currentID);
			this.queue.Add(receivedMessage);

			this.lengthPacket[currentID] = true;
			this.packetIndex[currentID] = 0;
			this.packetSize[currentID] = 0;

			if(!isTemporary)
    			this.connections[currentID].BeginReceive(this.receiveBuffer[currentID], 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), currentID);
		}
		catch(Exception e){
			Debug.Log("Message: " + e.Message + "\nTrace: " + e.StackTrace + "\nSource: " + e.Source + "\nTarget: " + e.TargetSite);
		}
	}

	// Gets the first valid temporary ID
	public ulong GetCurrentCode(){
		ulong code = ulong.MaxValue-1;

		while(this.temporaryConnections.ContainsKey(code))
			code--;

		return code;
	}

	/* ===========================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from Server
	public void HandleReceivedMessage(byte[] data, ulong id){
		try{
			NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], id, 0);
		}
		catch{
			return;
		}

		switch((NetCode)data[0]){
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
			case NetCode.REQUESTPLAYERAPPEARANCE:
				RequestPlayerAppearance(data, id);
				break;
			case NetCode.DISCONNECT:
				Disconnect(id, DisconnectType.QUIT);
				break;
			case NetCode.INTERACT:
				Interact(data, id);
				break;
			case NetCode.HEARTBEAT:
				Heartbeat(id);
				break;
			case NetCode.CLIENTCHUNK:
				ClientChunk(data, id);
				break;
			case NetCode.DROPITEM:
				DropItem(data, id);
				break;
			case NetCode.BATCHLOADBUD:
				BatchLoadBUD(data, id);
				break;
			case NetCode.BLOCKDAMAGE:
				BlockDamage(data, id);
				break;
			case NetCode.SENDINVENTORY:
				SendInventory(data, id);
				break;
			case NetCode.REQUESTCHARACTEREXISTENCE:
				RequestCharacterExistence(data, id);
				break;
			case NetCode.REQUESTCHARACTERSHEET:
				RequestCharacterSheet(data, id);
				break;
			case NetCode.SENDCHARSHEET:
				SendCharSheet(data, id);
				break;
			case NetCode.SENDHOTBARPOSITION:
				SendHotbarPosition(data, id);
				break;
			case NetCode.DISCONNECTINFO:
				DisconnectInfo(id);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED");
				break;
		}
	}	

	// Captures client info
	private void SendClientInfo(byte[] data, ulong id){
		NetMessage message = new NetMessage(NetCode.SENDSERVERINFO);
		int inventoryLength;
		bool isEmptyInventory;
		ulong accountID = NetDecoder.ReadUlong(data, 1);
		int renderDistance = NetDecoder.ReadInt(data, 9); 
		int seed = NetDecoder.ReadInt(data, 13);
		int stringSize = NetDecoder.ReadInt(data, 17);
		string worldName = NetDecoder.ReadString(data, 21, stringSize);

		playerRenderDistances[accountID] = renderDistance;
		
		if(this.isLocal){
			World.worldName = worldName;
			World.worldSeed = seed;
		}

		// Sends Player Info
		if(this.cl.RECEIVEDWORLDDATA){
			// Sends player data
			PlayerData pdat = this.cl.regionHandler.LoadPlayer(accountID, fromServer:true);
			pdat.SetOnline(true);
			Vector3 playerPos = pdat.GetPosition();
			Vector3 playerDir = pdat.GetDirection();

			this.cachedSheet = this.cl.characterFileHandler.LoadCharacterSheet(id);

			message.SendServerInfo(playerPos.x, playerPos.y, playerPos.z, playerDir.x, playerDir.y, playerDir.z, this.cl.time.days, this.cl.time.hours, this.cl.time.minutes);
			this.Send(message.GetMessage(), message.size, id, temporary:true);

			// Sends player inventory data
			NetMessage inventoryMessage = new NetMessage(NetCode.SENDINVENTORY);
			inventoryLength = this.cl.playerServerInventory.LoadInventoryIntoBuffer(accountID, out isEmptyInventory);

			if(!isEmptyInventory)
				inventoryMessage.SendInventory(this.cl.playerServerInventory.GetBuffer(), inventoryLength);
			else
				inventoryMessage.SendInventory(this.cl.playerServerInventory.GetEmptyBuffer(), inventoryLength);

			this.Send(inventoryMessage.GetMessage(), inventoryMessage.size, id, temporary:true);

			// Sends global weather noise data
			NetMessage weatherMessage = new NetMessage(NetCode.SENDNOISE);
			weatherMessage.SendNoise(GenerationSeed.weatherNoise, World.worldSeed);
			this.Send(weatherMessage.GetMessage(), weatherMessage.size, id, temporary:true);
		}

		// If AccountID is already online, erase all memory from that connection
		if(this.connections.ContainsKey(accountID)){
			Disconnect(accountID, DisconnectType.LOGINOVERWRITE);
		}

		// Assigns a fixed ID
		this.connections.Add(accountID, this.temporaryConnections[id]);
		this.temporaryConnections.Remove(id);

    	this.lengthPacket[accountID] = true;
    	this.packetIndex[accountID] = 0;
    	this.connectionGraph.Add(accountID, new HashSet<ulong>());

    	this.receiveBuffer.Add(accountID, new byte[receiveBufferSize]);
    	this.receiveBuffer.Remove(id);

		this.cl.RECEIVEDWORLDDATA = true;

		if(this.firstConnectedID == ulong.MaxValue)
			this.firstConnectedID = accountID;

		this.timeoutTimers.Add(accountID, DateTime.Now);

		Debug.Log("Temporary ID: " + id + " was assigned to ID: " + accountID);

    	this.connections[accountID].BeginReceive(this.receiveBuffer[accountID], 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), accountID);
	}

	// Gets chunk information to player
	private void RequestChunkLoad(byte[] data, ulong id){RequestChunkLoad(NetDecoder.ReadChunkPos(data, 1), id);}

	public void RequestChunkLoad(ChunkPos pos, ulong id){
		// If is loaded
		if(this.cl.chunks.ContainsKey(pos) && this.cl.chunks[pos].needsGeneration == 0){
			if(!this.cl.loadedChunks.ContainsKey(pos))
				this.cl.loadedChunks.Add(pos, new HashSet<ulong>());

			if(!this.cl.loadedChunks[pos].Contains(id))
				this.cl.loadedChunks[pos].Add(id);

			if(this.chunksRequested.ContainsKey(pos)){
				this.chunksRequested[pos].Remove(id);

				if(this.chunksRequested[pos].Count == 0)
					this.chunksRequested.Remove(pos);
			}

			NetMessage message = new NetMessage(NetCode.SENDCHUNK);
			message.SendChunk(this.cl.chunks[pos]);

			this.Send(message.GetMessage(), message.size, id);
		}
		else{
			// If it's not loaded yet
			if(!this.cl.toLoad.Contains(pos))
				this.cl.toLoad.Add(pos);

			if(chunksRequested.ContainsKey(pos))
				chunksRequested[pos].Add(id);
			else
				chunksRequested.Add(pos, new HashSet<ulong>(){id});

			return;
		}

		NetMessage playerMessage = new NetMessage(NetCode.PLAYERLOCATION);

		// Sends logged in players data
		if(this.playersInChunk.ContainsKey(pos)){
			foreach(ulong code in this.playersInChunk[pos]){
				if(code == id)
					continue;
				if(this.cl.regionHandler.allPlayerData[code].IsOnline()){
					this.connectionGraph[code].Add(id);
					playerMessage.PlayerLocation(this.cl.regionHandler.allPlayerData[code]);
					this.Send(playerMessage.GetMessage(), playerMessage.size, id);
				}
			}
		}

		NetMessage itemMessage = new NetMessage(NetCode.ITEMENTITYDATA);

		// Connects new Dropped items to player
		if(this.entityHandler.Contains(EntityType.DROP, pos)){
			DroppedItemAI droppedItem;

			foreach(ulong itemCode in this.entityHandler.dropObject[pos].Keys){
				droppedItem = (DroppedItemAI)this.entityHandler.dropObject[pos][itemCode];
				itemMessage.ItemEntityData(droppedItem.position.x, droppedItem.position.y, droppedItem.position.z, droppedItem.rotation.x, droppedItem.rotation.y, droppedItem.rotation.z, (ushort)droppedItem.its.GetID(), droppedItem.its.GetAmount(), itemCode);
			}
		}
	}

	// Deletes the connection between a client and a chunk
	private void RequestChunkUnload(byte[] data, ulong id){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
        NetMessage killMessage = new NetMessage(NetCode.ENTITYDELETE);

        if(this.playersInChunk.ContainsKey(pos)){
	        foreach(ulong code in this.playersInChunk[pos]){
	        	if(code == id)
	        		continue;

	        	this.connectionGraph[code].Remove(id);

	        	killMessage.EntityDelete(EntityType.PLAYER, code);
	        	this.Send(killMessage.GetMessage(), killMessage.size, id);
	        }

	        this.playersInChunk[pos].Remove(id);
	    }

	    // CHANGE THIS TO A SINGLE NETCODE MESSAGE TO DELETE THE ENTIRE CHUNK FOR ENTITIES
        if(this.entityHandler.Contains(EntityType.DROP, pos)){
	        foreach(ulong itemCode in this.entityHandler.dropObject[pos].Keys){
	        	killMessage.EntityDelete(EntityType.DROP, itemCode);
	        	this.Send(killMessage.GetMessage(), killMessage.size, id);
	        }
	    }

        if(this.cl.UnloadChunk(pos, id))
	    	this.entityHandler.UnloadChunk(pos);

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
	// ComesFromMessage flag is used to call this function from within the Server Code and not triggered by any message
	private void DirectBlockUpdate(byte[] data, ulong id, bool comesFromMessage=true){
		ChunkPos pos;
		int x, y, z, facing;
		ushort blockCode, state, hp;
		byte slot, newQuantity;
		BUDCode type;
		NetMessage message;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		facing = NetDecoder.ReadInt(data, 22);
		blockCode = NetDecoder.ReadUshort(data, 26);
		state = 0;
		hp = 0;


		if(comesFromMessage){
			state = NetDecoder.ReadUshort(data, 28);
			hp = NetDecoder.ReadUshort(data, 30);
			type = (BUDCode)NetDecoder.ReadInt(data, 32);
		}
		else{
			type = (BUDCode)NetDecoder.ReadInt(data, 28);
		}

		slot = data[36];
		newQuantity = data[37];

		CastCoord lastCoord = new CastCoord(pos, x, y, z);

		switch(type){
			case BUDCode.PLACE:
				// if chunk is still loaded
				if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos())){

					// if it's a block
					if(blockCode <= ushort.MaxValue/2){
						// if placement rules fail
						if(!VoxelLoader.GetBlock(blockCode).PlacementRule(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl)){
							NetMessage denied = new NetMessage(NetCode.PLACEMENTDENIED);
							this.Send(denied.GetMessage(), denied.size, id);
							return;
						}
					}
					// if it's an object
					else{
						if(!VoxelLoader.GetObject(blockCode).PlacementRule(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl)){
							NetMessage denied = new NetMessage(NetCode.PLACEMENTDENIED);
							this.Send(denied.GetMessage(), denied.size, id);
							return;
						}
					}

					// Check if is trying to put block on players
					if(this.playersInChunk.ContainsKey(pos)){
						foreach(ulong code in this.playersInChunk[pos]){
							if(code == id)
								continue;

							if(!this.cl.regionHandler.allPlayerData[code].CheckValidPlacement(lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ())){
								NetMessage denied = new NetMessage(NetCode.PLACEMENTDENIED);
								this.Send(denied.GetMessage(), denied.size, id);
								return;
							}
						}
					}

					// If doesn't have special place handling
					if(!VoxelLoader.CheckCustomPlace(blockCode)){
						// Actually places block/asset into terrain
						cl.chunks[lastCoord.GetChunkPos()].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
						//cl.budscheduler.ScheduleReload(lastCoord.GetChunkPos(), 0);
						EmitBlockUpdate(BUDCode.CHANGE, lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ(), 0, cl);


						// Applies OnPlace Event
						if(blockCode <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(blockCode).OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						else{
							VoxelLoader.GetObject(blockCode).OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						}

						this.cl.playerServerInventory.ChangeQuantity(id, slot, newQuantity);

						// Sends the updated voxel to loaded clients
						message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
						message.DirectBlockUpdate(BUDCode.PLACE, lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, blockCode, this.cl.chunks[lastCoord.GetChunkPos()].metadata.GetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ), this.cl.chunks[lastCoord.GetChunkPos()].metadata.GetHP(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ));
						SendToClients(lastCoord.GetChunkPos(), message);
						
						this.cl.regionHandler.SaveChunk(this.cl.chunks[pos]);
					}

					// If has special handling
					else{
						// Actually places block/asset into terrain
						this.cl.chunks[lastCoord.GetChunkPos()].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);

						if(blockCode <= ushort.MaxValue/2){
							VoxelLoader.GetBlock(blockCode).OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						}
						else{
							VoxelLoader.GetObject(blockCode).OnPlace(lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
						}
					}

					this.cl.playerServerInventory.ChangeQuantity(id, slot, newQuantity);

					// Make entities in this chunk update their TerrainVision
					this.entityHandler.SetRefreshVision(EntityType.DROP, lastCoord.GetChunkPos());

				}
				break;
			case BUDCode.SETSTATE:
				if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos())){
					this.cl.chunks[lastCoord.GetChunkPos()].metadata.SetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
					this.entityHandler.SetRefreshVision(EntityType.DROP, lastCoord.GetChunkPos());
				}
				break;

			case BUDCode.BREAK:
				// If doesn't has special break handling
				if(!VoxelLoader.CheckCustomBreak(blockCode)){

					// Actually breaks new block and updates chunk
					this.cl.chunks[pos].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, 0);
					this.cl.chunks[pos].metadata.Reset(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ);

					// Triggers OnBreak
					if(blockCode <= ushort.MaxValue/2)
						VoxelLoader.GetBlock(blockCode).OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);
					else
						VoxelLoader.GetObject(blockCode).OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);

					EmitBlockUpdate(BUDCode.BREAK, lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ(), 0, this.cl);
					
				}
				// If has special break handlings
				else{

					if(blockCode <= ushort.MaxValue/2){
						VoxelLoader.GetBlock(blockCode).OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);
					}
					else{
						VoxelLoader.GetObject(blockCode).OnBreak(pos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, this.cl);
					}
				}

				// Sends the updated voxel to loaded clients
				this.entityHandler.SetRefreshVision(EntityType.DROP, lastCoord.GetChunkPos());
				message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
				message.DirectBlockUpdate(BUDCode.BREAK, lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, 0, ushort.MaxValue, ushort.MaxValue);
				SendToClients(lastCoord.GetChunkPos(), message);				
				this.cl.regionHandler.SaveChunk(this.cl.chunks[pos]);

				break;

			case BUDCode.LOAD:
				// HP is set as the Chunk Coordinates vs World Coordinates flag
				if(hp == ushort.MaxValue)
					lastCoord = new CastCoord(new Vector3(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ));
				
				blockCode = this.cl.GetBlock(lastCoord);

				if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos())){
					
					if(blockCode <= ushort.MaxValue/2){
						VoxelLoader.GetBlock(blockCode).OnLoad(lastCoord, this.cl);
					}
					else{
						VoxelLoader.GetObject(blockCode).OnLoad(lastCoord, this.cl);
					}
				}
				break;

			default:
				break;
		}
	}

	// Receives player position and adds it to PlayerPositions Dict
	private void ClientPlayerPosition(byte[] data, ulong id){
		float3 pos, dir;
		ChunkPos cp;
		NetMessage graphMessage = new NetMessage(NetCode.PLAYERLOCATION);
	
		pos = NetDecoder.ReadFloat3(data, 1);
		dir = NetDecoder.ReadFloat3(data, 13);

		this.cl.regionHandler.allPlayerData[id].SetPosition(pos.x, pos.y, pos.z);
		this.cl.regionHandler.allPlayerData[id].SetDirection(dir.x, dir.y, dir.z);

		cp = this.cl.regionHandler.allPlayerData[id].GetChunkPos();

		if(!this.entityHandler.Contains(EntityType.PLAYER, cp, id))
			this.entityHandler.AddPlayer(cp, id, pos, dir, cl);

		this.entityHandler.SetPosition(EntityType.PLAYER, id, cp, pos, dir);

		// Propagates data to all network
		if(this.connectionGraph.ContainsKey(id)){
			foreach(ulong code in this.connectionGraph[id]){
				graphMessage.PlayerLocation(this.cl.regionHandler.allPlayerData[id]);
				this.Send(graphMessage.GetMessage(), graphMessage.size, code);
			}
		}
	}

	// Receives a request from a player to fetch another player's appearance (or himself)
	private void RequestPlayerAppearance(byte[] data, ulong id){
		ulong requestedID = NetDecoder.ReadUlong(data, 1);

		CharacterSheet cs = this.cl.characterFileHandler.LoadCharacterSheet(requestedID);
		CharacterAppearance app;
		bool isMale;

		if(cs == null)
			return;
		else{
			app = cs.GetCharacterAppearance();
			isMale = cs.GetGender();
		}

		NetMessage message = new NetMessage(NetCode.SENDPLAYERAPPEARANCE);
		message.SendPlayerAppearance(requestedID, app, isMale);
		this.Send(message.GetMessage(), message.size, id);
	}

	// Receives a disconnect call from client
	private void Disconnect(ulong id, DisconnectType type){
		if(!this.connections.ContainsKey(id))
			return;

		List<ChunkPos> toRemove = new List<ChunkPos>();
		NetMessage killMessage = new NetMessage(NetCode.ENTITYDELETE);
		killMessage.EntityDelete(EntityType.PLAYER, id);

		// Captures and removes all chunks
		foreach(KeyValuePair<ChunkPos, HashSet<ulong>> item in this.cl.loadedChunks){
			if(this.cl.loadedChunks[item.Key].Contains(id)){
				toRemove.Add(item.Key);
			}
		}

		foreach(ChunkPos pos in toRemove){
			if(this.cl.UnloadChunk(pos, id))
				this.entityHandler.UnloadChunk(pos);
		}

		this.connections[id].Close();
		this.connections.Remove(id);
		this.timeoutTimers.Remove(id);
		this.lengthPacket.Remove(id);
		this.dataBuffer.Remove(id);
		this.packetIndex.Remove(id);
		this.packetSize.Remove(id);
		this.playerRenderDistances.Remove(id);
		this.connectionGraph.Remove(id);
		this.receiveBuffer.Remove(id);

		this.entityHandler.Remove(EntityType.PLAYER, this.cl.regionHandler.allPlayerData[id].GetChunkPos(), id);
		
		foreach(ulong code in this.cl.regionHandler.allPlayerData.Keys){
			if(code == id)
				continue;
			// If iterates through non-online user
			if(!this.cl.regionHandler.allPlayerData[code].IsOnline())
				continue;
			// If finds connection to it, erase
			if(this.connectionGraph[code].Contains(id)){
				this.connectionGraph[code].Remove(id);
				this.Send(killMessage.GetMessage(), killMessage.size, code);
			}
		}

		this.cl.regionHandler.SavePlayers();
		this.cl.regionHandler.allPlayerData[id].SetOnline(false);

		if(this.playersInChunk.ContainsKey(this.cl.regionHandler.allPlayerData[id].GetChunkPos())){
			if(this.playersInChunk[this.cl.regionHandler.allPlayerData[id].GetChunkPos()].Count > 1)
				this.playersInChunk[this.cl.regionHandler.allPlayerData[id].GetChunkPos()].Remove(id);
			else
				this.playersInChunk.Remove(this.cl.regionHandler.allPlayerData[id].GetChunkPos());
		}

		toRemove = new List<ChunkPos>();

		// Removes ChunksRequested
		foreach(ChunkPos pos in chunksRequested.Keys){
			toRemove.Add(pos);
		}

		foreach(ChunkPos pos in toRemove){
			chunksRequested[pos].Remove(id);
			if(chunksRequested[pos].Count == 0)
				chunksRequested.Remove(pos);
		}

		if(type == DisconnectType.QUIT)
			Debug.Log("ID: " + id + " has disconnected");
		else if(type == DisconnectType.LOSTCONNECTION)
			Debug.Log("ID: " + id + " has lost connection");
		else if(type == DisconnectType.TIMEDOUT)
			Debug.Log("ID: " + id + " has timed out");
		else if(type == DisconnectType.LOGINOVERWRITE)
			Debug.Log("ID: " + id + " has overwritten its login");
		else
			Debug.Log("ID: " + id + " has quit due to unknown issue");

		if(this.isLocal)
			Application.Quit();
	}

	// Receives an Interaction command from client
	private void Interact(byte[] data, ulong id){
		// DEBUG
		this.cl.TestInventoryReceive(id);

		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x = NetDecoder.ReadInt(data, 10);
		int y = NetDecoder.ReadInt(data, 14);
		int z = NetDecoder.ReadInt(data, 18);
		int facing = NetDecoder.ReadInt(data, 22);
		int callback;

		CastCoord current = new CastCoord(pos, x, y, z);

		ushort blockCode = this.cl.GetBlock(current);

		if(blockCode <= ushort.MaxValue/2)
			callback = VoxelLoader.GetBlock(blockCode).OnInteract(pos, current.blockX, current.blockY, current.blockZ, this.cl);
		else
			callback = VoxelLoader.GetObject(blockCode).OnInteract(pos, current.blockX, current.blockY, current.blockZ, this.cl);

		// Actual handling of message
		CallbackHandler(callback, pos, current, facing);		
	}

	// Receives a heartbeat from a client to reset it's timeoutTimers
	private void Heartbeat(ulong id){
		this.timeoutTimers[id] = DateTime.Now;
	}

	// Receives the client's current chunk and finds the connections it has with other players
	private void ClientChunk(byte[] data, ulong id){
		ChunkPos lastPos = NetDecoder.ReadChunkPos(data, 1);
		ChunkPos newPos = NetDecoder.ReadChunkPos(data, 10);

		// Removes last ChunkPos if exists
		if(lastPos != newPos){
			if(this.playersInChunk.ContainsKey(lastPos)){
				if(this.playersInChunk[lastPos].Count > 1){
					this.playersInChunk[lastPos].Remove(id);
				}
				else{
					this.playersInChunk.Remove(lastPos);
				}

				if(this.entityHandler.Contains(EntityType.PLAYER, lastPos, id))
					this.entityHandler.Remove(EntityType.PLAYER, lastPos, id);
			}
		}

		// Add new ChunkPos
		if(!this.playersInChunk.ContainsKey(newPos))
			this.playersInChunk.Add(newPos, new HashSet<ulong>(){id});
		else
			this.playersInChunk[newPos].Add(id);

		// Finds the connections
		ChunkPos targetPos;
		NetMessage killMessage = new NetMessage(NetCode.ENTITYDELETE);

		foreach(ulong code in this.cl.regionHandler.allPlayerData.Keys){
			// If iterates through itself
			if(code == id)
				continue;
			// If iterates through non-online user
			if(!this.cl.regionHandler.allPlayerData[code].IsOnline())
				continue;

			// Check if code should still be connected
			if(this.connectionGraph[id].Contains(code)){
				targetPos = this.cl.regionHandler.allPlayerData[code].GetChunkPos();
				if(!this.cl.loadedChunks.ContainsKey(newPos)){
					this.cl.loadedChunks.Add(newPos, new HashSet<ulong>());
				}
				if(!this.cl.loadedChunks[newPos].Contains(code)){
					this.connectionGraph[id].Remove(code);
					killMessage.EntityDelete(EntityType.PLAYER, id);
					this.Send(killMessage.GetMessage(), killMessage.size, code);
				}
			}
			// Check if code should be connected
			else{
				if(!this.cl.loadedChunks.ContainsKey(newPos)){
					this.cl.loadedChunks.Add(newPos, new HashSet<ulong>());
				}

				if(this.cl.loadedChunks[newPos].Contains(code)){
					NetMessage liveMessage = new NetMessage(NetCode.PLAYERLOCATION);

					this.connectionGraph[id].Add(code);
					liveMessage.PlayerLocation(this.cl.regionHandler.allPlayerData[id]);
					this.Send(liveMessage.GetMessage(), liveMessage.size, code);					
				}				
			}
		}
	}

	// Receives a Drop Item notification and creates the DropItemAI Entity
	private void DropItem(byte[] data, ulong id){
		float3 pos, rot, move;
		ushort itemCode;
		byte amount, slot;
		NetMessage message = new NetMessage(NetCode.ITEMENTITYDATA);

		pos = NetDecoder.ReadFloat3(data, 1);
		move = NetDecoder.ReadFloat3(data, 13);
		itemCode = NetDecoder.ReadUshort(data, 25);
		amount = data[27];
		slot = data[28];

		rot = new float3(0,0,0);

		CastCoord coord = new CastCoord(pos);
		ChunkPos cp = coord.GetChunkPos();

		ulong code = this.entityHandler.AddItem(pos, rot, move, itemCode, amount, id, this.cl);

		message.ItemEntityData(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, itemCode, amount, code);
		this.SendToClients(cp, message);

		this.cl.playerServerInventory.ChangeQuantity(id, slot, (byte)(this.cl.playerServerInventory.GetQuantity(id, slot) - amount));
	}

	// Receives a BatchLoadBUD request from client, with plenty of block coordinates in a chunk
	private void BatchLoadBUD(byte[] data, ulong id){
		ChunkPos pos;
		int x, y, z, facing;
		ushort blockCode, state, hp;

		int currentByte = 10;
		CastCoord lastCoord;

		pos = NetDecoder.ReadChunkPos(data, 1);

		while(data.Length > currentByte){
			x = NetDecoder.ReadInt(data, currentByte);
			y = NetDecoder.ReadInt(data, currentByte+4);
			z = NetDecoder.ReadInt(data, currentByte+8);
			facing = NetDecoder.ReadInt(data, currentByte+12);	
			blockCode = NetDecoder.ReadUshort(data, currentByte+16);
			state = NetDecoder.ReadUshort(data, currentByte+18);
			hp = NetDecoder.ReadUshort(data, currentByte+20);
			currentByte += 22;	

			lastCoord = new CastCoord(pos, x, y, z);


			// HP is set as the Chunk Coordinates vs World Coordinates flag
			if(hp == ushort.MaxValue)
				lastCoord = new CastCoord(new Vector3(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ));
			
			blockCode = this.cl.GetBlock(lastCoord);

			if(this.cl.chunks.ContainsKey(lastCoord.GetChunkPos())){
				
				if(blockCode <= ushort.MaxValue/2){
					VoxelLoader.GetBlock(blockCode).OnLoad(lastCoord, this.cl);
				}
				else{
					VoxelLoader.GetObject(blockCode).OnLoad(lastCoord, this.cl);
				}
			}	
		}

		this.SendAccumulatedChunks();
	}

	// Receives a block damage from client and processes block HP
	private void BlockDamage(byte[] data, ulong id){
		// Loaded from message
		ChunkPos pos;
		int x, y, z;
		ushort blockDamage;
		Chunk c;

		// Auxiliary
		int currentHP, actualDamage;
		ushort block;
		int decalBefore, decalAfter;
		bool isBlock;
		NetMessage message;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		blockDamage = NetDecoder.ReadUshort(data, 22);

		if(this.cl.chunks.ContainsKey(pos)){
			c = this.cl.chunks[pos];

			block = c.data.GetCell(x, y, z);
			currentHP = c.metadata.GetHP(x, y, z);

			if(block <= ushort.MaxValue/2){
				isBlock = true;
			}
			else{
				isBlock = false;
			}

			if(currentHP == 0 || currentHP == ushort.MaxValue){
				if(isBlock){
					currentHP = BlockEncyclopediaECS.blockHP[block];
				}
				else{
					currentHP = BlockEncyclopediaECS.objectHP[ushort.MaxValue - block];
				}
			}

			actualDamage = VoxelLoader.GetDamageReceived(block, blockDamage);

			if(actualDamage == 0)
				return;

			decalBefore = GetDecalCode(block, currentHP);
			currentHP -= actualDamage;
			decalAfter = GetDecalCode(block, currentHP);

			// If block has broken
			if(currentHP <= 0){
				this.cacheBreakData[0] = 0;
				NetDecoder.WriteChunkPos(pos, this.cacheBreakData, 1);
				NetDecoder.WriteInt(x, this.cacheBreakData, 10);
				NetDecoder.WriteInt(y, this.cacheBreakData, 14);
				NetDecoder.WriteInt(z, this.cacheBreakData, 18);
				NetDecoder.WriteInt(0, this.cacheBreakData, 22);
				NetDecoder.WriteUshort(block, this.cacheBreakData, 26);
				NetDecoder.WriteInt((int)BUDCode.BREAK, this.cacheBreakData, 28);
				this.cacheBreakData[36] = 0;
				this.cacheBreakData[37] = 0;

				this.DirectBlockUpdate(this.cacheBreakData, id, comesFromMessage:false);
			}
			// If damage wasn't significant enough for a decal change
			else{
				c.metadata.SetHP(x, y, z, (ushort)currentHP);

				message = new NetMessage(NetCode.BLOCKDAMAGE);
				message.BlockDamage(pos, x, y, z, (ushort)currentHP, decalBefore != decalAfter);
				SendToClients(pos, message);
				this.cl.regionHandler.SaveChunk(c);
			}
		}
	}

	// Receives the inventory of client and saves it
	private void SendInventory(byte[] data, ulong id){
		this.cl.playerServerInventory.AddInventory(id, data);
	}

	// Receives a request to check character existence, if exists, sends CharacterAppearance
	public void RequestCharacterExistence(byte[] data, ulong id){
		ulong charID = NetDecoder.ReadUlong(data, 1);
		NetMessage message = new NetMessage(NetCode.SENDCHARACTERPRELOAD);

		this.cachedSheet = this.cl.characterFileHandler.LoadCharacterSheet(charID);

		if(this.cachedSheet == null)
			message.SendCharacterPreload(null, false);
		else
			message.SendCharacterPreload(this.cachedSheet.GetCharacterAppearance(), this.cachedSheet.GetGender());

		this.Send(message.GetMessage(), message.size, id, temporary:true);
	}

	// Receives the request for sending a CharacterSheet to user
	public void RequestCharacterSheet(byte[] data, ulong id){
		ulong code = NetDecoder.ReadUlong(data, 1);

		this.cachedSheet = this.cl.characterFileHandler.LoadCharacterSheet(code);

		if(this.cachedSheet == null)
			return;

		NetMessage message = new NetMessage(NetCode.SENDCHARSHEET);
		message.SendCharSheet(code, this.cachedSheet);
		this.Send(message.GetMessage(), message.size, id);
	}

	// Receives the character sheet from a new character
	public void SendCharSheet(byte[] data, ulong id){
		ulong charID = NetDecoder.ReadUlong(data, 1);
		CharacterSheet sheet = NetDecoder.ReadCharacterSheet(data, 9);

		this.cl.characterFileHandler.SaveCharacterSheet(charID, sheet);
	}

	// Receives the current hotbar slot in player's hand
	public void SendHotbarPosition(byte[] data, ulong id){
		byte slot = NetDecoder.ReadByte(data, 1);

		return;
	}

	// Receives a Disconnect message from InfoClient
	public void DisconnectInfo(ulong id){
		Debug.Log("ID: " + id + " was sent to character creation");

		this.temporaryConnections[id].Close();
		this.temporaryConnections.Remove(id);
	}


	/*
	// Auxiliary Functions
	*/

	// Send input message to all Clients connected to a given Chunk
	public void SendToClients(ChunkPos pos, NetMessage message){
		if(!this.cl.loadedChunks.ContainsKey(pos))
			return;

		foreach(ulong i in this.cl.loadedChunks[pos]){
			this.Send(message.GetMessage(), message.size, i);
		}
	}

	// Send input message to all Clients connected to a given Chunk except the given one
	public void SendToClientsExcept(ChunkPos pos, NetMessage message, ulong exception){
		if(!this.cl.loadedChunks.ContainsKey(pos))
			return;

		foreach(ulong i in this.cl.loadedChunks[pos]){
			if(i == exception)
				continue;
			this.Send(message.GetMessage(), message.size, i);
		}
	}

	// Register the current chunk to a set of chunks that should be triggered at the end of the current operation
	// This way, only one chunk update message can be sent per multiple block/tile-entity operations
	public void RegisterChunkToSend(ChunkPos pos){
		this.chunksToSend.Add(pos);
	}

	// Clears the Chunks to Send set
	private void ClearChunkToSend(){
		this.chunksToSend.Clear();
	}

	// Sends all chunks in the ChunksToSend set to clients
	private void SendAccumulatedChunks(){
		foreach(ChunkPos pos in this.chunksToSend){
			if(!this.cl.loadedChunks.ContainsKey(pos))
				continue;

			foreach(ulong i in this.cl.loadedChunks[pos]){
				NetMessage message = new NetMessage(NetCode.SENDCHUNK);
				message.SendChunk(this.cl.chunks[pos]);
				this.Send(message.GetMessage(), message.size, i);
			}	
		}

		this.ClearChunkToSend();	
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
		// 1: Saves chunk and sends a DIRECTBLOCKUPDATE to all connected clients
		else if(code == 1){
			ushort blockCode = this.cl.GetBlock(thisPos);
			ushort state = this.cl.GetState(thisPos);
			ushort hp = this.cl.GetHP(thisPos);

			this.cl.regionHandler.SaveChunk(this.cl.chunks[targetChunk]);
			NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
			message.DirectBlockUpdate(BUDCode.CHANGE, targetChunk, thisPos.blockX, thisPos.blockY, thisPos.blockZ, facing, blockCode, state, hp);
			SendToClients(targetChunk, message);
		}
		// 2: Saves Chunk only
		else if(code == 2){
			this.cl.budscheduler.ScheduleSave(targetChunk);
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

		int faceCounter=0;

		foreach(CastCoord c in neighbors){
			// Ignores void updates
			if(c.blockY < 0 || c.blockY > Chunk.chunkDepth-1){
				continue;
			}

			cachedBUD = new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]);
			cl.budscheduler.ScheduleBUD(cachedBUD, tickOffset); 
		
			faceCounter++;
		}
	}

	private CastCoord GetCoordinates(int x, int y, int z){
		return new CastCoord(new Vector3(x ,y ,z));
	}

	// Returns an int-sized byte[] with the length packet
	private byte[] LengthPacket(int a){
		byte[] output = new byte[4];

		output[0] = (byte)(a >> 24);
		output[1] = (byte)(a >> 16);
		output[2] = (byte)(a >> 8);
		output[3] = (byte)a;

		return output;
	}

	// Checks timeout in all sockets
	public void CheckTimeout(){
		List<ulong> toRemove = new List<ulong>();

		foreach(ulong id in this.timeoutTimers.Keys){
			if((DateTime.Now - this.timeoutTimers[id]).Seconds > Server.timeoutSeconds){
				toRemove.Add(id);
			}
		}

		foreach(ulong id in toRemove)
			Disconnect(id, DisconnectType.TIMEDOUT);
	}

	// Returns the decal code for a given block and HP
	public int GetDecalCode(ushort block, int hp){
		ushort maxHP;
		float lifePercentage;

		if(block <= ushort.MaxValue/2)
			maxHP = BlockEncyclopediaECS.blockHP[block];
		else
			maxHP = BlockEncyclopediaECS.objectHP[ushort.MaxValue - block];

		lifePercentage = (float)hp / (float)maxHP;

	    for(int i=0; i < Constants.DECAL_STAGE_SIZE; i++){
			if(lifePercentage <= Constants.DECAL_STAGE_PERCENTAGE[i])
				return (Constants.DECAL_STAGE_SIZE - 1) - i;
		}

		return -1;
	}

}

public enum DisconnectType{
	QUIT, // Voluntarily quit
	LOSTCONNECTION, // Exceptions
	TIMEDOUT, // CheckTimeout() has detected a timeout
	LOGINOVERWRITE // Logged into an already logged in session
}
