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

    public BiomeTable(ChunkDepthID layer){
        switch(layer){
            case ChunkDepthID.SURFACE:
                SetupSurfaceBiomes();
                return;
            case ChunkDepthID.UNDERGROUND:
                SetupUndergroundBiomes();
                return;
            default:
                return;
        }
    }

    private void SetupSurfaceBiomes(){
        this.baseErosionTable = new BiomeType[,]{
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID},
            {BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID},
            {BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
            {BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
        };

        this.oceanTempHumidTable = new BiomeCode[,]{
            {BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN},
            {BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN},
            {BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN, BiomeCode.ICE_OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN},
            {BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN, BiomeCode.OCEAN}
        };

        this.lowTempHumidTable = new BiomeCode[,]{
            {BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS},
            {BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS},
            {BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS, BiomeCode.SNOWY_PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS, BiomeCode.PLAINS},
            {BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
            {BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT, BiomeCode.DESERT},
        };

        this.midTempHumidTable = new BiomeCode[,]{
            {BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST},
            {BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST},
            {BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST, BiomeCode.SNOWY_FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
            {BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST, BiomeCode.FOREST},
        };

        this.peakTempHumidTable = new BiomeCode[,]{
            {BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS},
            {BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS},
            {BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS},
            {BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS, BiomeCode.SNOWY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
            {BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS, BiomeCode.GRASSY_HIGHLANDS},
        };
    }

    public void SetupUndergroundBiomes(){
        this.baseErosionTable = new BiomeType[,]{
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.OCEAN, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW},
            {BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID},
            {BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID},
            {BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK},
            {BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
            {BiomeType.LOW, BiomeType.MID, BiomeType.MID, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK, BiomeType.PEAK},
        };

        this.oceanTempHumidTable = new BiomeCode[,]{
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES},
            {BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES, BiomeCode.UNDERWATER_CAVES}
        };

        this.lowTempHumidTable = new BiomeCode[,]{
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES},
            {BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES, BiomeCode.ICE_CAVES}
        };

        this.midTempHumidTable = new BiomeCode[,]{
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS},
            {BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS, BiomeCode.CAVERNS}
        };

        this.peakTempHumidTable = new BiomeCode[,]{
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES},
            {BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES, BiomeCode.BASALT_CAVES}
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
