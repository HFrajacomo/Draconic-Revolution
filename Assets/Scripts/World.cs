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

    private static Random rng;


    // Sets world name
    public static void SetWorldName(string name){
    	World.worldName = name;
    }

    // Sets seed as integer
    public static void SetWorldSeed(string seed){
    	World.worldSeed = Convert.ToInt32(seed);
        World.worldSeed = Mathf.Abs(World.worldSeed);

        GenerationSeed.Initialize(World.worldSeed);
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

    public static int GetWorldSeed(){
        return World.worldSeed;
    }

    public static void SetRNG(int seed){
        rng = new Random(seed);
    }

    public static int NextRandom(int min, int max){
        return rng.Next(min, max);
    }
}