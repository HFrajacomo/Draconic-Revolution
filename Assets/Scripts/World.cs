using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class World
{
    
    public static string worldName;
    public static int worldSeed;
    public static int renderDistance;
    public static bool isClient;
    public static string IP;
    public static ulong accountID;

    public static readonly int[] baseNoise = new int[257];
    public static readonly float[] baseNoiseSplineX = new float[]{-1, -0.7f, -0.3f, 0, 0.4f, 0.65f, 0.8f, 1};
    public static readonly int[] baseNoiseSplineY = new int[]{    30, 50,    80,   100,120,  150,    210, 250};

    /*
    TESTING PURPOSES ONLY
    */
    public static void DrawSplines(){
        Texture2D image = new Texture2D(512, 512);
        int index = World.baseNoiseSplineX.Length-2;
        float analogX;
        float inverseLerp;
        int y;

        for(int x = 0; x < 512; x++){
            analogX = Mathf.Lerp(-1, 1, x/512f);

            for(int i=1; i < baseNoiseSplineX.Length; i++){
                if(World.baseNoiseSplineX[i] >= analogX){
                    index = i-1;
                    break;
                }
            }

            inverseLerp = (analogX - World.baseNoiseSplineX[index])/(World.baseNoiseSplineX[index+1] - World.baseNoiseSplineX[index]);
            if(World.baseNoiseSplineY[index] > World.baseNoiseSplineY[index+1])
                y = Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                y = Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));


            image.SetPixel(x, y, Color.red);
        }

        image.Apply();

        File.WriteAllBytes("spline.png", ImageConversion.EncodeToPNG(image));        
    }

    // Sets world name
    public static void SetWorldName(string name){
    	World.worldName = name;
    }

    // Sets seed as integer
    public static void SetWorldSeed(string seed){
    	World.worldSeed = Convert.ToInt32(seed);
        World.worldSeed = Mathf.Abs(World.worldSeed);

        BuildBaseNoiseMap(World.worldSeed);
    }

    // Sets seed as integer
    public static void SetWorldSeed(int seed){
        SetWorldSeed(seed.ToString());
    }

    // Sets accountID in client to join a server
    public static void SetAccountID(ulong code){
        World.accountID = code;
    }
    public static void SetAccountID(string code){
        long temp = Convert.ToInt64(code);

        if(temp < 0)
            temp *= -1;

        World.accountID = (ulong)temp;
    }

    public static void SetRenderDistance(string rd){
        World.renderDistance = Convert.ToInt32(rd);

        // If negative
        if(World.renderDistance <= 0){
            World.renderDistance = 5;
        }

        // If bigger than 20
        if(World.renderDistance > 20){
            World.renderDistance = 20;
        }
    }

    public static void SetIP(string ip){
        World.IP = ip;
    }

    public static void SetToClient(){
        World.isClient = true;
        World.SetAccountID((ulong)0);
    }

    public static void SetToServer(){
        World.isClient = false;
    }

    // Constructs the baseNoise map
    private static void BuildBaseNoiseMap(int seed){
        Random rng = new Random(seed);

        for(int i=0; i < 256; i++){
            baseNoise[i] = rng.Next(0, 256);
        }
        baseNoise[256] = baseNoise[0];
    }

}
