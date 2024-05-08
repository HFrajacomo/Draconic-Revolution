using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class InfoClient
{
	// Internet Objects
	public bool ended = false;
	public bool backToMenu = false;
	private Socket socket;
	private SocketError err;
	private int timeoutSeconds = 5;
	private static readonly int RECEIVE_BUFFER_SIZE = 1200;
	private static readonly int MAXIMUM_PACKET_SIZE = 1200;

	// Address Information
	public IPAddress ip;
	public int port = 33000;

	// Packet Management
	private bool lengthPacket = true;
	private int packetIndex = 0;
	private int packetSize = 0;

	// Data Management
	private byte[] receiveBuffer;
	private byte[] dataBuffer;
	private byte[] lengthBuffer = new byte[4];
	private List<NetMessage> queue = new List<NetMessage>();


	public InfoClient(){
		this.receiveBuffer = new byte[RECEIVE_BUFFER_SIZE];
		this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

		if(World.isClient){
			this.ip = new IPAddress(new byte[4]{127,0,0,1});
			this.Connect();
		}
		else{
			this.ip = IPAddress.Parse(World.IP);
			this.Connect();
		}
	}

	public bool Connect(){
        IAsyncResult result = this.socket.BeginConnect(this.ip, this.port, null, null);

        bool success = result.AsyncWaitHandle.WaitOne(this.timeoutSeconds * 1000, true);

        if (success && socket.Connected){
        	Debug.Log("Connected");
            this.socket.EndConnect(result);
            this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(Receive), null);

            return true;
        }
        else{
            this.socket.Close();
            Debug.Log("Failed to establish and info connection to server at: " + this.ip);
            return false;
        }
	}

	public void Receive(IAsyncResult result){
		try{
			if(this.ended || this.backToMenu){
				return;
			}

			int bytesReceived = this.socket.EndReceive(result);

			// If is a length packet
			if(this.lengthPacket){
				int size = NetDecoder.ReadInt(receiveBuffer, 0);

				// Ignores packets way too big
				if(size > MAXIMUM_PACKET_SIZE){
					this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(Receive), null);
					return;
				}

				this.dataBuffer = new byte[size];
				this.packetSize = size;
				this.packetIndex = 0;
				this.lengthPacket = false;

				this.socket.BeginReceive(receiveBuffer, 0, size, 0, out this.err, new AsyncCallback(Receive), null);
				return;
			}

			// If is segmented package

			if(bytesReceived + this.packetIndex < this.packetSize){
				Array.Copy(receiveBuffer, 0, this.dataBuffer, this.packetIndex, bytesReceived);
				this.packetIndex = this.packetIndex + bytesReceived;
				this.socket.BeginReceive(receiveBuffer, 0, this.packetSize-this.packetIndex, 0, out this.err, new AsyncCallback(Receive), null);
				return;
			}

			Array.Copy(receiveBuffer, 0, this.dataBuffer, this.packetIndex, bytesReceived);
			NetMessage.Broadcast(NetBroadcast.RECEIVED, dataBuffer[0], 0, this.packetSize);

			NetMessage receivedMessage = new NetMessage(this.dataBuffer, 0);
			this.queue.Add(receivedMessage);
			this.lengthPacket = true;
			this.packetSize = 0;
			this.packetIndex = 0;

			this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(Receive), null);
		}
		catch(Exception e){
			Debug.Log(e.ToString());
		}
	}

	public bool Send(byte[] data, int length){
		try{
			NetMessage.Broadcast(NetBroadcast.SENT, dataBuffer[0], 0, length);
			LengthPacket(length);

			this.socket.Send(this.lengthBuffer, 4, SocketFlags.None);
			this.socket.Send(data, length, SocketFlags.None);
			return true;
		}
		catch(Exception e){
			Debug.Log("SEND ERROR: " + e.ToString());
			return false;
		}
	}

	public void Close(){
		Debug.Log("SOCKET CLOSED");
		this.ended = true;
		this.socket.Close();
	}

	// Sets a byte array representation of a int
	private void LengthPacket(int a){
		this.lengthBuffer[0] = (byte)(a >> 24);
		this.lengthBuffer[1] = (byte)(a >> 16);
		this.lengthBuffer[2] = (byte)(a >> 8);
		this.lengthBuffer[3] = (byte)a;
	}


	/*
	Handling NetMessages
	*/

	public void HandleReceivedMessages(){
		if(this.queue.Count == 0 || this.ended)
			return;

		byte[] data = this.queue[0].GetData();
		this.queue.RemoveAt(0);

		if(data.Length == 0)
			return;

		NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], 0, 0);

		switch((NetCode)data[0]){
			case NetCode.ACCEPTEDCONNECT:
				AcceptedConnect();
				break;
			case NetCode.SENDCHARACTERPRELOAD:
				SendCharacterPreload(data);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED: " + (NetCode)data[0]);
				break;
		}
	}

	public void AcceptedConnect(){
		// If character creation was not forced upon server entering
		if(!PlayerAppearanceData.GetPreloadFlag()){
			NetMessage message = new NetMessage(NetCode.REQUESTCHARACTEREXISTENCE);
			message.RequestCharacterExistence(Configurations.accountID);
			this.Send(message.GetMessage(), message.size);
			Debug.Log("Requesting Character Existence");
		}
		else{
			Debug.Log("Sending Character Sheet to Server");
			SendCharAndDisconnectInfo();
			PlayerAppearanceData.SetPreloadFlag(false);
		}
	}

	public void SendCharacterPreload(byte[] data){
		bool flag = NetDecoder.ReadBool(data, 1);

		// If character does not exist
		if(!flag){
			PlayerAppearanceData.SetPreloadFlag(true);
			MenuManager.SetInitCharacterCreationFlag(true);
			this.backToMenu = true;
			Debug.Log("back to CharacterCreation");
		}
		else{
			Debug.Log("Received Character from server");
			CharacterAppearance app = NetDecoder.ReadCharacterAppearance(data, 2);
			bool isMale = NetDecoder.ReadBool(data, 249);

			NetMessage disconnectMessage = new NetMessage(NetCode.DISCONNECTINFO);
			this.Send(disconnectMessage.GetMessage(), disconnectMessage.size);
			this.Close();

			PlayerAppearanceData.SetAppearance(app);
			PlayerAppearanceData.SetGender(isMale);
		}
	}


	/*
	Auxiliary Functions
	*/

	private void SendCharAndDisconnectInfo(){
		NetMessage message = new NetMessage(NetCode.SENDCHARSHEET);
		message.SendCharSheet(Configurations.accountID, CharacterCreationData.GetCharacterSheet());
		this.Send(message.GetMessage(), message.size);

		NetMessage disconnectMessage = new NetMessage(NetCode.DISCONNECTINFO);
		this.Send(disconnectMessage.GetMessage(), disconnectMessage.size);
		this.Close();
		Debug.Log("Sent character infor and Disconnected");
	
		PlayerAppearanceData.SetAppearance(CharacterCreationData.GetCharacterSheet().GetCharacterAppearance());
		PlayerAppearanceData.SetGender(CharacterCreationData.GetCharacterSheet().GetGender());
	}
}