using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

public class GlobalWindHandler{
	private CloudLayer clouds;

	private Vector2 globalWind = Vector2.zero;
	private Vector2 globalResistantWind = Vector2.zero;

	private bool isRainOn = false;
	private int currentTick = 0;
	private static readonly int rainTicks = 40;

	private static readonly float MAX_GLOBAL_WIND_POWER = 40f;
	private static readonly float MAX_RESISTANCE = 0.7f;
	private static readonly int CLOUD_LAYER_ANGLE_DIFF = 40;
	private static readonly int MAX_CLOUD_MOVEMENT = 300;


	public GlobalWindHandler(CloudLayer cl){
		this.clouds = cl;
	}

	public float GetMaxWindPower(){return MAX_GLOBAL_WIND_POWER;}
	public Vector2 GetGlobalWind(){return this.globalWind;}
	public Vector2 GetGlobalWindResistant(){return this.globalResistantWind;}

	public void Tick(int ticks, int timeInSeconds, int day, bool isRaining){
		float x, z, resistance, cloudSpeed, cloudAngle;

		x = NoiseMaker.WeatherNoise((ticks + timeInSeconds*TimeOfDay.tickRate)*GenerationSeed.windNoiseStep1, day*GenerationSeed.windNoiseStep2 + World.worldSeed*GenerationSeed.windNoiseStep2) * MAX_GLOBAL_WIND_POWER/2;
		z = NoiseMaker.WeatherNoise((ticks + timeInSeconds*TimeOfDay.tickRate)*GenerationSeed.windNoiseStep3, day*GenerationSeed.windNoiseStep4 + World.worldSeed*GenerationSeed.windNoiseStep4) * MAX_GLOBAL_WIND_POWER/2;
		resistance = Mathf.Lerp(MAX_RESISTANCE, 1f, NoiseMaker.NormalizedWeatherNoise1D((ticks + timeInSeconds*TimeOfDay.tickRate) * GenerationSeed.windResistanceStep));
		cloudSpeed = NoiseMaker.NormalizedWeatherNoise1D(timeInSeconds*GenerationSeed.windCloudStep + day*GenerationSeed.windNoiseStep1 + World.worldSeed*GenerationSeed.windNoiseStep3) * MAX_CLOUD_MOVEMENT/2;
		cloudAngle = Mathf.Lerp(0, 360, NoiseMaker.NormalizedWeatherNoise1D(timeInSeconds*GenerationSeed.windCloudOrientStep));

		// Advance rain tick
		if(isRainOn && currentTick < rainTicks){
			currentTick++;
		}

		// Check if started/stopped raining
		if(!isRainOn && isRaining){
			isRainOn = true;
			currentTick = 0;
		}
		if(isRainOn && !isRaining){
			isRainOn = false;
			currentTick = 0;
		}


		// Rain Modifier
		if(isRainOn){
			x *= Mathf.Lerp(1, 2, currentTick/rainTicks);
			z *= Mathf.Lerp(1, 2, currentTick/rainTicks);
			cloudSpeed *= Mathf.Lerp(1, 2, currentTick/rainTicks);
		}

		this.globalWind = new Vector2(x, z);
		this.globalResistantWind = new Vector2((x/MAX_GLOBAL_WIND_POWER) * Mathf.Lerp(MAX_RESISTANCE, 1, resistance), (z/MAX_GLOBAL_WIND_POWER) * Mathf.Lerp(MAX_RESISTANCE, 1, resistance));
	
		// Sets Cloud Info
		this.clouds.layerA.scrollSpeed = new WindSpeedParameter(cloudSpeed, WindParameter.WindOverrideMode.Custom, true);
		this.clouds.layerB.scrollSpeed = new WindSpeedParameter(cloudSpeed*.9f, WindParameter.WindOverrideMode.Custom, true);
		this.clouds.layerA.scrollOrientation = new WindOrientationParameter(cloudAngle, WindParameter.WindOverrideMode.Custom, true);
		this.clouds.layerB.scrollOrientation = new WindOrientationParameter(GetAngle(cloudAngle, CLOUD_LAYER_ANGLE_DIFF), WindParameter.WindOverrideMode.Custom, true);
	}

	private float GetAngle(float angle, int diff){
		if(angle + diff > 360)
			return angle - diff;
		else
			return angle + diff;
	}
}