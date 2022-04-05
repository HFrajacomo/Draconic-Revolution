using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeTable
{
    public static int separatorSize = 10;

    private BiomeType[,] baseErosionTable;

    private BiomeCode[,] oceanTempHumidTable;
    private BiomeCode[,] lowTempHumidTable;
    private BiomeCode[,] midTempHumidTable;
    private BiomeCode[,] peakTempHumidTable;

    public BiomeTable(){
        this.baseErosionTable = new BiomeType[,]{
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
        };

        this.oceanTempHumidTable = new BiomeCode[,]{
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN}
        };

        this.lowTempHumidTable = new BiomeCode[,]{
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
        };

        this.midTempHumidTable = new BiomeCode[,]{
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
        };

        this.peakTempHumidTable = new BiomeCode[,]{
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
        };
    }

    public BiomeCode GetBiome(float5 data){
        BiomeType type = this.baseErosionTable[data.b, data.e];

        if(type == BiomeType.OCEAN)
            return this.oceanTempHumidTable[data.t, data.h];
        else if(type == BiomeType.LOW)
            return this.lowTempHumidTable[data.t, data.h];
        else if(type == BiomeType.MID)
            return this.midTempHumidTable[data.t, data.h];
        else if(type == BiomeType.PEAK)
            return this.peakTempHumidTable[data.t, data.h];
        else
            return BiomeCode.DESERT;
    }
}
