using System.Collections;
using System.Collections.Generic;
using Random = System.Random;
using System;
using System.IO;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public static class GenerationSeed
{
    public static int seed;

    public static int multiplier = 19;

    // Base Noise
    public static byte[] baseNoise = new byte[257];
    public static readonly float[] baseNoiseSplineX = new float[]{-1, -0.6f, -0.5f, 0.15f,  0.2f, 0.3f, 0.5f, 0.6f, 0.8f, 1};
    public static readonly int[] baseNoiseSplineY = new int[]{    60, 70,    80,    90,     100,  140,  140,  170 , 200,  200};
    public static readonly float baseNoiseStep1 = 0.00019f;
    public static readonly float baseNoiseStep2 = 0.0013f;
    public static readonly float baseNoiseStep3 = 0.031f;
    public static readonly float baseNoiseStep4 = 0.0093f;
    public static readonly float baseNoiseStep5 = 0.0021f;
    public static readonly float baseNoiseStep6 = 0.00773f;
    public static readonly float baseYStep = 0.00117f;
    public static readonly float baseYStep2 = 0.000931f;
    // ---- Hell Splines
    public static readonly float[] baseHellNoiseSplineX = new float[]{-1, -0.65f, -0.6f, -0.1f, 0f, 0.2f, 0.25f, 0.4f, 0.67f, 1f};
    public static readonly int[] baseHellNoiseSplineY = new int[]{    0,  0,     130,    150,   170,180,  190,   200,  210,   220};
    // ---- Core Splines
    public static readonly float[] baseCoreNoiseSplineX = new float[]{-1, -0.6f, -0.2f, 0f, 0.4f, 0.75f, 0.79f, 0.8f, 1f};
    public static readonly int[] baseCoreNoiseSplineY = new int[]{    120,  125,  130,  140,140,  156,   130,   146, 156};


    // Erosion Noise
    public static byte[] erosionNoise = new byte[257];
    public static readonly float[] erosionNoiseSplineX = new float[]{-1, -0.6f, -0.5f, -0.2f, 0.5f, 1};
    public static readonly float[] erosionNoiseSplineY = new float[]{0.6f, 0.7f, 0.8f,  0.9f, 1f, 1f};
    public static readonly float erosionNoiseStep1 = 0.0047f;
    public static readonly float erosionNoiseStep2 = 0.00063f;
    public static readonly float erosionYStep = 0.00071f;
    public static readonly float erosionYStep2 = 0.00083f;
    // ---- Hell Splines
    public static readonly float[] erosionHellNoiseSplineX = new float[]{-1, -0.6f, -0.56f, -0.1f,  0f,    0.4f, 1f};
    public static readonly float[] erosionHellNoiseSplineY = new float[]{    0.7f,0.77f, 0.8f,   0.85f, 0.95f, 1f,   1f};
    // ---- Core Splines
    public static readonly float[] erosionCoreNoiseSplineX = new float[]{-1, -0.7f, -0.4f, -0.1f,  0f, 0.3f, 0.6f, 0.64f, 0.67f, 0.8f, 1f};
    public static readonly float[] erosionCoreNoiseSplineY = new float[]{0.5f, 0.62f, 0.75f, 0.8f, 0.9f, 1f,   1f, 0.4f,  1f,    1f,   1f};

    // Peaks Noise
    public static byte[] peakNoise = new byte[257];
    public static readonly float[] peakNoiseSplineX = new float[]{-1, -0.5f, -0.22f, -0.2f, 0, 0.3f, 0.5f, 0.7f, 1};
    public static readonly int[] peakNoiseSplineY = new int[]{   -30, -20,   -12,      -2,    0, 0,    10,   30,   50};
    public static readonly float peakNoiseStep1 = 0.00471f;
    public static readonly float peakNoiseStep2 = 0.00617f;
    public static readonly float peakNoiseStep3 = 0.0221f;
    public static readonly float peakNoiseStep4 = 0.0367f;
    public static readonly float peakNoiseStep5 = 0.0521f;
    public static readonly float peakNoiseStep6 = 0.0767f;
    public static readonly float peakNoiseStep7 = 0.00721f;
    public static readonly float peakNoiseStep8 = 0.00367f;
    public static readonly float peakYStep = 0.00561f;
    public static readonly float peakYStep2 = 0.00617f;
    // ---- Hell Splines
    public static readonly float[] peakHellNoiseSplineX = new float[]{-1, -0.5f, -0.2f, -0.1f,  0f, 0.3f, 0.6f, 1f};
    public static readonly int[] peakHellNoiseSplineY = new int[]{    -15, -8,   -2,   0,     0,  8,   15,   30};
    // ---- Core Splines
    public static readonly float[] peakCoreNoiseSplineX = new float[]{-1, -0.7f, -0.5f, -0.2f,  0f, 0.2f, 0.5f, 0.8f, 1f};
    public static readonly int[] peakCoreNoiseSplineY = new int[]{    140, 125,     105,  100,  80, 80,   70,   50,   40};

    // Temperature Noise
    public static byte[] temperatureNoise = new byte[257];
    public static readonly float[] temperatureOffsetX = new float[1];
    public static readonly float[] temperatureOffsetY = new float[1];
    public static readonly float temperatureNoiseStep1 = 0.000093f;
    public static readonly float temperatureNoiseStep2 = 0.000127f;
    public static readonly float temperatureNoiseStep3 = 0.000623f;
    public static readonly float temperatureNoiseStep4 = 0.000773f;
    public static readonly float temperatureNoiseStep5 = 0.0323f;
    public static readonly float temperatureNoiseStep6 = 0.0927f;

    // Humidity Noise
    public static byte[] humidityNoise = new byte[257];
    public static readonly float[] humidityOffsetX = new float[1];
    public static readonly float[] humidityOffsetY = new float[1];
    public static readonly float humidityNoiseStep1 = 0.000121f;
    public static readonly float humidityNoiseStep2 = 0.000057f;

    // Patch Noise
    public static byte[] patchNoise = new byte[257];
    public static readonly float patchNoiseStep1 = 0.1121f;
    public static readonly float patchNoiseStep2 = 0.0799f;
    public static readonly float patchNoiseStep3 = 1.279f;
    public static readonly float patchNoiseStep4 = 0.1427f;
    public static readonly float patchNoiseStep5 = 0.0431f;
    public static readonly float patchMultStep = 1.73f;
    public static readonly float patchRandomStep = 1.3839f;
    public static readonly float patchRandomStep2 = 0.4217f;

    // Cave Noise
    public static byte[] caveNoise = new byte[257];
    public static readonly float caveNoiseStep1 = 0.00247f;
    public static readonly float caveNoiseStep2 = 0.00283f;
    public static readonly float caveYStep1 = 0.0079f;
    public static readonly float caveYStep2 = 0.0071f;

    // Cave Mask Noise
    public static byte[] cavemaskNoise = new byte[257];
    public static readonly float cavemaskNoiseStep1 = 0.0363f;
    public static readonly float cavemaskNoiseStep2 = 0.0347f;
    public static readonly float cavemaskYStep1 = 0.01721f;
    public static readonly float cavemaskYStep2 = 0.0177f;

    // Blend Noise: warps biome borders (see BiomeBlend)
    public static byte[] blendNoise = new byte[257];
    public static readonly float blendWarpStep1 = 0.03f;
    public static readonly float blendWarpStep2 = 0.061f;

    // Warp Noise: domain warping of the Surface, Hell and Core terrain (see TerrainShape)
    public static byte[] warpNoise = new byte[257];

    /*
    Terrain Shape (see TerrainShape). Steps are frequencies (1/wavelength in blocks), amplitudes are in blocks.
    A warp folds the terrain when amplitude*step goes above ~0.15
    */
    // ---- Surface
    public static readonly float baseWarpStep = 0.000667f;          // ~1500 blocks
    public static readonly float baseWarpAmplitude = 220f;
    public static readonly float localWarpStep = 0.00385f;          // ~260 blocks
    public static readonly float localWarpAmplitude = 36f;
    public static readonly float baseStep = 0.000417f;              // ~2400 blocks
    public static readonly float erosionStep = 0.000909f;           // ~1100 blocks
    public static readonly float weirdnessStep = 0.00095f;          // ~1050 blocks, makes the Peaks and Valleys bands (lower = wider bands)
    public static readonly float ridgeStep = 0.00435f;              // ~230 blocks, narrow (steep) mountains
    public static readonly float wideRidgeStep = 0.0022f;           // ~450 blocks, wide (gentle) mountains
    public static readonly float detailStep = 0.0278f;              // ~36 blocks
    public static readonly float detailAmplitudeMin = 1.5f;
    public static readonly float detailAmplitudeMax = 6f;
    // Fbm values are bell shaped, these gains spread them back to a roughly uniform [-1, 1] (more octaves need a higher gain)
    public static readonly float baseSpread = 4.9f;
    public static readonly float erosionSpread = 4.7f;
    public static readonly float weirdnessSpread = 5f;
    // Base range where hills, valleys and mountains fade in from the coast
    public static readonly float inlandStart = -0.25f;
    public static readonly float inlandEnd = 0.05f;
    public static readonly float mountainInlandStart = -0.05f;
    public static readonly float mountainInlandEnd = 0.45f;
    // Peaks and Valleys range where mountain ranges fade in
    public static readonly float mountainBandStart = -0.2f;
    public static readonly float mountainBandEnd = 0.6f;
    // Ridged values below this become valley floors between crests
    public static readonly float ridgeFloor = 0.2f;
    // Heights above the soft cap are compressed smoothly towards the max height, so peaks never become flat plateaus
    public static readonly float surfaceSoftCap = 210f;
    public static readonly float surfaceMaxHeight = 240f;

    /*
    Base -> height. The coast (WORLD_WATER_LEVEL = 60) is around -0.14. Flat segments make plateaus and steep ones make cliff bands along the Base contours:
        -1 to -0.6:     deep trenches rising to a flat abyssal plain
        -0.42 to -0.3:  an undersea ridge running parallel to the coast
        -0.24 to -0.16: continental slope and a shallow shelf
        -0.13 to -0.08: wide, flat beaches and coastal lowlands just above the sea
        0.08 to 0.18:   flat inland plains, ending in an escarpment (0.18 to 0.26)
        0.34 to 0.46:   upland plateau
        0.75 to 0.88:   high plateau before the highest inland terrain
    */
    public static readonly float[] baseHeightSplineX = new float[]{-1f, -0.85f, -0.7f, -0.6f, -0.5f, -0.42f, -0.36f, -0.3f, -0.24f, -0.19f, -0.16f, -0.13f, -0.08f, -0.02f, 0.08f, 0.18f, 0.26f, 0.34f, 0.46f, 0.75f, 0.88f, 1f};
    public static readonly float[] baseHeightSplineY = new float[]{8f,  10f,    16f,   22f,   23f,   32f,    28f,    34f,   44f,    54f,    57f,    61f,    62f,    66f,    70f,   72f,   82f,   88f,   90f,   106f,  110f,  116f};

    /*
    Nested spline: one Peaks and Valleys -> height offset row per Erosion key. Higher Erosion = more rugged.
    Valleys deeper than the land around them flood into long lakes and inlets. Each erosion range has its own landform:
        -1 to -0.5:  gentle rolling plains
        -0.3 to -0.1: low hills with a bench halfway down the valleys
        0.1:          terraced hills, with flat steps on both slopes
        0.3:          mesas: a cliff band (0 to 0.2) up to a flat top
        0.5:          deep valleys and tall rounded hills
        0.75:         canyons: narrow, flat floors with steep walls
        1:            alpine: deepest valleys and the tallest peaks
    */
    public static readonly float[] peaksValleysErosionKeys = new float[]{-1f, -0.75f, -0.5f, -0.3f, -0.1f, 0.1f, 0.3f, 0.5f, 0.75f, 1f};
    public static readonly float[] peaksValleysSplineX = new float[]{-1f, -0.9f, -0.75f, -0.6f, -0.4f, -0.2f, 0f, 0.2f, 0.45f, 0.7f, 0.88f, 1f};
    public static readonly float[] peaksValleysSplineY = new float[]{
        -3f,  -3f,  -2f,  -1f,   0f,  0f,  1f,  2f,  3f,  4f,  5f,  5f,   // Erosion -1
        -5f,  -4f,  -3f,  -2f,  -1f,  0f,  1f,  3f,  5f,  7f,  8f,  9f,   // Erosion -0.75
        -8f,  -7f,  -5f,  -3f,  -1f,  0f,  2f,  5f,  9f, 12f, 14f, 15f,   // Erosion -0.5
        -12f, -11f, -7f,  -5f,  -4f, -3f,  2f,  7f, 13f, 18f, 21f, 22f,   // Erosion -0.3
        -15f, -14f, -9f,  -6f,  -5f, -4f,  3f, 10f, 18f, 25f, 29f, 31f,   // Erosion -0.1
        -18f, -17f, -11f, -10f, -10f, -4f,  5f, 14f, 15f, 30f, 36f, 38f,  // Erosion 0.1
        -21f, -20f, -13f, -12f, -12f, -3f, 12f, 30f, 32f, 34f, 41f, 46f,  // Erosion 0.3
        -25f, -24f, -17f, -11f,  -6f, -2f,  6f, 18f, 32f, 45f, 53f, 56f,  // Erosion 0.5
        -30f, -29f, -14f, -10f,  -8f, -3f,  6f, 20f, 36f, 52f, 62f, 64f,  // Erosion 0.75
        -32f, -30f, -20f, -12f,  -6f, -1f,  8f, 24f, 42f, 58f, 68f, 72f   // Erosion 1
    };

    // Erosion -> height of the ridged mountain ranges. The flat segment (0.25 to 0.45) makes many medium foothill ranges before the alpine ones
    public static readonly float[] mountainAmplitudeSplineX = new float[]{-1f, -0.6f, -0.35f, -0.15f, 0.05f, 0.25f, 0.45f, 0.65f, 0.85f, 1f};
    public static readonly float[] mountainAmplitudeSplineY = new float[]{0f,  0f,    6f,     20f,    42f,   60f,   66f,   118f,  150f,  172f};

    /*
    Slope control by Erosion: up to ~0.4 slopes are gradual, then they get steeper towards the alpine terrain.
    Peaks and Valleys sharpness: how much the flat peaks and valley floors widen. The slope between them is at most this many times
    the plain slope, so lower = more gradual hillsides. Keep it at 1.6 or more, or peaks and valleys become less common
    */
    public static readonly float[] peaksValleysSharpnessSplineX = new float[]{-1f,  -0.4f, 0.1f, 0.4f, 0.65f, 0.85f, 1f};
    public static readonly float[] peaksValleysSharpnessSplineY = new float[]{1.6f, 1.6f,  1.6f, 1.7f, 2f,    2.6f,  3f};
    // Mountain sharpness: 0 = wide mountains (half as steep), 1 = narrow mountains with steep sides
    public static readonly float[] mountainSharpnessSplineX = new float[]{-1f, 0.2f, 0.45f, 0.7f, 0.9f, 1f};
    public static readonly float[] mountainSharpnessSplineY = new float[]{0f,  0f,   0.25f, 0.65f, 1f,  1f};

    // ---- Hell
    public static readonly float hellWarpStep = 0.00111f;           // ~900 blocks
    public static readonly float hellWarpAmplitude = 110f;
    public static readonly float hellLocalWarpStep = 0.005f;        // ~200 blocks
    public static readonly float hellLocalWarpAmplitude = 22f;
    public static readonly float hellBaseStep = 0.000588f;          // ~1700 blocks
    public static readonly float hellRidgeStep = 0.00667f;          // ~150 blocks
    public static readonly float hellCeilingStep = 0.0111f;         // ~90 blocks
    public static readonly float hellCeilingRidgeStep = 0.00909f;   // ~110 blocks
    public static readonly float hellCeilingSpread = 4.4f;
    // Erosion -> height of the ridged relief on the Hell floor. Flat segments make regions of similar ridges, steps make sudden changes between them
    public static readonly float[] hellReliefSplineX = new float[]{-1f, -0.7f, -0.45f, -0.2f, 0.1f, 0.4f, 0.7f, 1f};
    public static readonly float[] hellReliefSplineY = new float[]{6f,  8f,    13f,    15f,   21f,  28f,  30f,  36f};
    // Erosion -> length of the ridges hanging from the Hell ceiling
    public static readonly float[] hellCeilingSplineX = new float[]{-1f, -0.6f, -0.2f, 0.2f, 0.6f, 1f};
    public static readonly float[] hellCeilingSplineY = new float[]{8f,  12f,   20f,   23f,  30f,  36f};

    // ---- Core
    public static readonly float coreWarpStep = 0.00143f;           // ~700 blocks
    public static readonly float coreWarpAmplitude = 80f;
    public static readonly float coreLocalWarpStep = 0.00625f;      // ~160 blocks
    public static readonly float coreLocalWarpAmplitude = 18f;
    public static readonly float coreBaseStep = 0.00238f;           // ~420 blocks
    public static readonly float coreBottomStep = 0.00417f;         // ~240 blocks
    public static readonly float coreRidgeStep = 0.00909f;          // ~110 blocks
    public static readonly float coreDetailStep = 0.0333f;          // ~30 blocks
    public static readonly float coreDetailAmplitude = 3f;
    // Erosion -> length of the ridges hanging from the bottom of the Core
    public static readonly float[] coreHangSplineX = new float[]{-1f, -0.6f, -0.2f, 0.2f, 0.6f, 1f};
    public static readonly float[] coreHangSplineY = new float[]{3f,  5f,    9f,    11f,  17f,  22f};

    // Weather Noise
    public static byte[] weatherNoise = new byte[257];
    public static readonly float weatherNoiseStep = 0.0021f;
    public static readonly float weatherDayStep = 0.17f;
    public static readonly float fogNoiseStep = 0.0002f;
    public static readonly float weatherSeedStep = 0.0019f;
    public static readonly float windNoiseStep1 = 0.00011f;
    public static readonly float windNoiseStep2 = 0.3137f;
    public static readonly float windNoiseStep3 = 0.00017f;
    public static readonly float windNoiseStep4 = 0.279f;
    public static readonly float windResistanceStep = 0.179f;
    public static readonly float windCloudStep = 0.071f;
    public static readonly float windCloudOrientStep = 0.021f;

    public static void Initialize(int sed){
        seed = sed;

        World.SetRNG(seed);

        // Base Noise
        for(int i=0; i < 256; i++){
            baseNoise[i] = (byte)World.NextRandom(0, 256);
        }
        baseNoise[256] = baseNoise[0];

        // Erosion Noise
        World.SetRNG((seed << 2) + 3);
        for(int i=0; i < 256; i++){
            erosionNoise[i] = (byte)World.NextRandom(0, 256);
        }
        erosionNoise[256] = erosionNoise[0];

        // Peak Noise
        World.SetRNG((int)((seed >> 2) | 0xc0000000));
        for(int i=0; i < 256; i++){
            peakNoise[i] = (byte)World.NextRandom(0, 256);
        }
        peakNoise[256] = peakNoise[0];

        // Temperature Noise
        World.SetRNG((int)((seed >> 1) | 0x80000000));
        for(int i=0; i < 256; i++){
            temperatureNoise[i] = (byte)World.NextRandom(0, 256);
        }
        temperatureNoise[256] = temperatureNoise[0];
        temperatureOffsetX[0] = World.NextFloat() * multiplier;
        temperatureOffsetY[0] = World.NextFloat() * multiplier;

        // Humidity Noise
        World.SetRNG(seed);
        for(int i=0; i < 256; i++){
            humidityNoise[i] = (byte)World.NextRandom(0, 256);
        }
        humidityNoise[256] = humidityNoise[0];
        humidityOffsetX[0] = World.NextFloat() * multiplier;
        humidityOffsetY[0] = World.NextFloat() * multiplier;

        // Patch Noise
        World.SetRNG(seed);
        for(int i=0; i < 256; i++){
            patchNoise[i] = (byte)World.NextRandom(0, 256);
        }
        patchNoise[256] = patchNoise[0];

        // Cave Noise
        World.SetRNG(~seed);
        for(int i=0; i < 256; i++){
            caveNoise[i] = (byte)World.NextRandom(0, 256);
        }
        caveNoise[256] = caveNoise[0];

        // Cave Noise
        World.SetRNG(seed*5+48);
        for(int i=0; i < 256; i++){
            cavemaskNoise[i] = (byte)World.NextRandom(0, 256);
        }
        cavemaskNoise[256] = cavemaskNoise[0];

        // Weather Noise
        World.SetRNG(~seed ^ 0x7AAAAAAA);
        for(int i=0; i < 256; i++){
            weatherNoise[i] = (byte)World.NextRandom(0, 256);
        }
        weatherNoise[256] = weatherNoise[0];        

        // Blend Noise
        World.SetRNG(seed ^ 0x2545F491);
        for(int i=0; i < 256; i++){
            blendNoise[i] = (byte)World.NextRandom(0, 256);
        }
        blendNoise[256] = blendNoise[0];

        // Warp Noise
        World.SetRNG(seed ^ 0x5BD1E995);
        for(int i=0; i < 256; i++){
            warpNoise[i] = (byte)World.NextRandom(0, 256);
        }
        warpNoise[256] = warpNoise[0];
    }


    /*
    TESTING PURPOSES ONLY
    */
    public static void DrawSplines(){
        Texture2D image = new Texture2D(512, 100);
        int index = peakNoise.Length-2;
        float analogX;
        float inverseLerp;
        int y;

        for(int x = 0; x < 512; x++){
            analogX = Mathf.Lerp(-1, 1, x/512f);

            for(int i=1; i < peakNoiseSplineX.Length; i++){
                if(peakNoiseSplineX[i] >= analogX){
                    index = i-1;
                    break;
                }
            }

            inverseLerp = (analogX - peakNoiseSplineX[index])/(peakNoiseSplineX[index+1] - peakNoiseSplineX[index]);
            if(peakNoiseSplineY[index] > peakNoiseSplineY[index+1])
                y = Mathf.CeilToInt(Mathf.Lerp(peakNoiseSplineY[index], peakNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2))) + 40;
            else
                y = Mathf.CeilToInt(Mathf.Lerp(peakNoiseSplineY[index], peakNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f))) + 40;


            image.SetPixel(x, y, Color.red);
        }

        image.Apply();

        File.WriteAllBytes("spline.png", ImageConversion.EncodeToPNG(image));        
    }
}


public enum NoiseMap : byte{
    BASE,
    EROSION,
    PEAK,
    TEMPERATURE,
    HUMIDITY,
    CAVE
}