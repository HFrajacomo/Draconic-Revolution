using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BiomeHandler
{
	public static Dictionary<byte, Biome> dataset = new Dictionary<byte, Biome>();
	public static Dictionary<byte, string> codeToBiome = new Dictionary<byte, string>();

	private int currentBiome = 0;

	// Cache Information
	private static byte[] cachedByte = new byte[1];

	public BiomeHandler(){
		Biome plains = new Biome("Plains", BiomeCode.PLAINS,
		 new BiomeRange(0, 3), new BiomeRange(1, 3), new BiomeRange(0f, 0.4f), new BiomeRange(-0.5f, 0), new BiomeRange(-1, 0.4f), 
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,3,2, 4},
		 new List<float>(){0.07f, 0.05f, 1f, 1f, 0.01f, 1f, 1f, 1f});

		Biome grassyHighlands = new Biome("Grassy Highlands", BiomeCode.GRASSY_HIGHLANDS,
		 new BiomeRange(0, 6), new BiomeRange(0, 6), new BiomeRange(0.4f, 1f), new BiomeRange(0, 1), new BiomeRange(0.4f, 1), 
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,5,4, 8},
		 new List<float>(){0.2f, 0.1f, 1f, 1f, 0.02f, 1f, 1f, 1f});

		Biome ocean = new Biome("Ocean", BiomeCode.OCEAN,
		 new BiomeRange(0, 6), new BiomeRange(0, 6), new BiomeRange(-1f, 0f), new BiomeRange(-1f, -0.5f), new BiomeRange(-1, 1f), 
		 new List<int>(){},
		 new List<int>(){},
		 new List<float>(){});

		Biome forest = new Biome("Forest", BiomeCode.FOREST,
		 new BiomeRange(0, 6), new BiomeRange(4, 6), new BiomeRange(0f, 0.4f), new BiomeRange(-0.5f, 0), new BiomeRange(-1, 0.4f), 
		 new List<int>(){6,1,2,7,8,9,10,11},
		 new List<int>(){1,2,2,1,1,3,2, 4},
		 new List<float>(){0.05f, 1f, 0.5f, 0.1f, 0.3f, 1f, 1f, 1f});

		Biome desert = new Biome("Desert", BiomeCode.DESERT,
		 new BiomeRange(4, 6), new BiomeRange(0, 3), new BiomeRange(0f, 0.4f), new BiomeRange(-0.5f, 0), new BiomeRange(-1, 0.4f), 
		 new List<int>(){5,9,10,11},
		 new List<int>(){1,3,2, 4},
		 new List<float>(){0.01f, 1f, 1f, 1f});

		AddBiome(plains);
		AddBiome(grassyHighlands);
		AddBiome(ocean);
		AddBiome(forest);
		AddBiome(desert);
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
	public static List<int> GetBiomeStructs(byte biome){
		return dataset[biome].structCodes;
	}

	// Returns the list of possible Amounts in a biome
	public static List<int> GetBiomeAmounts(byte biome){
		return dataset[biome].amountStructs;
	}

	// Returns the list of possible Percentages in a biome
	public static List<float> GetBiomePercentages(byte biome){
		return dataset[biome].percentageStructs;
	}

	/*
	Main Function, assigns biome based on 
	*/
	public byte AssignBiome(float[] data){
		List<Biome> biomes = new List<Biome>();
		List<int> toRemove = new List<int>();

		foreach(Biome b in dataset.Values)
			biomes.Add(b);

		// Temp
		for(int i=0; i < biomes.Count; i++){
			if(biomes[i].temperatureRange.GetLower() > data[0] || biomes[i].temperatureRange.GetUpper() < data[0]){
				toRemove.Add(i);
			}
		}

		for(int d = toRemove.Count-1; d > 0; d--){
			if(biomes.Count == 1)
				return biomes[0].biomeCode;

			biomes.RemoveAt(toRemove[d]);
			toRemove.RemoveAt(d);
		}

		// Humidity
		for(int i=0; i < biomes.Count; i++){
			if(biomes[i].humidityRange.GetLower() > data[1] || biomes[i].humidityRange.GetUpper() < data[1]){
				toRemove.Add(i);
			}
		}

		for(int d = toRemove.Count-1; d > 0; d--){
			if(biomes.Count == 1)
				return biomes[0].biomeCode;
				
			biomes.RemoveAt(toRemove[d]);
			toRemove.RemoveAt(d);
		}

		// Base
		for(int i=0; i < biomes.Count; i++){
			if(biomes[i].baseRange.GetLower() > data[2] || biomes[i].baseRange.GetUpper() < data[2]){
				toRemove.Add(i);
			}
		}

		for(int d = toRemove.Count-1; d > 0; d--){
			if(biomes.Count == 1)
				return biomes[0].biomeCode;
				
			biomes.RemoveAt(toRemove[d]);
			toRemove.RemoveAt(d);
		}

		// Erosion
		for(int i=0; i < biomes.Count; i++){
			if(biomes[i].erosionRange.GetLower() > data[3] || biomes[i].erosionRange.GetUpper() < data[3]){
				toRemove.Add(i);
			}
		}

		for(int d = toRemove.Count-1; d > 0; d--){
			if(biomes.Count == 1)
				return biomes[0].biomeCode;
				
			biomes.RemoveAt(toRemove[d]);
			toRemove.RemoveAt(d);
		}

		// Peak
		for(int i=0; i < biomes.Count; i++){
			if(biomes[i].peakRange.GetLower() > data[4] || biomes[i].peakRange.GetUpper() < data[4]){
				toRemove.Add(i);
			}
		}

		for(int d = toRemove.Count-1; d > 0; d--){
			if(biomes.Count == 1)
				return biomes[0].biomeCode;
				
			biomes.RemoveAt(toRemove[d]);
			toRemove.RemoveAt(d);
		}

		return biomes[0].biomeCode;
	}

}


public struct Biome{
	public string name;
	public byte biomeCode;

	public BiomeRange temperatureRange;
	public BiomeRange humidityRange;
	public BiomeRange baseRange;
	public BiomeRange erosionRange;
	public BiomeRange peakRange;

	public List<int> structCodes;
	public List<int> amountStructs;
	public List<float> percentageStructs;

	public Biome(string n, BiomeCode code, BiomeRange t, BiomeRange h, BiomeRange b, BiomeRange e, BiomeRange p, List<int> structCodes, List<int> amountStructs, List<float> percentageStructs){
		this.name = n;
		this.biomeCode = (byte)code;
		
		this.temperatureRange = t;
		this.humidityRange = h;
		this.baseRange = b;
		this.erosionRange = e;
		this.peakRange = p;

		this.structCodes = structCodes;
		this.amountStructs = amountStructs;
		this.percentageStructs = percentageStructs;
	}
}

public struct BiomeRange{
	public float x;
	public float y;

	public BiomeRange(float x, float y){
		this.x = x;
		this.y = y;
	} 

	public float GetLower(){return this.x;}
	public float GetUpper(){return this.y;}
}

public enum BiomeCode : byte{
	PLAINS,
	GRASSY_HIGHLANDS,
	OCEAN,
	FOREST,
	DESERT
}