using System;

/*
Square window of chunks around a center chunk, on the center's layer and optionally on one vertical neighbor layer
Shared by Client and Server so both sides always agree on which chunks should be loaded
*/
public struct StreamWindow : IEquatable<StreamWindow>{
	public ChunkPos center;
	public int verticalLayer; // -1, 0 or 1
	public int radius;

	public StreamWindow(ChunkPos center, int verticalLayer, int radius){
		int layer = Math.Sign(verticalLayer);

		if(!StreamWindow.IsValidLayer(center.y + layer))
			layer = 0;

		this.center = center;
		this.verticalLayer = layer;
		this.radius = Math.Max(radius, 0);
	}

	public static bool IsValidLayer(int y){
		return y >= 0 && y <= Chunk.chunkMaxY;
	}

	public bool HasSecondLayer(){return this.verticalLayer != 0;}
	public int GetSecondLayer(){return this.center.y + this.verticalLayer;}
	public int GetSide(){return this.radius*2 + 1;}
	public int GetLayerCount(){return HasSecondLayer() ? 2 : 1;}
	public int Count(){return GetSide() * GetSide() * GetLayerCount();}

	public bool ContainsLayer(int y){
		if(y == this.center.y)
			return true;

		return HasSecondLayer() && y == GetSecondLayer();
	}

	public bool Contains(ChunkPos pos){
		if(Math.Abs(pos.x - this.center.x) > this.radius)
			return false;
		if(Math.Abs(pos.z - this.center.z) > this.radius)
			return false;

		return ContainsLayer(pos.y);
	}

	// Maps an index in [0, Count()) to a chunk of the window
	public ChunkPos GetByIndex(int index){
		int side = GetSide();
		int area = side*side;
		int y = this.center.y;

		if(index >= area){
			y = GetSecondLayer();
			index -= area;
		}

		return new ChunkPos(this.center.x - this.radius + (index % side), this.center.z - this.radius + (index / side), y);
	}

	public bool Equals(StreamWindow other){
		return this.center == other.center && this.verticalLayer == other.verticalLayer && this.radius == other.radius;
	}

	public override bool Equals(System.Object a){
		if(!(a is StreamWindow))
			return false;

		return Equals((StreamWindow)a);
	}

	public override int GetHashCode(){
		unchecked{
			return (this.center.GetHashCode() * 397) ^ (this.verticalLayer * 31) ^ this.radius;
		}
	}

	public static bool operator==(StreamWindow a, StreamWindow b){return a.Equals(b);}
	public static bool operator!=(StreamWindow a, StreamWindow b){return !a.Equals(b);}

	public override string ToString(){
		return "[" + this.center + " | layer " + this.verticalLayer + " | radius " + this.radius + "]";
	}
}
