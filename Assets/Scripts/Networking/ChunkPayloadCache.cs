using System.Collections.Generic;

/*
Compressed SENDCHUNK messages of loaded chunks, so a chunk is compressed once no matter how many times or to how many clients it's sent
	An entry is only used while its chunk keeps the same ChunkChangeSet version it was built with
	Entries are dropped from the least recently used once the memory budget is spent
	The first byte of an entry is the NetCode, which may be changed between SENDCHUNK and STREAMCHUNK before sending
*/
public class ChunkPayloadCache
{
	private class Entry{
		public byte[] data;
		public int length;
		public ChunkChangeSet changeSet;
		public int version;
		public LinkedListNode<ChunkPos> node;
	}

	private Dictionary<ChunkPos, Entry> entries = new Dictionary<ChunkPos, Entry>();
	private LinkedList<ChunkPos> usage = new LinkedList<ChunkPos>(); // Most recently used first
	private long totalBytes = 0;
	private long maxBytes;


	public ChunkPayloadCache(int maxMegabytes){
		this.maxBytes = (long)maxMegabytes * 1024 * 1024;
	}

	public bool TryGet(Chunk c, out byte[] data, out int length){
		Entry entry;

		data = null;
		length = 0;

		if(c.changeSet == null || !this.entries.TryGetValue(c.pos, out entry))
			return false;

		if(entry.changeSet != c.changeSet || entry.version != c.changeSet.GetVersion()){
			Remove(c.pos);
			return false;
		}

		this.usage.Remove(entry.node);
		this.usage.AddFirst(entry.node);

		data = entry.data;
		length = entry.length;
		return true;
	}

	// Keeps a SENDCHUNK message of the current version of a chunk. The array is owned by the cache from now on
	public void Put(Chunk c, byte[] data, int length){
		Entry entry;

		if(c.changeSet == null)
			return;

		Remove(c.pos);

		entry = new Entry();
		entry.data = data;
		entry.length = length;
		entry.changeSet = c.changeSet;
		entry.version = c.changeSet.GetVersion();
		entry.node = this.usage.AddFirst(c.pos);

		this.entries.Add(c.pos, entry);
		this.totalBytes += data.Length;

		while(this.totalBytes > this.maxBytes && this.usage.Count > 1)
			Remove(this.usage.Last.Value);
	}

	public void Remove(ChunkPos pos){
		Entry entry;

		if(!this.entries.TryGetValue(pos, out entry))
			return;

		this.usage.Remove(entry.node);
		this.entries.Remove(pos);
		this.totalBytes -= entry.data.Length;
	}

	public void Clear(){
		this.entries.Clear();
		this.usage.Clear();
		this.totalBytes = 0;
	}
}
