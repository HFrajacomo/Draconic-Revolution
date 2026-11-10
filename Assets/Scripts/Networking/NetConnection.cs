using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

/*
A TCP connection using the framed protocol: every frame is a 4-byte big-endian length followed by a NetMessage
	Receiving runs in IO threads. Every complete frame is copied into a pooled array (NetBufferPool) and queued for the main thread
	Sending is main-thread only. Messages are appended to a buffer that is written to the socket once per frame by Flush()
	A connection closed by the peer or by an error queues a single close event, so the owner handles it in order with the messages before it
*/
public class NetConnection
{
	// Owner information. Only used by the main thread
	public ulong id;
	public bool isTemporary;
	public bool removed = false;

	private Socket socket;
	private ConcurrentQueue<NetMessage> queue;

	// Receiving (IO threads)
	private SocketAsyncEventArgs receiveArgs;
	private byte[] receiveBuffer;
	private int receiveCount = 0;

	// Sending. The back buffer is filled by the main thread while the front buffer is being written to the socket
	private SocketAsyncEventArgs sendArgs;
	private byte[] backBuffer;
	private int backCount = 0;
	private byte[] frontBuffer;
	private int frontCount = 0;
	private int frontOffset = 0;
	private int sending = 0;

	// State
	private volatile bool closed = false;
	private int closeSignaled = 0;
	private long lastReceiveTimestamp;
	private long lastSendTimestamp;
	private string closeReason = "";

	private static readonly int INITIAL_BUFFER_SIZE = 64*1024;


	public NetConnection(Socket socket, ulong id, bool isTemporary, ConcurrentQueue<NetMessage> queue){
		this.socket = socket;
		this.id = id;
		this.isTemporary = isTemporary;
		this.queue = queue;

		try{
			// Messages are already batched once per frame, so Nagle's algorithm would only add latency
			this.socket.NoDelay = true;
			this.socket.SendBufferSize = Constants.NET_SOCKET_BUFFER_SIZE;
			this.socket.ReceiveBufferSize = Constants.NET_SOCKET_BUFFER_SIZE;
		}
		catch(Exception){}

		this.receiveBuffer = new byte[INITIAL_BUFFER_SIZE];
		this.backBuffer = new byte[INITIAL_BUFFER_SIZE];
		this.frontBuffer = new byte[INITIAL_BUFFER_SIZE];

		this.receiveArgs = new SocketAsyncEventArgs();
		this.receiveArgs.Completed += OnReceiveCompleted;
		this.sendArgs = new SocketAsyncEventArgs();
		this.sendArgs.Completed += OnSendCompleted;

		long now = Stopwatch.GetTimestamp();
		this.lastReceiveTimestamp = now;
		this.lastSendTimestamp = now;
	}

	// Starts receiving frames
	public void Start(){
		StartReceive();
	}

	public bool IsClosed(){return this.closed;}
	public string GetCloseReason(){return this.closeReason;}

	public string GetRemoteAddress(){
		try{
			return this.socket.RemoteEndPoint.ToString();
		}
		catch(Exception){
			return "Unknown";
		}
	}

	public double SecondsSinceReceive(){
		return (Stopwatch.GetTimestamp() - Interlocked.Read(ref this.lastReceiveTimestamp)) / (double)Stopwatch.Frequency;
	}

	public double SecondsSinceSend(){
		return (Stopwatch.GetTimestamp() - this.lastSendTimestamp) / (double)Stopwatch.Frequency;
	}

	// Amount of bytes queued or being written to the socket
	public int PendingBytes(){
		int pending = this.backCount;

		if(Volatile.Read(ref this.sending) == 1)
			pending += Math.Max(0, this.frontCount - Volatile.Read(ref this.frontOffset));

		return pending;
	}

	/*
	Sending (main thread)
	*/

	// Queues a message to be written in the next Flush()
	public bool Send(byte[] data, int length){
		if(this.closed)
			return false;

		int needed = this.backCount + 4 + length;

		if(needed > Constants.NET_MAX_PENDING_BYTES){
			SignalClose("Peer can't keep up with the data being sent");
			return false;
		}

		if(needed > this.backBuffer.Length)
			this.backBuffer = Grow(this.backBuffer, this.backCount, needed);

		NetDecoder.WriteInt(length, this.backBuffer, this.backCount);
		Buffer.BlockCopy(data, 0, this.backBuffer, this.backCount+4, length);
		this.backCount = needed;
		this.lastSendTimestamp = Stopwatch.GetTimestamp();

		return true;
	}

	// Writes every queued message to the socket in a single operation. If the last write is still running, they are written in the next call
	public void Flush(){
		if(this.closed || this.backCount == 0)
			return;
		if(Interlocked.CompareExchange(ref this.sending, 1, 0) != 0)
			return;

		byte[] aux = this.frontBuffer;
		this.frontBuffer = this.backBuffer;
		this.frontCount = this.backCount;
		this.frontOffset = 0;
		this.backBuffer = aux;
		this.backCount = 0;

		StartSend();
	}

	// Closes the connection locally. No close event is queued
	public void Close(bool flush){
		if(flush)
			FlushBlocking(Constants.NET_CLOSE_FLUSH_TIMEOUT_MS);

		this.closed = true;
		Interlocked.Exchange(ref this.closeSignaled, 1);

		try{
			this.socket.Shutdown(SocketShutdown.Both);
		}
		catch(Exception){}

		try{
			this.socket.Close();
		}
		catch(Exception){}
	}

	// Waits for the running write and writes everything still queued, so it reaches the peer before the connection is closed
	private void FlushBlocking(int timeoutMs){
		if(this.closed)
			return;

		Stopwatch stopwatch = Stopwatch.StartNew();
		int sent = 0;

		while(Volatile.Read(ref this.sending) == 1){
			if(stopwatch.ElapsedMilliseconds >= timeoutMs)
				return;

			Thread.Sleep(1);
		}

		try{
			while(sent < this.backCount)
				sent += this.socket.Send(this.backBuffer, sent, this.backCount - sent, SocketFlags.None);
		}
		catch(Exception){}

		this.backCount = 0;
	}

	private void StartSend(){
		bool pending;

		while(true){
			try{
				this.sendArgs.SetBuffer(this.frontBuffer, this.frontOffset, this.frontCount - this.frontOffset);
				pending = this.socket.SendAsync(this.sendArgs);
			}
			catch(Exception e){
				Volatile.Write(ref this.sending, 0);
				SignalClose("Send failed: " + e.Message);
				return;
			}

			if(pending)
				return;
			if(!ProcessSend())
				return;
		}
	}

	private void OnSendCompleted(object sender, SocketAsyncEventArgs args){
		if(ProcessSend())
			StartSend();
	}

	// Returns true if part of the current write still has to be sent
	private bool ProcessSend(){
		if(this.sendArgs.SocketError != SocketError.Success || this.sendArgs.BytesTransferred <= 0){
			Volatile.Write(ref this.sending, 0);
			SignalClose("Send failed: " + this.sendArgs.SocketError);
			return false;
		}

		Volatile.Write(ref this.frontOffset, this.frontOffset + this.sendArgs.BytesTransferred);

		if(this.frontOffset < this.frontCount)
			return true;

		this.frontCount = 0;
		this.frontOffset = 0;
		Volatile.Write(ref this.sending, 0);
		return false;
	}

	/*
	Receiving (IO threads)
	*/

	private void StartReceive(){
		bool pending;

		while(!this.closed){
			try{
				this.receiveArgs.SetBuffer(this.receiveBuffer, this.receiveCount, this.receiveBuffer.Length - this.receiveCount);
				pending = this.socket.ReceiveAsync(this.receiveArgs);
			}
			catch(Exception e){
				SignalClose("Receive failed: " + e.Message);
				return;
			}

			if(pending)
				return;
			if(!ProcessReceive())
				return;
		}
	}

	private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args){
		if(ProcessReceive())
			StartReceive();
	}

	// Queues every complete frame in the receive buffer. Returns false if the connection was closed
	private bool ProcessReceive(){
		int offset = 0;
		int size;
		byte[] frame;

		if(this.receiveArgs.SocketError != SocketError.Success){
			SignalClose("Receive failed: " + this.receiveArgs.SocketError);
			return false;
		}
		if(this.receiveArgs.BytesTransferred == 0){
			SignalClose("Closed by peer");
			return false;
		}

		Interlocked.Exchange(ref this.lastReceiveTimestamp, Stopwatch.GetTimestamp());
		this.receiveCount += this.receiveArgs.BytesTransferred;

		while(this.receiveCount - offset >= 4){
			size = NetDecoder.ReadInt(this.receiveBuffer, offset);

			// Protocol violation. Reading further would desync the stream
			if(size <= 0 || size > Constants.NET_MAX_FRAME_SIZE){
				SignalClose("Invalid frame size: " + size);
				return false;
			}

			if(this.receiveCount - offset - 4 < size)
				break;

			frame = NetBufferPool.Rent(size);
			Buffer.BlockCopy(this.receiveBuffer, offset+4, frame, 0, size);
			this.queue.Enqueue(new NetMessage(frame, size, this));

			offset += 4 + size;
		}

		// Keeps the incomplete frame at the start of the buffer
		if(offset > 0){
			this.receiveCount -= offset;

			if(this.receiveCount > 0)
				Buffer.BlockCopy(this.receiveBuffer, offset, this.receiveBuffer, 0, this.receiveCount);
		}

		// Makes room for a frame bigger than the buffer
		if(this.receiveCount >= 4){
			size = NetDecoder.ReadInt(this.receiveBuffer, 0);

			if(size + 4 > this.receiveBuffer.Length)
				this.receiveBuffer = Grow(this.receiveBuffer, this.receiveCount, size + 4);
		}

		return true;
	}

	// Queues the close event only once, no matter how many operations fail
	private void SignalClose(string reason){
		this.closed = true;

		if(Interlocked.Exchange(ref this.closeSignaled, 1) == 1)
			return;

		this.closeReason = reason;

		try{
			this.socket.Close();
		}
		catch(Exception){}

		this.queue.Enqueue(NetMessage.CloseEvent(this));
	}

	private static byte[] Grow(byte[] buffer, int usedBytes, int minimumSize){
		int newSize = buffer.Length;

		while(newSize < minimumSize)
			newSize *= 2;

		byte[] output = new byte[newSize];
		Buffer.BlockCopy(buffer, 0, output, 0, usedBytes);

		return output;
	}
}
