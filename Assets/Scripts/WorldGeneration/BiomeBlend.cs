using Unity.Collections;
using Unity.Mathematics;

// Biomes of the 3x3 chunks around a chunk, indexed by the offset (-1..1) to it
public struct BiomeNeighborhood{
	public byte xmzm, xmz, xmzp;
	public byte xzm, xz, xzp;
	public byte xpzm, xpz, xpzp;

	public byte Get(int dx, int dz){
		switch((dx+1)*3 + dz+1){
			case 0: return this.xmzm;
			case 1: return this.xmz;
			case 2: return this.xmzp;
			case 3: return this.xzm;
			case 4: return this.xz;
			case 5: return this.xzp;
			case 6: return this.xpzm;
			case 7: return this.xpz;
			default: return this.xpzp;
		}
	}

	// Whether every chunk around has the same biome as the center one
	public bool IsUniform(){
		byte b = this.xz;
		return this.xmzm == b && this.xmz == b && this.xmzp == b && this.xzm == b && this.xzp == b && this.xpzm == b && this.xpz == b && this.xpzp == b;
	}
}

/*
Biome of a block column, used to blend biome borders. Biomes are assigned per chunk, so their borders would be straight lines on the chunk grid.
	1. Warp: the column position is moved by a smooth, low frequency offset of the Blend Noise, so borders become wide curves.
	   The warp changes slower than the position (slope below 1), so it never folds into slivers or islands.
	2. Membership: the warped point weights the biomes of the 4 nearest chunk centers bilinearly, and takes the strongest one.
	   Between 2 biomes the border is the chunk edge, but where more biomes meet, corners become rounded curves instead of squares.
It only depends on world coordinates, the seed and chunk biomes, so neighbor chunks always agree, no matter their generation order.
WARP_AMPLITUDE must be at most half a chunk, so only the 3x3 chunks around the column's chunk are ever read
*/
public static class BiomeBlend{
	public static readonly float WARP_AMPLITUDE = 8f;
	// Perlin noise mostly stays within +-0.7, so the warp rarely reaches the clamp, which would make straight segments
	private static readonly float WARP_LOW_SCALE = 8f;
	private static readonly float WARP_HIGH_SCALE = 2.5f;

	// Decorrelates the samples taken from the same noise map
	private static readonly float2 OFFSET_X = new float2(17.31f, 113.7f);
	private static readonly float2 OFFSET_Z = new float2(151.9f, 41.23f);

	public static byte GetColumnBiome(int worldX, int worldZ, ChunkPos pos, in BiomeNeighborhood biomes, NativeArray<byte> blendNoise){
		if(biomes.IsUniform())
			return biomes.xz;

		float px = worldX + Warp(worldX, worldZ, OFFSET_X, blendNoise);
		float pz = worldZ + Warp(worldX, worldZ, OFFSET_Z, blendNoise);

		// Position in a grid whose nodes are the chunk centers
		float gx = px / Chunk.chunkWidth - 0.5f;
		float gz = pz / Chunk.chunkWidth - 0.5f;
		int x0 = (int)math.floor(gx);
		int z0 = (int)math.floor(gz);
		float fx = gx - x0;
		float fz = gz - z0;
		x0 -= pos.x;
		z0 -= pos.z;

		byte b00 = biomes.Get(math.clamp(x0, -1, 1), math.clamp(z0, -1, 1));
		byte b10 = biomes.Get(math.clamp(x0+1, -1, 1), math.clamp(z0, -1, 1));
		byte b01 = biomes.Get(math.clamp(x0, -1, 1), math.clamp(z0+1, -1, 1));
		byte b11 = biomes.Get(math.clamp(x0+1, -1, 1), math.clamp(z0+1, -1, 1));

		float w00 = (1f-fx)*(1f-fz);
		float w10 = fx*(1f-fz);
		float w01 = (1f-fx)*fz;
		float w11 = fx*fz;

		byte best = b00;
		float bestWeight = Membership(b00, b00, b10, b01, b11, w00, w10, w01, w11);

		Pick(b10, b00, b10, b01, b11, w00, w10, w01, w11, ref best, ref bestWeight);
		Pick(b01, b00, b10, b01, b11, w00, w10, w01, w11, ref best, ref bestWeight);
		Pick(b11, b00, b10, b01, b11, w00, w10, w01, w11, ref best, ref bestWeight);

		return best;
	}

	// Keeps the biome with the largest membership. Ties go to the lowest biome code, so every chunk resolves them the same way
	private static void Pick(byte candidate, byte b00, byte b10, byte b01, byte b11, float w00, float w10, float w01, float w11, ref byte best, ref float bestWeight){
		if(candidate == best)
			return;

		float weight = Membership(candidate, b00, b10, b01, b11, w00, w10, w01, w11);

		if(weight > bestWeight || (weight == bestWeight && candidate < best)){
			best = candidate;
			bestWeight = weight;
		}
	}

	private static float Membership(byte biome, byte b00, byte b10, byte b01, byte b11, float w00, float w10, float w01, float w11){
		float weight = 0f;

		if(b00 == biome) weight += w00;
		if(b10 == biome) weight += w10;
		if(b01 == biome) weight += w01;
		if(b11 == biome) weight += w11;

		return weight;
	}

	private static float Warp(int worldX, int worldZ, float2 offset, NativeArray<byte> blendNoise){
		float low = NoiseMaker.Noise2D(worldX*GenerationSeed.blendWarpStep1 + offset.x, worldZ*GenerationSeed.blendWarpStep1 + offset.y, blendNoise);
		float high = NoiseMaker.Noise2D(worldX*GenerationSeed.blendWarpStep2 + offset.y, worldZ*GenerationSeed.blendWarpStep2 + offset.x, blendNoise);

		return math.clamp(low*WARP_LOW_SCALE + high*WARP_HIGH_SCALE, -WARP_AMPLITUDE, WARP_AMPLITUDE);
	}
}
