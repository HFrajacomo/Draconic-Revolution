using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;

public class Client
{
	// Internet Objects
	private Socket socket;
	private SocketError err;

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
	public IPAddress ip = new IPAddress(0x0800A8C0);
	public int port = 33000;

	// Unity References
	public ChunkLoader cl;

	
	public Client(ChunkLoader cl){
		this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
		receiveBuffer = new byte[receiveBufferSize];
		this.cl = cl;
		this.Connect();	
	}
	
	
	public void Connect(){
		this.socket.Connect(this.ip, this.port);
		this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), null);		
	}

	// Receive call handling
	private void ReceiveCallback(IAsyncResult result){
		try{
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
	public void SendCallback(IAsyncResult result){
		
	}

	/* 
	=========================================================================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from Server
	public void HandleReceivedMessage(byte[] data){
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

	// Receives Player Information as int on startup
	private void SendServerInfo(byte[] data){
		int x, y, z;

		x = NetDecoder.ReadInt(data, 1);
		y = NetDecoder.ReadInt(data, 5);
		z = NetDecoder.ReadInt(data, 9);

		this.cl.PLAYERSPAWNED = true;
		this.cl.playerX = x;
		this.cl.playerY = y;
		this.cl.playerZ = z;
	}

	// Receives a Chunk
	private void SendChunk(byte[] data){
		this.cl.toLoad.Add(data);
	}

	// Receives a disconnect call from server
	private void Disconnect(){
		this.socket.Close();
		SceneManager.LoadScene(0);
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

		CastCoord current = new CastCoord(new Vector3(x,y,z));

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
					this.cl.AddToDraw(pos);
				}
				break;
			case BUDCode.BREAK:
				if(this.cl.chunks.ContainsKey(pos)){
					this.cl.chunks[pos].data.SetCell(x, y, z, 0);
					this.cl.chunks[pos].metadata.SetState(x, y, z, ushort.MaxValue);
					this.cl.chunks[pos].metadata.SetHP(x, y, z, ushort.MaxValue);
					this.cl.AddToDraw(pos);
				}	
				break;
			case BUDCode.CHANGE:
				if(this.cl.chunks.ContainsKey(pos)){
					this.cl.chunks[pos].data.SetCell(x, y, z, blockCode);
					this.cl.chunks[pos].metadata.SetState(x, y, z, state);
					this.cl.chunks[pos].metadata.SetHP(x, y, z, hp);
					this.cl.AddToDraw(pos);
				}
				break;
			default:
				break;
		}


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
