using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour
{
	public TcpClient socket;

	private const int sendBufferSize = 4096; // 4KB
	private const int receiveBufferSize = 4096*4096; // 2MB

	private NetworkStream stream;
	private byte[] receiveBuffer;

	public string ip = "127.0.0.1";
	public int port = 33000;

	public void Start(){
		socket = new TcpClient();

		receiveBuffer = new byte[receiveBufferSize];
		this.Connect();		
	}

	/*
	public Client(){

	}
	*/

	public void Connect(){
		socket.BeginConnect(this.ip, this.port, ConnectCallback, socket);		

	}

	private void ConnectCallback(IAsyncResult result){
		socket.EndConnect(result);

		if(!socket.Connected)
			return;

		stream = socket.GetStream();

		stream.BeginRead(receiveBuffer, 0, receiveBufferSize, ReceiveCallback, null);
		Debug.Log(receiveBuffer);
	}

	private void ReceiveCallback(IAsyncResult result){
		try{
			int bytesReceived = stream.EndRead(result);

			if(bytesReceived <= 0){
				return;
			}

			byte[] data = new byte[bytesReceived];
			Array.Copy(receiveBuffer, data, bytesReceived);
		}
		catch{
			// Disconnect
		}
	}
}
