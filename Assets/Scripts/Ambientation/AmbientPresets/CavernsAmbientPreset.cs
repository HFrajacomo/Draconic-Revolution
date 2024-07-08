using UnityEngine;
using Unity.Mathematics;

public class CavernsAmbientPreset: BaseAmbientPreset{
	public CavernsAmbientPreset(){
		this.horizonTintDay = new Color(0.1724368f, 0.2021929f, 0.2358491f);
		this.zenithTintDay = new Color(0.3301887f, 0.3301887f, 0.3301887f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0.5f, 0.5f, 0.5f, 0.3333333f);

		this.fogAttenuation = 8f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = 0.2f;

		this.wbTemperature = 10f;
		this.wbTint = 0f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 2.5f;
		this.sunDiameter = 0f;
		this.moonDiameter = 0f;

		this.isSurface = false;
		this.rainSpawnRate = 0;
	}
	public override float GetSunDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
	public override float GetMoonDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
	public override float GetSunIntensity(float t){
		return this.lightIntensity;
	}
	public override float GetMoonIntensity(float t){
		return this.lightIntensity;
	}
	public override float GetFogBaseHeight(float t){
		return this.fogBaseHeight;
	}
	public override float GetFogAttenuation(float t){
		return this.fogAttenuation;
	}
	public override int GetRainSpawnRate(WeatherCast ws){return 0;}
}