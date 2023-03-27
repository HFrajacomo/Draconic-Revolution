using UnityEngine;

public struct ChunkPos{
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

	public float DistanceFrom(ChunkPos otherPos){
		float bonus;
		int xDiff, zDiff, yDiff;
		int maximizedDistance;

		xDiff = Mathf.Abs(otherPos.x - this.x);
		yDiff = Mathf.Abs(otherPos.y - this.y);
		zDiff = Mathf.Abs(otherPos.z - this.z);

		maximizedDistance = Mathf.Max(xDiff, zDiff);

		if(maximizedDistance > World.renderDistance){
			return 999f;
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
		return this.x ^ this.z;
	}

	public override bool Equals(System.Object a){
		if(a == null)
			return false;

		ChunkPos item = (ChunkPos)a;
		return this == item;
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