using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeHandler
{
	public Dictionary<string, Point4D> dataset = new Dictionary<string, Point4D>();
	private float dispersionSeed;

	public BiomeHandler(float seed){
		dispersionSeed = seed;
		AddBiome(new Biome("Plains", 0.3f, 0.5f, 0.6f, 1f));
		AddBiome(new Biome("Grassy Highlands", 0.7f, 0.5f, 0.6f, 0.9f));
	}

	private void AddBiome(Biome b){
		dataset.Add(b.name, new Point4D(b.altitude, b.humidity, b.temperature, b.lightning));
	}

	/*
	BiomeHandler's main function
	Used to assign a biome to a new chunk.
	Play arround with the seed value in each of the 4 biome features to change the behaviour
		of the biome distribution.
	*/
	public string Assign(ChunkPos pos, float seed){
		float currentAltitude = Perlin.Noise(pos.x*dispersionSeed*72.272f+(seed*0.0027f), pos.z*dispersionSeed*23.389f+(seed*0.1027f));
		float currentHumidity = Perlin.Noise(pos.x*dispersionSeed*177.741f+(seed*0.0027f), pos.z*dispersionSeed*18.864f+(seed*0.0327f));
		float currentTemperature = Perlin.Noise(pos.x*dispersionSeed*35.524f+(seed*0.0027f), pos.z*dispersionSeed*141.161f+(seed*0.4027f));
		float currentLightning = Perlin.Noise(pos.x*dispersionSeed*422.271f+(seed*0.0027f), pos.z*dispersionSeed*533.319f+(seed*0.002702f));
	
		float lowestDistance = 99;
		string lowestBiome = "";
		float distance;

		Point4D currentSettings = new Point4D(currentAltitude, currentHumidity, currentTemperature, currentLightning);

		foreach(string s in dataset.Keys){
			distance = Point4D.Distance(currentSettings, dataset[s]);

			if(distance <= lowestDistance){
				lowestDistance = distance;
				lowestBiome = s;
			}
		}
		return lowestBiome;
	}

	public Point4D GetFeatures(ChunkPos pos, float seed){
		float currentAltitude = Perlin.Noise(pos.x*dispersionSeed*72.272f+(seed*0.0027f), pos.z*dispersionSeed*23.389f+(seed*0.1027f));
		float currentHumidity = Perlin.Noise(pos.x*dispersionSeed*177.741f+(seed*0.0027f), pos.z*dispersionSeed*18.864f+(seed*0.0327f));
		float currentTemperature = Perlin.Noise(pos.x*dispersionSeed*35.524f+(seed*0.0027f), pos.z*dispersionSeed*141.161f+(seed*0.4027f));
		float currentLightning = Perlin.Noise(pos.x*dispersionSeed*422.271f+(seed*0.0027f), pos.z*dispersionSeed*533.319f+(seed*0.002702f));
	

		return new Point4D(currentAltitude, currentHumidity, currentTemperature, currentLightning);
	}

}


public struct Biome{
	public string name;
	public float altitude;
	public float humidity;
	public float temperature;
	public float lightning;

	public Biome(string n, float a, float h, float t, float l){
		this.name = n;
		this.altitude = a;
		this.humidity = h;
		this.temperature = t;
		this.lightning = l;
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