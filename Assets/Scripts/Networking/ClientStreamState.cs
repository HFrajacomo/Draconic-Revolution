using System.Collections.Generic;

/*
Server-side streaming state of a single client
*/
public class ClientStreamState
{
	public ulong id;
	public StreamWindow window;

	// Chunks sent to the client that are still inside its window
	public HashSet<ChunkPos> sent = new HashSet<ChunkPos>();
	// Chunks inside the window that weren't sent yet, nearest first
	public ChunkPriorityQueue pending = new ChunkPriorityQueue(DistanceMetric.STREAMING);
	// Chunks that should be sent before pending ones: freshly generated or requested again by the client
	public Queue<ChunkPos> front = new Queue<ChunkPos>();

	// Amount of streamed chunks that the client didn't acknowledge yet and the adaptive limit for it
	public int inFlight = 0;
	public int sendWindow;
	public bool windowLimited = false;

	// Amount of chunks of this client in the generation queue
	public int waitingCount = 0;

	public ClientStreamState(ulong id, StreamWindow window, int sendWindow){
		this.id = id;
		this.window = window;
		this.sendWindow = sendWindow;
		this.pending.SetPlayerPosition(window.center);
	}
}
