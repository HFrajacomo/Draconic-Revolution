using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureGenerator
{
    public WorldGenerator wgen;

    public StructureGenerator(WorldGenerator wgen){
        this.wgen = wgen;
    }

    public void GenerateBiomeStructures(ChunkLoader_Server cl, ChunkPos pos, BiomeCode biome, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        switch(biome){
            case BiomeCode.PLAINS:
                GeneratePlainsStructures(cl, pos, blockdata, statedata, hpdata);
                break;
            case BiomeCode.GRASSY_HIGHLANDS:
                GenerateGrassyHighlandsStructures(cl, pos, blockdata, statedata, hpdata);
                break;
            case BiomeCode.OCEAN:
                break;
            case BiomeCode.FOREST:
                GenerateForestStructures(cl, pos, blockdata, statedata, hpdata);
                break;
            case BiomeCode.DESERT:
                GenerateDesertStructures(cl, pos, blockdata, statedata, hpdata);
                break;
            case BiomeCode.SNOWY_PLAINS:
                GenerateSnowyPlainsStructures(cl, pos, blockdata, statedata, hpdata);
                break;
            case BiomeCode.SNOWY_HIGHLANDS:
                GenerateSnowyHighlandsStructures(cl, pos, blockdata, statedata, hpdata);
                break;
            case BiomeCode.ICE_OCEAN:
                break;
            default:
                break;
        }
    }

    public void GeneratePlainsStructures(ChunkLoader_Server cl, ChunkPos pos, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(BiomeCode.PLAINS)){
            if(structCode == 1 || structCode == 2) // Trees
                wgen.GenerateStructures(pos, BiomeCode.PLAINS, structCode, 0, blockdata, statedata, hpdata);
            else if(structCode == 3 || structCode == 4) // Dirt Piles
                wgen.GenerateStructures(pos, BiomeCode.PLAINS, structCode, 3, blockdata, statedata, hpdata, range:true);
            else if(structCode == 5) // Boulder
                wgen.GenerateStructures(pos, BiomeCode.PLAINS, structCode, 1, blockdata, statedata, hpdata);
            else if(structCode >= 9 && structCode <= 11) // Metal veins
                wgen.GenerateStructures(pos, BiomeCode.PLAINS, structCode, 9, blockdata, statedata, hpdata, range:true);                
        }   
    }

    public void GenerateGrassyHighlandsStructures(ChunkLoader_Server cl, ChunkPos pos, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(BiomeCode.GRASSY_HIGHLANDS)){
            if(structCode == 2) // Tree
                wgen.GenerateStructures(pos, BiomeCode.GRASSY_HIGHLANDS, structCode, 0, blockdata, statedata, hpdata);
            else if(structCode == 3 || structCode == 4) // Dirt Piles
                wgen.GenerateStructures(pos, BiomeCode.GRASSY_HIGHLANDS, structCode, 3, blockdata, statedata, hpdata, range:true);
            else if(structCode == 5) // Boulder
                wgen.GenerateStructures(pos, BiomeCode.GRASSY_HIGHLANDS, structCode, 1, blockdata, statedata, hpdata);
            else if(structCode >= 9 && structCode <= 11) // Metal veins
                wgen.GenerateStructures(pos, BiomeCode.GRASSY_HIGHLANDS, structCode, 9, blockdata, statedata, hpdata, range:true);                
        }   
    }

    public void GenerateForestStructures(ChunkLoader_Server cl, ChunkPos pos, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(BiomeCode.FOREST)){
            if(structCode == 6 || structCode == 1 || structCode == 2 || structCode == 8) // Trees
                wgen.GenerateStructures(pos, BiomeCode.FOREST, structCode, 0, blockdata, statedata, hpdata);
            else if(structCode == 3 || structCode == 4) // Dirt Piles
                wgen.GenerateStructures(pos, BiomeCode.FOREST, structCode, 3, blockdata, statedata, hpdata, range:true);
            else if(structCode >= 9 && structCode <= 11) // Metal veins
                wgen.GenerateStructures(pos, BiomeCode.FOREST, structCode, 9, blockdata, statedata, hpdata, range:true);           
        }   
    }

    public void GenerateDesertStructures(ChunkLoader_Server cl, ChunkPos pos, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(BiomeCode.DESERT)){
            if(structCode >= 9 && structCode <= 11) // Metal veins
                wgen.GenerateStructures(pos, BiomeCode.DESERT, structCode, 9, blockdata, statedata, hpdata, range:true);           
        }   
    }

    public void GenerateSnowyPlainsStructures(ChunkLoader_Server cl, ChunkPos pos, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(BiomeCode.SNOWY_PLAINS)){
            if(structCode == 1 || structCode == 2) // Trees
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_PLAINS, structCode, 0, blockdata, statedata, hpdata);
            else if(structCode == 3 || structCode == 4) // Dirt Piles
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_PLAINS, structCode, 3, blockdata, statedata, hpdata, range:true);
            else if(structCode == 5) // Boulder
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_PLAINS, structCode, 1, blockdata, statedata, hpdata);
            else if(structCode >= 9 && structCode <= 11) // Metal veins
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_PLAINS, structCode, 9, blockdata, statedata, hpdata, range:true);                
        }   
    }

    public void GenerateSnowyHighlandsStructures(ChunkLoader_Server cl, ChunkPos pos, ushort[] blockdata, ushort[] statedata, ushort[] hpdata){
        foreach(int structCode in BiomeHandler.GetBiomeStructs(BiomeCode.SNOWY_HIGHLANDS)){
            if(structCode == 2) // Tree
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_HIGHLANDS, structCode, 0, blockdata, statedata, hpdata);
            else if(structCode == 3 || structCode == 4) // Dirt Piles
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_HIGHLANDS, structCode, 3, blockdata, statedata, hpdata, range:true);
            else if(structCode == 5) // Boulder
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_HIGHLANDS, structCode, 1, blockdata, statedata, hpdata);
            else if(structCode >= 9 && structCode <= 11) // Metal veins
                wgen.GenerateStructures(pos, BiomeCode.SNOWY_HIGHLANDS, structCode, 9, blockdata, statedata, hpdata, range:true);                
        }   
    }
}
