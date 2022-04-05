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

    // Base Noise
    public static byte[] baseNoise = new byte[257];
    public static readonly float[] baseNoiseSplineX = new float[]{-1, -0.6f, -0.5f, -0.1f, 0, 0.4f, 0.5f, 0.8f, 1};
    public static readonly int[] baseNoiseSplineY = new int[]{60, 60, 80, 90, 110, 120, 130, 160, 185};
    public static readonly float baseNoiseStep1 = 0.00029f;
    public static readonly float baseNoiseStep2 = 0.0023f;

    // Erosion Noise
    public static byte[] erosionNoise = new byte[257];
    public static readonly float[] erosionNoiseSplineX = new float[]{-1, -0.6f, -0.5f, -0.2f, 0.5f, 1};
    public static readonly float[] erosionNoiseSplineY = new float[]{0.2f, 0.3f, 0.5f,  0.9f, 1f, 1f};
    public static readonly float erosionNoiseStep1 = 0.00117f;
    public static readonly float erosionNoiseStep2 = 0.00143f;

    // Peaks Noise
    public static byte[] peakNoise = new byte[257];
    public static readonly float[] peakNoiseSplineX = new float[]{-1, -0.7f, -0.45f -0.2f, 0, 0.2f, 0.7f, 1};
    public static readonly int[] peakNoiseSplineY = new int[]{-59, -40, -30, 0, 0, 20, 40, 70};
    public static readonly float peakNoiseStep1 = 0.00671f;
    public static readonly float peakNoiseStep2 = 0.00817f;

    // Temperature Noise
    public static byte[] temperatureNoise = new byte[257];
    public static readonly float[] temperatureNoiseSplineX = new float[]{-1, -0.7f, -0.69f, -0.4f, -0.39f, -0.1f, -0.09f, 0.2f, 0.21f, 0.5f, 0.51f, 0.8f, 0.81f, 1};
    public static readonly int[] temperatureNoiseSplineY = new int[]{0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6};
    public static readonly float temperatureNoiseStep1 = 0.00071f;
    public static readonly float temperatureNoiseStep2 = 0.00117f;

    // Humidity Noise
    public static byte[] humidityNoise = new byte[257];
    public static readonly float[] humidityNoiseSplineX = new float[]{-1, -0.7f, -0.69f, -0.4f, -0.39f, -0.1f, -0.09f, 0.2f, 0.21f, 0.5f, 0.51f, 0.8f, 0.81f, 1};
    public static readonly int[] humidityNoiseSplineY = new int[]{0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6};
    public static readonly float humidityNoiseStep1 = 0.00121f;
    public static readonly float humidityNoiseStep2 = 0.00057f;

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
        World.SetRNG((int)((seed >> 16) + (seed << 16)));
        for(int i=0; i < 256; i++){
            temperatureNoise[i] = (byte)World.NextRandom(0, 256);
        }
        temperatureNoise[256] = temperatureNoise[0];

        // Humidity Noise
        World.SetRNG(seed);
        for(int i=0; i < 256; i++){
            humidityNoise[i] = (byte)World.NextRandom(0, 256);
        }
        humidityNoise[256] = humidityNoise[0];
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
    HUMIDITY
}