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
	private float[] lerpX = new float[]{-1f, -.55f, -.45f, -.35f, -.25f, -.05f, 0.05f, .45f, .55f, 1.1f};
	private WeatherState[] lerpY = new WeatherState[]{WeatherState.RAINY, WeatherState.TRANSITION, WeatherState.OVERCAST,
		WeatherState.TRANSITION, WeatherState.CLOUDY2, WeatherState.TRANSITION,  WeatherState.CLOUDY1, WeatherState.TRANSITION, WeatherState.SUNNY};

	// Fog Attenuation Dictionary
	private Dictionary<WeatherState, float> stateFogMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, FOG_RAINY},
		{WeatherState.OVERCAST, FOG_OVERCAST},
		{WeatherState.CLOUDY2, FOG_CLOUDY2},
		{WeatherState.CLOUDY1, FOG_CLOUDY1},
		{WeatherState.SUNNY, FOG_SUNNY}
	};

	// Fog Max Height Dictionary
	private Dictionary<WeatherState, float> stateFogMaxMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, FOG_MAX_HEIGHT_RAIN},
		{WeatherState.OVERCAST, FOG_MAX_HEIGHT_OVERCAST},
		{WeatherState.CLOUDY2, FOG_MAX_HEIGHT},
		{WeatherState.CLOUDY1, FOG_MAX_HEIGHT},
		{WeatherState.SUNNY, FOG_MAX_HEIGHT}
	};

	// Fog Base Height Dictionary
	private Dictionary<WeatherState, float> stateFogBaseMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, FOG_BASE_HEIGHT_RAIN},
		{WeatherState.OVERCAST, FOG_BASE_HEIGHT_RAIN},
		{WeatherState.CLOUDY2, FOG_BASE_HEIGHT},
		{WeatherState.CLOUDY1, FOG_BASE_HEIGHT},
		{WeatherState.SUNNY, FOG_BASE_HEIGHT}
	};

	// Fog Color Dictionary
	private Dictionary<WeatherState, Color> stateFogColorMap = new Dictionary<WeatherState, Color>(){
		{WeatherState.RAINY, FOG_COLOR_RAIN},
		{WeatherState.OVERCAST, FOG_COLOR_NORMAL},
		{WeatherState.CLOUDY2, FOG_COLOR_NORMAL},
		{WeatherState.CLOUDY1, FOG_COLOR_NORMAL},
		{WeatherState.SUNNY, FOG_COLOR_NORMAL}
	};

	// Cloud B Multiplier Dictionary
	private Dictionary<WeatherState, float> stateCloudBMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, CLOUD_BLAYER_SHOW},
		{WeatherState.OVERCAST, CLOUD_BLAYER_SHOW},
		{WeatherState.CLOUDY2, CLOUD_BLAYER_SHOW},
		{WeatherState.CLOUDY1, CLOUD_BLAYER_HIDE},
		{WeatherState.SUNNY, CLOUD_BLAYER_HIDE}
	};

	// Cloud Local Opacity Dictionary
	private Dictionary<WeatherState, float> stateCloudLocalOpacityMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, CLOUD_LOCAL_OPACITY_OVERCAST},
		{WeatherState.OVERCAST, CLOUD_LOCAL_OPACITY_CLOUDY},
		{WeatherState.CLOUDY2, CLOUD_LOCAL_OPACITY_NORMAL},
		{WeatherState.CLOUDY1, CLOUD_LOCAL_OPACITY_NORMAL},
		{WeatherState.SUNNY, CLOUD_LOCAL_OPACITY_NORMAL}
	};

	// Cloud Global Opacity Dictionary
	private Dictionary<WeatherState, float> stateCloudGlobalOpacityMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, CLOUD_GLOBAL_OPACITY_MAX},
		{WeatherState.OVERCAST, CLOUD_GLOBAL_OPACITY_MAX},
		{WeatherState.CLOUDY2, CLOUD_GLOBAL_OPACITY_MAX},
		{WeatherState.CLOUDY1, CLOUD_GLOBAL_OPACITY_MAX},
		{WeatherState.SUNNY, CLOUD_GLOBAL_OPACITY_MIN}
	};

	// Noise Values
	private float weatherNoise = -10;
	private float fogNoise;

	// Cache
	private WeatherState initTransitionState;
	private WeatherState endTransitionState;
	private float minTransitionValue;

	// Constants
	// --- Fog Attenuation
	private static readonly float MAX_FOG_RANDOM = -4f;
	private static readonly float MIN_FOG_RANDOM = 0f;
	private static readonly float FOG_SUNNY = 0f;
	private static readonly float FOG_CLOUDY1 = -1f;
	private static readonly float FOG_CLOUDY2 = -2f;
	private static readonly float FOG_OVERCAST = -3f;
	private static readonly float FOG_RAINY = -5f;
	// ---- Fog Height
	private static readonly float FOG_MAX_HEIGHT = 0f;
	private static readonly float FOG_MAX_HEIGHT_OVERCAST = 80f;
	private static readonly float FOG_MAX_HEIGHT_RAIN = 120f;
	// ---- Fog Base
	private static readonly float FOG_BASE_HEIGHT = 0f;
	private static readonly float FOG_BASE_HEIGHT_RAIN = 100f;
	// ---- Fog Albedo (subtractive)
	private static readonly Color FOG_COLOR_NORMAL = Color.black;
	private static readonly Color FOG_COLOR_RAIN = new Color(.24f, .24f, .24f);
	// ---- Cloud B Tint
	private static readonly float CLOUD_BLAYER_HIDE = 0f;
	private static readonly float CLOUD_BLAYER_SHOW = 1f;
	// ---- Cloud Opacity
	private static readonly float CLOUD_LOCAL_OPACITY_NORMAL = 0.2f;
	private static readonly float CLOUD_LOCAL_OPACITY_CLOUDY = 0.5f;
	private static readonly float CLOUD_LOCAL_OPACITY_OVERCAST = 1f;
	private static readonly float CLOUD_GLOBAL_OPACITY_MAX = 0.2f;
	private static readonly float CLOUD_GLOBAL_OPACITY_MIN = 0f;


	// Parameters
	// ---- Fog
	private float fogFromWeather;
	private float fogFromRandomness;
	private float maximumHeight;
	private float baseHeight;
	private Color fogColor;
	// ---- Clouds
	private float layerBMultiplier;
	private float cloudsLocalOpacity;
	private float cloudsGlobalOpacity;


	// Calculates the entire fog value for Random Fog and Weather Fog
	public float GetAdditionalFog(){return fogFromRandomness+fogFromWeather;}
	public float GetMaximumHeight(){return maximumHeight;}
	public float GetBaseHeight(){return baseHeight;}
	public Color GetSubtractiveFogColor(){return fogColor;}
	public float GetCloudBMultiplier(){return layerBMultiplier;}
	public float GetCloudLocalOpacity(){return cloudsLocalOpacity;}
	public float GetCloudGlobalOpacity(){return cloudsGlobalOpacity;}

	// Sets calculation for Fog Noise
	public void SetFogNoise(int totalTicks, uint days){
		this.fogNoise = NoiseMaker.WeatherNoise(totalTicks*GenerationSeed.fogNoiseStep, days*GenerationSeed.weatherDayStep + World.worldSeed*GenerationSeed.weatherSeedStep);

		if(this.fogNoise < 0)
			this.fogFromRandomness = 0;
		else
			this.fogFromRandomness = Mathf.Lerp(MIN_FOG_RANDOM, MAX_FOG_RANDOM, this.fogNoise);
	}

	// Sets calculation for Weather Noise
	public void SetWeatherNoise(int totalSeconds, uint days){
		this.weatherNoise = NoiseMaker.WeatherNoise(totalSeconds*GenerationSeed.weatherNoiseStep, days*GenerationSeed.weatherDayStep + World.worldSeed*GenerationSeed.weatherSeedStep);

		WeatherState state = GetCurrentWeather(this.weatherNoise);

		if(state == WeatherState.TRANSITION){
			this.fogFromWeather = Mathf.Lerp(stateFogMap[this.initTransitionState], stateFogMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
			this.maximumHeight = Mathf.Lerp(stateFogMaxMap[this.initTransitionState], stateFogMaxMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
			this.baseHeight = Mathf.Lerp(stateFogBaseMap[this.initTransitionState], stateFogBaseMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
			this.fogColor = Color.Lerp(stateFogColorMap[this.initTransitionState], stateFogColorMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
			this.layerBMultiplier = Mathf.Lerp(stateCloudBMap[this.initTransitionState], stateCloudBMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
			this.cloudsLocalOpacity = Mathf.Lerp(stateCloudLocalOpacityMap[this.initTransitionState], stateCloudLocalOpacityMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
			this.cloudsGlobalOpacity = Mathf.Lerp(stateCloudGlobalOpacityMap[this.initTransitionState], stateCloudGlobalOpacityMap[this.endTransitionState], NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue));
		}
		else{
			this.fogFromWeather = this.stateFogMap[state];
			this.maximumHeight = this.stateFogMaxMap[state];
			this.baseHeight = this.stateFogBaseMap[state];
			this.fogColor = this.stateFogColorMap[state];
			this.layerBMultiplier = this.stateCloudBMap[state];
			this.cloudsLocalOpacity = this.stateCloudLocalOpacityMap[state];
			this.cloudsGlobalOpacity = this.stateCloudGlobalOpacityMap[state];
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
