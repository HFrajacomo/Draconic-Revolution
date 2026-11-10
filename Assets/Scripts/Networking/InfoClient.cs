using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/*
Short-lived connection used in the Menu to check the character of the player before loading the world
	Connecting never blocks the main thread. Update() must be called every frame (Redirector)
*/
public class InfoClient
{
	// Results
	public bool ended = false;
	public bool backToMenu = false;

	// Connection
	private Socket socket;
	private IAsyncResult connectResult;
	private NetConnection connection;
	private int attempt = 0;
	private float attemptStartTime;
	private float nextAttemptTime = 0f;
	private static readonly int ATTEMPTS = 5;
	private static readonly float ATTEMPT_SECONDS = 3f;

	// Address Information
	public IPAddress ip;
	public int port = 33000;

	// Filled by the socket threads and consumed by the main thread
	private ConcurrentQueue<NetMessage> queue = new ConcurrentQueue<NetMessage>();


	public InfoClient(){
		if(World.isClient)
			this.ip = new IPAddress(new byte[4]{127,0,0,1});
		else
			this.ip = IPAddress.Parse(World.IP);

		StartAttempt();
	}

	public void Close(){
		this.ended = true;

		if(this.connection != null)
			this.connection.Close(flush:true);
		else if(this.socket != null)
			this.socket.Close();
	}

	// Advances the connection attempts and handles received messages
	public void Update(){
		if(this.ended || this.backToMenu)
			return;

		if(this.connection == null){
			UpdateConnection();
			return;
		}

		HandleReceivedMessages();

		if(this.connection != null && !this.ended)
			this.connection.Flush();
	}

	private void StartAttempt(){
		this.attempt++;
		this.attemptStartTime = Time.realtimeSinceStartup;

		try{
			this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			this.connectResult = this.socket.BeginConnect(this.ip, this.port, null, null);
		}
		catch(Exception e){
			Debug.Log("InfoClient failed to start connecting: " + e.Message);
			this.connectResult = null;
		}
	}

	private void UpdateConnection(){
		// Waiting to retry
		if(this.connectResult == null){
			if(Time.realtimeSinceStartup < this.nextAttemptTime)
				return;

			StartAttempt();

			if(this.connectResult == null)
				FailAttempt();

			return;
		}

		if(this.connectResult.IsCompleted){
			try{
				this.socket.EndConnect(this.connectResult);
			}
			catch(Exception){
				FailAttempt();
				return;
			}

			this.connection = new NetConnection(this.socket, 0, false, this.queue);
			this.connection.Start();
			Debug.Log($"[Attempt #{this.attempt}] InfoClient connected to {this.ip}");
			return;
		}

		if(Time.realtimeSinceStartup - this.attemptStartTime >= ATTEMPT_SECONDS)
			FailAttempt();
	}

	// Retries until all attempts are used. Attempts last at least ATTEMPT_SECONDS, so a server that is still starting has time to open
	private void FailAttempt(){
		try{
			this.socket.Close();
		}
		catch(Exception){}

		this.connectResult = null;

		if(this.attempt >= ATTEMPTS){
			this.backToMenu = true;
			Debug.Log("Failed to establish an info connection to server at: " + this.ip);
			return;
		}

		this.nextAttemptTime = this.attemptStartTime + ATTEMPT_SECONDS;
	}

	private bool Send(NetMessage message){
		NetMessage.Broadcast(NetBroadcast.SENT, message.GetMessage()[0], 0, message.size);

		return this.connection.Send(message.GetMessage(), message.size);
	}


	/*
	Handling NetMessages
	*/

	private void HandleReceivedMessages(){
		NetMessage message;
		byte[] data;
		int queueCount = this.queue.Count;

		for(int i=0; i < queueCount; i++){
			if(this.ended || this.backToMenu)
				return;
			if(!this.queue.TryDequeue(out message))
				return;

			if(message.IsCloseEvent()){
				Debug.Log("InfoClient lost connection: " + message.GetConnection().GetCloseReason());
				this.backToMenu = true;
				return;
			}

			data = message.GetData();

			NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], 0, message.size);

			switch((NetCode)data[0]){
				case NetCode.ACCEPTEDCONNECT:
					AcceptedConnect();
					break;
				case NetCode.HEARTBEAT:
					break;
				case NetCode.SENDCHARACTERPRELOAD:
					SendCharacterPreload(data);
					break;
				default:
					Debug.Log("UNKNOWN NETMESSAGE RECEIVED: " + (NetCode)data[0]);
					break;
			}

			NetBufferPool.Return(data);
		}
	}

	public void AcceptedConnect(){
		// If character creation was not forced upon server entering
		if(!PlayerAppearanceData.GetPreloadFlag()){
			NetMessage message = new NetMessage(NetCode.REQUESTCHARACTEREXISTENCE);
			message.RequestCharacterExistence(Configurations.accountID);
			this.Send(message);
		}
		else{
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

			NetMessage disconnectMessage = new NetMessage(NetCode.DISCONNECTINFO);
			this.Send(disconnectMessage);
			this.connection.Close(flush:true);
			this.backToMenu = true;
		}
		else{
			CharacterAppearance app = NetDecoder.ReadCharacterAppearance(data, 2);
			bool isMale = NetDecoder.ReadBool(data, 249);

			NetMessage disconnectMessage = new NetMessage(NetCode.DISCONNECTINFO);
			this.Send(disconnectMessage);
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
		this.Send(message);

		NetMessage disconnectMessage = new NetMessage(NetCode.DISCONNECTINFO);
		this.Send(disconnectMessage);
		this.Close();
	
		PlayerAppearanceData.SetAppearance(CharacterCreationData.GetCharacterSheet().GetCharacterAppearance());
		PlayerAppearanceData.SetGender(CharacterCreationData.GetCharacterSheet().GetGender());
	}
}
