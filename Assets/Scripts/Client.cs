using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.Mathematics;

public class Client
{
	// Internet Objects
	private Socket socket;
	private SocketError err;
	private DateTime lastMessageTime;
	private int timeoutSeconds = 5;
	private int connectionTimeout = 100;

	// Entity Handler
	public EntityHandler entityHandler = new EntityHandler();

	// Data Management
	private const int sendBufferSize = 4096; // 4KB
	private const int receiveBufferSize = 4096*4096; // 2MB
	private byte[] receiveBuffer;
	public List<NetMessage> queue = new List<NetMessage>();

	// Packet Management
	private bool lengthPacket = true;
	private int packetIndex = 0;
	private int packetSize = 0;
	private byte[] dataBuffer;

	// Address Information
	public IPAddress ip = new IPAddress(0x0F00A8C0);
	public int port = 33000;

	// Unity References
	public ChunkLoader cl;

	// Windows External Process
	public Process lanServerProcess;

	
	public Client(ChunkLoader cl){
		this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
		receiveBuffer = new byte[receiveBufferSize];
		this.cl = cl;

		// If game world is in client
		if(World.isClient){
			// Startup local server
			this.lanServerProcess = new Process();
			this.lanServerProcess.StartInfo.Arguments = "-Local";

			// Debug Unity edition only
			#if UNITY_EDITOR
				// Main PC
				if(File.Exists("C:\\Users\\User\\Draconic Revolution\\Build\\Server\\Server.exe"))
					this.lanServerProcess.StartInfo.FileName = "C:\\Users\\User\\Draconic Revolution\\Build\\Server\\Server.exe";
				// Support Notebook
				else
					this.lanServerProcess.StartInfo.FileName = "C:\\Users\\henri\\Desktop\\-Unity-Draconic-Revolution-RPG\\Build\\Server\\Server.exe";					
			// Standalone edition
			#else
				if(File.Exists("..\\Server\\Server.exe")){
					this.lanServerProcess.StartInfo.UseShellExecute = false;
					this.lanServerProcess.StartInfo.CreateNoWindow = true;
					this.lanServerProcess.StartInfo.FileName = "..\\Server\\Server.exe";
				}
				else{
					Panic();
				}
			#endif



			try{
				this.lanServerProcess.Start();
			}
			catch(Exception e){
				Debug.Log(e);
				Panic();
			}

			this.ip = new IPAddress(new byte[4]{127, 0, 0, 1});
		}

		// If game world is in server
		else{
			string[] segmentedIP = World.IP.Split('.');
			byte[] connectionIP = new byte[4];

			// If it's not a valid IPv4
			if(segmentedIP.Length != 4)
				Panic();
			// Tailors the IP
			else{
				for(int i=0; i < 4; i++){
					try{
						connectionIP[i] = (byte)Convert.ToInt16(segmentedIP[i]);
					}
					catch(Exception e){
						Debug.Log(e);
						Panic();
					}
				}

				this.ip = new IPAddress(connectionIP);
			}
		}

		this.Connect();	
	}

	// Triggers hazard protection and sends user back to menu screen
	public void Panic(){
        SceneManager.LoadScene("Menu");
	}
	
	
	public void Connect(){
		int attempts = 0;

		while(attempts < this.connectionTimeout){
			try{
				this.socket.Connect(this.ip, this.port);
				break;
			} catch {
				attempts++;
				continue;
			}
		}

		if(attempts == this.connectionTimeout)
			Panic();


		this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), null);		
	}

	// Receive call handling
	private void ReceiveCallback(IAsyncResult result){
		try{
			// Resets timeout timer
			this.lastMessageTime = DateTime.Now;

			int bytesReceived = this.socket.EndReceive(result);

			// If has received a length Packet
			if(this.lengthPacket){
				int size = NetDecoder.ReadInt(receiveBuffer, 0);
				this.dataBuffer = new byte[size];
				this.packetSize = size;
				this.packetIndex = 0;
				this.lengthPacket = false;

				this.socket.BeginReceive(receiveBuffer, 0, size, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
				return;
			}

			// If has received a segmented packet
			if(bytesReceived + this.packetIndex < this.packetSize){
				Array.Copy(receiveBuffer, 0, this.dataBuffer, this.packetIndex, bytesReceived);
				this.packetIndex = this.packetIndex + bytesReceived;
				this.socket.BeginReceive(receiveBuffer, 0, this.packetSize-this.packetIndex, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
				return;
			}

			Array.Copy(receiveBuffer, 0, this.dataBuffer, this.packetIndex, bytesReceived);

			NetMessage.Broadcast(NetBroadcast.RECEIVED, dataBuffer[0], 0, this.packetSize);

			NetMessage receivedMessage = new NetMessage(this.dataBuffer, 0);
			this.queue.Add(receivedMessage);

			this.lengthPacket = true;
			this.packetSize = 0;
			this.packetIndex = 0;

			this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
		}
		catch(Exception e){
			Debug.Log(e.ToString());
		}
	}

	// Sends a byte[] to the server
	public bool Send(byte[] data, int length){
		try{
			IAsyncResult lenResult = this.socket.BeginSend(this.LengthPacket(length), 0, 4, 0, out this.err, null, null);
			this.socket.EndSend(lenResult);

			IAsyncResult result = this.socket.BeginSend(data, 0, length, 0, out this.err, null, null);
			this.socket.EndSend(result);

			NetMessage.Broadcast(NetBroadcast.SENT, data[0], 0, length);
			return true;
		}
		catch(Exception e){
			Debug.Log("SEND ERROR: " + e.ToString());
			return false;
		}
	}

	// Send callback to end package
	public void SendCallback(IAsyncResult result){}

	// Checks if timeout has occured on server
	public void CheckTimeout(){
		if((DateTime.Now - this.lastMessageTime).Seconds > this.timeoutSeconds){
	        Debug.Log("Timeout");

	        Disconnect();
		}
	}

	/* 
	=========================================================================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from Server
	public void HandleReceivedMessage(byte[] data){
		if(data.Length == 0)
			return;

		NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], 0, 0);

		switch((NetCode)data[0]){
			case NetCode.ACCEPTEDCONNECT:
				AcceptConnect();
				break;
			case NetCode.SENDSERVERINFO:
				SendServerInfo(data);
				break;
			case NetCode.SENDCHUNK:
				SendChunk(data);
				break;
			case NetCode.DISCONNECT:
				Disconnect();
				break;
			case NetCode.DIRECTBLOCKUPDATE:
				DirectBlockUpdate(data);
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
			case NetCode.ENTITYDATA:
				EntityData(data);
				break;
			case NetCode.ENTITYDELETE:
				EntityDelete(data);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED: " + (NetCode)data[0]);
				break;
		}
	}

	// Permits the loading of Game Scene
	private void AcceptConnect(){
		this.cl.CONNECTEDTOSERVER = true;
		Debug.Log("Connected to Server at " + this.socket.RemoteEndPoint.ToString());
	}

	// Receives Player Information saved on server on startup
	private void SendServerInfo(byte[] data){
		float x, y, z, xDir, yDir, zDir;
		CastCoord initialCoord;

		x = NetDecoder.ReadFloat(data, 1);
		y = NetDecoder.ReadFloat(data, 5);
		z = NetDecoder.ReadFloat(data, 9);
		xDir = NetDecoder.ReadFloat(data, 13);
		yDir = NetDecoder.ReadFloat(data, 17);
		zDir = NetDecoder.ReadFloat(data, 21);

		this.cl.PLAYERSPAWNED = true;
		this.cl.playerX = x;
		this.cl.playerY = y;
		this.cl.playerZ = z;
		this.cl.playerDirX = xDir;
		this.cl.playerDirY = yDir;
		this.cl.playerDirZ = zDir;

		// Finds current Chunk and sends position data
		initialCoord = new CastCoord(x, y, z);
		this.cl.playerMovement.SetCurrentChunkPos(initialCoord.GetChunkPos());
		this.cl.playerMovement.SendChunkPosMessage();
	}

	// Receives a Chunk
	private void SendChunk(byte[] data){
		this.cl.toLoad.Add(data);
	}

	// Receives a disconnect call from server
	private void Disconnect(){
		this.socket.Close();
		Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Menu");
	}

	// Receives a Direct Block Update from server
	private void DirectBlockUpdate(byte[] data){
		ChunkPos pos;
		int x, y, z;
		ushort blockCode, state, hp;
		BUDCode type;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 9);
		y = NetDecoder.ReadInt(data, 13);
		z = NetDecoder.ReadInt(data, 17);
		int facing = NetDecoder.ReadInt(data, 21);

		blockCode = NetDecoder.ReadUshort(data, 25);
		state = NetDecoder.ReadUshort(data, 27);
		hp = NetDecoder.ReadUshort(data, 29);
		type = (BUDCode)NetDecoder.ReadInt(data, 31);

		switch(type){
			case BUDCode.PLACE:
				if(this.cl.chunks.ContainsKey(pos)){
					this.cl.chunks[pos].data.SetCell(x, y, z, blockCode);
					this.cl.chunks[pos].metadata.SetState(x, y, z, state);
					this.cl.chunks[pos].metadata.SetHP(x, y, z, hp);
					this.cl.AddToUpdate(pos);
				}
				break;
			case BUDCode.BREAK:
				if(this.cl.chunks.ContainsKey(pos)){
					this.cl.chunks[pos].data.SetCell(x, y, z, 0);
					this.cl.chunks[pos].metadata.Reset(x,y,z);
					this.cl.AddToUpdate(pos);
				}	
				break;
			case BUDCode.CHANGE:
				if(this.cl.chunks.ContainsKey(pos)){
					this.cl.chunks[pos].data.SetCell(x, y, z, blockCode);
					this.cl.chunks[pos].metadata.SetState(x, y, z, state);
					this.cl.chunks[pos].metadata.SetHP(x, y, z, hp);
					this.cl.AddToUpdate(pos);
				}
				break;
			default:
				break;
		}
	}

	// Receives data on a VFX operation
	private void VFXData(byte[] data){
		ChunkPos pos;
		int x,y,z,facing;
		ushort blockCode, state;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 9);
		y = NetDecoder.ReadInt(data, 13);
		z = NetDecoder.ReadInt(data, 17);
		facing = NetDecoder.ReadInt(data, 21);
		blockCode = NetDecoder.ReadUshort(data, 25);
		state = NetDecoder.ReadUshort(data, 27);

		if(blockCode <= ushort.MaxValue/2){
			this.cl.blockBook.blocks[blockCode].OnVFXBuild(pos, x, y, z, facing, state, cl);
		}
		else{
			this.cl.blockBook.objects[ushort.MaxValue - blockCode].OnVFXBuild(pos, x, y, z, facing, state, cl);
		}
	}

	// Receives change in state of a VFX
	private void VFXChange(byte[] data){
		ChunkPos pos;
		int x,y,z,facing;
		ushort blockCode, state;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 9);
		y = NetDecoder.ReadInt(data, 13);
		z = NetDecoder.ReadInt(data, 17);
		facing = NetDecoder.ReadInt(data, 21);
		blockCode = NetDecoder.ReadUshort(data, 25);
		state = NetDecoder.ReadUshort(data, 27);

		if(blockCode <= ushort.MaxValue/2){
			this.cl.blockBook.blocks[blockCode].OnVFXChange(pos, x, y, z, facing, state, cl);
		}
		else{
			this.cl.blockBook.objects[ushort.MaxValue - blockCode].OnVFXChange(pos, x, y, z, facing, state, cl);
		}
	}

	// Receives a deletion of a VFX
	private void VFXBreak(byte[] data){
		ChunkPos pos;
		int x,y,z;
		ushort blockCode, state;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 9);
		y = NetDecoder.ReadInt(data, 13);
		z = NetDecoder.ReadInt(data, 17);
		blockCode = NetDecoder.ReadUshort(data, 21);
		state = NetDecoder.ReadUshort(data, 23);

		if(blockCode <= ushort.MaxValue/2){
			this.cl.blockBook.blocks[blockCode].OnVFXBreak(pos, x, y, z, state, cl);
		}
		else{
			this.cl.blockBook.objects[ushort.MaxValue - blockCode].OnVFXBreak(pos, x, y, z, state, cl);
		}
	}

	// Receives dd:hh:mm from server
	private void SendGameTime(byte[] data){
		uint days = NetDecoder.ReadUint(data, 1);
		byte hours = data[5];
		byte minutes = data[6];

		this.cl.time.SetTime(days, hours, minutes);
	}

	// Receives data regarding an entity
	private void EntityData(byte[] data){
		EntityType type = (EntityType)data[1];
		ulong code = NetDecoder.ReadUlong(data, 2);
		float3 pos = NetDecoder.ReadFloat3(data, 10);
		float3 dir = NetDecoder.ReadFloat3(data, 22);

		if(this.entityHandler.Contains(type, code))
			this.entityHandler.Move(type, code, pos, dir);
		else
			this.entityHandler.Add(type, code, pos, dir);
	}

	// Receives entity deletion command
	private void EntityDelete(byte[] data){
		EntityType type = (EntityType)data[1];
		ulong code = NetDecoder.ReadUlong(data, 2);

		this.entityHandler.Remove(type, code);	
	}


	// Returns a byte array representation of a int
	private byte[] LengthPacket(int a){
		byte[] output = new byte[4];

		output[0] = (byte)(a >> 24);
		output[1] = (byte)(a >> 16);
		output[2] = (byte)(a >> 8);
		output[3] = (byte)a;

		return output;
	}

}
