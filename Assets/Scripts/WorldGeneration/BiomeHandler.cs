﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BiomeHandler
{
	// Main References
	public WorldGenerator generator;

	// Biome Information
	public static Dictionary<byte, Biome> dataset = new Dictionary<byte, Biome>();
	public static Dictionary<byte, string> codeToBiome = new Dictionary<byte, string>();
	public ushort[] biomeBlendingValue;
	private BiomeTable biomeTable;

	private int currentBiome = 0;

	// Cache Information
	private static byte[] cachedByte = new byte[1];
	private Dictionary<Biome, int> score = new Dictionary<Biome, int>();


	public BiomeHandler(){
		this.biomeTable = new BiomeTable();

		Biome plains = new Biome("Plains", BiomeCode.PLAINS, BiomeType.LOW,
		 1, 
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,6,5, 5},
		 new List<float>(){0.2f, 0.1f, 1f, 1f, 0.01f, 1f, 1f, 1f});

		Biome grassyHighlands = new Biome("Grassy Highlands", BiomeCode.GRASSY_HIGHLANDS, BiomeType.PEAK,
		 3,
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,10,8, 14},
		 new List<float>(){0.3f, 0.2f, 1f, 1f, 0.02f, 1f, 1f, 1f});

		Biome ocean = new Biome("Ocean", BiomeCode.OCEAN, BiomeType.OCEAN,
		 8,
		 new List<int>(){},
		 new List<int>(){},
		 new List<float>(){});

		Biome forest = new Biome("Forest", BiomeCode.FOREST, BiomeType.MID,
		 1,
		 new List<int>(){6,1,2,7,8,9,10,11},
		 new List<int>(){1,3,3,1,1,7,7, 7},
		 new List<float>(){0.05f, 1f, 0.5f, 0.1f, 0.3f, 1f, 1f, 1f});

		Biome desert = new Biome("Desert", BiomeCode.DESERT, BiomeType.LOW,
		 8,
		 new List<int>(){5,9,10,11,7},
		 new List<int>(){1,6,6, 6, 1},
		 new List<float>(){0.01f, 1f, 1f, 1f, 0.05f});

		Biome snowPlains = new Biome("Snowy Plains", BiomeCode.SNOWY_PLAINS, BiomeType.LOW,
		 9,
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,6,5, 5},
		 new List<float>(){0.2f, 0.1f, 1f, 1f, 0.01f, 1f, 1f, 1f});

		Biome snowyHighlands = new Biome("Snowy Highlands", BiomeCode.SNOWY_HIGHLANDS, BiomeType.PEAK,
		 9,
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,10,8, 14},
		 new List<float>(){0.3f, 0.2f, 1f, 1f, 0.02f, 1f, 1f, 1f});

		Biome iceOcean = new Biome("Ice Ocean", BiomeCode.ICE_OCEAN, BiomeType.OCEAN,
		 9,
		 new List<int>(){},
		 new List<int>(){},
		 new List<float>(){});

		Biome snowyForest = new Biome("Snow Forest", BiomeCode.SNOWY_FOREST, BiomeType.MID,
		 9,
		 new List<int>(){1,2,8,9,10,11},
		 new List<int>(){3,3,1,7,7, 7},
		 new List<float>(){1f, 0.5f, 0.3f, 1f, 1f, 1f});

		AddBiome(plains);
		AddBiome(grassyHighlands);
		AddBiome(ocean);
		AddBiome(forest);
		AddBiome(desert);
		AddBiome(snowPlains);
		AddBiome(snowyHighlands);
		AddBiome(iceOcean);
		AddBiome(snowyForest);

		this.biomeBlendingValue = new ushort[this.currentBiome];

		for(byte i=0; i < this.currentBiome; i++)
			this.biomeBlendingValue[i] = dataset[i].blendingBlock; 
	}

	// Sets the WorldGenerator obj reference
	public void SetWorldGenerator(WorldGenerator wgen){
		this.generator = wgen;
		this.generator.SetBiomeBlending(this.biomeBlendingValue);
	}

	// Gets biome byte code from name
	public static byte BiomeToByte(string biomeName){
		foreach(Biome b in dataset.Values){
			if(b.name == biomeName)
				return b.biomeCode;
		}
		return 0;
	}

	// Gets biome name from byte code
	public static string ByteToBiome(byte code){
		return codeToBiome[code];
	}


	// Initializes biome in Biome Handler at start of runtime
	private void AddBiome(Biome b){
		dataset.Add(b.biomeCode, b);
		codeToBiome.Add(b.biomeCode, b.name);

		currentBiome++;
	}

	// Clears everything in BiomeHandler
	public void Clear(){
		BiomeHandler.dataset.Clear();
		BiomeHandler.codeToBiome.Clear();
	}

	// Returns the list of possible Structures in a biome
	public static List<int> GetBiomeStructs(BiomeCode biome){
		return dataset[(byte)biome].structCodes;
	}

	// Returns the list of possible Amounts in a biome
	public static List<int> GetBiomeAmounts(BiomeCode biome){
		return dataset[(byte)biome].amountStructs;
	}

	// Returns the list of possible Percentages in a biome
	public static List<float> GetBiomePercentages(BiomeCode biome){
		return dataset[(byte)biome].percentageStructs;
	}

	/*
	Main Function, assigns biome based on 
	*/
	public byte AssignBiome(float[] data){
		float5 biomeInfo = new float5(data[0], data[1], data[2], data[3], data[4]);
		
		return (byte)this.biomeTable.GetBiome(biomeInfo);
	}
}


public struct Biome{
	public string name;
	public byte biomeCode;
	public byte biomeType;

	public ushort blendingBlock;

	public List<int> structCodes;
	public List<int> amountStructs;
	public List<float> percentageStructs;

	public Biome(string n, BiomeCode code, BiomeType type, ushort blendingBlock, List<int> structCodes, List<int> amountStructs, List<float> percentageStructs){
		this.name = n;
		this.biomeCode = (byte)code;
		this.biomeType = (byte)type;

		this.blendingBlock = blendingBlock;
		
		this.structCodes = structCodes;
		this.amountStructs = amountStructs;
		this.percentageStructs = percentageStructs;
	}
}

public struct float5{
	public int t;
	public int h;
	public int b;
	public int e;
	public int p;

	public float5(float x, float y, float z, float w, float k){
		this.t = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (x+1)/2f)));
		this.h = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (y+1)/2f)));
		this.b = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (z+1)/2f)));
		this.e = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (w+1)/2f)));
		this.p = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (k+1)/2f)));

		int LimitValue(int x){
			if(x > BiomeTable.separatorSize-1)
				return BiomeTable.separatorSize-1;
			return x;
		}
	}
}

public enum BiomeType : byte{
	OCEAN,
	LOW,
	MID,
	PEAK
}

public enum BiomeCode : byte{
	PLAINS,
	GRASSY_HIGHLANDS,
	OCEAN,
	FOREST,
	DESERT,
	SNOWY_PLAINS,
	SNOWY_HIGHLANDS,
	ICE_OCEAN,
	SNOWY_FOREST
}