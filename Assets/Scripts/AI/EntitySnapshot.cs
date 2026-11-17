using UnityEngine;

// A single networked pose of an entity at a given (unwrapped) sender time in seconds
public struct EntitySnapshot {
	public double time;
	public Vector3 position;
	public float yaw;
	public float pitch;

	public EntitySnapshot(double time, Vector3 position, float yaw, float pitch){
		this.time = time;
		this.position = position;
		this.yaw = yaw;
		this.pitch = pitch;
	}

	public bool PoseEquals(EntitySnapshot other){
		return this.position == other.position && this.yaw == other.yaw && this.pitch == other.pitch;
	}

	public override string ToString(){
		return "Time: " + this.time + "   Pos: " + this.position + "   Yaw: " + this.yaw + "   Pitch: " + this.pitch;
	}
}
