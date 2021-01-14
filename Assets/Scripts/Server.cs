using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class Server : MonoBehaviour
{
	public int maxPlayers = 8;
	public int port = 33000;
	private TcpListener listener;
	private bool isLocal = true;
	public Dictionary<int, TcpClient> connections;
	private int currentCode = 0;


    // Start is called before the first frame update
    void Start()
    {
    	connections = new Dictionary<int, TcpClient>();

    	if(!this.isLocal)
        	this.listener = new TcpListener(IPAddress.Any, this.port);
        else
        	this.listener = new TcpListener(IPAddress.Loopback, this.port);

        print("Starting Server");
        listener.Start();
        listener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
    }



    // Callback function for TCPConnect
    private void TCPConnectCallback(IAsyncResult result){
    	TcpClient client = this.listener.EndAcceptTcpClient(result);
    	this.connections[currentCode] = client;
    	this.currentCode++;

    	Debug.Log(client.ToString() + " has connected");
    	listener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
    }
}
