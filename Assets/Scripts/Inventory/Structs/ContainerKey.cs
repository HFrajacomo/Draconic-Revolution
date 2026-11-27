using System;
using UnityEngine;

/*
Identifies a container voxel by its chunk and its position inside the chunk
Written in messages as [ChunkPos 9][Local 2]
*/
public struct ContainerKey : IEquatable<ContainerKey>{
	public static readonly int SIZE = 11;

	public ChunkPos pos;
	public byte x;
	public byte y;
	public byte z;

	public ContainerKey(ChunkPos pos, int x, int y, int z){
		this.pos = pos;
		this.x = (byte)x;
		this.y = (byte)y;
		this.z = (byte)z;
	}

	public CastCoord GetCoord(){return new CastCoord(this.pos, this.x, this.y, this.z);}

	// Index of the voxel inside its chunk. Fits in 16 bits
	public int GetVoxelIndex(){return this.x + this.z*Chunk.chunkWidth + this.y*Chunk.chunkWidth*Chunk.chunkWidth;}

	// Absolute world position of the voxel
	public Vector3 GetWorldPosition(){
		CastCoord coord = GetCoord();
		return new Vector3(coord.GetWorldX(), coord.GetWorldY(), coord.GetWorldZ());
	}

	public void Write(byte[] data, int position){
		NetDecoder.WriteChunkPos(this.pos, data, position);
		NetDecoder.WriteLocalPos(this.x, this.y, this.z, data, position+9);
	}

	public static ContainerKey Read(byte[] data, int position){
		int x, y, z;

		NetDecoder.ReadLocalPos(data, position+9, out x, out y, out z);
		return new ContainerKey(NetDecoder.ReadChunkPos(data, position), x, y, z);
	}

	public bool Equals(ContainerKey other){
		return this.pos == other.pos && this.x == other.x && this.y == other.y && this.z == other.z;
	}

	public override bool Equals(System.Object a){
		if(!(a is ContainerKey))
			return false;

		return Equals((ContainerKey)a);
	}

	public override int GetHashCode(){
		unchecked{
			return this.pos.GetHashCode() * 31 + GetVoxelIndex();
		}
	}

	public static bool operator==(ContainerKey a, ContainerKey b){return a.Equals(b);}
	public static bool operator!=(ContainerKey a, ContainerKey b){return !a.Equals(b);}

	public override string ToString(){
		return $"{this.pos} [{this.x}, {this.y}, {this.z}]";
	}
}
