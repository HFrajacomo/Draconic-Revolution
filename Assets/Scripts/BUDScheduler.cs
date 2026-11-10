using System;
using System.Collections.Generic;
using UnityEngine;

/*
Runs Block Updates (BUD) and chunk saves at a given tick of the in-game time
	Ticks are absolute and counted by TimeOfDay.GetBUDTick()
	Every tick that is due runs in order, so ticks skipped by long frames are caught up instead of lost
	Offsets scheduled while a tick runs count from that tick, so chained updates keep deterministic timings
*/
public class BUDScheduler : MonoBehaviour {
	public TimeOfDay schedulerTime;
	public ChunkLoader_Server loader;

	private Dictionary<long, TickBucket> buckets = new Dictionary<long, TickBucket>();
	private SortedSet<long> pendingTicks = new SortedSet<long>();
	private Stack<TickBucket> bucketPool = new Stack<TickBucket>();
	private HashSet<ChunkPos> toPropagate = new HashSet<ChunkPos>();
	private long lastTick;
	private long runningTick;
	private bool isRunning = false;

	private CastCoord cachedCoord;
	private ushort cachedCode;


	void Start(){
		this.lastTick = this.schedulerTime.GetBUDTick();
	}

	void Update(){
		long now = this.schedulerTime.GetBUDTick();

		// Saves the World Data every tick
		if(now != this.lastTick){
			if(this.loader.regionHandler != null)
				this.loader.regionHandler.SaveWorld();

			this.lastTick = now;
		}

		RunDueTicks(now);
		PropagateChanges();
	}

	// Runs every tick up to the current one, in order
	private void RunDueTicks(long now){
		TickBucket bucket;
		long tick;

		while(this.pendingTicks.Count > 0){
			tick = this.pendingTicks.Min;

			if(tick > now)
				break;

			bucket = this.buckets[tick];
			this.runningTick = tick;
			this.isRunning = true;

			RunSignals(bucket);
			RunSaves(bucket);

			this.isRunning = false;
			this.pendingTicks.Remove(tick);
			this.buckets.Remove(tick);
			ReleaseBucket(bucket);
		}
	}

	// BUDs scheduled to the same tick while it runs are appended to it and run as well
	private void RunSignals(TickBucket bucket){
		BUDSignal signal;

		for(int i=0; i < bucket.signals.Count; i++){
			signal = bucket.signals[i];
			Dispatch(signal);
			bucket.signalSet.Remove(signal);
		}
	}

	// Runs a BUD in the voxel it targets if it's still in the loaded area
	// Exceptions are logged so one failing BUD can't stop the Scheduler
	private void Dispatch(BUDSignal signal){
		ChunkPos pos;

		this.cachedCoord = new CastCoord(new Vector3(signal.x, signal.y, signal.z));
		pos = this.cachedCoord.GetChunkPos();

		if(!this.loader.Contains(pos))
			return;

		this.cachedCode = this.loader.GetChunk(pos).data.GetCell(this.cachedCoord.blockX, this.cachedCoord.blockY, this.cachedCoord.blockZ);

		try{
			if(this.cachedCode <= ushort.MaxValue/2)
				VoxelLoader.GetBlock(this.cachedCode).OnBlockUpdate(signal.type, signal.x, signal.y, signal.z, signal.budX, signal.budY, signal.budZ, signal.facing, this.loader);
			else
				VoxelLoader.GetObject(this.cachedCode).OnBlockUpdate(signal.type, signal.x, signal.y, signal.z, signal.budX, signal.budY, signal.budZ, signal.facing, this.loader);
		}
		catch(Exception e){
			Debug.LogException(e);
		}
	}

	private void RunSaves(TickBucket bucket){
		foreach(ChunkPos pos in bucket.saves){
			if(this.loader.Contains(pos))
				this.loader.regionHandler.SaveChunk(this.loader.GetChunk(pos));
		}
	}

	// Propagates the voxels changed in saved chunks to users
	private void PropagateChanges(){
		if(this.toPropagate.Count == 0)
			return;

		foreach(ChunkPos pos in this.toPropagate)
			this.loader.server.SendChunkChanges(pos);

		this.toPropagate.Clear();
	}

	// Schedules a BUD request in the system. Identical requests to the same tick are only run once
	public void ScheduleBUD(BUDSignal b, int tickOffset){
		TickBucket bucket = GetBucket(GetBaseTick() + tickOffset);

		if(bucket.signalSet.Add(b))
			bucket.signals.Add(b);
	}

	// Schedules a BUD request in the system in the current tick, even if an identical one was already scheduled
	public void ScheduleBUDNow(BUDSignal b){
		TickBucket bucket = GetBucket(GetBaseTick());

		bucket.signalSet.Add(b);
		bucket.signals.Add(b);
	}

	// Schedules a SaveChunk() operation to the next tick
	public void ScheduleSave(ChunkPos pos){
		GetBucket(GetBaseTick() + 1).saves.Add(pos);
	}

	// Schedules a Server.SendChunkChanges() operation to the end of the frame
	public void SchedulePropagation(ChunkPos pos){
		this.toPropagate.Add(pos);
	}

	// Offsets count from the tick that is running, or from the current tick otherwise
	private long GetBaseTick(){
		if(this.isRunning)
			return this.runningTick;
		return this.schedulerTime.GetBUDTick();
	}

	private TickBucket GetBucket(long tick){
		TickBucket bucket;

		if(this.buckets.TryGetValue(tick, out bucket))
			return bucket;

		if(this.bucketPool.Count > 0)
			bucket = this.bucketPool.Pop();
		else
			bucket = new TickBucket();

		this.buckets.Add(tick, bucket);
		this.pendingTicks.Add(tick);
		return bucket;
	}

	private void ReleaseBucket(TickBucket bucket){
		bucket.signals.Clear();
		bucket.signalSet.Clear();
		bucket.saves.Clear();
		this.bucketPool.Push(bucket);
	}


	// Everything scheduled to a single tick
	private class TickBucket{
		public List<BUDSignal> signals = new List<BUDSignal>();
		public HashSet<BUDSignal> signalSet = new HashSet<BUDSignal>(BUDSignalComparer.INSTANCE);
		public HashSet<ChunkPos> saves = new HashSet<ChunkPos>();
	}

	// Compares every field of BUDSignals without boxing them
	private class BUDSignalComparer : IEqualityComparer<BUDSignal>{
		public static readonly BUDSignalComparer INSTANCE = new BUDSignalComparer();

		public bool Equals(BUDSignal a, BUDSignal b){
			return a.type == b.type && a.x == b.x && a.y == b.y && a.z == b.z && a.budX == b.budX && a.budY == b.budY && a.budZ == b.budZ && a.facing == b.facing;
		}

		public int GetHashCode(BUDSignal b){
			unchecked{
				int hash = (int)b.type;
				hash = hash*31 + b.x;
				hash = hash*31 + b.y;
				hash = hash*31 + b.z;
				hash = hash*31 + b.budX;
				hash = hash*31 + b.budY;
				hash = hash*31 + b.budZ;
				hash = hash*31 + b.facing;
				return hash;
			}
		}
	}
}
