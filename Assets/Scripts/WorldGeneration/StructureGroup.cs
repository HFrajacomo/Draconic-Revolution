using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StructureGroup
{
    // Mapper
    private static Dictionary<StructureGroupID, List<StructSpawn>> map = new Dictionary<StructureGroupID, List<StructSpawn>>();

    // Structure Groups
    private static List<StructSpawn> plainTrees = new List<StructSpawn>();
    private static List<StructSpawn> grassHighlandsTrees = new List<StructSpawn>();
    private static List<StructSpawn> desertTrees = new List<StructSpawn>();
    private static List<StructSpawn> icePlainsTrees = new List<StructSpawn>();
    private static List<StructSpawn> iceHighlandsTrees = new List<StructSpawn>();
    private static List<StructSpawn> forestTrees = new List<StructSpawn>();
    private static List<StructSpawn> iceForestTrees = new List<StructSpawn>();
    private static List<StructSpawn> surfaceOres = new List<StructSpawn>();
    private static List<StructSpawn> dirtPatches = new List<StructSpawn>();
    private static List<StructSpawn> gravelSurfacePatches = new List<StructSpawn>();
    private static List<StructSpawn> gravelPatches = new List<StructSpawn>();
    private static List<StructSpawn> boulders_LowDensity = new List<StructSpawn>();
    private static List<StructSpawn> boulders_MediumDensity = new List<StructSpawn>();
    private static List<StructSpawn> undergroundOres = new List<StructSpawn>();

    // Static Constructor
    static StructureGroup(){
        PopulatePlainsTrees();
        map.Add(StructureGroupID.PLAINS_TREES, plainTrees);
        PopulateGrassHighlandsTrees();
        map.Add(StructureGroupID.GRASS_HIGHLANDS_TREES, grassHighlandsTrees);
        PopulateDesertTrees();
        map.Add(StructureGroupID.DESERT_TREES, desertTrees);
        PopulateIcePlainsTrees();
        map.Add(StructureGroupID.ICE_PLAINS_TREES, icePlainsTrees);
        PopulateIceHighlandsTrees();
        map.Add(StructureGroupID.ICE_HIGHLANDS_TREES, iceHighlandsTrees);
        PopulateForestTrees();
        map.Add(StructureGroupID.FOREST_TREES, forestTrees);
        PopulateIceForestTrees();
        map.Add(StructureGroupID.ICE_FOREST_TREES, iceForestTrees);
        PopulateSurfaceOres();
        map.Add(StructureGroupID.SURFACE_ORES, surfaceOres);
        PopulateDirtPatches();
        map.Add(StructureGroupID.DIRT_PATCHES, dirtPatches);
        PopulateGravelSurfacePatches();
        map.Add(StructureGroupID.GRAVEL_PATCHES_SURFACE, gravelSurfacePatches);
        PopulateGravelPatches();
        map.Add(StructureGroupID.GRAVEL_PATCHES, gravelPatches);        
        PopulateBoulders_LowDensity();
        map.Add(StructureGroupID.BOULDERS_LOW_DENSITY, boulders_LowDensity);
        PopulateBoulders_MediumDensity();
        map.Add(StructureGroupID.BOULDERS_MID_DENSITY, boulders_MediumDensity);
        PopulateUndergroundOres();
        map.Add(StructureGroupID.UNDERGROUND_ORES, undergroundOres);
    }

    public static void AddStructureGroup(StructureGroupID id, Biome b){
        foreach(StructSpawn s in map[id]){
            s.AddToSpawn(b.structCodes, b.amountStructs, b.percentageStructs, b.depthValues, b.hardSetDepth, b.hasRange);
        }
    }


    private static void PopulatePlainsTrees(){
        plainTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.2f, 0, -1, false));
        plainTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.1f, 0, -1, false));        
    }

    private static void PopulateGrassHighlandsTrees(){
        grassHighlandsTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.3f, 0, -1, false));
        grassHighlandsTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.2f, 0, -1, false));             
    }

    private static void PopulateDesertTrees(){
        desertTrees.Add(new StructSpawn(StructureCode.TreeCrookedMediumA, 1, 0.02f, 0, -1, false));
    }

    // To change once Pine Trees are created
    private static void PopulateIcePlainsTrees(){
        icePlainsTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.2f, 0, -1, false));
        icePlainsTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.1f, 0, -1, false)); 
    }
    private static void PopulateIceHighlandsTrees(){
        iceHighlandsTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.2f, 0, -1, false));
        iceHighlandsTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.1f, 0, -1, false)); 
    }

    private static void PopulateForestTrees(){
        forestTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 3, 1f, 0, -1, false));
        forestTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 2, 0.5f, 0, -1, false));
        forestTrees.Add(new StructSpawn(StructureCode.TreeBigA, 1, 0.1f, 0, -1, false));
    }

    // To Change once Pine Trees are created
    private static void PopulateIceForestTrees(){
        forestTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 3, 1f, 0, -1, false));
        forestTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 3, 0.5f, 0, -1, false));
        forestTrees.Add(new StructSpawn(StructureCode.TreeSmallB, 1, 0.3f, 0, -1, false));
    }

    private static void PopulateSurfaceOres(){
        surfaceOres.Add(new StructSpawn(StructureCode.IronVeinA, 5, 1f, 0, 90, true));
        surfaceOres.Add(new StructSpawn(StructureCode.IronVeinB, 5, 1f, 0, 90, true));
        surfaceOres.Add(new StructSpawn(StructureCode.IronVeinC, 5, 1f, 0, 90, true));
        surfaceOres.Add(new StructSpawn(StructureCode.CoalVeinA, 6, 1f, 0, -1, true));
        surfaceOres.Add(new StructSpawn(StructureCode.CoalVeinB, 6, 1f, 0, -1, true));
        surfaceOres.Add(new StructSpawn(StructureCode.CoalVeinC, 6, 1f, 0, -1, true));
        surfaceOres.Add(new StructSpawn(StructureCode.CopperVeinA, 10, 1f, 0, 120, true));
        surfaceOres.Add(new StructSpawn(StructureCode.CopperVeinB, 10, 1f, 0, 120, true));
        surfaceOres.Add(new StructSpawn(StructureCode.TinVeinA, 6, 1f, 0, 100, true));
        surfaceOres.Add(new StructSpawn(StructureCode.TinVeinB, 6, 1f, 0, 100, true));
        surfaceOres.Add(new StructSpawn(StructureCode.GoldVeinA, 3, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.GoldVeinB, 3, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.AluminiumVeinA, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.AluminiumVeinB, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeriumVeinA, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeriumVeinB, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.UraniumVeinA, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.UraniumVeinB, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.MagnetiteVeinA, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.MagnetiteVeinB, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeraldVeinA, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeraldVeinB, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.RubyVeinA, 1, 0.5f, 0, 60, true));
        surfaceOres.Add(new StructSpawn(StructureCode.RubyVeinB, 1, 0.5f, 0, 60, true));
    }

    private static void PopulateDirtPatches(){
        dirtPatches.Add(new StructSpawn(StructureCode.DirtPileA, 3, 1f, 3, -1, true));
        dirtPatches.Add(new StructSpawn(StructureCode.DirtPileB, 2, 1f, 3, -1, true));
    }

    private static void PopulateGravelSurfacePatches(){
        gravelSurfacePatches.Add(new StructSpawn(StructureCode.GravelPile, 2, 0.5f, -1, -1, false));
    }

    private static void PopulateGravelPatches(){
        gravelPatches.Add(new StructSpawn(StructureCode.GravelPile, 16, 1f, 1, -1, true));
    }

    private static void PopulateBoulders_LowDensity(){
        boulders_LowDensity.Add(new StructSpawn(StructureCode.BoulderNormalA, 1, 0.02f, 1, -1, false));
    }

    private static void PopulateBoulders_MediumDensity(){
        boulders_MediumDensity.Add(new StructSpawn(StructureCode.BoulderNormalA, 1, 0.05f, 1, -1, false));
    }

    private static void PopulateUndergroundOres(){
        undergroundOres.Add(new StructSpawn(StructureCode.IronVeinA, 12, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.IronVeinB, 12, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.IronVeinC, 12, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.CoalVeinA, 18, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.CoalVeinB, 18, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.CoalVeinC, 18, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.CopperVeinA, 10, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.CopperVeinB, 10, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.TinVeinA, 6, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.TinVeinB, 6, 1f, 0, -1, true));
        undergroundOres.Add(new StructSpawn(StructureCode.GoldVeinA, 6, 1f, 0, 240, true));
        undergroundOres.Add(new StructSpawn(StructureCode.GoldVeinB, 6, 1f, 0, 240, true));
        undergroundOres.Add(new StructSpawn(StructureCode.AluminiumVeinA, 4, 0.8f, 0, 200, true));
        undergroundOres.Add(new StructSpawn(StructureCode.AluminiumVeinB, 4, 0.8f, 0, 200, true));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeriumVeinA, 3, 0.8f, 0, 160, true));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeriumVeinB, 3, 0.8f, 0, 160, true));
        undergroundOres.Add(new StructSpawn(StructureCode.UraniumVeinA, 2, 0.8f, 0, 150, true));
        undergroundOres.Add(new StructSpawn(StructureCode.UraniumVeinB, 2, 0.8f, 0, 150, true));
        undergroundOres.Add(new StructSpawn(StructureCode.MagnetiteVeinA, 5, 0.8f, 0, 150, true));
        undergroundOres.Add(new StructSpawn(StructureCode.MagnetiteVeinB, 5, 0.8f, 0, 150, true));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeraldVeinA, 2, 0.9f, 0, 200, true));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeraldVeinB, 2, 0.9f, 0, 200, true));
        undergroundOres.Add(new StructSpawn(StructureCode.RubyVeinA, 2, 0.9f, 0, 200, true));
        undergroundOres.Add(new StructSpawn(StructureCode.RubyVeinB, 2, 0.9f, 0, 200, true));        
    }
}

public enum StructureGroupID : int
{
    PLAINS_TREES,
    GRASS_HIGHLANDS_TREES,
    DESERT_TREES,
    ICE_PLAINS_TREES,
    ICE_HIGHLANDS_TREES,
    FOREST_TREES,
    ICE_FOREST_TREES,
    SURFACE_ORES,
    DIRT_PATCHES,
    GRAVEL_PATCHES_SURFACE,
    GRAVEL_PATCHES,
    BOULDERS_LOW_DENSITY,
    BOULDERS_MID_DENSITY,
    UNDERGROUND_ORES
}