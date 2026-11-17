using System;
using UnityEngine;

public struct ChunkPos : IEquatable<ChunkPos>{
	public int x;
	public int z;
	public byte y;

	public ChunkPos(int a, int b, ChunkDepthID y){
		this.x = a;
		this.z = b;
		this.y = (byte)y;
	}

	public ChunkPos(int a, int b, byte y){
		this.x = a;
		this.z = b;
		this.y = y;
	}

	public ChunkPos(int a, int b, int y){
		this.x = a;
		this.z = b;
		this.y = (byte)y;
	}

	// edgeLimit is only used by EDGE_LIMITING: chunks farther than it (in Chebyshev distance) are sent to the lowest priority
	public int DistanceFrom(ChunkPos pos, DistanceMetric metric, int edgeLimit=0){
		switch(metric){
			case DistanceMetric.MANHATTAN:
				return DistanceManhattan(pos);
			case DistanceMetric.EDGE_LIMITING:
				return DistanceEdgeLimiting(pos, edgeLimit);
			case DistanceMetric.STREAMING:
				return DistanceStreaming(pos);
			default:
				return DistanceManhattan(pos);
		}
	}

	// Chebyshev rings, where the vertical layer of a ring comes right after the main layer of the same ring
	public int DistanceStreaming(ChunkPos otherPos){
		int xDiff = Mathf.Abs(otherPos.x - this.x);
		int zDiff = Mathf.Abs(otherPos.z - this.z);
		int yDiff = Mathf.Abs(otherPos.y - this.y);

		return Mathf.Max(xDiff, zDiff)*2 + yDiff;
	}

	public int DistanceManhattan(ChunkPos otherPos){
		int xDiff, zDiff, yDiff;

		xDiff = Mathf.Abs(otherPos.x - this.x);
		yDiff = Mathf.Abs(otherPos.y - this.y);
		zDiff = Mathf.Abs(otherPos.z - this.z);

		return xDiff + yDiff + zDiff;
	}

	public int DistanceEdgeLimiting(ChunkPos otherPos, int edgeLimit){
		int xDiff, zDiff, yDiff;
		int maximizedDistance;

		xDiff = Mathf.Abs(otherPos.x - this.x);
		yDiff = Mathf.Abs(otherPos.y - this.y);
		zDiff = Mathf.Abs(otherPos.z - this.z);

		maximizedDistance = Mathf.Max(xDiff, zDiff);

		if(maximizedDistance > edgeLimit){
			return (edgeLimit*5)-1;
		}
		else{
			return xDiff + yDiff + zDiff;
		}
	}

	public override string ToString(){
		return "(" + this.x + ", " + this.z + ", " + ((ChunkDepthID)this.y).ToString()[0] + ")";
	}

	public static bool operator==(ChunkPos a, ChunkPos b){
		if(a.x == b.x && a.z == b.z && a.y == b.y)
			return true;
		return false;
	}

	public static bool operator!=(ChunkPos a, ChunkPos b){
		if(a.x == b.x && a.z == b.z && a.y == b.y)
			return false;
		return true;
	}

	public override int GetHashCode(){
		unchecked{
			int hash = this.x * 73856093;
			hash ^= this.z * 19349663;
			hash ^= this.y * 83492791;
			return hash;
		}
	}

	public bool Equals(ChunkPos other){
		return this.x == other.x && this.z == other.z && this.y == other.y;
	}

	public override bool Equals(System.Object a){
		if(!(a is ChunkPos))
			return false;

		return Equals((ChunkPos)a);
	}

	/*
	Returns the direction the player must have moved to find a new chunk
	Used after ChunkPos - ChunkPos
	0 = East
	1 = South
	2 = West
	3 = North

	4 = Northeast
	5 = Southeast
	6 = Southwest
	7 = Northwest
	*/
	public int dir(){
		if(this.x == 1 && this.z == 1){
			return 4;
		}
		if(this.x == 1 && this.z == -1){
			return 5;
		}
		if(this.x == -1 && this.z == -1){
			return 6;
		}
		if(this.x == -1 && this.z == 1){
			return 7;
		}
		if(this.x == 1){
			return 0;
		}
		if(this.x == -1){
			return 2;
		}
		if(this.z == 1){
			return 3;
		}
		if(this.z == -1){
			return 1;
		}
		return -1;
	}


	public static ChunkPos operator-(ChunkPos a, ChunkPos b){
		return new ChunkPos(a.x - b.x, a.z - b.z, a.y);
	}
}

public enum DistanceMetric : byte{
	MANHATTAN,
	EDGE_LIMITING,
	STREAMING
}