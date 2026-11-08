/*
Layout of the padded neighborhood snapshot used by the Chunk Builder.
The chunk occupies the center of the grid and is surrounded by a 1-voxel border taken from its 26 neighbors.
Coordinates go from -1 to Chunk.chunkWidth (x and z) and from -1 to Chunk.chunkDepth (y)
*/
public static class PaddedChunk{
	public const int WIDTH = 18; // Chunk.chunkWidth + 2
	public const int HEIGHT = 258; // Chunk.chunkDepth + 2
	public const int SIZE = WIDTH*HEIGHT*WIDTH;
	public const int MAP_SIZE = WIDTH*WIDTH;

	public static int Index(int x, int y, int z){
		return ((x+1)*HEIGHT + (y+1))*WIDTH + (z+1);
	}

	public static int MapIndex(int x, int z){
		return (x+1)*WIDTH + (z+1);
	}
}
