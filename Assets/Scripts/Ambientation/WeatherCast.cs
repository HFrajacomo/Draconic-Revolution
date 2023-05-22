using UnityEngine;

public class WeatherCast{
	private float[] transitionSplineX = new float[]{-1, -.65f, -.55f, -.54999f, -.25f, -.15f, -.14999f, .05f, .15f,  .15001f, .65f, .75f, .75001f, 1.1f};
	private bool[] transitionSplineY = new bool[]{false, true, true,  false,    true,  true,  false,    true,  true, false,   true, true, false, false};
	private float weatherNoise = -10;
	private float fogNoise;

	// Constants
	private static readonly float MAX_FOG_RANDOM = -4f;
	private static readonly float MIN_FOG_RANDOM = 0f;
	//private static readonly float MAX_FOG_WEATHER = -4f;
	//private static readonly float MIN_FOG_WEATHER = 0f;

	// Parameters
	private float fogFromWeather;
	private float fogFromRandomness;

	public float GetAdditionalFog(){return fogFromRandomness+fogFromWeather;}

	public bool IsTransitioning(int ticks, uint days){
		if(this.weatherNoise == -10)
			SetWeatherNoise(ticks, days);

		for(int i=1; i < this.transitionSplineX.Length; i++){
			if(this.weatherNoise <= transitionSplineX[i]){
				return transitionSplineY[i-1];
			}
		}
		return false;
	}

	public void SetFogNoise(int totalTicks, uint days){
		this.fogNoise = NoiseMaker.WeatherNoise(totalTicks*GenerationSeed.fogNoiseStep, days*GenerationSeed.weatherDayStep);

		if(this.fogNoise < 0)
			this.fogFromRandomness = 0;
		else
			this.fogFromRandomness = Mathf.Lerp(MIN_FOG_RANDOM, MAX_FOG_RANDOM, this.fogNoise);
	}

	public void SetWeatherNoise(int totalSeconds, uint days){
		this.weatherNoise = NoiseMaker.WeatherNoise(totalSeconds*GenerationSeed.weatherNoiseStep, days*GenerationSeed.weatherDayStep);

		this.fogFromWeather = 0;//Mathf.Lerp(MIN_FOG_WEATHER, MAX_FOG_WEATHER, NormalizeWeather());
	}

	private float NormalizeWeather(){return (this.weatherNoise+1)/2;}

}

//-1  Rainy
//-.6 Rainy - Overcast
// -.2 Overcast - Cloudy 2
// .1 Cloudy 2
// .7 Cloudy 1
// 1 Sunny