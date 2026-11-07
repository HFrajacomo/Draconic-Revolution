using UnityEngine;

/*
Timestamped pose buffer of a single remote entity used for Snapshot Interpolation
	Entities are rendered INTERPOLATION_DELAY seconds in the past, interpolating between the two received snapshots around that time
	When no newer snapshot exists, the newest one is held instead of extrapolated
*/
public class SnapshotBuffer {
	private EntitySnapshot[] snapshots;
	private int head; // Index of the oldest snapshot
	private int count;

	// Sender clock
	private uint lastRawTimestamp;
	private double remoteTime;

	// Estimated (localTime - remoteTime) of the fastest packets received
	private double clockOffset;

	private EntitySnapshot lastApplied;
	private bool hasApplied;

	private static readonly int CAPACITY = 32;
	// How far in the past entities are rendered. Must cover the network jitter. Raise it if entities still stutter on bad connections
	public static readonly double INTERPOLATION_DELAY = TimeOfDay.timeRate * 2.5;
	// Expected interval between snapshots of a moving entity
	private static readonly double SEND_INTERVAL = TimeOfDay.timeRate;
	// Snapshots further apart than this mean the entity was idle (nothing is sent while idle)
	private static readonly double MAX_SNAPSHOT_GAP = TimeOfDay.timeRate * 4;
	// Moving further than this between two snapshots snaps the entity instead of interpolating
	private static readonly float TELEPORT_DISTANCE = 8f;
	// Fraction of the difference that the clock offset rises per received snapshot when latency increases
	private static readonly double OFFSET_RISE_RATE = 0.02;
	// Timestamps going this far backwards mean the sender's clock was reset
	private static readonly int RESYNC_THRESHOLD_MS = 1000;


	public SnapshotBuffer(uint timestamp, Vector3 position, float yaw, float pitch, double localTime){
		this.snapshots = new EntitySnapshot[CAPACITY];
		this.lastRawTimestamp = timestamp;
		this.remoteTime = 0;
		this.clockOffset = localTime;
		this.hasApplied = false;

		Push(new EntitySnapshot(this.remoteTime, position, yaw, pitch));
	}

	// Adds a received snapshot. Returns false if it was discarded
	public bool Add(uint timestamp, Vector3 position, float yaw, float pitch, double localTime){
		int delta = (int)(timestamp - this.lastRawTimestamp);

		if(delta <= 0){
			// Duplicated or resent snapshot
			if(delta > -RESYNC_THRESHOLD_MS)
				return false;

			this.lastRawTimestamp = timestamp;
			this.remoteTime += SEND_INTERVAL;
			this.clockOffset = localTime - this.remoteTime;
			Reset(new EntitySnapshot(this.remoteTime, position, yaw, pitch));
			return true;
		}

		this.lastRawTimestamp = timestamp;
		this.remoteTime += delta / 1000.0;
		UpdateClockOffset(localTime - this.remoteTime);

		EntitySnapshot newest = Get(this.count-1);
		EntitySnapshot snapshot = new EntitySnapshot(this.remoteTime, position, yaw, pitch);

		if((position - newest.position).sqrMagnitude > TELEPORT_DISTANCE*TELEPORT_DISTANCE){
			Reset(snapshot);
			return true;
		}

		// Entity was idle: makes it start moving from its last pose instead of crawling through the whole gap
		if(this.remoteTime - newest.time > MAX_SNAPSHOT_GAP)
			Push(new EntitySnapshot(this.remoteTime - SEND_INTERVAL, newest.position, newest.yaw, newest.pitch));

		Push(snapshot);
		return true;
	}

	/*
	Calculates the pose to be rendered at the current local time
		Returns false if the pose didn't change since the last applied one
	*/
	public bool Sample(double localTime, out EntitySnapshot result){
		double renderTime = localTime - this.clockOffset - INTERPOLATION_DELAY;

		// Discards snapshots older than the pair surrounding renderTime
		while(this.count >= 2 && Get(1).time <= renderTime)
			PopOldest();

		EntitySnapshot a = Get(0);

		if(this.count == 1 || renderTime <= a.time){
			result = a;
		}
		else{
			EntitySnapshot b = Get(1);
			float t = Mathf.Clamp01((float)((renderTime - a.time) / (b.time - a.time)));

			result = new EntitySnapshot(renderTime, Vector3.Lerp(a.position, b.position, t), Mathf.LerpAngle(a.yaw, b.yaw, t), Mathf.LerpAngle(a.pitch, b.pitch, t));
		}

		if(this.hasApplied && result.PoseEquals(this.lastApplied))
			return false;

		this.lastApplied = result;
		this.hasApplied = true;
		return true;
	}

	/*
	Lower samples mean faster packets and are followed immediately
	Higher samples are followed slowly to adapt to latency increase and clock drift without reacting to jitter
	*/
	private void UpdateClockOffset(double sample){
		if(sample < this.clockOffset)
			this.clockOffset = sample;
		else
			this.clockOffset += (sample - this.clockOffset) * OFFSET_RISE_RATE;
	}

	private EntitySnapshot Get(int index){
		return this.snapshots[(this.head + index) % CAPACITY];
	}

	private void Push(EntitySnapshot snapshot){
		if(this.count == CAPACITY)
			PopOldest();

		this.snapshots[(this.head + this.count) % CAPACITY] = snapshot;
		this.count++;
	}

	private void PopOldest(){
		this.head = (this.head + 1) % CAPACITY;
		this.count--;
	}

	private void Reset(EntitySnapshot snapshot){
		this.head = 0;
		this.count = 0;
		Push(snapshot);
	}
}
