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

	/*
	v[0] = Resistant Wind X
	v[1] = Resistant Wind Z
	v[2] = Transition to rain [0-1]
	v[3] = Is Raining [0 or 1]
	*/
	private Vector4 windShaderInformation;

	private static readonly int RAIN_TICKS = 800;

	private static readonly float MAX_GLOBAL_WIND_POWER = 20f;
	private static readonly int CLOUD_LAYER_ANGLE_DIFF = 40;
	private static readonly int MAX_CLOUD_MOVEMENT = 300;

	public GlobalWindHandler(CloudLayer cl){
		this.clouds = cl;
		Shader.SetGlobalFloat("_Total_Rain_Ticks", (float)RAIN_TICKS);
	}

	public float GetMaxWindPower(){return MAX_GLOBAL_WIND_POWER;}
	public Vector2 GetGlobalWind(){return this.globalWind;}
	public Vector2 GetGlobalWindResistant(){return this.globalResistantWind;}

	public void Tick(int ticks, int timeInSeconds, int day, bool isRaining){
		float x, z, cloudSpeed, cloudAngle;

		x = NoiseMaker.WeatherNoise((ticks + timeInSeconds*TimeOfDay.tickRate)*GenerationSeed.windNoiseStep1, day*GenerationSeed.windNoiseStep2 + World.worldSeed*GenerationSeed.windNoiseStep2) * MAX_GLOBAL_WIND_POWER;
		z = NoiseMaker.WeatherNoise((ticks + timeInSeconds*TimeOfDay.tickRate)*GenerationSeed.windNoiseStep3, day*GenerationSeed.windNoiseStep4 + World.worldSeed*GenerationSeed.windNoiseStep4) * MAX_GLOBAL_WIND_POWER;
		cloudSpeed = NoiseMaker.NormalizedWeatherNoise1D(timeInSeconds*GenerationSeed.windCloudStep + day*GenerationSeed.windNoiseStep1 + World.worldSeed*GenerationSeed.windNoiseStep3) * MAX_CLOUD_MOVEMENT/2;
		cloudAngle = Mathf.Lerp(0, 360, NoiseMaker.NormalizedWeatherNoise1D(timeInSeconds*GenerationSeed.windCloudOrientStep));

		// Advance rain tick
		if(this.isRainOn && currentTick < RAIN_TICKS){
			currentTick++;
		}

		// Check if started/stopped raining
		if(!this.isRainOn && isRaining){
			isRainOn = true;
			currentTick = 0;
		}
		if(this.isRainOn && !isRaining){
			this.isRainOn = false;
			currentTick = 0;
		}


		// Rain Modifier
		if(this.isRainOn){
			x *= Mathf.Lerp(1, 2, (float)currentTick/RAIN_TICKS);
			z *= Mathf.Lerp(1, 2, (float)currentTick/RAIN_TICKS);
			cloudSpeed *= Mathf.Lerp(1, 2, (float)currentTick/RAIN_TICKS);
		}


		this.globalWind = new Vector2(x, z);
		this.globalResistantWind = new Vector2((x/MAX_GLOBAL_WIND_POWER), (z/MAX_GLOBAL_WIND_POWER));

		this.windShaderInformation = new Vector4(this.globalResistantWind.x, this.globalResistantWind.y, currentTick, ConvertBool(this.isRainOn));

		Shader.SetGlobalVector("_Global_Wind_And_Rain", this.windShaderInformation);
	
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

	private float ConvertBool(bool b){
		if(b)
			return 1;
		return 0;
	}
}