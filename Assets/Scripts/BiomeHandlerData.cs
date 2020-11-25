using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BiomeHandlerData
{
	// Hashes
	public static readonly float ax = 72.272f;
	public static readonly float az = 23.389f;
	public static readonly float bx = 51.741f;
	public static readonly float bz = 18.864f;
	public static readonly float cx = 35.524f;
	public static readonly float cz = 141.161f;
	public static readonly float dx = 42.271f;
	public static readonly float dz = 533.319f;
	public static readonly float sx = 394.346f;
	public static readonly float sy = 224.3823246f;
	public static readonly float sz = 584.226f;
	public static readonly float sw = 705.002702f;
	public static readonly float ss = 834.3846f;

	public static readonly float featureModificationConstant = 0.00014f;

	public static ushort[] codeToWater;
	public static float4[] codeToStats;

	private int amountOfBiomes;

	public BiomeHandlerData(int amountOfBiomes){
		this.amountOfBiomes = amountOfBiomes;

		codeToWater = new ushort[amountOfBiomes];
		codeToStats = new float4[amountOfBiomes];
	}

	public static ushort GetWaterLevel(byte biome){
		return codeToWater[biome];
	}

	public static float4 GetStats(byte biome){
		return codeToStats[biome];
	}
}
