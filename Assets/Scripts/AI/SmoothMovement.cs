using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
Smooths the movement of remote entities using Snapshot Interpolation
	Every received position is buffered with its sender timestamp and entities are rendered slightly in the past (SnapshotBuffer.INTERPOLATION_DELAY)
*/
public class SmoothMovement {
	private EntityHandler handler;
	private Dictionary<ulong, SnapshotBuffer> players = new Dictionary<ulong, SnapshotBuffer>();
	private Dictionary<ulong, SnapshotBuffer> items = new Dictionary<ulong, SnapshotBuffer>();
	private EntitySnapshot cachedSnapshot;

	public SmoothMovement(EntityHandler hand){
		this.handler = hand;
	}

	// Rot is (pitch, yaw, roll) as sent by the Server
	public bool AddPlayer(ulong id, uint timestamp, float3 pos, float3 rot){
		if(this.players.ContainsKey(id))
			return false;

		this.players.Add(id, new SnapshotBuffer(timestamp, new Vector3(pos.x, pos.y, pos.z), rot.y, rot.x, Time.unscaledTimeAsDouble));
		return true;
	}

	public bool AddItem(ulong id, uint timestamp, float3 pos){
		if(this.items.ContainsKey(id))
			return false;

		this.items.Add(id, new SnapshotBuffer(timestamp, new Vector3(pos.x, pos.y, pos.z), 0f, 0f, Time.unscaledTimeAsDouble));
		return true;
	}

	public bool Remove(EntityType type, ulong id){
		if(type == EntityType.PLAYER)
			return this.players.Remove(id);
		else if(type == EntityType.DROP)
			return this.items.Remove(id);
		return false;
	}

	// Buffers a new position received from the Server. Rot is (pitch, yaw, roll) for players and ignored for drops
	public void AddSnapshot(EntityType type, ulong id, uint timestamp, float3 pos, float3 rot){
		Vector3 posV = new Vector3(pos.x, pos.y, pos.z);

		if(type == EntityType.PLAYER){
			if(this.players.ContainsKey(id))
				this.players[id].Add(timestamp, posV, rot.y, rot.x, Time.unscaledTimeAsDouble);
		}
		else if(type == EntityType.DROP){
			if(this.items.ContainsKey(id))
				this.items[id].Add(timestamp, posV, 0f, 0f, Time.unscaledTimeAsDouble);
		}
	}

	// Moves all entities to their interpolated pose for the current frame
	public void MoveEntities(){
		double now = Time.unscaledTimeAsDouble;

		foreach(KeyValuePair<ulong, SnapshotBuffer> item in this.players){
			if(item.Value.Sample(now, out this.cachedSnapshot))
				this.handler.SetPlayerTransform(item.Key, this.cachedSnapshot.position, this.cachedSnapshot.yaw, this.cachedSnapshot.pitch);
		}

		foreach(KeyValuePair<ulong, SnapshotBuffer> item in this.items){
			if(item.Value.Sample(now, out this.cachedSnapshot))
				this.handler.SetItemPosition(item.Key, this.cachedSnapshot.position);
		}
	}
}
