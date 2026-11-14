using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Debug = UnityEngine.Debug;

using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;
using Unity.Mathematics;

public class Client
{
	// Internet Objects
	private Socket socket;
	private volatile NetConnection connection;
	private volatile bool connectFailed = false;
	private bool panicked = false;
	private bool closed = false;

	// Entity Handler
	public EntityHandler entityHandler;
	public SmoothMovement smoothMovement;

	// Filled by the socket threads and consumed by the main thread
	public ConcurrentQueue<NetMessage> queue = new ConcurrentQueue<NetMessage>();

	// Remote players are identified by session NetIDs given by the Server. Dropped items use their entity code
	private Dictionary<uint, ulong> playerNetIDs = new Dictionary<uint, ulong>();
	private uint ownNetID = 0;
	private bool hasOwnNetID = false;

	// Address Information
	public IPAddress ip;
	public int port = 33000;

	// Unity References
	public ChunkLoader cl;
	public PlayerRaycast raycast;
	public PlayerHotbarHandler hotbarHandler;
	public PlayerModelHandler playerModelHandler;
	public PlayerInventoryManager playerInventoryManager;

	// Cache
	private bool[] cachedColumns = new bool[Chunk.chunkWidth*Chunk.chunkWidth];


	public Client(ChunkLoader cl){
		this.cl = cl;

		this.entityHandler = new EntityHandler(cl);
		this.smoothMovement = new SmoothMovement(this.entityHandler);

		this.ip = World.connectionIP;

		this.Connect();
	}

	// Triggers hazard protection and sends user back to menu screen
	public void Panic(){
		if(this.panicked)
			return;

		this.panicked = true;
		Debug.Log("Panic");
		SceneManager.LoadScene("Blank");
	}

	public void SetRaycast(PlayerRaycast raycast){
		this.raycast = raycast;
		this.raycast.SetFOV();
	}

	public void SetPlayerHotbarHandler(PlayerHotbarHandler handler){
		this.hotbarHandler = handler;
	}

	public void SetPlayerModelHandler(PlayerModelHandler handler){
		this.playerModelHandler = handler;
	}

	// Connects without blocking the main thread. The Server sends ACCEPTEDCONNECT once it's done
	public void Connect(){
		try{
			this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			this.socket.BeginConnect(this.ip, this.port, new AsyncCallback(ConnectCallback), null);
			Debug.Log("Connecting");
		}
		catch(Exception e){
			Debug.Log("Failed to connect: " + e.Message);
			this.connectFailed = true;
		}
	}

	private void ConnectCallback(IAsyncResult result){
		try{
			this.socket.EndConnect(result);

			NetConnection newConnection = new NetConnection(this.socket, 0, false, this.queue);
			this.connection = newConnection;
			newConnection.Start();
		}
		catch(Exception e){
			Debug.Log("Failed to connect: " + e.Message);
			this.connectFailed = true;
		}
	}

	public bool HasConnectionFailed(){return this.connectFailed;}

	// Queues a message to be sent in the next Flush()
	public bool Send(NetMessage message){
		NetConnection conn = this.connection;

		if(conn == null)
			return false;

		NetMessage.Broadcast(NetBroadcast.SENT, message.GetMessage()[0], Configurations.accountID, message.size);

		return conn.Send(message.GetMessage(), message.size);
	}

	// Writes every message queued this frame to the socket
	public void Flush(){
		NetConnection conn = this.connection;

		if(conn != null)
			conn.Flush();
	}

	// Disconnects on timeout and keeps the connection alive when nothing else was sent. Returns false if the Client was disconnected
	public bool CheckConnection(){
		NetConnection conn = this.connection;

		if(conn == null)
			return true;

		if(conn.SecondsSinceReceive() > Constants.NET_CLIENT_TIMEOUT_SECONDS){
			Debug.Log("Timeout");
			Disconnect();
			return false;
		}

		if(conn.SecondsSinceSend() >= Constants.NET_HEARTBEAT_INTERVAL_SECONDS){
			NetMessage message = new NetMessage(NetCode.HEARTBEAT);
			Send(message);
		}

		return true;
	}

	// Closes the connection, optionally telling the Server first
	public void Shutdown(bool notifyServer){
		if(this.closed)
			return;

		this.closed = true;
		NetConnection conn = this.connection;

		if(conn == null){
			try{
				this.socket.Close();
			}
			catch(Exception){}

			return;
		}

		if(notifyServer){
			NetMessage message = new NetMessage(NetCode.DISCONNECT);
			Send(message);
		}

		conn.Close(flush:notifyServer);
	}

	/* 
	=========================================================================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from Server
	public void HandleReceivedMessage(NetMessage message){
		if(message.IsCloseEvent()){
			Debug.Log("Lost connection to Server: " + message.GetConnection().GetCloseReason());
			Disconnect();
			return;
		}

		byte[] data = message.GetData();
		int length = message.GetSize();
		bool retained = false;

		NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], Configurations.accountID, length);

		switch((NetCode)data[0]){
			case NetCode.ACCEPTEDCONNECT:
				AcceptConnect();
				break;
			case NetCode.HEARTBEAT:
				break;
			case NetCode.SENDSERVERINFO:
				SendServerInfo(data);
				break;
			case NetCode.SENDCHUNK:
			case NetCode.STREAMCHUNK:
				// The ChunkLoader gives the data back to the pool once it's loaded
				SendChunk(data);
				retained = true;
				break;
			case NetCode.DISCONNECT:
				Disconnect();
				break;
			case NetCode.BLOCKUPDATE:
				BlockUpdate(data);
				break;
			case NetCode.BLOCKBATCH:
				BlockBatch(data);
				break;
			case NetCode.VFXDATA:
				VFXData(data);
				break;
			case NetCode.VFXCHANGE:
				VFXChange(data);
				break;
			case NetCode.VFXBREAK:
				VFXBreak(data);
				break;
			case NetCode.SENDGAMETIME:
				SendGameTime(data);
				break;
			case NetCode.PLAYERSPAWN:
				PlayerSpawn(data);
				break;
			case NetCode.DROPSPAWN:
				DropSpawn(data);
				break;
			case NetCode.ENTITYUPDATES:
				EntityUpdates(data);
				break;
			case NetCode.ENTITYDELETE:
				EntityDelete(data);
				break;
			case NetCode.ENTITYDELETEBATCH:
				EntityDeleteBatch(data);
				break;
			case NetCode.SENDCHARSHEET:
				SendCharSheet(data);
				break;
			case NetCode.PLACEMENTDENIED:
				PlacementDenied(data);
				break;
			case NetCode.BLOCKDAMAGE:
				BlockDamage(data);
				break;
			case NetCode.SENDINVENTORY:
				SendInventory(data, length);
				break;
			case NetCode.SFXPLAY:
				SFXPlay(data);
				break;
			case NetCode.SENDNOISE:
				SendNoise(data, length);
				break;
			case NetCode.SENDITEMINHAND:
				SendItemInHand(data);
				break;
			case NetCode.SENDANIMATIONLAYER:
				SendAnimationLayer(data);
				break;
			case NetCode.SENDBATTLESTYLE:
				SendBattleStyle(data);
				break;
			case NetCode.SENDANIMATORPARAMETER:
				SendAnimatorParameter(data);
				break;
			case NetCode.EQUIPITEM:
				EquipItem(data);
				break;
			case NetCode.ENTITYSFX:
				EntitySFX(data);
				break;
			case NetCode.SENDATTACHMENTANCHOR:
				SendAttachmentAnchor(data);
				break;
			case NetCode.ENTITYVFX:
				EntityVFX(data);
				break;
			case NetCode.ENTITYVOXELLIGHT:
				EntityVoxelLight(data);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED: " + (NetCode)data[0]);
				break;
		}

		if(!retained)
			NetBufferPool.Return(data);
	}

	// Permits the loading of Game Scene
	private void AcceptConnect(){
		this.cl.CONNECTEDTOSERVER = true;
		Debug.Log("Connected to Server at " + this.connection.GetRemoteAddress());
	}

	// Receives Player Information saved on server on startup
	private void SendServerInfo(byte[] data){
		float3 pos = NetDecoder.ReadFloat3(data, 1);
		float3 dir = NetDecoder.ReadFloat3(data, 13);
		uint day = NetDecoder.ReadUint(data, 25);
		byte hour = data[29];
		byte minute = data[30];

		this.ownNetID = NetDecoder.ReadUint(data, 31);
		this.hasOwnNetID = true;

		this.cl.time.SetTime(day, hour, minute);

		FloatingOrigin.SetInitialOrigin(pos.x, pos.z);

		this.cl.playerX = pos.x;
		this.cl.playerY = pos.y;
		this.cl.playerZ = pos.z;
		this.cl.playerDirX = dir.x;
		this.cl.playerDirY = dir.y;
		this.cl.playerDirZ = dir.z;
		this.cl.PLAYERSPAWNED = true;

		NetMessage message = new NetMessage(NetCode.REQUESTCHARACTERSHEET);
		message.RequestCharacterSheet(Configurations.accountID);
		this.Send(message);
	}

	// Receives a Chunk
	// Streamed chunks also count towards the streaming window and must be acknowledged once consumed
	private void SendChunk(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);

		this.cl.EnqueueChunkData(pos, data, (NetCode)data[0] == NetCode.STREAMCHUNK);
	}

	// Receives a disconnect call from server or loses the connection
	private void Disconnect(){
		if(this.cl == null || this.cl.client == null)
			return;

		Shutdown(notifyServer:false);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		this.cl.Cleanup(comesFromClient:true);
		SceneManager.LoadScene("Blank");
	}

	// Receives the new data of a single voxel
	private void BlockUpdate(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		BUDCode type = (BUDCode)data[12];
		ushort blockCode = NetDecoder.ReadUshort(data, 13);
		ushort state = NetDecoder.ReadUshort(data, 15);
		ushort hp = NetDecoder.ReadUshort(data, 17);
		Chunk c;

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(!this.cl.Contains(pos))
			return;

		c = this.cl.Get(pos);

		switch(type){
			case BUDCode.PLACE:
				c.data.SetCell(x, y, z, blockCode);
				c.metadata.SetState(x, y, z, state);
				c.metadata.SetHP(x, y, z, hp);
				c.data.CalculateHeightMap(x, z);
				this.cl.AddToUpdate(pos);
				ReloadNeighbors(pos, GetReloadMask(pos, x, y, z));
				break;
			case BUDCode.BREAK:
				c.data.SetCell(x, y, z, 0);
				c.metadata.Reset(x,y,z);
				c.data.CalculateHeightMap(x, z);
				this.cl.AddToUpdate(pos);
				ReloadNeighbors(pos, GetReloadMask(pos, x, y, z));
				break;
			case BUDCode.CHANGE:
				c.data.SetCell(x, y, z, blockCode);
				c.metadata.SetState(x, y, z, state);
				c.metadata.SetHP(x, y, z, hp);
				this.cl.AddToUpdate(pos);
				break;
			default:
				break;
		}
	}

	// Receives the new data of many voxels of a chunk. The chunk and its affected neighbors are only redrawn once
	private void BlockBatch(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int count = NetDecoder.ReadUshort(data, 10);
		int offset = 12;
		int reloadMask = 0;
		int x, y, z;
		Chunk c;

		if(!this.cl.Contains(pos))
			return;

		c = this.cl.Get(pos);
		Array.Clear(this.cachedColumns, 0, this.cachedColumns.Length);

		for(int i=0; i < count; i++){
			NetDecoder.ReadLocalPos(data, offset, out x, out y, out z);

			c.data.SetCell(x, y, z, NetDecoder.ReadUshort(data, offset+2));
			c.metadata.SetState(x, y, z, NetDecoder.ReadUshort(data, offset+4));
			c.metadata.SetHP(x, y, z, NetDecoder.ReadUshort(data, offset+6));

			this.cachedColumns[x*Chunk.chunkWidth+z] = true;
			reloadMask |= GetReloadMask(pos, x, y, z);
			offset += 8;
		}

		for(x=0; x < Chunk.chunkWidth; x++){
			for(z=0; z < Chunk.chunkWidth; z++){
				if(this.cachedColumns[x*Chunk.chunkWidth+z])
					c.data.CalculateHeightMap(x, z);
			}
		}

		this.cl.AddToUpdate(pos);
		ReloadNeighbors(pos, reloadMask);
	}

	// Receives data on a VFX operation
	private void VFXData(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		int facing = (sbyte)data[12];
		ushort blockCode = NetDecoder.ReadUshort(data, 13);
		ushort state = NetDecoder.ReadUshort(data, 15);

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnVFXBuild(pos, x, y, z, facing, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnVFXBuild(pos, x, y, z, facing, state, cl);
		}
	}

	// Receives change in state of a VFX
	private void VFXChange(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		int facing = (sbyte)data[12];
		ushort blockCode = NetDecoder.ReadUshort(data, 13);
		ushort state = NetDecoder.ReadUshort(data, 15);

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnVFXChange(pos, x, y, z, facing, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnVFXChange(pos, x, y, z, facing, state, cl);
		}
	}

	// Receives a deletion of a VFX
	private void VFXBreak(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		ushort blockCode = NetDecoder.ReadUshort(data, 12);
		ushort state = NetDecoder.ReadUshort(data, 14);

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnVFXBreak(pos, x, y, z, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnVFXBreak(pos, x, y, z, state, cl);
		}
	}

	// Receives dd:hh:mm from server
	private void SendGameTime(byte[] data){
		uint days = NetDecoder.ReadUint(data, 1);
		byte hours = data[5];
		byte minutes = data[6];

		this.cl.time.SetTime(days, hours, minutes);
	}

	// Receives a player that became visible, already with its CharacterSheet
	private void PlayerSpawn(byte[] data){
		uint netID = NetDecoder.ReadUint(data, 1);
		ulong code = NetDecoder.ReadUlong(data, 5);
		float3 pos = WorldOrigin.ToUnity(NetDecoder.ReadFloat3(data, 13));
		float3 dir = new float3(NetDecoder.ReadAngle(data, 25), NetDecoder.ReadAngle(data, 27), 0f);
		uint timestamp = NetDecoder.ReadUint(data, 29);
		CharacterSheet sheet = NetDecoder.ReadCharacterSheetNet(data, 33, compactAppearance:true);

		this.playerNetIDs[netID] = code;

		// Spawned again without being deleted
		if(this.entityHandler.Contains(EntityType.PLAYER, code)){
			this.smoothMovement.AddSnapshot(EntityType.PLAYER, code, timestamp, pos, dir);
			return;
		}

		this.entityHandler.AddPlayer(code, pos, dir);
		this.entityHandler.AddPlayerSheet(code, sheet);
		this.smoothMovement.AddPlayer(code, timestamp, pos, dir);
		this.entityHandler.UpdatePlayerModel(code, sheet.GetCharacterAppearance(), sheet.GetGender());
	}

	// Receives a dropped item that became visible
	private void DropSpawn(byte[] data){
		ulong code = NetDecoder.ReadUint(data, 1);
		float3 pos = WorldOrigin.ToUnity(NetDecoder.ReadFloat3(data, 5));
		float standing = NetDecoder.ReadBool(data, 17) ? 1f : 0f;
		float3 rot = new float3(standing, NetDecoder.ReadAngle(data, 18), NetDecoder.ReadAngle(data, 20));
		ItemStack its = new ItemStack(NetDecoder.ReadUshort(data, 22), data[24]);
		uint timestamp = NetDecoder.ReadUint(data, 25);

		// rot.x carries the standing flag
		if(this.entityHandler.Contains(EntityType.DROP, code)){
			this.smoothMovement.AddSnapshot(EntityType.DROP, code, timestamp, pos, rot);
			this.entityHandler.ToggleItemAnimation(EntityType.DROP, code, standing);
		}
		else{
			this.entityHandler.AddItem(code, pos, rot, its);
			this.entityHandler.ToggleItemAnimation(EntityType.DROP, code, standing);
			this.smoothMovement.AddItem(code, timestamp, pos);
		}
	}

	// Receives the movement of every visible entity that moved in a Server frame
	private void EntityUpdates(byte[] data){
		int offset = 1;
		int count;
		ulong code;
		float3 pos;
		float standing;
		uint timestamp;

		count = NetDecoder.ReadUshort(data, offset);
		offset += 2;

		for(int i=0; i < count; i++){
			if(this.playerNetIDs.TryGetValue(NetDecoder.ReadUint(data, offset), out code)){
				pos = WorldOrigin.ToUnity(NetDecoder.ReadFloat3(data, offset+4));
				timestamp = NetDecoder.ReadUint(data, offset+20);
				this.smoothMovement.AddSnapshot(EntityType.PLAYER, code, timestamp, pos, new float3(NetDecoder.ReadAngle(data, offset+16), NetDecoder.ReadAngle(data, offset+18), 0f));
			}

			offset += EntityUpdateBatcher.PLAYER_ENTRY_SIZE;
		}

		count = NetDecoder.ReadUshort(data, offset);
		offset += 2;

		for(int i=0; i < count; i++){
			code = NetDecoder.ReadUint(data, offset);

			if(this.entityHandler.Contains(EntityType.DROP, code)){
				pos = WorldOrigin.ToUnity(NetDecoder.ReadFloat3(data, offset+4));
				standing = NetDecoder.ReadBool(data, offset+16) ? 1f : 0f;
				timestamp = NetDecoder.ReadUint(data, offset+17);

				this.smoothMovement.AddSnapshot(EntityType.DROP, code, timestamp, pos, new float3(standing, 0f, 0f));
				this.entityHandler.ToggleItemAnimation(EntityType.DROP, code, standing);
			}

			offset += EntityUpdateBatcher.DROP_ENTRY_SIZE;
		}
	}

	// Receives entity deletion command
	private void EntityDelete(byte[] data){
		RemoveEntity((EntityType)data[1], NetDecoder.ReadUint(data, 2));
	}

	// Receives the deletion of many entities, usually because their chunk was unloaded
	private void EntityDeleteBatch(byte[] data){
		int offset = 1;
		int count;

		count = NetDecoder.ReadUshort(data, offset);
		offset += 2;

		for(int i=0; i < count; i++){
			RemoveEntity(EntityType.PLAYER, NetDecoder.ReadUint(data, offset));
			offset += 4;
		}

		count = NetDecoder.ReadUshort(data, offset);
		offset += 2;

		for(int i=0; i < count; i++){
			RemoveEntity(EntityType.DROP, NetDecoder.ReadUint(data, offset));
			offset += 4;
		}
	}

	private void RemoveEntity(EntityType type, uint id){
		ulong code;

		if(type == EntityType.PLAYER){
			if(!this.playerNetIDs.TryGetValue(id, out code))
				return;

			this.playerNetIDs.Remove(id);
		}
		else{
			code = id;
		}

		this.entityHandler.Remove(type, code);
		this.smoothMovement.Remove(type, code);
	}

	// Finds the AccountID of a remote player
	private bool TryGetPlayer(uint netID, out ulong code){
		return this.playerNetIDs.TryGetValue(netID, out code);
	}

	// Receives the own character's sheet from Server
	private void SendCharSheet(byte[] data){
		ulong code = NetDecoder.ReadUlong(data, 1);
		CharacterSheet sheet = NetDecoder.ReadCharacterSheetNet(data, 9, compactAppearance:false);

		this.entityHandler.AddPlayerSheet(code, sheet);

		if(code == Configurations.accountID){
			this.cl.playerSheetController.SetSheet(sheet);
			this.cl.hotbarHandler.ScrollToNormalSlot((byte)(sheet.GetHotbarSlot() - InventoryLoader.GetInventorySize("ACTION_HOTBAR")));
			this.cl.hotbarHandler.ScrollToActionSlot(sheet.GetAttackHotbarSlot());
			this.cl.playerActionController.UseStyle(sheet.GetBattleStyleCode());
		}
	}

	// Gets the flags of every neighbor chunk that touches a voxel
	private int GetReloadMask(ChunkPos pos, int x, int y, int z){
		int mask = 0;
		bool xMin = x == 0;
		bool xMax = x == Chunk.chunkWidth-1;
		bool zMin = z == 0;
		bool zMax = z == Chunk.chunkWidth-1;

		if(xMin)
			mask |= RELOAD_XM;
		if(xMax)
			mask |= RELOAD_XP;
		if(zMin)
			mask |= RELOAD_ZM;
		if(zMax)
			mask |= RELOAD_ZP;
		if(y == 0 && pos.y > 0)
			mask |= RELOAD_YM;
		if(y == Chunk.chunkDepth-1 && pos.y < Chunk.chunkMaxY)
			mask |= RELOAD_YP;
		if(xMin && zMin)
			mask |= RELOAD_XMZM;
		if(xMax && zMin)
			mask |= RELOAD_XPZM;
		if(xMax && zMax)
			mask |= RELOAD_XPZP;
		if(xMin && zMax)
			mask |= RELOAD_XMZP;

		return mask;
	}

	private static readonly int RELOAD_XM = 1;
	private static readonly int RELOAD_XP = 2;
	private static readonly int RELOAD_ZM = 4;
	private static readonly int RELOAD_ZP = 8;
	private static readonly int RELOAD_YM = 16;
	private static readonly int RELOAD_YP = 32;
	private static readonly int RELOAD_XMZM = 64;
	private static readonly int RELOAD_XPZM = 128;
	private static readonly int RELOAD_XPZP = 256;
	private static readonly int RELOAD_XMZP = 512;

	// Signals neighbor chunks to be reloaded after voxels on their border changed
	private void ReloadNeighbors(ChunkPos pos, int mask){
		if(mask == 0)
			return;

		if((mask & RELOAD_XM) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x-1, pos.z, pos.y));
		if((mask & RELOAD_XP) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x+1, pos.z, pos.y));
		if((mask & RELOAD_ZM) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x, pos.z-1, pos.y));
		if((mask & RELOAD_ZP) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x, pos.z+1, pos.y));
		if((mask & RELOAD_YM) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x, pos.z, pos.y-1));
		if((mask & RELOAD_YP) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x, pos.z, pos.y+1));
		if((mask & RELOAD_XMZM) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x-1, pos.z-1, pos.y));
		if((mask & RELOAD_XPZM) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x+1, pos.z-1, pos.y));
		if((mask & RELOAD_XPZP) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x+1, pos.z+1, pos.y));
		if((mask & RELOAD_XMZP) != 0)
			ReloadIfLoaded(new ChunkPos(pos.x-1, pos.z+1, pos.y));
	}

	private void ReloadIfLoaded(ChunkPos pos){
		if(this.cl.Contains(pos))
			this.cl.AddToUpdate(pos);
	}

	// Gives back the denied placed item to the slot it came from and removes the predicted collision of the voxel
	private void PlacementDenied(byte[] data){
		ushort itemID = NetDecoder.ReadUshort(data, 1);
		byte inventoryCode = data[3];
		byte slot = data[4];
		byte actionSlot = data[5];
		ushort actionID = NetDecoder.ReadUshort(data, 6);
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 8);
		int x, y, z;

		NetDecoder.ReadLocalPos(data, 17, out x, out y, out z);

		this.cl.playerInventoryManager.RestorePlacedItem(itemID, inventoryCode, slot, actionSlot, actionID);
		this.cl.placementPredictor.Deny(pos, x, y, z);
	}

	// Receives block damage information from server
	private void BlockDamage(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		ushort newHP = NetDecoder.ReadUshort(data, 12);
		bool shouldRedraw = NetDecoder.ReadBool(data, 14);

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(!this.cl.Contains(pos))
			return;

		this.cl.Get(pos).metadata.SetHP(x, y, z, newHP);

		if(shouldRedraw){
			this.cl.AddToUpdate(pos);
			ReloadNeighbors(pos, GetReloadMask(pos, x, y, z));
		}
	}

	// Receives player inventory information from server and builds into player inventory
	private void SendInventory(byte[] data, int length){
		if(this.playerInventoryManager == null)
			this.playerInventoryManager = this.cl.playerInventoryManager;

		if(this.hotbarHandler == null)
			this.hotbarHandler = this.cl.hotbarHandler;

		this.playerInventoryManager.LoadFromBytes(data, 1, length);
		this.hotbarHandler.SetHotbar(this.playerInventoryManager.GetMainInventory());	
		this.hotbarHandler.SetActionHotbar(this.playerInventoryManager.GetActionInventory());	
	}

	// Receives a request to register an SFX into SFXLoader
	private void SFXPlay(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		int x, y, z;
		ushort blockCode = NetDecoder.ReadUshort(data, 12);
		ushort state = NetDecoder.ReadUshort(data, 14);

		NetDecoder.ReadLocalPos(data, 10, out x, out y, out z);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnSFXPlay(pos, x, y, z, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnSFXPlay(pos, x, y, z, state, cl);
		}
	}

	// Receives weather Noise information from server
	private void SendNoise(byte[] data, int length){
		World.worldSeed = NetDecoder.ReadInt(data, 1);
		Array.Copy(data, 5, GenerationSeed.weatherNoise, 0, Math.Min(length - 5, GenerationSeed.weatherNoise.Length));
	}

	// Receives the item code for an item a player is holding
	private void SendItemInHand(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		bool isItem = NetDecoder.ReadBool(data, 5);
		ushort id = NetDecoder.ReadUshort(data, 6);
		ushort connectedItem = NetDecoder.ReadUshort(data, 8);
		byte amount = data[10];

		this.entityHandler.UpdatePlayerItem(playerCode, id, connectedItem, isItem, amount);
	}

	// Receives from server a single AnimationState for a player
	private void SendAnimationLayer(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		this.entityHandler.AnimateBone(playerCode, NetDecoder.ReadInt(data, 6), data[5]);
	}

	// Receives from server a single BattleStyle for a player and the items that skin its attachments
	private void SendBattleStyle(byte[] data){
		uint netID = NetDecoder.ReadUint(data, 1);
		int styleCode = NetDecoder.ReadInt(data, 5);
		AttachmentItems items = new AttachmentItems(NetDecoder.ReadUshort(data, 9), NetDecoder.ReadUshort(data, 11));
		ulong playerCode;

		if(this.hasOwnNetID && netID == this.ownNetID)
			this.cl.playerActionController.UseStyle(styleCode, items);
		else if(TryGetPlayer(netID, out playerCode))
			this.entityHandler.ChangePlayerBattleStyle(playerCode, styleCode, items);
	}

	// Receives from server a parameter from an animator
	private void SendAnimatorParameter(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		this.entityHandler.SetAnimatorParameter(EntityType.PLAYER, playerCode, NetDecoder.ReadInt(data, 5), NetDecoder.ReadFloat(data, 9));
	}

	// Receives from server the creation or destruction of an SFX attached to another player
	private void EntitySFX(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		EntityEffectOperation operation = (EntityEffectOperation)data[5];
		EntitySFXData sfx = NetDecoder.ReadEntitySFXData(data, 6);

		this.entityHandler.PlayerSFX(playerCode, operation, sfx);
	}

	// Receives from server the creation or destruction of a VFX attached to another player
	private void EntityVFX(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		EntityEffectOperation operation = (EntityEffectOperation)data[5];
		EntityVFXData vfx = NetDecoder.ReadEntityVFXData(data, 6);

		this.entityHandler.PlayerVFX(playerCode, operation, vfx);
	}

	// Receives from server the creation or destruction of the moving voxel light of another player
	private void EntityVoxelLight(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		EntityEffectOperation operation = (EntityEffectOperation)data[5];
		float radius = NetDecoder.ReadFloat(data, 6);

		this.entityHandler.PlayerVoxelLight(playerCode, operation, radius);
	}

	// Receives from server the switch of a BattleStyle attachment anchor in another player
	private void SendAttachmentAnchor(byte[] data){
		ulong playerCode;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		BoneAnchorType origin = (BoneAnchorType)data[5];
		BoneAnchorType target = (BoneAnchorType)data[6];
		bool flip = NetDecoder.ReadBool(data, 7);
		float heightOffset = NetDecoder.ReadFloat(data, 8);

		this.entityHandler.PlayerAttachmentAnchor(playerCode, origin, target, flip, heightOffset);
	}

	// Receives from server a change in equipment from another player
	private void EquipItem(byte[] data){
		ulong playerCode;
		int oldSize, newSize;

		if(!TryGetPlayer(NetDecoder.ReadUint(data, 1), out playerCode))
			return;

		Item oldItem = NetDecoder.ReadItemDeep(data, 5, out oldSize);
		Item newItem = NetDecoder.ReadItemDeep(data, 5+oldSize, out newSize);

		oldItem.OnUnequipClient(this.cl, oldItem, playerCode);
		newItem.OnEquipClient(this.cl, newItem, playerCode);
	}

	/* ================================================================================ */

	// Activates SmoothMovement in Entities for the current frame
	public void MoveEntities(){
		this.entityHandler.RunEntityActivation();
		this.entityHandler.RunPendingItemHolds();
		this.smoothMovement.MoveEntities();
	}

	// Received positions are buffered in Unity space (see FloatingOrigin)
	public void ShiftOrigin(Vector3 delta){
		this.smoothMovement.ShiftOrigin(delta);
	}
}
