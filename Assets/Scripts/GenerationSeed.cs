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
    public static readonly float[] baseNoiseSplineX = new float[]{-1, -0.7f, -0.67f -0.3f, 0, 0.4f, 0.65f, 0.7f, 0.8f, 1};
    public static readonly int[] baseNoiseSplineY = new int[]{30, 40, 80, 90, 120, 170, 200, 220, 240, 250};

    // Erosion Noise
    public static byte[] erosionNoise = new byte[257];
    public static readonly float[] erosionNoiseSplineX = new float[]{-1, -0.6f, -0.5f, -0.2f, 0.5f, 1};
    public static readonly float[] erosionNoiseSplineY = new float[]{0.2f, 0.67f, 0.7f,  0.8f, 1f, 1f};

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
    }


    /*
    TESTING PURPOSES ONLY
    */
    public static void DrawSplines(){
        Texture2D image = new Texture2D(512, 512);
        int index = erosionNoiseSplineX.Length-2;
        float analogX;
        float inverseLerp;
        int y;

        for(int x = 0; x < 512; x++){
            analogX = Mathf.Lerp(-1, 1, x/512f);

            for(int i=1; i < erosionNoiseSplineX.Length; i++){
                if(erosionNoiseSplineX[i] >= analogX){
                    index = i-1;
                    break;
                }
            }

            inverseLerp = (analogX - erosionNoiseSplineX[index])/(erosionNoiseSplineX[index+1] - erosionNoiseSplineX[index]);
            if(erosionNoiseSplineY[index] > erosionNoiseSplineY[index+1])
                y = Mathf.CeilToInt(Mathf.Lerp(erosionNoiseSplineY[index]*256, erosionNoiseSplineY[index+1]*256, Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                y = Mathf.CeilToInt(Mathf.Lerp(erosionNoiseSplineY[index]*256, erosionNoiseSplineY[index+1]*256, Mathf.Pow(inverseLerp, 0.8f)));


            image.SetPixel(x, y, Color.red);
        }

        image.Apply();

        File.WriteAllBytes("spline.png", ImageConversion.EncodeToPNG(image));        
    }
}


public enum NoiseMap : byte{
    BASE,
    EROSION
}