using System.Collections.Generic;

/*
Pool of full chunk metadata arrays, so chunks that stop being uniform don't allocate new arrays when streaming in and out
*/
public static class MetadataPool{
	public static readonly int ARRAY_SIZE = Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth;
	private static readonly int MAX_POOLED = 256;

	private static Stack<ushort[]> pool = new Stack<ushort[]>();

	public static ushort[] Rent(){
		if(pool.Count > 0)
			return pool.Pop();

		return new ushort[ARRAY_SIZE];
	}

	public static void Return(ushort[] array){
		if(array == null || array.Length != ARRAY_SIZE)
			return;
		if(pool.Count >= MAX_POOLED)
			return;

		pool.Push(array);
	}

	public static void Clear(){
		pool.Clear();
	}
}
