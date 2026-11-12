using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using static System.Environment;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Mathematics;

using Random = UnityEngine.Random;

/*
Authoritative side of the game
	Every connection is a NetConnection. Their data is only touched by the main thread: sockets accepted and frames received by
	IO threads are queued and handled in ProcessIncoming(), while sent messages are written once per frame in LateTick()
*/
public class Server {
	public int port = 33000;
	private bool isLocal = false;
	public bool applicationClose = false;
	public Socket masterSocket;
	private IPEndPoint serverIP;

	// Connections. Temporary ones haven't identified their AccountID yet
	public Dictionary<ulong, NetConnection> connections;
	private Dictionary<ulong, NetConnection> temporaryConnections;
	private ConcurrentQueue<Socket> acceptedSockets = new ConcurrentQueue<Socket>();

	// Filled by socket threads and consumed by the main thread
	public ConcurrentQueue<NetMessage> queue = new ConcurrentQueue<NetMessage>();

	private Dictionary<ulong, ChunkPos> playerToChunk;
	private Dictionary<ulong, Dictionary<int, AnimationData>> playerAnimationStates;
	private Dictionary<ulong, Dictionary<string, EntitySFXData>> playerActiveLoopSFX;
	private Dictionary<ulong, Dictionary<BoneAnchorType, AttachmentAnchorState>> playerAttachmentAnchors;
	private Dictionary<ulong, int> playerCarryStyles;
	private Dictionary<ulong, uint> playerPositionTimestamps;

	// Players are identified in the network by a NetID that lasts for the session. Dropped items use their entity code
	private Dictionary<ulong, uint> playerNetIDs;
	private uint nextNetID = 1;

	// Chunk synchronization
	private HashSet<ChunkPos> chunksToSend = new HashSet<ChunkPos>();
	private HashSet<ChunkPos> queuedChunkChanges = new HashSet<ChunkPos>();
	public ChunkPayloadCache chunkPayloadCache;

	// Entity movement sent at the end of the frame
	private EntityUpdateBatcher entityUpdates = new EntityUpdateBatcher();
	private HashSet<ulong> movedPlayers = new HashSet<ulong>();
	private Dictionary<DroppedItemAI, uint> movedDrops = new Dictionary<DroppedItemAI, uint>();

	public EntityHandler_Server entityHandler = new EntityHandler_Server();
	public ChunkStreamer chunkStreamer;

	// Players that know about each player (connectionGraph[player] = clients that have it spawned)
	public Dictionary<ulong, HashSet<ulong>> connectionGraph;
	private Dictionary<ChunkPos, HashSet<ulong>> playersInChunk;

	public Dictionary<ulong, int> playerRenderDistances = new Dictionary<ulong, int>();

	public ulong firstConnectedID = ulong.MaxValue;

	// Timers
	private float nextConnectionCheck = 0f;
	private float nextSheetFlush = 0f;
	private static readonly float CONNECTION_CHECK_INTERVAL = 1f;

	// Unity Reference
	public ChunkLoader_Server cl;

	// Cache
	private List<ulong> cachedIDs = new List<ulong>();
	private List<ChunkPos> cachedChunks = new List<ChunkPos>();
	private List<uint> cachedPlayerDeletes = new List<uint>();
	private List<uint> cachedDropDeletes = new List<uint>();
	private List<DroppedItemAI> cachedDrops = new List<DroppedItemAI>();
	private byte[] deleteBatchBuffer = new byte[256];

	public Server(ChunkLoader_Server cl){
    	ParseArguments();

    	// Parses config file if is a Dedicated Server
    	if(!this.isLocal){
    		ParseConfigFile();
    	}

		// Initiates Server data
    	connections = new Dictionary<ulong, NetConnection>();
    	temporaryConnections = new Dictionary<ulong, NetConnection>();
    	connectionGraph = new Dictionary<ulong, HashSet<ulong>>();
    	playersInChunk = new Dictionary<ChunkPos, HashSet<ulong>>();
    	playerToChunk = new Dictionary<ulong, ChunkPos>();
		playerAnimationStates = new Dictionary<ulong, Dictionary<int, AnimationData>>();
		playerActiveLoopSFX = new Dictionary<ulong, Dictionary<string, EntitySFXData>>();
		playerAttachmentAnchors = new Dictionary<ulong, Dictionary<BoneAnchorType, AttachmentAnchorState>>();
		playerCarryStyles = new Dictionary<ulong, int>();
		playerPositionTimestamps = new Dictionary<ulong, uint>();
		playerNetIDs = new Dictionary<ulong, uint>();

    	this.cl = cl;
    	this.entityHandler.SetChunkLoader(cl);
    	this.chunkStreamer = new ChunkStreamer(this, cl);
    	this.chunkPayloadCache = new ChunkPayloadCache(Constants.SERVER_CHUNK_PAYLOAD_CACHE_MB);
    	
    	if(!this.isLocal){
        	this.masterSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    		this.serverIP = new IPEndPoint(0, this.port);
    	}
        else{
        	this.masterSocket = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        	this.serverIP = new IPEndPoint(0x0100007F, this.port);
        }

        Debug.Log($"Starting Server at {this.serverIP}");

        this.masterSocket.Bind(this.serverIP);
        this.masterSocket.Listen(byte.MaxValue);

        this.masterSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
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
		// If a config file needs to be created
		else{
			file = File.Open("server.cfg", FileMode.Create);
			allBytes = System.Text.Encoding.ASCII.GetBytes("world_name=");
			file.Write(allBytes, 0, allBytes.Length);
			file.Close();
			Debug.Log("Generated .cfg file. Please restart!");

			#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
			#else
				this.applicationClose = true;
				Application.Quit();
			#endif
		}
	}

	public bool IsLocal(){return this.isLocal;}

	// Generates a random 8 letter code
	private string GenerateRandomName(){
		StringBuilder sb = new StringBuilder();
		char aux;

		for(int i=0; i<8; i++){
			bool uppercase = Random.Range(0,2) == 0;

			if(uppercase){
				aux = (char)Random.Range(65, 91);
			}
			else{
				aux = (char)Random.Range(97, 123);
			}

			sb.Append(aux);
		}

		return sb.ToString();
	}

	/*
	Connections
	*/

    // IO thread: hands accepted sockets to the main thread
    private void AcceptCallback(IAsyncResult result){
    	Socket client = null;

    	try{
    		client = this.masterSocket.EndAccept(result);
    	}
    	catch(ObjectDisposedException){
    		return;
    	}
    	catch(Exception e){
    		Debug.Log("Failed to accept a connection: " + e.Message);
    	}

    	if(client != null)
    		this.acceptedSockets.Enqueue(client);

    	try{
    		this.masterSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
    	}
    	catch(ObjectDisposedException){}
    }

    // Starts the connections accepted since the last frame
    private void ProcessAcceptedSockets(){
    	Socket socket;
    	NetConnection connection;
    	ulong temporaryCode;

    	while(this.acceptedSockets.TryDequeue(out socket)){
    		temporaryCode = GetTemporaryCode();
    		connection = new NetConnection(socket, temporaryCode, true, this.queue);
    		this.temporaryConnections.Add(temporaryCode, connection);

    		Debug.Log(connection.GetRemoteAddress() + " has connected with temporary ID " + temporaryCode);

    		connection.Start();

    		NetMessage message = new NetMessage(NetCode.ACCEPTEDCONNECT);
    		this.Send(message.GetMessage(), message.size, temporaryCode, temporary:true);
    	}
    }

	// Gets the first free temporary ID
	private ulong GetTemporaryCode(){
		ulong code = ulong.MaxValue-1;

		while(this.temporaryConnections.ContainsKey(code) || this.connections.ContainsKey(code))
			code--;

		return code;
	}

	// Queues a message to a client given its ID. Messages are written to the sockets at the end of the frame
	public void Send(byte[] data, int length, ulong id, bool temporary=false){
		NetConnection connection;

		if(temporary){
			if(!this.temporaryConnections.TryGetValue(id, out connection))
				return;
		}
		else if(!this.connections.TryGetValue(id, out connection)){
			return;
		}

		connection.Send(data, length);
		NetMessage.Broadcast(NetBroadcast.SENT, data[0], id, length);
	}

	// Sends a message to all IDs connected
	public void SendAll(byte[] data, int length){
		foreach(NetConnection connection in this.connections.Values)
			connection.Send(data, length);
	}

	// Amount of bytes waiting to be written to a client
	public int GetPendingBytes(ulong id){
		NetConnection connection;

		if(this.connections.TryGetValue(id, out connection))
			return connection.PendingBytes();
		return 0;
	}

	// Handles new connections and every message received since the last frame
	public void ProcessIncoming(){
		ProcessAcceptedSockets();

		// Messages that arrive while handling these are left for the next frame
		int queueCount = this.queue.Count;
		NetMessage message;

		for(int i=0; i < queueCount; i++){
			if(!this.queue.TryDequeue(out message))
				break;

			if(message.IsCloseEvent()){
				HandleConnectionClosed(message.GetConnection());
				continue;
			}

			// Messages of connections that were already dropped, like an overwritten login
			if(!message.GetConnection().removed)
				HandleReceivedMessage(message);

			NetBufferPool.Return(message.GetData());
		}
	}

	// Runs at the end of every frame: sends what was gathered during it and writes every queued message to the sockets
	public void LateTick(){
		ProcessQueuedChunkChanges();
		SendEntityUpdates();
		RunTimers();
		FlushAll();
	}

	private void FlushAll(){
		foreach(NetConnection connection in this.connections.Values)
			connection.Flush();
		foreach(NetConnection connection in this.temporaryConnections.Values)
			connection.Flush();
	}

	private void RunTimers(){
		float now = Time.realtimeSinceStartup;

		if(now >= this.nextConnectionCheck){
			this.nextConnectionCheck = now + CONNECTION_CHECK_INTERVAL;
			CheckTimeout();
			SendHeartbeats();
		}

		if(now >= this.nextSheetFlush){
			this.nextSheetFlush = now + Constants.SERVER_SHEET_FLUSH_SECONDS;
			this.entityHandler.FlushSheets();
		}
	}

	// Keeps idle clients from timing out
	private void SendHeartbeats(){
		NetMessage message = new NetMessage(NetCode.HEARTBEAT);

		foreach(NetConnection connection in this.connections.Values){
			if(connection.SecondsSinceSend() >= Constants.NET_HEARTBEAT_INTERVAL_SECONDS)
				connection.Send(message.GetMessage(), message.size);
		}
	}

	// Disconnects clients that stopped sending data, including heartbeats
	private void CheckTimeout(){
		this.cachedIDs.Clear();

		foreach(KeyValuePair<ulong, NetConnection> item in this.connections){
			if(item.Value.SecondsSinceReceive() > Constants.NET_SERVER_TIMEOUT_SECONDS)
				this.cachedIDs.Add(item.Key);
		}

		for(int i=0; i < this.cachedIDs.Count; i++)
			Disconnect(this.cachedIDs[i], DisconnectType.TIMEDOUT);

		this.cachedIDs.Clear();

		foreach(KeyValuePair<ulong, NetConnection> item in this.temporaryConnections){
			if(item.Value.SecondsSinceReceive() > Constants.NET_TEMPORARY_TIMEOUT_SECONDS)
				this.cachedIDs.Add(item.Key);
		}

		for(int i=0; i < this.cachedIDs.Count; i++){
			Debug.Log("Temporary ID: " + this.cachedIDs[i] + " has timed out");
			CloseTemporary(this.cachedIDs[i]);
		}
	}

	// A connection was closed by the peer or by an error
	private void HandleConnectionClosed(NetConnection connection){
		NetConnection current;

		if(connection.removed)
			return;

		if(connection.isTemporary){
			CloseTemporary(connection.id);
			return;
		}

		if(this.connections.TryGetValue(connection.id, out current) && current == connection){
			Debug.Log("Connection of ID: " + connection.id + " was closed: " + connection.GetCloseReason());
			Disconnect(connection.id, DisconnectType.LOSTCONNECTION);
		}
	}

	private void CloseTemporary(ulong id){
		NetConnection connection;

		if(!this.temporaryConnections.TryGetValue(id, out connection))
			return;

		connection.removed = true;
		connection.Close(flush:true);
		this.temporaryConnections.Remove(id);
	}

	// Gets the NetID of a player, assigning one if it doesn't have it yet
	public uint GetPlayerNetID(ulong accountID){
		uint netID;

		if(!this.playerNetIDs.TryGetValue(accountID, out netID)){
			netID = this.nextNetID;
			this.nextNetID++;
			this.playerNetIDs.Add(accountID, netID);
		}

		return netID;
	}

	// Gets the ID that identifies an entity in the network
	public uint GetEntityNetID(EntityType type, ulong code){
		if(type == EntityType.PLAYER)
			return GetPlayerNetID(code);
		return (uint)code;
	}

	/* ===========================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from a client
	private void HandleReceivedMessage(NetMessage message){
		byte[] data = message.GetData();
		int length = message.GetSize();
		NetConnection connection = message.GetConnection();
		ulong id = connection.id;

		NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], id, length);

		// Connections that didn't identify themselves can only do it or use the InfoClient protocol
		if(connection.isTemporary){
			switch((NetCode)data[0]){
				case NetCode.SENDCLIENTINFO:
					SendClientInfo(data, connection);
					break;
				case NetCode.REQUESTCHARACTEREXISTENCE:
					RequestCharacterExistence(data, id);
					break;
				case NetCode.SENDCHARSHEET:
					SendCharSheet(data, id, isTemporary:true);
					break;
				case NetCode.DISCONNECTINFO:
					CloseTemporary(id);
					break;
				case NetCode.HEARTBEAT:
					break;
				default:
					Debug.Log("NETMESSAGE " + (NetCode)data[0] + " RECEIVED FROM UNIDENTIFIED CONNECTION " + id);
					break;
			}

			return;
		}

		switch((NetCode)data[0]){
			case NetCode.HEARTBEAT:
				break;
			case NetCode.REQUESTCHUNKLOAD:
				RequestChunkLoad(data, id);
				break;
			case NetCode.STREAMCENTER:
				StreamCenter(data, id);
				break;
			case NetCode.CHUNKACK:
				ChunkAck(data, id);
				break;
			case NetCode.PLACEBLOCK:
				PlaceBlock(data, id);
				break;
			case NetCode.CLIENTPLAYERPOSITION:
				ClientPlayerPosition(data, id);
				break;
			case NetCode.DISCONNECT:
				Disconnect(id, DisconnectType.QUIT);
				break;
			case NetCode.INTERACT:
				Interact(data, id);
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
				SendInventory(data, length, id);
				break;
			case NetCode.REQUESTCHARACTERSHEET:
				RequestCharacterSheet(data, id);
				break;
			case NetCode.SENDCHARSHEET:
				SendCharSheet(data, id, isTemporary:false);
				break;
			case NetCode.SENDHOTBARPOSITION:
				SendHotbarPosition(data, id);
				break;
			case NetCode.SENDANIMATIONLAYER:
				SendAnimationLayer(data, id);
				break;
			case NetCode.SENDANIMATORPARAMETER:
				SendAnimatorParameter(data, id);
				break;
			case NetCode.EQUIPITEM:
				EquipItem(data, id);
				break;
			case NetCode.ENTITYSFX:
				EntitySFX(data, id);
				break;
			case NetCode.SENDATTACHMENTANCHOR:
				SendAttachmentAnchor(data, id);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED: " + (NetCode)data[0]);
				break;
		}
	}	

	// Captures client info and assigns its AccountID to the connection
	private void SendClientInfo(byte[] data, NetConnection connection){
		ulong id = connection.id;
		int stringSize;
		ulong accountID = NetDecoder.ReadUlong(data, 1);
		int renderDistance = NetDecoder.ReadInt(data, 9); 
		int seed = NetDecoder.ReadInt(data, 13);
		string worldName = NetDecoder.ReadString16(data, 17, out stringSize);
		CharacterSheet sheet;
		uint netID;

		// If AccountID is already online, erase all memory from that connection before starting the new one
		if(this.connections.ContainsKey(accountID)){
			Disconnect(accountID, DisconnectType.LOGINOVERWRITE);
		}

		this.playerRenderDistances[accountID] = renderDistance;
		netID = GetPlayerNetID(accountID);
		
		if(this.isLocal){
			World.worldName = worldName;
			World.worldSeed = seed;
		}

		// Assigns a fixed ID. The connection keeps receiving, so its next messages already use the AccountID
		this.temporaryConnections.Remove(id);
		connection.id = accountID;
		connection.isTemporary = false;
		this.connections.Add(accountID, connection);
		this.connectionGraph.Add(accountID, new HashSet<ulong>());

		// Sends Player Info
		if(this.cl.RECEIVEDWORLDDATA){
			// Sends player data
			PlayerData pdat = this.cl.regionHandler.LoadPlayer(accountID);
			Vector3 playerPos = pdat.GetPosition();
			Vector3 playerDir = pdat.GetDirection();
			ChunkPos playerChunkPos = new CastCoord(playerPos).GetChunkPos();
			HashSet<ulong> players;
			int inventoryLength;
			bool isEmptyInventory;

			pdat.SetOnline(true);

			// Add to Chunk graph
			if(!this.playersInChunk.TryGetValue(playerChunkPos, out players)){
				players = new HashSet<ulong>();
				this.playersInChunk.Add(playerChunkPos, players);
			}

			players.Add(accountID);
			this.playerToChunk[accountID] = playerChunkPos;

			this.entityHandler.GetSheet(accountID);

			NetMessage message = new NetMessage(NetCode.SENDSERVERINFO);
			message.SendServerInfo(playerPos.x, playerPos.y, playerPos.z, playerDir.x, playerDir.y, playerDir.z, this.cl.time.days, this.cl.time.hours, this.cl.time.minutes, netID);
			this.Send(message.GetMessage(), message.size, accountID);

			// Sends player inventory data
			NetMessage inventoryMessage = new NetMessage(NetCode.SENDINVENTORY);
			inventoryLength = this.cl.playerServerInventory.LoadInventoryIntoBuffer(accountID, out isEmptyInventory);
			inventoryMessage.SendInventory(this.cl.playerServerInventory.GetBuffer(), inventoryLength);
			this.Send(inventoryMessage.GetMessage(), inventoryMessage.size, accountID);

			// Sends global weather noise data
			NetMessage weatherMessage = new NetMessage(NetCode.SENDNOISE);
			weatherMessage.SendNoise(GenerationSeed.weatherNoise, World.worldSeed);
			this.Send(weatherMessage.GetMessage(), weatherMessage.size, accountID);
		}

		this.cl.RECEIVEDWORLDDATA = true;

		if(this.firstConnectedID == ulong.MaxValue)
			this.firstConnectedID = accountID;

		Debug.Log("Temporary ID: " + id + " was assigned to ID: " + accountID);

		// Players always join with the Normal Hotbar selected and their weapon sheathed
		sheet = this.entityHandler.GetSheet(accountID);

		if(!sheet.IsInNormalHotbar()){
			sheet.SetIsInNormalHotbar(true);
			this.entityHandler.MarkSheetDirty(accountID);
		}

    	bool isEmpty;
		byte hotbarSlot = sheet.GetHotbarSlot();
		this.cl.playerServerInventory.LoadInventoryIntoBuffer(accountID, out isEmpty);

		ItemStack its = this.cl.playerServerInventory.GetSlot(accountID, hotbarSlot).GetItemStack();
		its.GetItem().OnHoldServer(this.cl, its, accountID);
	}

	// Client reports a chunk of its StreamWindow that it's missing
	private void RequestChunkLoad(byte[] data, ulong id){
		this.chunkStreamer.Resync(id, NetDecoder.ReadChunkPos(data, 1));
	}

	// Sends a loaded chunk to a client and the entities in it
	// Streamed chunks count towards the client's streaming window
	public void SendChunkToClient(ChunkPos pos, ulong id, bool stream){
		HashSet<ulong> clients;
		HashSet<ulong> players;
		byte[] payload;
		int length;

		if(!this.cl.loadedChunks.TryGetValue(pos, out clients)){
			clients = new HashSet<ulong>();
			this.cl.loadedChunks.Add(pos, clients);
		}

		clients.Add(id);

		GetChunkPayload(this.cl.GetChunk(pos), out payload, out length);
		payload[0] = (byte)(stream ? NetCode.STREAMCHUNK : NetCode.SENDCHUNK);
		this.Send(payload, length, id);

		// Players in the chunk
		if(this.playersInChunk.TryGetValue(pos, out players)){
			foreach(ulong code in players){
				if(code == id)
					continue;
				if(!this.connections.ContainsKey(code))
					continue;

				ShowPlayerTo(code, id);
			}
		}

		// Dropped items in the chunk
		this.entityHandler.GetDropsInChunk(pos, this.cachedDrops);

		for(int i=0; i < this.cachedDrops.Count; i++)
			SendDropSpawn(this.cachedDrops[i], id);
	}

	// Spawns a player for a client, with everything needed to show its current state
	private void ShowPlayerTo(ulong player, ulong recipient){
		HashSet<ulong> graph;
		PlayerData pdat = this.cl.regionHandler.allPlayerData[player];
		CharacterSheet sheet = this.entityHandler.GetSheet(player);

		if(sheet == null)
			return;

		if(this.connectionGraph.TryGetValue(player, out graph))
			graph.Add(recipient);

		NetMessage message = new NetMessage(NetCode.PLAYERSPAWN);
		message.PlayerSpawn(GetPlayerNetID(player), player, new float3(pdat.posX, pdat.posY, pdat.posZ), pdat.dirX, pdat.dirY, GetPlayerPositionTimestamp(player), sheet);
		this.Send(message.GetMessage(), message.size, recipient);

		SendItemInHandSnapshot(player, recipient);
		SendBattleStyleSnapshot(player, recipient);
		SendAttachmentAnchorSnapshot(player, recipient);
		SendAnimationSnapshot(player, recipient);
		SendAnimatorParameterSnapshot(player, recipient);
		SendEntitySFXSnapshot(player, recipient);
	}

	// Spawns a dropped item for a client
	private void SendDropSpawn(DroppedItemAI drop, ulong recipient){
		NetMessage message = BuildDropSpawn(drop);
		this.Send(message.GetMessage(), message.size, recipient);
	}

	private NetMessage BuildDropSpawn(DroppedItemAI drop){
		NetMessage message = new NetMessage(NetCode.DROPSPAWN);
		message.DropSpawn((uint)drop.entityCode, drop.position.x, drop.position.y, drop.position.z, drop.IsStanding(), drop.rotation.y, drop.rotation.z, (ushort)drop.its.GetID(), drop.its.GetAmount(), NetMessage.GetTimestamp());
		return message;
	}

	// Spawns a newly created dropped item for every client that has its chunk
	public void BroadcastDropSpawn(DroppedItemAI drop){
		NetMessage message = BuildDropSpawn(drop);
		SendToClients(drop.GetChunkPos(), message);
	}

	// Sends the movement of a dropped item at the end of the frame
	public void MarkDropMoved(DroppedItemAI drop){
		this.movedDrops[drop] = NetMessage.GetTimestamp();
	}

	// Spawns or deletes a dropped item for the clients that started or stopped seeing it after it changed chunks
	public void MoveDropBetweenChunks(DroppedItemAI drop, ChunkPos previousChunk){
		HashSet<ulong> previousClients;
		HashSet<ulong> newClients;
		ChunkPos newChunk = drop.GetChunkPos();
		bool hasPrevious = this.cl.loadedChunks.TryGetValue(previousChunk, out previousClients);
		bool hasNew = this.cl.loadedChunks.TryGetValue(newChunk, out newClients);

		if(hasPrevious){
			NetMessage killMessage = new NetMessage(NetCode.ENTITYDELETE);
			killMessage.EntityDelete(EntityType.DROP, (uint)drop.entityCode);

			foreach(ulong client in previousClients){
				if(hasNew && newClients.Contains(client))
					continue;

				this.Send(killMessage.GetMessage(), killMessage.size, client);
			}
		}

		if(hasNew){
			NetMessage spawnMessage = BuildDropSpawn(drop);

			foreach(ulong client in newClients){
				if(hasPrevious && previousClients.Contains(client))
					continue;

				this.Send(spawnMessage.GetMessage(), spawnMessage.size, client);
			}
		}
	}

	// Deletes an entity for every client that has its chunk
	public void SendEntityDelete(EntityType type, ulong code, ChunkPos pos){
		NetMessage message = new NetMessage(NetCode.ENTITYDELETE);
		message.EntityDelete(type, GetEntityNetID(type, code));
		SendToClients(pos, message);
	}

	// Deletes the connection between a client and a chunk and the entities in it
	public void UnloadChunkForClient(ChunkPos pos, ulong id){
		HashSet<ulong> players;
		HashSet<ulong> graph;

		this.cachedPlayerDeletes.Clear();
		this.cachedDropDeletes.Clear();

        if(this.playersInChunk.TryGetValue(pos, out players)){
	        foreach(ulong code in players){
	        	if(code == id)
	        		continue;

	        	if(this.connectionGraph.TryGetValue(code, out graph))
	        		graph.Remove(id);

	        	this.cachedPlayerDeletes.Add(GetPlayerNetID(code));
	        }

	        players.Remove(id);
	    }

	    // Drops that were already deleted or that moved to another chunk aren't included
	    this.entityHandler.GetDropsInChunk(pos, this.cachedDrops);

	    for(int i=0; i < this.cachedDrops.Count; i++)
	    	this.cachedDropDeletes.Add((uint)this.cachedDrops[i].entityCode);

	    SendEntityDeleteBatch(id, this.cachedPlayerDeletes, this.cachedDropDeletes);

        if(this.cl.UnloadChunk(pos, id))
	    	this.entityHandler.UnloadChunk(pos);
	}

	// Deletes many entities of a client in a single message
	private void SendEntityDeleteBatch(ulong id, List<uint> players, List<uint> drops){
		if(players.Count == 0 && drops.Count == 0)
			return;

		int size = 5 + (players.Count + drops.Count) * 4;
		int offset = 1;

		if(this.deleteBatchBuffer.Length < size)
			this.deleteBatchBuffer = new byte[Math.Max(size, this.deleteBatchBuffer.Length*2)];

		this.deleteBatchBuffer[0] = (byte)NetCode.ENTITYDELETEBATCH;
		NetDecoder.WriteUshort((ushort)players.Count, this.deleteBatchBuffer, offset);
		offset += 2;

		for(int i=0; i < players.Count; i++){
			NetDecoder.WriteUint(players[i], this.deleteBatchBuffer, offset);
			offset += 4;
		}

		NetDecoder.WriteUshort((ushort)drops.Count, this.deleteBatchBuffer, offset);
		offset += 2;

		for(int i=0; i < drops.Count; i++){
			NetDecoder.WriteUint(drops[i], this.deleteBatchBuffer, offset);
			offset += 4;
		}

		this.Send(this.deleteBatchBuffer, offset, id);
	}

	// Receives the center of a client's StreamWindow
	private void StreamCenter(byte[] data, ulong id){
		ChunkPos center = NetDecoder.ReadChunkPos(data, 1);
		int verticalLayer = (sbyte)data[10];

		this.chunkStreamer.SetStreamCenter(id, center, verticalLayer);
	}

	// Receives the amount of streamed chunks a client consumed
	private void ChunkAck(byte[] data, ulong id){
		int consumed = NetDecoder.ReadUshort(data, 1);
		int backlog = NetDecoder.ReadUshort(data, 3);

		this.chunkStreamer.ChunkAck(id, consumed, backlog);
	}

	/*
	Voxels
	*/

	// Receives a block placement from a client
	private void PlaceBlock(byte[] data, ulong id){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		int facing = (sbyte)data[12];
		ushort blockCode = NetDecoder.ReadUshort(data, 13);
		byte slot = data[15];
		byte newQuantity = data[16];
		byte inventoryCode = data[17];
		ushort itemID = NetDecoder.ReadUshort(data, 18);
		byte actionSlot = data[20];
		ushort actionID = NetDecoder.ReadUshort(data, 21);

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		ApplyPlace(id, pos, x, y, z, facing, blockCode, slot, newQuantity, inventoryCode, itemID, actionSlot, actionID);
	}

	// Places a block or object for a player, giving the item back if the placement is not allowed
	private void ApplyPlace(ulong id, ChunkPos pos, int x, int y, int z, int facing, ushort blockCode, byte slot, byte newQuantity, byte inventoryCode, ushort itemID, byte actionSlot, ushort actionID){
		CastCoord lastCoord = new CastCoord(pos, x, y, z);
		ChunkPos chunkPos = lastCoord.GetChunkPos();
		HashSet<ulong> players;
		ItemStack heldStack;
		Chunk chunk;
		bool allowed;

		// Chunk is not loaded in the server, so the item must be given back
		if(!this.cl.Contains(chunkPos)){
			SendPlacementDenied(id, itemID, inventoryCode, slot, actionSlot, actionID);
			return;
		}

		if(blockCode <= ushort.MaxValue/2)
			allowed = VoxelLoader.GetBlock(blockCode).PlacementRule(chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
		else
			allowed = VoxelLoader.GetObject(blockCode).PlacementRule(chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);

		if(!allowed){
			SendPlacementDenied(id, itemID, inventoryCode, slot, actionSlot, actionID);
			return;
		}

		// Check if is trying to put block on players
		if(this.playersInChunk.TryGetValue(pos, out players)){
			foreach(ulong code in players){
				if(code == id)
					continue;

				if(!this.cl.regionHandler.allPlayerData[code].CheckValidPlacement(lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ())){
					SendPlacementDenied(id, itemID, inventoryCode, slot, actionSlot, actionID);
					return;
				}
			}
		}

		chunk = this.cl.GetChunk(chunkPos);

		// Actually places block/asset into terrain
		chunk.data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);

		// If doesn't have special place handling
		if(!VoxelLoader.CheckCustomPlace(blockCode)){
			EmitBlockUpdate(BUDCode.CHANGE, lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ(), 0, cl);

			if(blockCode <= ushort.MaxValue/2)
				VoxelLoader.GetBlock(blockCode).OnPlace(chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
			else
				VoxelLoader.GetObject(blockCode).OnPlace(chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);

			// Sends the updated voxel to loaded clients
			SendBlockUpdate(BUDCode.PLACE, chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode, chunk.metadata.GetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ), chunk.metadata.GetHP(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ));
			this.cl.regionHandler.SaveChunk(chunk);
		}
		// If has special handling
		else{
			if(blockCode <= ushort.MaxValue/2)
				VoxelLoader.GetBlock(blockCode).OnPlace(chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
			else
				VoxelLoader.GetObject(blockCode).OnPlace(chunkPos, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, cl);
		}

		// The stack is captured before its quantity changes, so its Unhold event can run if it's used up
		heldStack = this.cl.playerServerInventory.GetSlot(id, inventoryCode, slot).GetItemStack();
		this.cl.playerServerInventory.ChangeQuantity(id, inventoryCode, slot, newQuantity);

		// If quantity becomes zero or less, runs OnUnhold
		if(newQuantity <= 0){
			if(heldStack != null)
				heldStack.GetItem().OnUnholdServer(this.cl, heldStack, id);

			NetMessage unholdMessage = new NetMessage(NetCode.SENDITEMINHAND);
			unholdMessage.SendItemInHand(GetPlayerNetID(id), true, 0, 0);
			this.SendToClientsExcept(id, unholdMessage);
		}

		// Make entities in this chunk update their TerrainVision
		this.entityHandler.SetRefreshVision(EntityType.DROP, chunkPos);
	}

	// Breaks the voxel in a position, running its break events
	private void ApplyBreak(ChunkPos pos, int x, int y, int z){
		Chunk chunk;
		ushort blockCode;
		CastCoord lastCoord;

		if(!this.cl.Contains(pos))
			return;

		chunk = this.cl.GetChunk(pos);
		blockCode = chunk.data.GetCell(x, y, z);
		lastCoord = new CastCoord(pos, x, y, z);

		// If doesn't has special break handling
		if(!VoxelLoader.CheckCustomBreak(blockCode)){
			// Actually breaks new block and updates chunk
			chunk.data.SetCell(x, y, z, 0);
			chunk.metadata.Reset(x, y, z);

			// Triggers OnBreak
			if(blockCode <= ushort.MaxValue/2)
				VoxelLoader.GetBlock(blockCode).OnBreak(pos, x, y, z, this.cl);
			else
				VoxelLoader.GetObject(blockCode).OnBreak(pos, x, y, z, this.cl);

			EmitBlockUpdate(BUDCode.BREAK, lastCoord.GetWorldX(), lastCoord.GetWorldY(), lastCoord.GetWorldZ(), 0, this.cl);
		}
		// If has special break handlings
		else{
			if(blockCode <= ushort.MaxValue/2)
				VoxelLoader.GetBlock(blockCode).OnBreak(pos, x, y, z, this.cl);
			else
				VoxelLoader.GetObject(blockCode).OnBreak(pos, x, y, z, this.cl);
		}

		// Sends the updated voxel to loaded clients
		this.entityHandler.SetRefreshVision(EntityType.DROP, pos);
		SendBlockUpdate(BUDCode.BREAK, pos, x, y, z, 0, ushort.MaxValue, ushort.MaxValue);
		this.cl.regionHandler.SaveChunk(chunk);
	}

	// Tells a client its placement was denied, echoing the origin of the placed item so it can be given back to the same slot
	private void SendPlacementDenied(ulong id, ushort itemID, byte inventoryCode, byte slot, byte actionSlot, ushort actionID){
		NetMessage denied = new NetMessage(NetCode.PLACEMENTDENIED);
		denied.PlacementDenied(itemID, inventoryCode, slot, actionSlot, actionID);
		this.Send(denied.GetMessage(), denied.size, id);
	}

	// Receives an Interaction command from client
	private void Interact(byte[] data, ulong id){
		// DEBUG
		this.cl.TestInventoryReceive(id);

		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		int facing = (sbyte)data[12];
		int callback;

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		CastCoord current = new CastCoord(pos, x, y, z);

		ushort blockCode = this.cl.GetBlock(current);

		if(blockCode <= ushort.MaxValue/2)
			callback = VoxelLoader.GetBlock(blockCode).OnInteract(pos, current.blockX, current.blockY, current.blockZ, this.cl);
		else
			callback = VoxelLoader.GetObject(blockCode).OnInteract(pos, current.blockX, current.blockY, current.blockZ, this.cl);

		// Actual handling of message
		CallbackHandler(callback, pos, current, facing);		
	}

	// Receives a BatchLoadBUD request from client, with the voxels of a chunk that have load events
	private void BatchLoadBUD(byte[] data, ulong id){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int count = NetDecoder.ReadUshort(data, 10);
		int x, y, z;
		ushort blockCode;
		CastCoord lastCoord;

		if(!this.cl.Contains(pos))
			return;

		for(int i=0; i < count; i++){
			NetDecoder.ReadLocalPos(data, 12 + i*2, out x, out y, out z);
			lastCoord = new CastCoord(pos, x, y, z);
			blockCode = this.cl.GetBlock(lastCoord);

			if(blockCode <= ushort.MaxValue/2)
				VoxelLoader.GetBlock(blockCode).OnLoad(lastCoord, this.cl);
			else
				VoxelLoader.GetObject(blockCode).OnLoad(lastCoord, this.cl);
		}

		this.SendAccumulatedChunks();
	}

	// Receives a block damage from client and processes block HP
	private void BlockDamage(byte[] data, ulong id){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		ushort blockDamage = NetDecoder.ReadUshort(data, 12);
		Chunk c;

		// Auxiliary
		int currentHP, actualDamage;
		ushort block;
		int decalBefore, decalAfter;
		NetMessage message;

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(!this.cl.Contains(pos))
			return;

		c = this.cl.GetChunk(pos);

		block = c.data.GetCell(x, y, z);
		currentHP = c.metadata.GetHP(x, y, z);

		if(currentHP == 0 || currentHP == ushort.MaxValue){
			if(block <= ushort.MaxValue/2)
				currentHP = BlockEncyclopediaECS.blockHP[block];
			else
				currentHP = BlockEncyclopediaECS.objectHP[ushort.MaxValue - block];
		}

		actualDamage = VoxelLoader.GetDamageReceived(block, blockDamage);

		if(actualDamage == 0)
			return;

		decalBefore = GetDecalCode(block, currentHP);
		currentHP -= actualDamage;
		decalAfter = GetDecalCode(block, currentHP);

		// If block has broken
		if(currentHP <= 0){
			ApplyBreak(pos, x, y, z);
		}
		// If damage wasn't significant enough for a decal change
		else{
			c.metadata.SetHP(x, y, z, (ushort)currentHP);

			message = new NetMessage(NetCode.BLOCKDAMAGE);
			message.BlockHP(pos, x, y, z, (ushort)currentHP, decalBefore != decalAfter);
			SendToClients(pos, message);
			this.cl.regionHandler.SaveChunk(c);
		}
	}

	/*
	Chunk synchronization
	*/

	// Sends the complete data of a single voxel to every client that has its chunk
	public void SendBlockUpdate(BUDCode type, ChunkPos pos, int x, int y, int z, ushort blockCode, ushort state, ushort hp){
		NetMessage message = new NetMessage(NetCode.BLOCKUPDATE);
		message.BlockUpdate(type, pos, x, y, z, blockCode, state, hp);
		SendToClients(pos, message);

		// Clients already have the current data of this voxel
		if(this.cl.Contains(pos)){
			ChunkChangeSet changes = this.cl.GetChunk(pos).changeSet;

			if(changes != null)
				changes.Forget(ChunkChangeSet.GetIndex(x, y, z));
		}
	}

	// Sends the voxels of a chunk changed since it was last synchronized to every client that has it
	// Falls back to the whole chunk if the changes are unknown or too many
	public void SendChunkChanges(ChunkPos pos){
		Chunk chunk;
		HashSet<ulong> clients;
		ChunkChangeSet changes;
		int x, y, z;

		if(!this.cl.Contains(pos))
			return;

		chunk = this.cl.GetChunk(pos);
		changes = chunk.changeSet;

		if(!this.cl.loadedChunks.TryGetValue(pos, out clients) || clients.Count == 0){
			if(changes != null)
				changes.Clear();
			return;
		}

		if(changes == null || changes.IsOverflowed() || changes.Count() == 0){
			SendWholeChunk(chunk, clients);
		}
		else{
			NetMessage message = new NetMessage(NetCode.BLOCKBATCH);
			message.BlockBatch(pos);

			foreach(int index in changes.GetChanges()){
				ChunkChangeSet.GetCoords(index, out x, out y, out z);
				message.AddBlockBatch(x, y, z, chunk.data.GetCell(x, y, z), chunk.metadata.GetState(x, y, z), chunk.metadata.GetHP(x, y, z));
			}

			foreach(ulong client in clients)
				this.Send(message.GetMessage(), message.size, client);
		}

		if(changes != null)
			changes.Clear();
	}

	// Sends the changes of a chunk at the end of the frame, merging every change made to it until then
	public void QueueChunkChanges(ChunkPos pos){
		this.queuedChunkChanges.Add(pos);
	}

	private void ProcessQueuedChunkChanges(){
		if(this.queuedChunkChanges.Count == 0)
			return;

		foreach(ChunkPos pos in this.queuedChunkChanges)
			SendChunkChanges(pos);

		this.queuedChunkChanges.Clear();
	}

	// Sends the whole chunk to every client that has it. Used when the chunk was changed in ways that can't be tracked
	public void SendWholeChunk(ChunkPos pos){
		HashSet<ulong> clients;

		if(!this.cl.Contains(pos))
			return;

		Chunk chunk = this.cl.GetChunk(pos);

		if(chunk.changeSet != null)
			chunk.changeSet.MarkAll();

		if(this.cl.loadedChunks.TryGetValue(pos, out clients) && clients.Count > 0)
			SendWholeChunk(chunk, clients);

		if(chunk.changeSet != null)
			chunk.changeSet.Clear();
	}

	private void SendWholeChunk(Chunk chunk, HashSet<ulong> clients){
		byte[] payload;
		int length;

		GetChunkPayload(chunk, out payload, out length);
		payload[0] = (byte)NetCode.SENDCHUNK;

		foreach(ulong client in clients)
			this.Send(payload, length, client);
	}

	// Gets the SENDCHUNK message of a chunk, compressing it only if it changed since it was last compressed
	private void GetChunkPayload(Chunk chunk, out byte[] payload, out int length){
		if(this.chunkPayloadCache.TryGet(chunk, out payload, out length))
			return;

		NetMessage message = new NetMessage(NetCode.SENDCHUNK);
		message.SendChunk(chunk);

		length = message.size;
		payload = new byte[length];
		Array.Copy(message.GetMessage(), 0, payload, 0, length);

		this.chunkPayloadCache.Put(chunk, payload, length);
	}

	// Keeps the compressed data read from disk as the chunk's SENDCHUNK message, so it's sent without being compressed again
	public void SeedChunkPayload(Chunk chunk, byte[] compressed, int blockSize, int hpSize, int stateSize){
		byte[] payload = NetMessage.BuildChunkPayload(chunk.pos, chunk.biome, compressed, blockSize, hpSize, stateSize);
		this.chunkPayloadCache.Put(chunk, payload, payload.Length);
	}

	// Register the current chunk to a set of chunks that should be sent at the end of the current operation
	// This way, only one chunk update message is sent per multiple block/tile-entity operations
	public void RegisterChunkToSend(ChunkPos pos){
		this.chunksToSend.Add(pos);
	}

	// Sends the changes of all chunks in the ChunksToSend set to clients
	private void SendAccumulatedChunks(){
		foreach(ChunkPos pos in this.chunksToSend)
			SendChunkChanges(pos);

		this.chunksToSend.Clear();	
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
		// 1: Saves chunk and sends the voxel to all connected clients
		else if(code == 1){
			ushort blockCode = this.cl.GetBlock(thisPos);
			ushort state = this.cl.GetState(thisPos);
			ushort hp = this.cl.GetHP(thisPos);

			this.cl.regionHandler.SaveChunk(this.cl.GetChunk(targetChunk));
			SendBlockUpdate(BUDCode.CHANGE, targetChunk, thisPos.blockX, thisPos.blockY, thisPos.blockZ, blockCode, state, hp);
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

	/*
	Entities
	*/

	// Receives player position. It's sent to the players that see it at the end of the frame
	private void ClientPlayerPosition(byte[] data, ulong id){
		float3 pos = NetDecoder.ReadFloat3(data, 1);
		float3 dir = new float3(NetDecoder.ReadAngle(data, 13), NetDecoder.ReadAngle(data, 15), 0f);
		uint timestamp = NetDecoder.ReadUint(data, 17);
		PlayerData pdat;
		ChunkPos cp;

		if(!this.cl.regionHandler.allPlayerData.TryGetValue(id, out pdat))
			return;

		this.playerPositionTimestamps[id] = timestamp;

		// Sets PlayerData
		pdat.SetPosition(pos.x, pos.y, pos.z);
		pdat.SetDirection(dir.x, dir.y, dir.z);

		cp = pdat.GetChunkPos();

		// Sets EntityHandler
		if(!this.entityHandler.Contains(EntityType.PLAYER, cp, id))
			this.entityHandler.AddPlayer(cp, id, pos, dir, cl);

		this.entityHandler.SetPosition(EntityType.PLAYER, id, cp, pos, dir);

		this.movedPlayers.Add(id);
	}

	// Gets the sender timestamp of the last position received from a player. Used for Snapshot Interpolation in Clients
	private uint GetPlayerPositionTimestamp(ulong id){
		uint timestamp;

		if(this.playerPositionTimestamps.TryGetValue(id, out timestamp))
			return timestamp;
		return 0;
	}

	// Sends the movement of every player and dropped item that moved this frame, in a single message per client
	private void SendEntityUpdates(){
		HashSet<ulong> recipients;
		PlayerData pdat;
		DroppedItemAI drop;
		uint netID;
		uint timestamp;

		if(this.movedPlayers.Count > 0){
			foreach(ulong code in this.movedPlayers){
				if(!this.connectionGraph.TryGetValue(code, out recipients) || recipients.Count == 0)
					continue;
				if(!this.cl.regionHandler.allPlayerData.TryGetValue(code, out pdat))
					continue;

				netID = GetPlayerNetID(code);
				timestamp = GetPlayerPositionTimestamp(code);

				foreach(ulong recipient in recipients)
					this.entityUpdates.AddPlayer(this, recipient, netID, pdat.posX, pdat.posY, pdat.posZ, pdat.dirX, pdat.dirY, timestamp);
			}

			this.movedPlayers.Clear();
		}

		if(this.movedDrops.Count > 0){
			foreach(KeyValuePair<DroppedItemAI, uint> item in this.movedDrops){
				drop = item.Key;

				// Its ENTITYDELETE was already broadcasted
				if(drop.markedForDelete)
					continue;
				if(!this.cl.loadedChunks.TryGetValue(drop.GetChunkPos(), out recipients))
					continue;

				foreach(ulong recipient in recipients)
					this.entityUpdates.AddDrop(this, recipient, (uint)drop.entityCode, drop.position, drop.IsStanding(), item.Value);
			}

			this.movedDrops.Clear();
		}

		this.entityUpdates.Flush(this);
	}

	// Receives a disconnect call from client or drops it
	private void Disconnect(ulong id, DisconnectType type){
		NetConnection connection;
		HashSet<ulong> viewers;
		HashSet<ulong> players;
		PlayerData pdat;

		if(!this.connections.TryGetValue(id, out connection))
			return;

		// Captures and removes all chunks
		this.cachedChunks.Clear();

		foreach(KeyValuePair<ChunkPos, HashSet<ulong>> item in this.cl.loadedChunks){
			if(item.Value.Contains(id))
				this.cachedChunks.Add(item.Key);
		}

		for(int i=0; i < this.cachedChunks.Count; i++){
			if(this.cl.UnloadChunk(this.cachedChunks[i], id))
				this.entityHandler.UnloadChunk(this.cachedChunks[i]);
		}

		connection.removed = true;
		connection.Close(flush:true);
		this.connections.Remove(id);

		// Deletes this player for everyone that sees it and forgets the players it sees
		NetMessage killMessage = new NetMessage(NetCode.ENTITYDELETE);
		killMessage.EntityDelete(EntityType.PLAYER, GetPlayerNetID(id));

		if(this.connectionGraph.TryGetValue(id, out viewers)){
			foreach(ulong viewer in viewers)
				this.Send(killMessage.GetMessage(), killMessage.size, viewer);
		}

		this.connectionGraph.Remove(id);

		foreach(HashSet<ulong> graph in this.connectionGraph.Values)
			graph.Remove(id);

		this.playerRenderDistances.Remove(id);
		this.playerAnimationStates.Remove(id);
		this.playerActiveLoopSFX.Remove(id);
		this.playerAttachmentAnchors.Remove(id);
		this.playerCarryStyles.Remove(id);
		this.playerPositionTimestamps.Remove(id);
		this.playerNetIDs.Remove(id);
		this.movedPlayers.Remove(id);
		this.entityUpdates.RemoveRecipient(id);

		pdat = this.cl.regionHandler.allPlayerData[id];

		this.entityHandler.Remove(EntityType.PLAYER, pdat.GetChunkPos(), id);
		this.entityHandler.RemoveSheet(id);

		this.cl.regionHandler.SavePlayers();
		pdat.SetOnline(false);

		if(this.playersInChunk.TryGetValue(pdat.GetChunkPos(), out players)){
			if(players.Count > 1)
				players.Remove(id);
			else
				this.playersInChunk.Remove(pdat.GetChunkPos());
		}

		// Stops streaming chunks to this client
		this.chunkStreamer.RemoveClient(id);

		// Remove player last known chunk
		this.playerToChunk.Remove(id);

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

	// Receives the client's current chunk and finds the connections it has with other players
	private void ClientChunk(byte[] data, ulong id){
		ChunkPos lastPos = NetDecoder.ReadChunkPos(data, 1);
		ChunkPos newPos = NetDecoder.ReadChunkPos(data, 10);
		HashSet<ulong> players;
		HashSet<ulong> viewers;
		HashSet<ulong> graph;
		bool sees;

		// Removes last ChunkPos if exists
		if(lastPos != newPos){
			if(this.playersInChunk.TryGetValue(lastPos, out players)){
				if(players.Count > 1)
					players.Remove(id);
				else
					this.playersInChunk.Remove(lastPos);

				if(this.entityHandler.Contains(EntityType.PLAYER, lastPos, id))
					this.entityHandler.Remove(EntityType.PLAYER, lastPos, id);
			}
		}

		// Add new ChunkPos
		if(!this.playersInChunk.TryGetValue(newPos, out players)){
			players = new HashSet<ulong>();
			this.playersInChunk.Add(newPos, players);
		}

		players.Add(id);
		this.playerToChunk[id] = newPos;

		if(!this.connectionGraph.TryGetValue(id, out graph))
			return;

		// Clients that have the new chunk see this player
		this.cl.loadedChunks.TryGetValue(newPos, out viewers);

		NetMessage killMessage = new NetMessage(NetCode.ENTITYDELETE);
		killMessage.EntityDelete(EntityType.PLAYER, GetPlayerNetID(id));

		foreach(ulong code in this.connections.Keys){
			if(code == id)
				continue;

			sees = viewers != null && viewers.Contains(code);

			// Check if code should still be connected
			if(graph.Contains(code)){
				if(!sees){
					graph.Remove(code);
					this.Send(killMessage.GetMessage(), killMessage.size, code);
				}
			}
			// Check if code should be connected
			else if(sees){
				ShowPlayerTo(id, code);
			}
		}
	}

	// Receives a Drop Item notification and creates the DropItemAI Entity
	private void DropItem(byte[] data, ulong id){
		float3 pos = NetDecoder.ReadFloat3(data, 1);
		float3 move = NetDecoder.ReadFloat3(data, 13);
		ushort itemCode = NetDecoder.ReadUshort(data, 25);
		byte amount = data[27];
		byte slot = data[28];
		NetMessage unholdMessage;

		// The new entity is spawned for the clients by the EntityHandler
		this.entityHandler.AddItem(pos, new float3(0,0,0), move, itemCode, amount, id, this.cl);

		if(slot != byte.MaxValue){
			// If quantity becomes zero or less, runs OnUnhold
			if(this.cl.playerServerInventory.GetQuantity(id, slot) - amount <= 0){
				ItemStack its = new ItemStack(itemCode, amount);
				ItemLoader.GetItem(itemCode).OnUnholdServer(this.cl, its , id);

				unholdMessage = new NetMessage(NetCode.SENDITEMINHAND);
				unholdMessage.SendItemInHand(GetPlayerNetID(id), true, 0, 0);
				this.SendToClientsExcept(id, unholdMessage);
			}
		}
	}

	/*
	Characters and Inventory
	*/

	// Receives the inventory of client and saves it
	private void SendInventory(byte[] data, int length, ulong id){
		byte selectedHotbarSlot, selectedAttackSlot;
		Item prevItem, currItem;
		ClickableSlot previousSlot, currentSlot;
		CharacterSheet sheet;
		NetMessage message;
		bool isNormalHotbar;

		if(!this.entityHandler.ContainsSheet(id))
			return;

		sheet = this.entityHandler.GetSheet(id);
		isNormalHotbar = sheet.IsInNormalHotbar();
		selectedHotbarSlot = sheet.GetHotbarSlot();
		selectedAttackSlot = sheet.GetAttackHotbarSlot();

		if(isNormalHotbar)
			previousSlot = this.cl.playerServerInventory.GetSlot(id, selectedHotbarSlot).GetItemStack();
		else
			previousSlot = this.cl.playerServerInventory.GetSlot(id, selectedAttackSlot).GetAction();

		this.cl.playerServerInventory.AddInventory(id, data, length);

		if(isNormalHotbar)
			currentSlot = this.cl.playerServerInventory.GetSlot(id, selectedHotbarSlot).GetItemStack();
		else
			currentSlot = this.cl.playerServerInventory.GetSlot(id, selectedAttackSlot).GetAction();

		if(previousSlot.GetID() == currentSlot.GetID())
			return;

		message = new NetMessage(NetCode.SENDITEMINHAND);

		if(isNormalHotbar){
			prevItem = ((ItemStack)previousSlot).GetItem();
			currItem = ((ItemStack)currentSlot).GetItem();
			prevItem.OnUnholdServer(this.cl, (ItemStack)previousSlot, id);
			currItem.OnHoldServer(this.cl, (ItemStack)currentSlot, id);
			message.SendItemInHand(GetPlayerNetID(id), true, currentSlot.GetID(), ((ItemStack)currentSlot).GetAmount());
		}
		else{
			((EntityAction)previousSlot).OnUnholdServer(this.cl, ((EntityAction)previousSlot).GetItemStack(null), id);
			((EntityAction)currentSlot).OnHoldServer(this.cl, ((EntityAction)currentSlot).GetItemStack(null), id);
			EntityAction ea = (EntityAction)currentSlot;
			message.SendItemInHand(GetPlayerNetID(id), false, currentSlot.GetID(), (ushort)this.cl.playerServerInventory.GetSlot(id, ea.GetConnectedStackInventory(), ea.GetConnectedStackSlot()).GetItemID(), 0);
		}

		this.SendToClientsExcept(id, message);
	}

	// Receives a request to check character existence, if exists, sends CharacterAppearance
	private void RequestCharacterExistence(byte[] data, ulong id){
		ulong charID = NetDecoder.ReadUlong(data, 1);
		NetMessage message = new NetMessage(NetCode.SENDCHARACTERPRELOAD);
		CharacterSheet sheet = this.entityHandler.FindSheet(charID);

		if(sheet == null)
			message.SendCharacterPreload(null, false);
		else
			message.SendCharacterPreload(sheet.GetCharacterAppearance(), sheet.GetGender());

		this.Send(message.GetMessage(), message.size, id, temporary:true);
	}

	// Receives the request for sending a CharacterSheet to user
	private void RequestCharacterSheet(byte[] data, ulong id){
		ulong code = NetDecoder.ReadUlong(data, 1);
		CharacterSheet sheet;

		if(code == id)
			sheet = this.entityHandler.GetSheet(code);
		else
			sheet = this.entityHandler.FindSheet(code);

		if(sheet == null)
			return;

		NetMessage message = new NetMessage(NetCode.SENDCHARSHEET);
		message.SendCharSheet(code, sheet);
		this.Send(message.GetMessage(), message.size, id);

		// The owner's sheet only carries the style, so the equipped items that skin its attachments are sent after it
		if(code == id)
			SendBattleStyleSnapshot(code, id);
	}

	// Receives the character sheet from a new character (InfoClient) or from its owner
	private void SendCharSheet(byte[] data, ulong id, bool isTemporary){
		ulong charID = NetDecoder.ReadUlong(data, 1);
		CharacterSheet sheet = NetDecoder.ReadCharacterSheetNet(data, 9, compactAppearance:false);

		// Players can only change their own sheet
		if(!isTemporary && charID != id)
			return;

		if(this.entityHandler.ContainsSheet(charID))
			this.entityHandler.ReplaceSheet(charID, sheet);
		else
			this.cl.characterFileHandler.SaveCharacterSheet(charID, sheet);
	}

	// Receives the current hotbar slot in player's hand
	private void SendHotbarPosition(byte[] data, ulong id){
		byte slot = NetDecoder.ReadByte(data, 1);
		byte attackSlot = NetDecoder.ReadByte(data, 2);
		bool isNormalHotbar = NetDecoder.ReadBool(data, 3);
		byte previousSlot;

		CharacterSheet sheet;
		PlayerServerInventorySlot psiSlot, psiPrevious;
		ItemStack hotbarStack = null;
		ItemStack previousStack = null;
		EntityAction hotbarAction = null;
		EntityAction previousAction = null;
		NetMessage message;

		byte inventory, invSlot;

		if(!this.entityHandler.ContainsSheet(id))
			return;

		sheet = this.entityHandler.GetSheet(id);
		
		// Check if the previous selected slot was in Normal Hotbar
		if(sheet.IsInNormalHotbar()){
			previousSlot = sheet.GetHotbarSlot();

			if(previousSlot != slot || !isNormalHotbar){
				psiPrevious = this.cl.playerServerInventory.GetSlot(id, previousSlot);
				previousStack = psiPrevious.GetItemStack();
				previousStack.GetItem().OnUnholdServer(this.cl, previousStack, id);
			}
		}
		// Check if the previous selected slot was in Action Hotbar
		else{
			previousSlot = sheet.GetAttackHotbarSlot();

			if(previousSlot != attackSlot || isNormalHotbar){
				psiPrevious = this.cl.playerServerInventory.GetSlot(id, previousSlot);

				if(psiPrevious is ActionInventorySlot){
					previousAction = ((ActionInventorySlot)psiPrevious).GetAction();

					// If this action can't exist without an ItemStack
					if(((ActionInventorySlot)psiPrevious).IsConnectedToItem()){
						inventory = ((ActionInventorySlot)psiPrevious).GetStackInventory();
						invSlot = ((ActionInventorySlot)psiPrevious).GetStackSlot();
						previousStack = this.cl.playerServerInventory.GetSlot(id, inventory, invSlot).GetItemStack();
					}

					previousAction.OnUnholdServer(this.cl, previousStack, id);
				}
			}
		}

		sheet.SetIsInNormalHotbar(isNormalHotbar);
		this.entityHandler.MarkSheetDirty(id);

		// Check if the current selected slot is in Normal Hotbar
		if(isNormalHotbar){
			sheet.SetHotbarSlot(slot);

			psiSlot = this.cl.playerServerInventory.GetSlot(id, slot);
			hotbarStack = psiSlot.GetItemStack();

			hotbarStack.GetItem().OnHoldServer(this.cl, hotbarStack, id);

			message = new NetMessage(NetCode.SENDITEMINHAND);
			message.SendItemInHand(GetPlayerNetID(id), true, hotbarStack.GetID(), hotbarStack.GetAmount());
			this.SendToClientsExcept(id, message);
		}
		// Check if the current selected slot is in Action Hotbar
		else{
			sheet.SetAttackHotbarSlot(attackSlot);
			psiSlot = this.cl.playerServerInventory.GetSlot(id, attackSlot);

			if(psiSlot is ActionInventorySlot){
				hotbarAction = ((ActionInventorySlot)psiSlot).GetAction();

				// If this action can't exist without an ItemStack
				if(((ActionInventorySlot)psiSlot).IsConnectedToItem()){
					inventory = ((ActionInventorySlot)psiSlot).GetStackInventory();
					invSlot = ((ActionInventorySlot)psiSlot).GetStackSlot();
					hotbarStack = this.cl.playerServerInventory.GetSlot(id, inventory, invSlot).GetItemStack();
				}
				
				hotbarAction.OnHoldServer(this.cl, hotbarStack, id);

				message = new NetMessage(NetCode.SENDITEMINHAND);
				message.SendItemInHand(GetPlayerNetID(id), false, hotbarAction.GetID(), (ushort)this.cl.playerServerInventory.GetSlot(id, hotbarAction.GetConnectedStackInventory(), hotbarAction.GetConnectedStackSlot()).GetItemID(), 0);
				this.SendToClientsExcept(id, message);
			}
		}
	}

	/*
	Animation, Equipment and Entity SFX of players
		Clients only send them about their own player. The Server keeps the latest state to show it to clients that start seeing the player
	*/

	// Receives an AnimationLayer from a player
	private void SendAnimationLayer(byte[] data, ulong id){
		AnimationData animationData = new AnimationData(NetDecoder.ReadInt(data, 2), data[1]);
		Dictionary<int, AnimationData> states;

		if(!this.playerAnimationStates.TryGetValue(id, out states)){
			states = new Dictionary<int, AnimationData>();
			this.playerAnimationStates.Add(id, states);
		}

		states[animationData.layer] = animationData;

		NetMessage message = new NetMessage(NetCode.SENDANIMATIONLAYER);
		message.SendAnimationLayer(GetPlayerNetID(id), animationData);
		this.SendToClientsExcept(id, message);
	}

	// Changes the BattleStyle of a player and informs every client that sees it, including itself
	public void SendBattleStyle(ulong playerCode, int style){
		CharacterSheet sheet;

		ClearAttachmentAnchorsOnStyleChange(playerCode, style);
		this.entityHandler.ChangeBattleStyle(playerCode, style);
		sheet = this.entityHandler.GetSheet(playerCode);
		sheet.SetBattleStyleCode(style);
		this.entityHandler.MarkSheetDirty(playerCode);

		NetMessage message = new NetMessage(NetCode.SENDBATTLESTYLE);
		message.SendBattleStyle(GetPlayerNetID(playerCode), style, BattleStyleDeterminator.ResolveAttachmentItems(this.cl, playerCode));
		this.SendToClients(playerCode, message);
	}

	// Receives an animator value from client
	private void SendAnimatorParameter(byte[] data, ulong id){
		int parameterHash = NetDecoder.ReadInt(data, 1);
		float val = NetDecoder.ReadFloat(data, 5);

		this.entityHandler.AddParameterValue(id, parameterHash, val);

		NetMessage message = new NetMessage(NetCode.SENDANIMATORPARAMETER);
		message.SendAnimatorParameter(GetPlayerNetID(id), parameterHash, val);
		this.SendToClientsExcept(id, message);
	}

	// Receives an user changing equipments notification from client
	private void EquipItem(byte[] data, ulong id){
		int oldSize, newSize;
		Item oldItem = NetDecoder.ReadItemDeep(data, 1, out oldSize);
		Item newItem = NetDecoder.ReadItemDeep(data, 1+oldSize, out newSize);
		CharacterSheet cs = this.entityHandler.GetSheet(id);

		// Handle onEquip and onUnequip events on Items
		oldItem.OnUnequipServer(this.cl, oldItem, id);
		newItem.OnEquipServer(this.cl, newItem, id);

		// Handles Style change
		SendBattleStyle(id, BattleStyleDeterminator.Resolve(this.cl, id));

		// Handles OnEquipChange events on Actions
		if(!cs.IsInNormalHotbar()){
			PlayerServerInventorySlot psiSlot = this.cl.playerServerInventory.GetSlot(id, 0, cs.GetAttackHotbarSlot());

			if(psiSlot is ActionInventorySlot){
				EntityAction ea = ((ActionInventorySlot)psiSlot).GetAction();

				if(ea != null)
					ea.OnEquipChangeServer(this.cl, id);
			}
		}

		// Handle the sending of onEquips and onUnequips to clients
		NetMessage equipMessage = new NetMessage(NetCode.EQUIPITEM);
		equipMessage.EquipItem(GetPlayerNetID(id), oldItem, newItem);
		SendToClientsExcept(id, equipMessage);
	}

	// Receives an Entity SFX creation/destruction from the client that owns the entity and relays it to nearby clients
	private void EntitySFX(byte[] data, ulong id){
		EntitySFXOperation operation = (EntitySFXOperation)data[1];
		EntitySFXData sfx = NetDecoder.ReadEntitySFXData(data, 2);
		Dictionary<string, EntitySFXData> loops;

		if(operation == EntitySFXOperation.CREATE){
			if(sfx.isLoop){
				if(!this.playerActiveLoopSFX.TryGetValue(id, out loops)){
					loops = new Dictionary<string, EntitySFXData>();
					this.playerActiveLoopSFX.Add(id, loops);
				}

				loops[sfx.name] = sfx;
			}
		}
		else if(operation == EntitySFXOperation.DESTROY){
			if(this.playerActiveLoopSFX.TryGetValue(id, out loops)){
				loops.Remove(sfx.name);

				if(loops.Count == 0)
					this.playerActiveLoopSFX.Remove(id);
			}
		}
		else{
			return;
		}

		NetMessage message = new NetMessage(NetCode.ENTITYSFX);
		message.EntitySFX(GetPlayerNetID(id), operation, sfx);
		this.SendToClientsExcept(id, message);
	}

	// Receives a BattleStyle attachment anchor switch from the client that owns the entity and relays it to nearby clients
	private void SendAttachmentAnchor(byte[] data, ulong id){
		BoneAnchorType origin = (BoneAnchorType)data[1];
		BoneAnchorType target = (BoneAnchorType)data[2];
		bool flip = NetDecoder.ReadBool(data, 3);
		float heightOffset = NetDecoder.ReadFloat(data, 4);
		Dictionary<BoneAnchorType, AttachmentAnchorState> anchors;

		if(!this.playerAttachmentAnchors.TryGetValue(id, out anchors)){
			anchors = new Dictionary<BoneAnchorType, AttachmentAnchorState>();
			this.playerAttachmentAnchors.Add(id, anchors);
		}

		bool applied = AttachmentAnchorState.Apply(anchors, origin, target, flip, heightOffset);

		if(anchors.Count == 0)
			this.playerAttachmentAnchors.Remove(id);

		if(!applied)
			return;

		NetMessage message = new NetMessage(NetCode.SENDATTACHMENTANCHOR);
		message.SendAttachmentAnchor(GetPlayerNetID(id), origin, target, flip, heightOffset);
		this.SendToClientsExcept(id, message);
	}

	/*
	Auxiliary Functions
	*/

	// Sends what a player is holding to a recipient that just started seeing it
	private void SendItemInHandSnapshot(ulong playerCode, ulong recipientCode){
		CharacterSheet cs = this.entityHandler.GetSheet(playerCode);
		ItemStack selectionIts;
		EntityAction selectionEa;
		PlayerServerInventorySlot psiSlot;
		ActionInventorySlot actionSlot;
		ushort connectedItem;
		bool trash;

		if(cs == null)
			return;

		if(!this.cl.playerServerInventory.HasInventory(playerCode))
			this.cl.playerServerInventory.LoadInventoryIntoBuffer(playerCode, out trash);

		NetMessage itemMessage = new NetMessage(NetCode.SENDITEMINHAND);

		// Sheet hotbar slots are flat indexes into the player's inventory list
		if(cs.IsInNormalHotbar()){
			selectionIts = this.cl.playerServerInventory.GetSlot(playerCode, cs.GetHotbarSlot()).GetItemStack();
			itemMessage.SendItemInHand(GetPlayerNetID(playerCode), true, selectionIts.GetID(), selectionIts.GetAmount());
		}
		else{
			psiSlot = this.cl.playerServerInventory.GetSlot(playerCode, cs.GetAttackHotbarSlot());

			if(!(psiSlot is ActionInventorySlot))
				return;

			actionSlot = (ActionInventorySlot)psiSlot;
			selectionEa = actionSlot.GetAction();
			connectedItem = 0;

			if(actionSlot.IsConnectedToItem())
				connectedItem = (ushort)this.cl.playerServerInventory.GetSlot(playerCode, actionSlot.GetStackInventory(), actionSlot.GetStackSlot()).GetItemID();

			itemMessage.SendItemInHand(GetPlayerNetID(playerCode), false, selectionEa.GetID(), connectedItem, 0);
		}

		this.Send(itemMessage.GetMessage(), itemMessage.size, recipientCode);
	}

	private void SendAnimationSnapshot(ulong playerCode, ulong recipientCode){
		Dictionary<int, AnimationData> states;

		if(!this.playerAnimationStates.TryGetValue(playerCode, out states))
			return;

		NetMessage message = new NetMessage(NetCode.SENDANIMATIONLAYER);

		foreach(AnimationData animationData in states.Values){
			message.SendAnimationLayer(GetPlayerNetID(playerCode), animationData);
			this.Send(message.GetMessage(), message.size, recipientCode);
		}
	}

	// Sends the last value of every animator parameter of a player to a recipient that just started seeing them
	private void SendAnimatorParameterSnapshot(ulong playerCode, ulong recipientCode){
		Dictionary<int, float> parameters = this.entityHandler.GetParameterValues(playerCode);

		if(parameters == null)
			return;

		NetMessage message = new NetMessage(NetCode.SENDANIMATORPARAMETER);

		foreach(KeyValuePair<int, float> parameter in parameters){
			message.SendAnimatorParameter(GetPlayerNetID(playerCode), parameter.Key, parameter.Value);
			this.Send(message.GetMessage(), message.size, recipientCode);
		}
	}

	// Sends all active looping SFX of a player to a recipient that just started seeing them
	private void SendEntitySFXSnapshot(ulong playerCode, ulong recipientCode){
		Dictionary<string, EntitySFXData> loops;

		if(!this.playerActiveLoopSFX.TryGetValue(playerCode, out loops))
			return;

		NetMessage message = new NetMessage(NetCode.ENTITYSFX);

		foreach(EntitySFXData sfx in loops.Values){
			message.EntitySFX(GetPlayerNetID(playerCode), EntitySFXOperation.CREATE, sfx);
			this.Send(message.GetMessage(), message.size, recipientCode);
		}
	}

	// Sends the current BattleStyle and attachment items of a player to a recipient that just started seeing them
	private void SendBattleStyleSnapshot(ulong playerCode, ulong recipientCode){
		if(!this.entityHandler.ContainsSheet(playerCode))
			return;

		NetMessage message = new NetMessage(NetCode.SENDBATTLESTYLE);
		message.SendBattleStyle(GetPlayerNetID(playerCode), this.entityHandler.GetSheet(playerCode).GetBattleStyleCode(), BattleStyleDeterminator.ResolveAttachmentItems(this.cl, playerCode));
		this.Send(message.GetMessage(), message.size, recipientCode);
	}

	// Sends all BattleStyle attachments of a player that are away from their default anchor to a recipient that just started seeing them
	private void SendAttachmentAnchorSnapshot(ulong playerCode, ulong recipientCode){
		Dictionary<BoneAnchorType, AttachmentAnchorState> anchors;

		if(!this.playerAttachmentAnchors.TryGetValue(playerCode, out anchors))
			return;

		NetMessage message = new NetMessage(NetCode.SENDATTACHMENTANCHOR);

		foreach(KeyValuePair<BoneAnchorType, AttachmentAnchorState> item in anchors){
			message.SendAttachmentAnchor(GetPlayerNetID(playerCode), item.Key, item.Value.anchor, item.Value.flip, item.Value.heightOffset);
			this.Send(message.GetMessage(), message.size, recipientCode);
		}
	}

	// Attachments are rebuilt in their default anchors whenever a BattleStyle or the carried off-hand style changes
	private void ClearAttachmentAnchorsOnStyleChange(ulong playerCode, int style){
		int carryStyle = BattleStyleDeterminator.GetCarryStyleCode(style, BattleStyleDeterminator.ResolveAttachmentItems(this.cl, playerCode));
		int previousCarryStyle;

		if(!this.playerCarryStyles.TryGetValue(playerCode, out previousCarryStyle))
			previousCarryStyle = BattleStyleDeterminator.NO_CARRY;

		if(this.entityHandler.GetSheet(playerCode).GetBattleStyleCode() != style || previousCarryStyle != carryStyle)
			this.playerAttachmentAnchors.Remove(playerCode);

		this.playerCarryStyles[playerCode] = carryStyle;
	}

	// Send input message to all Clients connected to a given Chunk
	public void SendToClients(ChunkPos pos, NetMessage message){
		HashSet<ulong> clients;

		if(!this.cl.loadedChunks.TryGetValue(pos, out clients))
			return;

		foreach(ulong i in clients)
			this.Send(message.GetMessage(), message.size, i);
	}

	// Send input message to all Clients connected to a given Chunk that are not connected to the excluded Chunk
	public void SendToClientsExcept(ChunkPos pos, ChunkPos excluded, NetMessage message){
		HashSet<ulong> clients;
		HashSet<ulong> excludedClients;

		if(!this.cl.loadedChunks.TryGetValue(pos, out clients))
			return;

		bool hasExcluded = this.cl.loadedChunks.TryGetValue(excluded, out excludedClients);

		foreach(ulong i in clients){
			if(hasExcluded && excludedClients.Contains(i))
				continue;

			this.Send(message.GetMessage(), message.size, i);
		}
	}

	// Send input message to all Clients connected to the chunk of a player
	public void SendToClients(ulong playerCode, NetMessage message){
		ChunkPos pos;

		if(!this.playerToChunk.TryGetValue(playerCode, out pos))
			return;

		SendToClients(pos, message);
	}

	// Send input message to all Clients connected to the chunk of a player, except the player itself
	public void SendToClientsExcept(ulong playerCode, NetMessage message){
		ChunkPos pos;
		HashSet<ulong> clients;

		if(!this.playerToChunk.TryGetValue(playerCode, out pos))
			return;
		if(!this.cl.loadedChunks.TryGetValue(pos, out clients))
			return;

		foreach(ulong i in clients){
			if(i == playerCode)
				continue;

			this.Send(message.GetMessage(), message.size, i);
		}
	}
}

public enum DisconnectType{
	QUIT, // Voluntarily quit
	LOSTCONNECTION, // Exceptions
	TIMEDOUT, // CheckTimeout() has detected a timeout
	LOGINOVERWRITE // Logged into an already logged in session
}
