using Unity.Collections;
using Unity.Mathematics;

// Terrain values of a block column. Biome selection reads the same values the terrain is built from, so biomes always match the terrain
public struct TerrainSample{
	public float baseValue;	// [-1, 1]: ocean -> coast -> inland
	public float erosion;			// [-1, 1]: flat -> rugged
	public float peaksValleys;		// [-1, 1]: valleys -> peaks, folded into long bands
	public float ridge;				// [0, 1]: 1 on the crests of ridged mountains
	public float height;			// Surface height, Hell floor or Core top
	public float secondary;			// Hell ceiling or Core bottom
	public float2 warped;			// Position after the base warp, used to bend the climate with the terrain
}

/*
Builds the terrain heights of the Surface, Hell and Core layers.
	1. Domain warp: a large, slow warp bends continents, erosion and climate, and a small, faster one bends ridges and valleys
	2. Fbm base and erosion pick the base height and how rugged the land is
	3. Peaks and Valleys folds a smooth field into long bands, and ridged noise draws sharp crests on the peak bands
	4. Nested splines combine them: the hills and valleys spline is chosen by erosion, and mountains only grow inland on rugged land
Only depends on world coordinates and the seed, so neighbor chunks always agree no matter their generation order
*/
public static class TerrainShape{
	// Decorrelate samples taken from the same permutation table
	private static readonly float2 OFFSET_BASE_WARP = new float2(13.71f, 57.19f);
	private static readonly float2 OFFSET_LOCAL_WARP = new float2(91.37f, 7.53f);
	private static readonly float2 OFFSET_RIDGE = new float2(43.11f, 129.7f);
	private static readonly float2 OFFSET_WIDE_RIDGE = new float2(97.53f, 11.29f);
	private static readonly float2 OFFSET_DETAIL = new float2(201.3f, 77.77f);
	private static readonly float2 OFFSET_HELL_WARP = new float2(151.9f, 233.1f);
	private static readonly float2 OFFSET_HELL_LOCAL_WARP = new float2(61.83f, 181.4f);
	private static readonly float2 OFFSET_HELL_CEILING = new float2(117.3f, 29.41f);
	private static readonly float2 OFFSET_CORE_WARP = new float2(211.7f, 97.31f);
	private static readonly float2 OFFSET_CORE_LOCAL_WARP = new float2(33.33f, 251.9f);
	private static readonly float2 OFFSET_CORE_RIDGE = new float2(173.1f, 141.4f);

	// Hell constants (same as the original Hell generation)
	private static readonly float HELL_CEILING_MARGIN = 8f;
	private static readonly float HELL_CEILING_BOOST = 8f;
	private static readonly float HELL_CEILING_DIVISOR = 15f;
	private static readonly float HELL_BEDROCK_HEIGHT = 30f;
	private static readonly float HELL_FLOOR_RELIEF_FADE = 130f;
	private static readonly float HELL_CEILING_LIFT = 6f;


	public static TerrainSample SampleSurface(float x, float z, NativeArray<byte> baseNoise, NativeArray<byte> erosionNoise, NativeArray<byte> peakNoise, NativeArray<byte> warpNoise){
		TerrainSample sample = new TerrainSample();

		float2 q = TerrainNoise.Warp(x, z, warpNoise, GenerationSeed.baseWarpStep, GenerationSeed.baseWarpAmplitude, 3, OFFSET_BASE_WARP);
		float2 r = TerrainNoise.Warp(q.x, q.y, warpNoise, GenerationSeed.localWarpStep, GenerationSeed.localWarpAmplitude, 2, OFFSET_LOCAL_WARP);

		sample.warped = q;
		sample.baseValue = TerrainNoise.Spread(TerrainNoise.Fbm(q.x*GenerationSeed.baseStep, q.y*GenerationSeed.baseStep, baseNoise, 5), GenerationSeed.baseSpread);
		sample.erosion = TerrainNoise.Spread(TerrainNoise.Fbm(q.x*GenerationSeed.erosionStep, q.y*GenerationSeed.erosionStep, erosionNoise, 4), GenerationSeed.erosionSpread);
		sample.peaksValleys = TerrainNoise.PeaksValleys(TerrainNoise.Spread(TerrainNoise.Fbm(r.x*GenerationSeed.weirdnessStep, r.y*GenerationSeed.weirdnessStep, peakNoise, 3), GenerationSeed.weirdnessSpread),
			TerrainNoise.Spline(TerrainSpline.PEAKS_VALLEYS_SHARPNESS, sample.erosion));
		sample.ridge = MountainRidge(r, TerrainNoise.Spline(TerrainSpline.MOUNTAIN_SHARPNESS, sample.erosion), peakNoise);

		float inland = math.smoothstep(GenerationSeed.inlandStart, GenerationSeed.inlandEnd, sample.baseValue);
		float mountainInland = math.smoothstep(GenerationSeed.mountainInlandStart, GenerationSeed.mountainInlandEnd, sample.baseValue);
		float mountainBand = math.smoothstep(GenerationSeed.mountainBandStart, GenerationSeed.mountainBandEnd, sample.peaksValleys);

		float hills = TerrainNoise.PeaksValleysOffset(sample.peaksValleys, sample.erosion) * inland;
		float mountains = TerrainNoise.Spline(TerrainSpline.MOUNTAIN_AMPLITUDE, sample.erosion) * mountainInland * mountainBand * Crest(sample.ridge);
		float detail = math.lerp(GenerationSeed.detailAmplitudeMin, GenerationSeed.detailAmplitudeMax, (sample.erosion+1f)*0.5f)
			* TerrainNoise.Fbm(r.x*GenerationSeed.detailStep + OFFSET_DETAIL.x, r.y*GenerationSeed.detailStep + OFFSET_DETAIL.y, erosionNoise, 3);

		sample.height = SoftCap(TerrainNoise.Spline(TerrainSpline.BASE, sample.baseValue) + hills + mountains + detail);

		return sample;
	}

	// Hell keeps its original floor/ceiling splines and erosion multiplier, but its relief comes from warped ridged noise instead of round bumps
	public static TerrainSample SampleHell(float x, float z, NativeArray<byte> baseNoise, NativeArray<byte> erosionNoise, NativeArray<byte> peakNoise, NativeArray<byte> temperatureNoise, NativeArray<byte> warpNoise){
		TerrainSample sample = new TerrainSample();

		float2 q = TerrainNoise.Warp(x, z, warpNoise, GenerationSeed.hellWarpStep, GenerationSeed.hellWarpAmplitude, 3, OFFSET_HELL_WARP);
		float2 r = TerrainNoise.Warp(q.x, q.y, warpNoise, GenerationSeed.hellLocalWarpStep, GenerationSeed.hellLocalWarpAmplitude, 2, OFFSET_HELL_LOCAL_WARP);

		sample.warped = q;
		sample.baseValue = TerrainNoise.Spread(TerrainNoise.Fbm(q.x*GenerationSeed.hellBaseStep, q.y*GenerationSeed.hellBaseStep, baseNoise, 5), GenerationSeed.baseSpread);
		sample.erosion = TerrainNoise.Spread(TerrainNoise.Fbm(q.x*GenerationSeed.erosionStep, q.y*GenerationSeed.erosionStep, erosionNoise, 4), GenerationSeed.erosionSpread);
		sample.ridge = TerrainNoise.Ridged(r.x*GenerationSeed.hellRidgeStep, r.y*GenerationSeed.hellRidgeStep, peakNoise, 5);
		sample.peaksValleys = sample.ridge*2f - 1f;

		float ceilingRidge = TerrainNoise.Ridged(r.x*GenerationSeed.hellCeilingRidgeStep + OFFSET_HELL_CEILING.x, r.y*GenerationSeed.hellCeilingRidgeStep + OFFSET_HELL_CEILING.y, peakNoise, 4);
		float ceilingShape = TerrainNoise.Spread(TerrainNoise.Fbm(r.x*GenerationSeed.hellCeilingStep + OFFSET_HELL_CEILING.y, r.y*GenerationSeed.hellCeilingStep + OFFSET_HELL_CEILING.x, baseNoise, 3), GenerationSeed.hellCeilingSpread);
		// Spikiness keeps the original Temperature Noise steps
		float groundSpikiness = math.lerp(0.35f, 1f, (TerrainNoise.Perlin2D(r.x*GenerationSeed.temperatureNoiseStep3, r.y*GenerationSeed.temperatureNoiseStep4, temperatureNoise)+1f)*0.5f);
		float ceilingSpikiness = math.lerp(0.7f, 1f, (TerrainNoise.Perlin2D(r.x*GenerationSeed.temperatureNoiseStep5, r.y*GenerationSeed.temperatureNoiseStep6, temperatureNoise)+1f)*0.5f);

		float erosionMultiplier = NoiseMaker.FindSplineHeight(sample.erosion, NoiseMap.EROSION, ChunkDepthID.HELL);
		float floor = NoiseMaker.FindSplineHeight(sample.baseValue, NoiseMap.BASE, ChunkDepthID.HELL);

		// Ridges rise above the floor and the ground between them sinks a little. Deep Cliffs (floor 0) get no relief, so they stay open
		float relief = TerrainNoise.Spline(TerrainSpline.HELL_RELIEF, sample.erosion) * groundSpikiness * (Crest(sample.ridge)*1.4f - 0.4f) * math.saturate(floor/HELL_FLOOR_RELIEF_FADE);
		float hang = TerrainNoise.Spline(TerrainSpline.HELL_CEILING, sample.erosion) * ceilingSpikiness * Crest(ceilingRidge) - HELL_CEILING_LIFT;

		sample.height = math.ceil(math.clamp((floor + relief) * erosionMultiplier, 0f, Chunk.chunkDepth));
		if(sample.height <= HELL_BEDROCK_HEIGHT)
			sample.height = 0f;

		float ceilingDrop = (hang + HELL_CEILING_BOOST + NoiseMaker.FindSplineHeight(ceilingShape, NoiseMap.BASE, ChunkDepthID.HELL)/HELL_CEILING_DIVISOR) * erosionMultiplier;
		if(ceilingDrop <= 0f)
			ceilingDrop = 1f;

		sample.secondary = math.ceil(Chunk.chunkDepth - HELL_CEILING_MARGIN - ceilingDrop);

		return sample;
	}

	// Core keeps its original top/bottom splines and erosion pits, with warped shapes and ridges hanging from its bottom
	public static TerrainSample SampleCore(float x, float z, NativeArray<byte> baseNoise, NativeArray<byte> erosionNoise, NativeArray<byte> peakNoise, NativeArray<byte> warpNoise){
		TerrainSample sample = new TerrainSample();

		float2 q = TerrainNoise.Warp(x, z, warpNoise, GenerationSeed.coreWarpStep, GenerationSeed.coreWarpAmplitude, 3, OFFSET_CORE_WARP);
		float2 r = TerrainNoise.Warp(q.x, q.y, warpNoise, GenerationSeed.coreLocalWarpStep, GenerationSeed.coreLocalWarpAmplitude, 2, OFFSET_CORE_LOCAL_WARP);

		sample.warped = q;
		sample.baseValue = TerrainNoise.Spread(TerrainNoise.Fbm(q.x*GenerationSeed.coreBaseStep, q.y*GenerationSeed.coreBaseStep, baseNoise, 4), GenerationSeed.erosionSpread);
		sample.erosion = TerrainNoise.Spread(TerrainNoise.Fbm(q.x*GenerationSeed.erosionStep, q.y*GenerationSeed.erosionStep, erosionNoise, 4), GenerationSeed.erosionSpread);
		sample.ridge = TerrainNoise.Ridged(r.x*GenerationSeed.coreRidgeStep + OFFSET_CORE_RIDGE.x, r.y*GenerationSeed.coreRidgeStep + OFFSET_CORE_RIDGE.y, peakNoise, 4);
		sample.peaksValleys = sample.ridge*2f - 1f;

		float bottomShape = TerrainNoise.Spread(TerrainNoise.Fbm(r.x*GenerationSeed.coreBottomStep, r.y*GenerationSeed.coreBottomStep, peakNoise, 4), GenerationSeed.erosionSpread);
		float detail = GenerationSeed.coreDetailAmplitude * TerrainNoise.Fbm(r.x*GenerationSeed.coreDetailStep + OFFSET_CORE_RIDGE.y, r.y*GenerationSeed.coreDetailStep + OFFSET_CORE_RIDGE.x, baseNoise, 2);
		float erosionMultiplier = NoiseMaker.FindSplineHeight(sample.erosion, NoiseMap.EROSION, ChunkDepthID.CORE);

		sample.height = math.ceil(NoiseMaker.FindSplineHeight(sample.baseValue, NoiseMap.BASE, ChunkDepthID.CORE) * erosionMultiplier + detail);
		sample.secondary = math.ceil(math.max(1f, NoiseMaker.FindSplineHeight(bottomShape, NoiseMap.PEAK, ChunkDepthID.CORE) - TerrainNoise.Spline(TerrainSpline.CORE_HANG, sample.erosion) * Crest(sample.ridge)));

		return sample;
	}

	// Blends wide mountains (gentle slopes) into narrow ones (steep slopes) by sharpness. Each ridge field is only sampled when it is used
	private static float MountainRidge(float2 r, float sharpness, NativeArray<byte> peakNoise){
		float wide = 0f;
		float narrow = 0f;

		if(sharpness < 1f)
			wide = TerrainNoise.Ridged(r.x*GenerationSeed.wideRidgeStep + OFFSET_WIDE_RIDGE.x, r.y*GenerationSeed.wideRidgeStep + OFFSET_WIDE_RIDGE.y, peakNoise, 4);
		if(sharpness > 0f)
			narrow = TerrainNoise.Ridged(r.x*GenerationSeed.ridgeStep + OFFSET_RIDGE.x, r.y*GenerationSeed.ridgeStep + OFFSET_RIDGE.y, peakNoise, 5);

		return math.lerp(wide, narrow, math.saturate(sharpness));
	}

	// Turns ridged noise into a mountain profile: 0 on the valley floors, sharp at the crests
	private static float Crest(float ridge){
		float c = math.saturate((ridge - GenerationSeed.ridgeFloor)/(1f - GenerationSeed.ridgeFloor));
		return c*c;
	}

	// Smoothly compresses heights above the soft cap towards the max height
	private static float SoftCap(float height){
		if(height > GenerationSeed.surfaceSoftCap){
			float range = GenerationSeed.surfaceMaxHeight - GenerationSeed.surfaceSoftCap;
			height = GenerationSeed.surfaceSoftCap + range*math.tanh((height - GenerationSeed.surfaceSoftCap)/range);
		}

		return math.clamp(height, 1f, GenerationSeed.surfaceMaxHeight);
	}
}
