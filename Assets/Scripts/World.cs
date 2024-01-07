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

    public static bool isInGame = false;

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
        int renderDistance = Convert.ToInt32(rd);
        World.SetRenderDistance(renderDistance);
    }

    public static void SetRenderDistance(int rd){
        // If negative
        if(rd <= 0){
            World.renderDistance = 5;
        }
        // If bigger than 32
        else if(rd > 32){
            World.renderDistance = 32;
        }
        else{
            World.renderDistance = rd;
        }
    }

    public static void SetIP(string ip){
        World.IP = ip;
    }

    public static void SetToClient(){
        World.isClient = true;
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

    public static float NextFloat(){
        return (float)rng.NextDouble();
    }

    public static void SetGameSceneFlag(bool b){
        World.isInGame = b;
    }

    public static bool GetSceneFlag(){
        return World.isInGame;
    } 
}