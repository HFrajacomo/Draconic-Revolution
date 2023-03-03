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
    private static List<StructSpawn> smallBoneFormation = new List<StructSpawn>();
    private static List<StructSpawn> greaterBonesFormation = new List<StructSpawn>();

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
        PopulateSmallBoneFormation();
        map.Add(StructureGroupID.SMALL_BONES, smallBoneFormation);
        PopulateGreaterBoneFormation();
        map.Add(StructureGroupID.GREATER_BONES, greaterBonesFormation);
    }

    public static void AddStructureGroup(StructureGroupID id, Biome b){
        foreach(StructSpawn s in map[id]){
            s.AddToSpawn(b.structCodes, b.amountStructs, b.percentageStructs, b.depthValues, b.hardSetDepth, b.hasRange, b.minHeight);
        }
    }


    private static void PopulatePlainsTrees(){
        plainTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.2f, 0, -1, false, 0));
        plainTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.1f, 0, -1, false, 0));        
    }

    private static void PopulateGrassHighlandsTrees(){
        grassHighlandsTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.3f, 0, -1, false, 0));
        grassHighlandsTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.2f, 0, -1, false, 0));             
    }

    private static void PopulateDesertTrees(){
        desertTrees.Add(new StructSpawn(StructureCode.TreeCrookedMediumA, 1, 0.02f, 0, -1, false, 0));
    }

    // To change once Pine Trees are created
    private static void PopulateIcePlainsTrees(){
        icePlainsTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.2f, 0, -1, false, 0));
        icePlainsTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.1f, 0, -1, false, 0)); 
    }
    private static void PopulateIceHighlandsTrees(){
        iceHighlandsTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 1, 0.2f, 0, -1, false, 0));
        iceHighlandsTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 1, 0.1f, 0, -1, false, 0)); 
    }

    private static void PopulateForestTrees(){
        forestTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 3, 1f, 0, -1, false, 0));
        forestTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 2, 0.5f, 0, -1, false, 0));
        forestTrees.Add(new StructSpawn(StructureCode.TreeBigA, 1, 0.1f, 0, -1, false, 0));
    }

    // To Change once Pine Trees are created
    private static void PopulateIceForestTrees(){
        forestTrees.Add(new StructSpawn(StructureCode.TreeSmallA, 3, 1f, 0, -1, false, 0));
        forestTrees.Add(new StructSpawn(StructureCode.TreeMediumA, 3, 0.5f, 0, -1, false, 0));
        forestTrees.Add(new StructSpawn(StructureCode.TreeSmallB, 1, 0.3f, 0, -1, false, 0));
    }

    private static void PopulateSurfaceOres(){
        surfaceOres.Add(new StructSpawn(StructureCode.IronVeinA, 5, 1f, 0, 90, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.IronVeinB, 5, 1f, 0, 90, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.IronVeinC, 5, 1f, 0, 90, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.CoalVeinA, 6, 1f, 0, -1, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.CoalVeinB, 6, 1f, 0, -1, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.CoalVeinC, 6, 1f, 0, -1, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.CopperVeinA, 10, 1f, 0, 120, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.CopperVeinB, 10, 1f, 0, 120, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.TinVeinA, 6, 1f, 0, 100, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.TinVeinB, 6, 1f, 0, 100, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.GoldVeinA, 3, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.GoldVeinB, 3, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.AluminiumVeinA, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.AluminiumVeinB, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeriumVeinA, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeriumVeinB, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.UraniumVeinA, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.UraniumVeinB, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.MagnetiteVeinA, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.MagnetiteVeinB, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeraldVeinA, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.EmeraldVeinB, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.RubyVeinA, 1, 0.5f, 0, 60, true, 0));
        surfaceOres.Add(new StructSpawn(StructureCode.RubyVeinB, 1, 0.5f, 0, 60, true, 0));
    }

    private static void PopulateDirtPatches(){
        dirtPatches.Add(new StructSpawn(StructureCode.DirtPileA, 3, 1f, 3, -1, true, 0));
        dirtPatches.Add(new StructSpawn(StructureCode.DirtPileB, 2, 1f, 3, -1, true, 0));
    }

    private static void PopulateGravelSurfacePatches(){
        gravelSurfacePatches.Add(new StructSpawn(StructureCode.GravelPile, 2, 0.5f, -1, -1, false, 0));
    }

    private static void PopulateGravelPatches(){
        gravelPatches.Add(new StructSpawn(StructureCode.GravelPile, 16, 1f, 1, -1, true, 0));
    }

    private static void PopulateBoulders_LowDensity(){
        boulders_LowDensity.Add(new StructSpawn(StructureCode.BoulderNormalA, 1, 0.02f, 1, -1, false, 0));
    }

    private static void PopulateBoulders_MediumDensity(){
        boulders_MediumDensity.Add(new StructSpawn(StructureCode.BoulderNormalA, 1, 0.05f, 1, -1, false, 0));
    }

    private static void PopulateUndergroundOres(){
        undergroundOres.Add(new StructSpawn(StructureCode.IronVeinA, 12, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.IronVeinB, 12, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.IronVeinC, 12, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.CoalVeinA, 18, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.CoalVeinB, 18, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.CoalVeinC, 18, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.CopperVeinA, 10, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.CopperVeinB, 10, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.TinVeinA, 6, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.TinVeinB, 6, 1f, 0, -1, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.GoldVeinA, 6, 1f, 0, 240, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.GoldVeinB, 6, 1f, 0, 240, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.AluminiumVeinA, 4, 0.8f, 0, 200, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.AluminiumVeinB, 4, 0.8f, 0, 200, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeriumVeinA, 3, 0.8f, 0, 160, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeriumVeinB, 3, 0.8f, 0, 160, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.UraniumVeinA, 2, 0.8f, 0, 150, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.UraniumVeinB, 2, 0.8f, 0, 150, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.MagnetiteVeinA, 5, 0.8f, 0, 150, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.MagnetiteVeinB, 5, 0.8f, 0, 150, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeraldVeinA, 2, 0.9f, 0, 200, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.EmeraldVeinB, 2, 0.9f, 0, 200, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.RubyVeinA, 2, 0.9f, 0, 200, true, 0));
        undergroundOres.Add(new StructSpawn(StructureCode.RubyVeinB, 2, 0.9f, 0, 200, true, 0));        
    }

    private static void PopulateSmallBoneFormation(){
        smallBoneFormation.Add(new StructSpawn(StructureCode.LittleBone1, 8, 0.5f, 0, -1, true, 0));
        smallBoneFormation.Add(new StructSpawn(StructureCode.LittleBone2, 6, 0.5f, 0, -1, true, 0));
    }

    private static void PopulateGreaterBoneFormation(){
        greaterBonesFormation.Add(new StructSpawn(StructureCode.LittleBone1, 2, 0.2f, 0, -1, true, -1));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.LittleBone2, 2, 0.2f, 0, -1, true, -1));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigFossil1, 1, 0.03f, 0, -1, true, -4));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigFossil2, 1, 0.03f, 0, -1, true, -4));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigUpBone, 1, 0.03f, 0, -1, true, -10));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigCrossBone, 1, 0.03f, 0, -1, true, -10));

        /*
        greaterBonesFormation.Add(new StructSpawn(StructureCode.LittleBone1, 8, 0.4f, 10, -1, true, 0));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.LittleBone2, 8, 0.4f, 10, -1, true, 0));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigFossil1, 3, 0.3f, 12, -1, true, 0));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigFossil2, 3, 0.3f, 12, -1, true, 0));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigUpBone, 3, 0.3f, 14, -1, true, 0));
        greaterBonesFormation.Add(new StructSpawn(StructureCode.BigCrossBone, 3, 0.3f, 15, -1, true, 0));
        */
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
    UNDERGROUND_ORES,
    SMALL_BONES,
    GREATER_BONES
}