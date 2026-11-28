using Unity.Collections;
using Unity.Mathematics;

/*
Noise primitives of the Surface, Hell and Core terrain (see TerrainShape).
	- Perlin2D uses 16 gradient directions instead of the 4 diagonals of NoiseMaker.Noise2D, removing the 45 degree bias
	- Fbm and Ridged rotate and shift every octave, so octaves never share grid axes and layers don't line up into round blobs
	- Spline is a monotone cubic, so it never overshoots its knots
Everything is static and allocation free, so it gives the same results in Burst jobs and on the main thread (biome selection)
*/
public static class TerrainNoise{
	// 16 unit gradients scaled by sqrt(2), so Perlin2D keeps a [-1, 1] range
	private static readonly float[] GRAD_X = new float[]{1.41421f, 1.30656f, 1f, 0.54120f, 0f, -0.54120f, -1f, -1.30656f, -1.41421f, -1.30656f, -1f, -0.54120f, 0f, 0.54120f, 1f, 1.30656f};
	private static readonly float[] GRAD_Y = new float[]{0f, 0.54120f, 1f, 1.30656f, 1.41421f, 1.30656f, 1f, 0.54120f, 0f, -0.54120f, -1f, -1.30656f, -1.41421f, -1.30656f, -1f, -0.54120f};

	// Octave transform: rotation by a 3-4-5 triangle angle (~36.87 degrees), doubled frequency and a shift
	private static readonly float OCTAVE_COS = 0.8f;
	private static readonly float OCTAVE_SIN = 0.6f;
	private static readonly float LACUNARITY = 2f;
	private static readonly float GAIN = 0.5f;
	private static readonly float OCTAVE_SHIFT_X = 17.13f;
	private static readonly float OCTAVE_SHIFT_Y = 31.71f;

	// How fast a ridge fades the octaves above it, making valleys smooth and crests detailed
	private static readonly float RIDGE_WEIGHT_GAIN = 2f;

	// Second warp component samples the same table far away, so both components are uncorrelated
	private static readonly float2 WARP_SECOND_OFFSET = new float2(71.37f, 19.73f);

	// Splines whose ID is at least this are rows of the nested Peaks and Valleys spline
	private static readonly int PV_ROW_ID = 100;


	public static float Perlin2D(float x, float y, NativeArray<byte> perm){
		float fx = math.floor(x);
		float fy = math.floor(y);
		int X = (int)fx & 0xff;
		int Y = (int)fy & 0xff;
		x -= fx;
		y -= fy;

		float u = Fade(x);
		float v = Fade(y);

		int A = perm[X] + Y;
		int B = perm[X+1] + Y;

		return math.lerp(math.lerp(Grad(perm[A & 0xff], x, y), Grad(perm[B & 0xff], x-1f, y), u),
						 math.lerp(Grad(perm[(A+1) & 0xff], x, y-1f), Grad(perm[(B+1) & 0xff], x-1f, y-1f), u), v);
	}

	// Fractal Brownian Motion: octaves of halving weight and doubling frequency. Returns roughly [-1, 1]
	public static float Fbm(float x, float y, NativeArray<byte> perm, int octaves){
		float sum = 0f;
		float amplitude = 1f;
		float norm = 0f;

		for(int i=0; i < octaves; i++){
			sum += Perlin2D(x, y, perm) * amplitude;
			norm += amplitude;
			amplitude *= GAIN;
			NextOctave(ref x, ref y);
		}

		return sum / norm;
	}

	// Ridged multifractal: sharp continuous crests where the noise crosses zero. Returns [0, 1], 1 at the crests
	public static float Ridged(float x, float y, NativeArray<byte> perm, int octaves){
		float sum = 0f;
		float amplitude = 1f;
		float norm = 0f;
		float weight = 1f;
		float signal;

		for(int i=0; i < octaves; i++){
			signal = 1f - math.abs(Perlin2D(x, y, perm));
			signal *= signal * weight;
			weight = math.saturate(signal * RIDGE_WEIGHT_GAIN);

			sum += signal * amplitude;
			norm += amplitude;
			amplitude *= GAIN;
			NextOctave(ref x, ref y);
		}

		return sum / norm;
	}

	// Folds a smooth field into long bands: 1 on peaks (|w| = 2/3), -1 on valleys (w = 0 and |w| = 1)
	// sharpness above 1 widens the flat peaks and valley floors, and the slope between them becomes at most sharpness times steeper.
	// Unlike a power below 1, the slope stays bounded, so hillsides never turn into cliffs
	public static float PeaksValleys(float w, float sharpness){
		float pv = 1f - math.abs(3f*math.abs(w) - 2f);
		return math.sign(pv) * (1f - math.pow(1f - math.abs(pv), sharpness));
	}

	// Sigmoid approximation of the Normal CDF. Spreads the bell shaped Fbm values back to a roughly uniform [-1, 1]
	public static float Spread(float value, float gain){
		return 2f/(1f + math.exp(-value*gain)) - 1f;
	}

	// Moves a position by a smooth Fbm offset of up to about the amplitude, in blocks
	// amplitude*frequency must stay low (~0.15), or the warp folds and creates slivers
	public static float2 Warp(float x, float y, NativeArray<byte> perm, float frequency, float amplitude, int octaves, float2 offset){
		float sx = x*frequency + offset.x;
		float sy = y*frequency + offset.y;

		return new float2(x + Fbm(sx, sy, perm, octaves)*amplitude,
						  y + Fbm(sx + WARP_SECOND_OFFSET.x, sy + WARP_SECOND_OFFSET.y, perm, octaves)*amplitude);
	}

	public static float Spline(TerrainSpline spline, float value){
		return Evaluate((int)spline, value);
	}

	// Nested spline: every Erosion key has its own Peaks and Valleys spline, and the result blends the two rows around the Erosion value
	public static float PeaksValleysOffset(float peaksValleys, float erosion){
		int rows = GenerationSeed.peaksValleysErosionKeys.Length;

		if(erosion <= GenerationSeed.peaksValleysErosionKeys[0])
			return Evaluate(PV_ROW_ID, peaksValleys);
		if(erosion >= GenerationSeed.peaksValleysErosionKeys[rows-1])
			return Evaluate(PV_ROW_ID + rows-1, peaksValleys);

		int row = 0;
		while(row < rows-2 && erosion > GenerationSeed.peaksValleysErosionKeys[row+1])
			row++;

		float t = (erosion - GenerationSeed.peaksValleysErosionKeys[row])/(GenerationSeed.peaksValleysErosionKeys[row+1] - GenerationSeed.peaksValleysErosionKeys[row]);

		return math.lerp(Evaluate(PV_ROW_ID + row, peaksValleys), Evaluate(PV_ROW_ID + row+1, peaksValleys), t);
	}

	// Monotone cubic Hermite interpolation of the knots of a spline. Values outside the knots are clamped
	private static float Evaluate(int id, float value){
		int count = KnotCount(id);

		if(value <= KnotX(id, 0))
			return KnotY(id, 0);
		if(value >= KnotX(id, count-1))
			return KnotY(id, count-1);

		int i = 0;
		while(i < count-2 && value > KnotX(id, i+1))
			i++;

		float x0 = KnotX(id, i);
		float x1 = KnotX(id, i+1);
		float y0 = KnotY(id, i);
		float y1 = KnotY(id, i+1);
		float h = x1 - x0;
		float slope = (y1 - y0)/h;

		float m0 = slope;
		float m1 = slope;

		if(i > 0)
			m0 = Tangent((y0 - KnotY(id, i-1))/(x0 - KnotX(id, i-1)), slope);
		if(i+2 < count)
			m1 = Tangent(slope, (KnotY(id, i+2) - y1)/(KnotX(id, i+2) - x1));

		float t = (value - x0)/h;
		float t2 = t*t;
		float t3 = t2*t;

		return (2f*t3 - 3f*t2 + 1f)*y0 + (t3 - 2f*t2 + t)*h*m0 + (-2f*t3 + 3f*t2)*y1 + (t3 - t2)*h*m1;
	}

	// Harmonic mean of the slopes around a knot (Fritsch-Butland), which keeps the curve monotone between knots
	private static float Tangent(float slopeBefore, float slopeAfter){
		if(slopeBefore*slopeAfter <= 0f)
			return 0f;

		return 2f*slopeBefore*slopeAfter/(slopeBefore + slopeAfter);
	}

	private static int KnotCount(int id){
		switch(id){
			case (int)TerrainSpline.BASE:
				return GenerationSeed.baseHeightSplineX.Length;
			case (int)TerrainSpline.MOUNTAIN_AMPLITUDE:
				return GenerationSeed.mountainAmplitudeSplineX.Length;
			case (int)TerrainSpline.PEAKS_VALLEYS_SHARPNESS:
				return GenerationSeed.peaksValleysSharpnessSplineX.Length;
			case (int)TerrainSpline.MOUNTAIN_SHARPNESS:
				return GenerationSeed.mountainSharpnessSplineX.Length;
			case (int)TerrainSpline.HELL_RELIEF:
				return GenerationSeed.hellReliefSplineX.Length;
			case (int)TerrainSpline.HELL_CEILING:
				return GenerationSeed.hellCeilingSplineX.Length;
			case (int)TerrainSpline.CORE_HANG:
				return GenerationSeed.coreHangSplineX.Length;
			default:
				return GenerationSeed.peaksValleysSplineX.Length;
		}
	}

	private static float KnotX(int id, int i){
		switch(id){
			case (int)TerrainSpline.BASE:
				return GenerationSeed.baseHeightSplineX[i];
			case (int)TerrainSpline.MOUNTAIN_AMPLITUDE:
				return GenerationSeed.mountainAmplitudeSplineX[i];
			case (int)TerrainSpline.PEAKS_VALLEYS_SHARPNESS:
				return GenerationSeed.peaksValleysSharpnessSplineX[i];
			case (int)TerrainSpline.MOUNTAIN_SHARPNESS:
				return GenerationSeed.mountainSharpnessSplineX[i];
			case (int)TerrainSpline.HELL_RELIEF:
				return GenerationSeed.hellReliefSplineX[i];
			case (int)TerrainSpline.HELL_CEILING:
				return GenerationSeed.hellCeilingSplineX[i];
			case (int)TerrainSpline.CORE_HANG:
				return GenerationSeed.coreHangSplineX[i];
			default:
				return GenerationSeed.peaksValleysSplineX[i];
		}
	}

	private static float KnotY(int id, int i){
		switch(id){
			case (int)TerrainSpline.BASE:
				return GenerationSeed.baseHeightSplineY[i];
			case (int)TerrainSpline.MOUNTAIN_AMPLITUDE:
				return GenerationSeed.mountainAmplitudeSplineY[i];
			case (int)TerrainSpline.PEAKS_VALLEYS_SHARPNESS:
				return GenerationSeed.peaksValleysSharpnessSplineY[i];
			case (int)TerrainSpline.MOUNTAIN_SHARPNESS:
				return GenerationSeed.mountainSharpnessSplineY[i];
			case (int)TerrainSpline.HELL_RELIEF:
				return GenerationSeed.hellReliefSplineY[i];
			case (int)TerrainSpline.HELL_CEILING:
				return GenerationSeed.hellCeilingSplineY[i];
			case (int)TerrainSpline.CORE_HANG:
				return GenerationSeed.coreHangSplineY[i];
			default:
				return GenerationSeed.peaksValleysSplineY[(id - PV_ROW_ID)*GenerationSeed.peaksValleysSplineX.Length + i];
		}
	}

	private static void NextOctave(ref float x, ref float y){
		float nx = (OCTAVE_COS*x - OCTAVE_SIN*y)*LACUNARITY + OCTAVE_SHIFT_X;
		y = (OCTAVE_SIN*x + OCTAVE_COS*y)*LACUNARITY + OCTAVE_SHIFT_Y;
		x = nx;
	}

	private static float Fade(float t){
		return t * t * t * (t * (t * 6f - 15f) + 10f);
	}

	private static float Grad(int hash, float x, float y){
		int h = hash & 15;
		return GRAD_X[h]*x + GRAD_Y[h]*y;
	}
}

public enum TerrainSpline : byte{
	BASE,
	MOUNTAIN_AMPLITUDE,
	HELL_RELIEF,
	HELL_CEILING,
	CORE_HANG,
	PEAKS_VALLEYS_SHARPNESS,
	MOUNTAIN_SHARPNESS
}
