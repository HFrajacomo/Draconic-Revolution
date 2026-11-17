using System;
using Unity.Collections;

/*
One bit per block column (x*chunkWidth+z) of a chunk, set where direct sunlight leaves the bottom of the chunk.
The chunk below receives direct sunlight only through the set columns.
Chunks are 16x16 columns, so 4 ulongs hold every column
*/
public struct SunExitMask : IEquatable<SunExitMask>{
	public static readonly SunExitMask ALL = new SunExitMask(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
	private static readonly int COLUMNS = Chunk.chunkWidth*Chunk.chunkWidth;

	private ulong a, b, c, d;


	private SunExitMask(ulong a, ulong b, ulong c, ulong d){
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
	}

	// Columns whose bottom voxel (y=0) is in direct sunlight (natural ShadowCode 2)
	public static SunExitMask FromShadowMap(byte[] shadowMap){
		SunExitMask mask = new SunExitMask();
		int column = 0;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int z=0; z < Chunk.chunkWidth; z++){
				if((shadowMap[x*Chunk.chunkWidth*Chunk.chunkDepth+z] & 0x0F) == 2)
					mask.Set(column);

				column++;
			}
		}

		return mask;
	}

	public bool Get(int column){
		ulong bit = 1UL << (column & 63);

		switch(column >> 6){
			case 0:
				return (this.a & bit) != 0;
			case 1:
				return (this.b & bit) != 0;
			case 2:
				return (this.c & bit) != 0;
			default:
				return (this.d & bit) != 0;
		}
	}

	public void CopyTo(NativeArray<bool> dest){
		for(int i=0; i < COLUMNS; i++)
			dest[i] = Get(i);
	}

	private void Set(int column){
		ulong bit = 1UL << (column & 63);

		switch(column >> 6){
			case 0:
				this.a |= bit;
				break;
			case 1:
				this.b |= bit;
				break;
			case 2:
				this.c |= bit;
				break;
			default:
				this.d |= bit;
				break;
		}
	}

	public bool Equals(SunExitMask other){
		return this.a == other.a && this.b == other.b && this.c == other.c && this.d == other.d;
	}

	public override bool Equals(object other){
		if(!(other is SunExitMask))
			return false;

		return Equals((SunExitMask)other);
	}

	public override int GetHashCode(){
		unchecked{
			return (this.a ^ (this.b * 31) ^ (this.c * 397) ^ (this.d * 7919)).GetHashCode();
		}
	}

	public static bool operator==(SunExitMask x, SunExitMask y){return x.Equals(y);}
	public static bool operator!=(SunExitMask x, SunExitMask y){return !x.Equals(y);}
}
