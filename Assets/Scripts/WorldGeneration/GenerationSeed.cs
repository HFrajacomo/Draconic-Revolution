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
    public static readonly float baseYStep = 0.00117f;
    public static readonly float baseYStep2 = 0.000931f;
    // ---- Hell Splines
    public static readonly float[] baseHellNoiseSplineX = new float[]{-1, -0.65f, -0.6f, -0.1f, 0f, 0.2f, 0.25f, 0.4f, 0.67f, 1f};
    public static readonly int[] baseHellNoiseSplineY = new int[]{    0,  0,     130,    150,   170,180,  190,   200,  210,   220};


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
    public static readonly float peakYStep = 0.00561f;
    public static readonly float peakYStep2 = 0.00617f;
    // ---- Hell Splines
    public static readonly float[] peakHellNoiseSplineX = new float[]{-1, -0.5f, -0.2f, -0.1f,  0f, 0.3f, 0.6f, 1f};
    public static readonly int[] peakHellNoiseSplineY = new int[]{    -15, -8,   -2,   0,     0,  8,   15,   30};

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

    // Cave Noise
    public static byte[] caveNoise = new byte[257];
    public static readonly float caveNoiseStep1 = 0.00181f;
    public static readonly float caveNoiseStep2 = 0.00233f;
    public static readonly float caveYStep1 = 0.0131f;
    public static readonly float caveYStep2 = 0.0237f;

    // Cave Mask Noise
    public static byte[] cavemaskNoise = new byte[257];
    public static readonly float cavemaskNoiseStep1 = 0.0363f;
    public static readonly float cavemaskNoiseStep2 = 0.0427f;
    public static readonly float cavemaskYStep1 = 0.01721f;
    public static readonly float cavemaskYStep2 = 0.0277f;

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