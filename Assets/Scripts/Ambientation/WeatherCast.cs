using System.Collections.Generic;
using UnityEngine;

/* -1  Rainy
// -.6 Rainy - Overcast
// -.2 Overcast - Cloudy 2
// .1 Cloudy 2
// .6 Cloudy 1
// 1 Sunny
//
// Rain: time is split into windows of RAIN_WINDOW_MINUTES. Each window rolls once (NoiseMaker.WeatherRoll)
// and rains in a state if the roll is below that state's rain chance, with the state's rain strength.
// Everything derives from the synced World Seed, Weather Noise and time, so all players see the same weather.
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

	// Flare Intensity Dictionary
	private Dictionary<WeatherState, float> stateFlareIntensityMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, FLARE_INTENSITY_MULTIPLIER_MIN},
		{WeatherState.OVERCAST, FLARE_INTENSITY_MULTIPLIER_MID},
		{WeatherState.CLOUDY2, FLARE_INTENSITY_MULTIPLIER_MAX},
		{WeatherState.CLOUDY1, FLARE_INTENSITY_MULTIPLIER_MAX},
		{WeatherState.SUNNY, FLARE_INTENSITY_MULTIPLIER_MAX}
	};

	// Rain Chance per Window Dictionary
	private Dictionary<WeatherState, float> stateRainChanceMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, RAIN_CHANCE_RAINY},
		{WeatherState.OVERCAST, RAIN_CHANCE_OVERCAST},
		{WeatherState.CLOUDY2, RAIN_CHANCE_CLOUDY2},
		{WeatherState.CLOUDY1, RAIN_CHANCE_NONE},
		{WeatherState.SUNNY, RAIN_CHANCE_NONE}
	};

	// Rain Strength Dictionary
	private Dictionary<WeatherState, float> stateRainStrengthMap = new Dictionary<WeatherState, float>(){
		{WeatherState.RAINY, RAIN_STRENGTH_RAINY},
		{WeatherState.OVERCAST, RAIN_STRENGTH_OVERCAST},
		{WeatherState.CLOUDY2, RAIN_STRENGTH_CLOUDY2},
		{WeatherState.CLOUDY1, RAIN_STRENGTH_NONE},
		{WeatherState.SUNNY, RAIN_STRENGTH_NONE}
	};

	// Noise Values
	private WeatherState weatherState;
	private float weatherNoise = -10;
	private float fogNoise;
	private float lerpValue = .5f;
	private float anisotropyInterval = 0.08f;

	// Cache
	private WeatherState initTransitionState;
	private WeatherState endTransitionState;
	private float minTransitionValue;
	private float transitionProgress;

	// Rain
	private float rainStrength;
	private float rainCurrentState;
	private float rainInitState;
	private float rainEndState;
	private float rollFrom;
	private float rollTo;
	private float rollBlend;

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
	// ---- Flare Intensity
	private static readonly float FLARE_INTENSITY_MULTIPLIER_MAX = 1f;
	private static readonly float FLARE_INTENSITY_MULTIPLIER_MID = .5f;
	private static readonly float FLARE_INTENSITY_MULTIPLIER_MIN = .1f;
	// ---- Rain
	private static readonly int RAIN_WINDOW_MINUTES = 120;
	private static readonly int RAIN_FADE_HALF_MINUTES = 15;
	private static readonly float RAIN_CHANCE_RAINY = 1f;
	private static readonly float RAIN_CHANCE_OVERCAST = .2f;
	private static readonly float RAIN_CHANCE_CLOUDY2 = .08f;
	private static readonly float RAIN_CHANCE_NONE = 0f;
	private static readonly float RAIN_STRENGTH_RAINY = 1f;
	private static readonly float RAIN_STRENGTH_OVERCAST = .6f;
	private static readonly float RAIN_STRENGTH_CLOUDY2 = .35f;
	private static readonly float RAIN_STRENGTH_NONE = 0f;
	

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
	// ---- Flare
	private float flareIntensityMultiplier;


	// Calculates the entire fog value for Random Fog and Weather Fog
	public float GetAdditionalFog(){return fogFromRandomness+fogFromWeather;}
	public float GetMaximumHeight(){return maximumHeight;}
	public float GetBaseHeight(){return baseHeight;}
	public Color GetSubtractiveFogColor(){return fogColor;}
	public float GetCloudBMultiplier(){return layerBMultiplier;}
	public float GetCloudLocalOpacity(){return cloudsLocalOpacity;}
	public float GetCloudGlobalOpacity(){return cloudsGlobalOpacity;}
	public float GetFlareMultiplier(){return flareIntensityMultiplier;}
	public float GetFogAnisotropyMultiplier(){
		if(this.lerpValue < this.lerpX[4])
			return 0f;
		if(this.lerpValue >= this.lerpX[4]+this.anisotropyInterval)
			return 1f;
		return (this.lerpValue - this.lerpX[4])/this.anisotropyInterval;
	}

	// State
	public WeatherState GetWeatherState(){return this.weatherState;}
	public WeatherState GetInitState(){return this.initTransitionState;}
	public WeatherState GetEndState(){return this.endTransitionState;}
	public float GetLerpValue(){return this.lerpValue;}
	public float GetRainStrength(){return this.rainStrength;}
	public bool IsRaining(){return this.rainStrength > 0f;}


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
		this.weatherState = state;

		SetRainWindow(totalSeconds);

		if(state == WeatherState.TRANSITION){
			this.transitionProgress = NormalizeRange(this.weatherNoise, 0.1f, this.minTransitionValue);
			this.rainInitState = GetStateRain(this.initTransitionState);
			this.rainEndState = GetStateRain(this.endTransitionState);
			this.rainStrength = Mathf.Lerp(this.rainInitState, this.rainEndState, this.transitionProgress);
		}
		else{
			this.rainCurrentState = GetStateRain(state);
			this.rainStrength = this.rainCurrentState;
		}

		this.fogFromWeather = GetWeatherValue(this.stateFogMap);
		this.maximumHeight = GetWeatherValue(this.stateFogMaxMap);
		this.baseHeight = GetWeatherValue(this.stateFogBaseMap);
		this.fogColor = GetWeatherValue(this.stateFogColorMap);
		this.layerBMultiplier = GetWeatherValue(this.stateCloudBMap);
		this.cloudsLocalOpacity = GetWeatherValue(this.stateCloudLocalOpacityMap);
		this.cloudsGlobalOpacity = GetWeatherValue(this.stateCloudGlobalOpacityMap);
		this.flareIntensityMultiplier = GetWeatherValue(this.stateFlareIntensityMap);
	}

	// Picks the rain windows to sample and crossfades them around each window boundary
	private void SetRainWindow(int totalMinutes){
		int window = totalMinutes / RAIN_WINDOW_MINUTES;
		int minuteInWindow = totalMinutes % RAIN_WINDOW_MINUTES;
		float fadeLength = RAIN_FADE_HALF_MINUTES*2;

		if(minuteInWindow < RAIN_FADE_HALF_MINUTES){
			this.rollFrom = NoiseMaker.WeatherRoll(window-1);
			this.rollTo = NoiseMaker.WeatherRoll(window);
			this.rollBlend = (minuteInWindow + RAIN_FADE_HALF_MINUTES)/fadeLength;
		}
		else if(minuteInWindow >= RAIN_WINDOW_MINUTES - RAIN_FADE_HALF_MINUTES){
			this.rollFrom = NoiseMaker.WeatherRoll(window);
			this.rollTo = NoiseMaker.WeatherRoll(window+1);
			this.rollBlend = (minuteInWindow - (RAIN_WINDOW_MINUTES - RAIN_FADE_HALF_MINUTES))/fadeLength;
		}
		else{
			this.rollFrom = NoiseMaker.WeatherRoll(window);
			this.rollTo = this.rollFrom;
			this.rollBlend = 0f;
		}
	}

	// Rain strength a non-transition state would have in the current rain window
	private float GetStateRain(WeatherState state){
		float chance = this.stateRainChanceMap[state];
		float strength = this.stateRainStrengthMap[state];
		float from = this.rollFrom < chance ? strength : 0f;
		float to = this.rollTo < chance ? strength : 0f;

		return Mathf.Lerp(from, to, this.rollBlend);
	}

	// Weather parameter of the current state, pulled towards the RAINY look by how much each state is raining
	private float GetWeatherValue(Dictionary<WeatherState, float> map){
		if(this.weatherState == WeatherState.TRANSITION)
			return Mathf.Lerp(BlendWithRain(map, this.initTransitionState, this.rainInitState), BlendWithRain(map, this.endTransitionState, this.rainEndState), this.transitionProgress);
		return BlendWithRain(map, this.weatherState, this.rainCurrentState);
	}

	private Color GetWeatherValue(Dictionary<WeatherState, Color> map){
		if(this.weatherState == WeatherState.TRANSITION)
			return Color.Lerp(BlendWithRain(map, this.initTransitionState, this.rainInitState), BlendWithRain(map, this.endTransitionState, this.rainEndState), this.transitionProgress);
		return BlendWithRain(map, this.weatherState, this.rainCurrentState);
	}

	private float BlendWithRain(Dictionary<WeatherState, float> map, WeatherState state, float rain){
		return Mathf.Lerp(map[state], map[WeatherState.RAINY], rain);
	}

	private Color BlendWithRain(Dictionary<WeatherState, Color> map, WeatherState state, float rain){
		return Color.Lerp(map[state], map[WeatherState.RAINY], rain);
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

			this.lerpValue = Mathf.Lerp(lerpX[index], lerpX[index+1], noise);
		}

		return state;
	}

	// TESTING FUNCTION
	public void Print(){
		Debug.Log("Random: " + this.fogNoise + " -> " + this.fogFromRandomness + "\nWeather: " + this.weatherNoise + " -> " + this.fogFromWeather);
	}
}

