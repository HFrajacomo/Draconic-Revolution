using System.Collections.Generic;
using UnityEngine;

/* -1  Rainy
// -.6 Rainy - Overcast
// -.2 Overcast - Cloudy 2
// .1 Cloudy 2
// .6 Cloudy 1
// 1 Sunny
*/
public class WeatherCast{
	// Splines
	private float[] lerpX = new float[]{-1f, -.65f, -.55f, -.25f, -.15f, .05f, 0.15f, .55f, .65f, 1.1f};
	private WeatherState[] lerpY = new WeatherState[]{WeatherState.RAINY, WeatherState.TRANSITION, WeatherState.OVERCAST,
		WeatherState.TRANSITION, WeatherState.CLOUDY2, WeatherState.TRANSITION,  WeatherState.CLOUDY1, WeatherState.TRANSITION, WeatherState.SUNNY};

	// Fog Dictionary
	private Dictionary<WeatherState, float> stateFogMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, FOG_RAINY},
		{WeatherState.OVERCAST, FOG_OVERCAST},
		{WeatherState.CLOUDY2, FOG_CLOUDY2},
		{WeatherState.CLOUDY1, FOG_CLOUDY1},
		{WeatherState.SUNNY, FOG_SUNNY}
	};

	// Noise Values
	private float weatherNoise = -10;
	private float fogNoise;

	// Cache
	private WeatherState initTransitionState;
	private WeatherState endTransitionState;
	private float minTransitionValue;

	// Constants
	private static readonly float MAX_FOG_RANDOM = -4f;
	private static readonly float MIN_FOG_RANDOM = 0f;
	private static readonly float FOG_SUNNY = 0f;
	private static readonly float FOG_CLOUDY1 = -1f;
	private static readonly float FOG_CLOUDY2 = -2f;
	private static readonly float FOG_OVERCAST = -3.5f;
	private static readonly float FOG_RAINY = -6f;

	// Parameters
	private float fogFromWeather;
	private float fogFromRandomness;

	// Calculates the entire fog value for Random Fog and Weather Fog
	public float GetAdditionalFog(){return fogFromRandomness+fogFromWeather;}

	// Sets calculation for Fog Noise
	public void SetFogNoise(int totalTicks, uint days){
		this.fogNoise = NoiseMaker.WeatherNoise(totalTicks*GenerationSeed.fogNoiseStep, days*GenerationSeed.weatherDayStep);

		if(this.fogNoise < 0)
			this.fogFromRandomness = 0;
		else
			this.fogFromRandomness = Mathf.Lerp(MIN_FOG_RANDOM, MAX_FOG_RANDOM, this.fogNoise);
	}

	// Sets calculation for Weather Noise
	public void SetWeatherNoise(int totalSeconds, uint days){
		this.weatherNoise = NoiseMaker.WeatherNoise(totalSeconds*GenerationSeed.weatherNoiseStep, days*GenerationSeed.weatherDayStep);

		WeatherState state = GetCurrentWeather(this.weatherNoise);

		if(state == WeatherState.TRANSITION){
			this.fogFromWeather = Mathf.Lerp(stateFogMap[this.initTransitionState], stateFogMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
		}
		else{
			this.fogFromWeather = this.stateFogMap[state];
		}
	}

	private float NormalizeRange(float noise, float range, float min){
		return (noise-min)/range;
	}

	private WeatherState GetCurrentWeather(float noise){
		int index = 0;
		float min;
		WeatherState state;

		for(int i=1; i < lerpX.Length; i++){
			if(noise <= lerpX[i]){
				index = i-1;
				min = lerpX[i-1];
				break;
			}
		}

		state = lerpY[index];

		if(state == WeatherState.TRANSITION){
			this.initTransitionState = lerpY[index-1];
			this.endTransitionState = lerpY[index+1];
			this.minTransitionValue = lerpX[index];
		}

		return state;
	}

	// TESTING FUNCTION
	public void Print(){
		Debug.Log("Random: " + this.fogNoise + " -> " + this.fogFromRandomness + "\nWeather: " + this.weatherNoise + " -> " + this.fogFromWeather);
	}
}

