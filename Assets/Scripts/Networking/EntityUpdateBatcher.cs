using System.Collections.Generic;
using UnityEngine;

/*
Groups the movement of every entity that moved in a Server frame into a single ENTITYUPDATES message per client
	{CODE}[PlayerCount 2] | PlayerCount x [NetID 4][Pos 12][Pitch 2][Yaw 2][Timestamp 4]
	      [DropCount 2]   | DropCount x   [ID 4][Pos 12][Standing][Timestamp 4]
*/
public class EntityUpdateBatcher
{
	public static readonly int PLAYER_ENTRY_SIZE = 24;
	public static readonly int DROP_ENTRY_SIZE = 21;
	private static readonly int MAX_ENTRIES = 2048; // Per entity type in a single message

	private class Batch{
		public byte[] players = new byte[PLAYER_ENTRY_SIZE * 16];
		public int playerCount = 0;
		public byte[] drops = new byte[DROP_ENTRY_SIZE * 16];
		public int dropCount = 0;
	}

	private Dictionary<ulong, Batch> batches = new Dictionary<ulong, Batch>();
	private List<ulong> activeRecipients = new List<ulong>();
	private byte[] messageBuffer = new byte[1024];


	public void AddPlayer(Server server, ulong recipient, uint netID, float x, float y, float z, float pitch, float yaw, uint timestamp){
		Batch batch = GetBatch(recipient);
		int offset = batch.playerCount * PLAYER_ENTRY_SIZE;

		if(batch.players.Length < offset + PLAYER_ENTRY_SIZE)
			batch.players = Grow(batch.players, offset + PLAYER_ENTRY_SIZE);

		NetDecoder.WriteUint(netID, batch.players, offset);
		NetDecoder.WriteFloat3(x, y, z, batch.players, offset+4);
		NetDecoder.WriteAngle(pitch, batch.players, offset+16);
		NetDecoder.WriteAngle(yaw, batch.players, offset+18);
		NetDecoder.WriteUint(timestamp, batch.players, offset+20);
		batch.playerCount++;

		if(batch.playerCount >= MAX_ENTRIES)
			Send(server, recipient, batch);
	}

	public void AddDrop(Server server, ulong recipient, uint id, Vector3 pos, bool standing, uint timestamp){
		Batch batch = GetBatch(recipient);
		int offset = batch.dropCount * DROP_ENTRY_SIZE;

		if(batch.drops.Length < offset + DROP_ENTRY_SIZE)
			batch.drops = Grow(batch.drops, offset + DROP_ENTRY_SIZE);

		NetDecoder.WriteUint(id, batch.drops, offset);
		NetDecoder.WriteFloat3(pos, batch.drops, offset+4);
		NetDecoder.WriteBool(standing, batch.drops, offset+16);
		NetDecoder.WriteUint(timestamp, batch.drops, offset+17);
		batch.dropCount++;

		if(batch.dropCount >= MAX_ENTRIES)
			Send(server, recipient, batch);
	}

	// Sends every batch built this frame
	public void Flush(Server server){
		Batch batch;

		for(int i=0; i < this.activeRecipients.Count; i++){
			if(this.batches.TryGetValue(this.activeRecipients[i], out batch))
				Send(server, this.activeRecipients[i], batch);
		}

		this.activeRecipients.Clear();
	}

	// Forgets a client that disconnected
	public void RemoveRecipient(ulong recipient){
		this.batches.Remove(recipient);
	}

	private Batch GetBatch(ulong recipient){
		Batch batch;

		if(!this.batches.TryGetValue(recipient, out batch)){
			batch = new Batch();
			this.batches.Add(recipient, batch);
		}

		if(batch.playerCount == 0 && batch.dropCount == 0)
			this.activeRecipients.Add(recipient);

		return batch;
	}

	private void Send(Server server, ulong recipient, Batch batch){
		if(batch.playerCount == 0 && batch.dropCount == 0)
			return;

		int playerBytes = batch.playerCount * PLAYER_ENTRY_SIZE;
		int dropBytes = batch.dropCount * DROP_ENTRY_SIZE;
		int size = 5 + playerBytes + dropBytes;

		if(this.messageBuffer.Length < size)
			this.messageBuffer = Grow(this.messageBuffer, size);

		this.messageBuffer[0] = (byte)NetCode.ENTITYUPDATES;
		NetDecoder.WriteUshort((ushort)batch.playerCount, this.messageBuffer, 1);
		System.Buffer.BlockCopy(batch.players, 0, this.messageBuffer, 3, playerBytes);
		NetDecoder.WriteUshort((ushort)batch.dropCount, this.messageBuffer, 3 + playerBytes);
		System.Buffer.BlockCopy(batch.drops, 0, this.messageBuffer, 5 + playerBytes, dropBytes);

		server.Send(this.messageBuffer, size, recipient);

		batch.playerCount = 0;
		batch.dropCount = 0;
	}

	private static byte[] Grow(byte[] buffer, int minimumSize){
		int newSize = buffer.Length;

		while(newSize < minimumSize)
			newSize *= 2;

		byte[] output = new byte[newSize];
		System.Buffer.BlockCopy(buffer, 0, output, 0, buffer.Length);

		return output;
	}
}
