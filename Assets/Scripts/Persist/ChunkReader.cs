using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

/*
Reads chunks and their entities from Region Files in a background thread, so loading chunks from disk doesn't stall the Server.
The main thread finds where the data is (index lookups) and processes the results. This thread only does the file reads
*/
public class ChunkReader{
	public class Request{
		public ChunkPos pos;
		public string chunkPath;
		public long chunkOffset;
		public bool hasEntities;
		public string entityPath;
		public long entityOffset;
	}

	public class Result{
		public ChunkPos pos;
		public bool failed;
		public byte[] header = new byte[RegionFileHandler.chunkHeaderSize];
		public byte[] chunkData = new byte[0];
		public int chunkSize;
		public bool hasEntities;
		public byte[] entityData = new byte[0];
		public int entitySize;
	}

	private static readonly int MAX_OPEN_STREAMS = 16;

	private Thread thread;
	private volatile bool running = true;
	private AutoResetEvent signal = new AutoResetEvent(false);
	private AutoResetEvent completedSignal = new AutoResetEvent(false);
	private ConcurrentQueue<Request> requests = new ConcurrentQueue<Request>();
	private ConcurrentQueue<Result> completed = new ConcurrentQueue<Result>();
	private ConcurrentQueue<Result> resultPool = new ConcurrentQueue<Result>();
	private ConcurrentQueue<Request> requestPool = new ConcurrentQueue<Request>();

	// Only used by the reader thread
	private Dictionary<string, FileStream> streams = new Dictionary<string, FileStream>();
	private List<string> streamOrder = new List<string>();
	private byte[] intBuffer = new byte[4];


	public ChunkReader(){
		this.thread = new Thread(Run);
		this.thread.Name = "ChunkReader";
		this.thread.IsBackground = true;
		this.thread.Start();
	}

	public Request GetRequest(){
		Request request;

		if(this.requestPool.TryDequeue(out request))
			return request;
		return new Request();
	}

	public void Enqueue(Request request){
		this.requests.Enqueue(request);
		this.signal.Set();
	}

	public bool TryGetCompleted(out Result result){
		return this.completed.TryDequeue(out result);
	}

	// Waits until a read finishes or the timeout ends. Returns false on timeout
	public bool WaitForCompleted(int timeoutMs){
		if(!this.completed.IsEmpty)
			return true;

		return this.completedSignal.WaitOne(timeoutMs);
	}

	// Gives a processed result back, so its buffers are reused
	public void Recycle(Result result){
		this.resultPool.Enqueue(result);
	}

	// Stops the thread after the requests already queued are read
	public void Stop(){
		this.running = false;
		this.signal.Set();

		if(this.thread != null && this.thread.IsAlive)
			this.thread.Join();

		this.thread = null;
	}

	private void Run(){
		Request request;

		while(true){
			while(this.requests.TryDequeue(out request)){
				this.completed.Enqueue(Read(request));
				this.requestPool.Enqueue(request);
				this.completedSignal.Set();
			}

			if(!this.running)
				break;

			this.signal.WaitOne(100);
		}

		CloseStreams();
	}

	private Result Read(Request request){
		Result result;

		if(!this.resultPool.TryDequeue(out result))
			result = new Result();

		result.pos = request.pos;
		result.failed = false;
		result.chunkSize = 0;
		result.hasEntities = false;
		result.entitySize = 0;

		try{
			FileStream file = GetStream(request.chunkPath);

			file.Seek(request.chunkOffset, SeekOrigin.Begin);
			ReadFully(file, result.header, RegionFileHandler.chunkHeaderSize);

			result.chunkSize = NetDecoder.ReadInt(result.header, 9) + NetDecoder.ReadInt(result.header, 13) + NetDecoder.ReadInt(result.header, 17);

			if(result.chunkData.Length < result.chunkSize)
				result.chunkData = new byte[result.chunkSize];

			ReadFully(file, result.chunkData, result.chunkSize);

			if(request.hasEntities){
				file = GetStream(request.entityPath);

				file.Seek(request.entityOffset, SeekOrigin.Begin);
				ReadFully(file, this.intBuffer, 4);

				result.entitySize = NetDecoder.ReadInt(this.intBuffer, 0);

				if(result.entityData.Length < result.entitySize)
					result.entityData = new byte[result.entitySize];

				ReadFully(file, result.entityData, result.entitySize);
				result.hasEntities = true;
			}
		}
		catch(Exception){
			result.failed = true;
		}

		return result;
	}

	// Stream.Read may return less bytes than asked
	private void ReadFully(Stream stream, byte[] buffer, int size){
		int read = 0;
		int amount;

		while(read < size){
			amount = stream.Read(buffer, read, size - read);

			if(amount <= 0)
				throw new EndOfStreamException();

			read += amount;
		}
	}

	private FileStream GetStream(string path){
		FileStream stream;

		if(this.streams.TryGetValue(path, out stream)){
			this.streamOrder.Remove(path);
			this.streamOrder.Add(path);
			return stream;
		}

		if(this.streamOrder.Count >= MAX_OPEN_STREAMS){
			this.streams[this.streamOrder[0]].Close();
			this.streams.Remove(this.streamOrder[0]);
			this.streamOrder.RemoveAt(0);
		}

		// No buffering, since the file is written by other streams between reads
		stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.RandomAccess);
		this.streams.Add(path, stream);
		this.streamOrder.Add(path);

		return stream;
	}

	private void CloseStreams(){
		foreach(FileStream stream in this.streams.Values)
			stream.Close();

		this.streams.Clear();
		this.streamOrder.Clear();
	}
}
