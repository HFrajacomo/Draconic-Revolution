using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeHandler
{
	public static Dictionary<string, Biome> dataset = new Dictionary<string, Biome>();
	public static Dictionary<byte, string> codeToBiome = new Dictionary<byte, string>();
	private float dispersionSeed;
	private float featureModificationConstant = 0.00014f;

	// Cache Information
	private static byte[] cachedByte = new byte[1];

	public BiomeHandler(float seed){
		dispersionSeed = seed;

		Biome plains = new Biome("Plains", 0, 0.3f, 0.5f, 0.6f, 1f,
		 new List<int>(){1,2,3,4,5},
		 new List<int>(){1,1,3,2,1},
		 new List<float>(){0.1f, 0.05f, 1f, 1f, 0.01f});

		Biome grassyHighlands = new Biome("Grassy Highlands", 1, 0.7f, 0.5f, 0.6f, 0.9f,
		 new List<int>(){1,2,3,4,5},
		 new List<int>(){1,1,3,2,1},
		 new List<float>(){0.2f, 0.1f, 1f, 1f, 0.02f}); 

		Biome ocean = new Biome("Ocean", 2, 0f, 1f, 0.5f, 0.3f,
		 new List<int>(){},
		 new List<int>(){},
		 new List<float>(){});

		AddBiome(plains);
		AddBiome(grassyHighlands);
		AddBiome(ocean);
	}

	// Gets biome byte code from name
	public static byte BiomeToByte(string biomeName){
		return dataset[biomeName].biomeCode;
	}

	// Gets biome name from byte code
	public static string ByteToBiome(byte code){
		return codeToBiome[code];
	}


	// Initializes biome in Biome Handler at start of runtime
	private void AddBiome(Biome b){
		dataset.Add(b.name, b);
		codeToBiome.Add(b.biomeCode, b.name);
	}

	// Returns the list of possible Structures in a biome
	public static List<int> GetBiomeStructs(byte biome){
		return dataset[BiomeHandler.ByteToBiome(biome)].structCodes;
	}

	// Returns the list of possible Amounts in a biome
	public static List<int> GetBiomeAmounts(byte biome){
		return dataset[BiomeHandler.ByteToBiome(biome)].amountStructs;
	}

	// Returns the list of possible Percentages in a biome
	public static List<float> GetBiomePercentages(byte biome){
		return dataset[BiomeHandler.ByteToBiome(biome)].percentageStructs;
	}

	/*
	BiomeHandler's main function
	Used to assign a biome to a new chunk.
	Play arround with the seed value in each of the 4 biome features to change the behaviour
		of the biome distribution.
	*/
	public string Assign(ChunkPos pos, float seed){
		float currentAltitude = Perlin.Noise(pos.x*featureModificationConstant*72.272f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*23.389f+((seed*394.346f)%1000));
		float currentHumidity = Perlin.Noise(pos.x*featureModificationConstant*51.741f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*18.864f+((seed*224.3823246f)%1000));
		float currentTemperature = Perlin.Noise(pos.x*featureModificationConstant*35.524f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*141.161f+((seed*584.226f)%1000));
		float currentLightning = Perlin.Noise(pos.x*featureModificationConstant*42.271f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*533.319f+((seed*705.002702f)%1000));

		float lowestDistance = 99;
		string lowestBiome = "";
		float distance;

		Point4D currentSettings = new Point4D(currentAltitude, currentHumidity, currentTemperature, currentLightning);

		foreach(string s in dataset.Keys){
			distance = Point4D.Distance(currentSettings, dataset[s].GetStats());

			if(distance <= lowestDistance){
				lowestDistance = distance;
				lowestBiome = s;
			}
		}
		return lowestBiome;
	}

	public Point4D GetFeatures(ChunkPos pos, float seed){
		float currentAltitude = Perlin.Noise(pos.x*featureModificationConstant*72.272f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*23.389f+((seed*394.346f)%1000));
		float currentHumidity = Perlin.Noise(pos.x*featureModificationConstant*51.741f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*18.864f+((seed*224.3823246f)%1000));
		float currentTemperature = Perlin.Noise(pos.x*featureModificationConstant*35.524f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*141.161f+((seed*584.226f)%1000));
		float currentLightning = Perlin.Noise(pos.x*featureModificationConstant*42.271f+((seed*834.3846f)%1000), pos.z*featureModificationConstant*533.319f+((seed*705.002702f)%1000));
	
		return new Point4D(currentAltitude, currentHumidity, currentTemperature, currentLightning);
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

	public Biome(string n, byte code, float a, float h, float t, float l, List<int> structCodes, List<int> amountStructs, List<float> percentageStructs){
		this.name = n;
		this.biomeCode = code;
		this.altitude = a;
		this.humidity = h;
		this.temperature = t;
		this.lightning = l;

		this.structCodes = structCodes;
		this.amountStructs = amountStructs;
		this.percentageStructs = percentageStructs;
	}

	public Point4D GetStats(){
		return new Point4D(this.altitude, this.humidity, this.temperature, this.lightning);
	}
}

// Used to represent a biome central features in the 1NN model
public struct Point4D{
	public float a;
	public float b;
	public float c;
	public float d;


	public Point4D(float x, float y, float z, float w){
		this.a = x;
		this.b = y;
		this.c = z;
		this.d = w;
	}

	// Calculates the euclidean distance of two Point4D elements 
	public static float Distance(Point4D first, Point4D second){
		return Mathf.Sqrt(Mathf.Pow(first.a-second.a, 2) + Mathf.Pow(first.b-second.b, 2) + Mathf.Pow(first.c-second.c, 2) + Mathf.Pow(first.d-second.d, 2));
	}

	public override string ToString(){
		return "(" + a.ToString("0.##") + ", " + b.ToString("0.##") + ", " + c.ToString("0.##") + ", " + d.ToString("0.##") + ")";
	}
}