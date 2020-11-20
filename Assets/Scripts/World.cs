using System;
using System.Collections;
using System.Collections.Generic;

public static class World
{
    
    public static string worldName;
    public static int worldSeed;

    // Sets world name
    public static void SetWorldName(string name){
    	World.worldName = name;
    }

    // Sets seed as integer
    public static void SetWorldSeed(string seed){
    	World.worldSeed = Convert.ToInt32(seed);
    }

}
