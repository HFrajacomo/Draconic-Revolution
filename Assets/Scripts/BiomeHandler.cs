using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BiomeHandler
{
	public BiomeHandlerData biomeHandlerData;

	public static Dictionary<byte, Biome> dataset = new Dictionary<byte, Biome>();
	public static Dictionary<byte, string> codeToBiome = new Dictionary<byte, string>();

	private static int amountOfBiomes;
	private int currentBiome = 0;
	private float dispersionSeed;

	// Cache Information
	private static byte[] cachedByte = new byte[1];

	public BiomeHandler(float seed){
		dispersionSeed = seed;

		amountOfBiomes = 4; // SET THIS EVERYTIME YOU ADD A NEW BIOME

		biomeHandlerData = new BiomeHandlerData(amountOfBiomes);

		Biome plains = new Biome("Plains", 0, 0.3f, 0.5f, 0.6f, 1f, 22,
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,3,2, 4},
		 new List<float>(){0.1f, 0.05f, 1f, 1f, 0.01f, 1f, 1f, 1f});

		Biome grassyHighlands = new Biome("Grassy Highlands", 1, 0.7f, 0.5f, 0.6f, 0.9f, 42,
		 new List<int>(){1,2,3,4,5,9,10,11},
		 new List<int>(){1,1,3,2,1,5,4, 8},
		 new List<float>(){0.2f, 0.1f, 1f, 1f, 0.02f, 1f, 1f, 1f});

		Biome ocean = new Biome("Ocean", 2, 0f, 1f, 0.5f, 0.3f, 20,
		 new List<int>(){},
		 new List<int>(){},
		 new List<float>(){});

		Biome forest = new Biome("Forest", 3, 0.3f, 0.5f, 0.5f, 0.7f, 21,
		 new List<int>(){6,1,2,7,8,9,10,11},
		 new List<int>(){1,2,2,1,1,3,2, 4},
		 new List<float>(){0.05f, 1f, 0.5f, 0.1f, 0.3f, 1f, 1f, 1f});

		AddBiome(plains);
		AddBiome(grassyHighlands);
		AddBiome(ocean);
		AddBiome(forest);


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

		BiomeHandlerData.codeToWater[currentBiome] = b.waterLevel;
		BiomeHandlerData.codeToStats[currentBiome] = new float4(b.altitude, b.humidity, b.temperature, b.lightning);

		currentBiome++;
	}

	// Returns the Water Level of a biome
	public static ushort GetWaterLevel(byte biome){
		if(biome == 255)
			return 99;

		return BiomeHandlerData.codeToWater[biome];
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
	BiomeHandler's main function
	Used to assign a biome to a new chunk.
	Play arround with the seed value in each of the 4 biome features to change the behaviour
		of the biome distribution.
	*/
	public byte Assign(ChunkPos pos){
		float currentAltitude = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.ax+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.az+((dispersionSeed*BiomeHandlerData.sx)%1000));
		float currentHumidity = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.bx+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.bz+((dispersionSeed*BiomeHandlerData.sy)%1000));
		float currentTemperature = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.cx+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.cz+((dispersionSeed*BiomeHandlerData.sz)%1000));
		float currentLightning = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.dx+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.dz+((dispersionSeed*BiomeHandlerData.sw)%1000));

		float lowestDistance = 99;
		byte lowestBiome = 255;
		float distance;

		float4 currentSettings = new float4(currentAltitude, currentHumidity, currentTemperature, currentLightning);

		for(byte s=0; s < amountOfBiomes; s++){
			distance = BiomeHandler.Distance(currentSettings, BiomeHandlerData.codeToStats[s]);

			if(distance <= lowestDistance){
				lowestDistance = distance;
				lowestBiome = s;
			}
		}
		return lowestBiome;
	}


	public float4 GetFeatures(ChunkPos pos){
		float currentAltitude = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.ax+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.az+((dispersionSeed*BiomeHandlerData.sx)%1000));
		float currentHumidity = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.bx+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.bz+((dispersionSeed*BiomeHandlerData.sy)%1000));
		float currentTemperature = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.cx+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.cz+((dispersionSeed*BiomeHandlerData.sz)%1000));
		float currentLightning = Perlin.Noise(pos.x*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.dx+((dispersionSeed*BiomeHandlerData.ss)%1000), pos.z*BiomeHandlerData.featureModificationConstant*BiomeHandlerData.dz+((dispersionSeed*BiomeHandlerData.sw)%1000));

		return new float4(currentAltitude, currentHumidity, currentTemperature, currentLightning);
	}

	// Calculates distance between two 4D points
	public static float Distance(float4 first, float4 second){
		return Mathf.Sqrt(Mathf.Pow(first.x-second.x, 2) + Mathf.Pow(first.y-second.y, 2) + Mathf.Pow(first.z-second.z, 2) + Mathf.Pow(first.w-second.w, 2));		
	}

}


public struct Biome{
	public string name;
	public byte biomeCode;
	public float altitude;
	public float humidity;
	public float temperature;
	public float lightning;

	public List<int> structCodes;
	public List<int> amountStructs;
	public List<float> percentageStructs;
	public ushort waterLevel;

	public Biome(string n, byte code, float a, float h, float t, float l, ushort water, List<int> structCodes, List<int> amountStructs, List<float> percentageStructs){
		this.name = n;
		this.biomeCode = code;
		this.altitude = a;
		this.humidity = h;
		this.temperature = t;
		this.lightning = l;

		this.waterLevel = water;

		this.structCodes = structCodes;
		this.amountStructs = amountStructs;
		this.percentageStructs = percentageStructs;
	}

	public float4 GetStats(){
		return new float4(this.altitude, this.humidity, this.temperature, this.lightning);
	}
}