using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class World
{
    
    public static string worldName;
    public static int worldSeed;
    public static int renderDistance;

    // Sets world name
    public static void SetWorldName(string name){
    	World.worldName = name;
    }

    // Sets seed as integer
    public static void SetWorldSeed(string seed){
    	World.worldSeed = Convert.ToInt32(seed);
        World.worldSeed = Mathf.Abs(World.worldSeed);
    }

    public static void SetRenderDistance(string rd){
        World.renderDistance = Convert.ToInt32(rd);

        // If negative
        if(World.renderDistance < 0){
            World.renderDistance = 5;
        }

        // If bigger than 20
        if(World.renderDistance > 20){
            World.renderDistance = 20;
        }
    }

}
