using UnityEngine;
using Unity.Mathematics;

public class CoreAmbientPreset: BaseAmbientPreset{
	public CoreAmbientPreset(){
		this.horizonTintDay = new Color(1f, 1f, 1f);
		this.zenithTintDay = new Color(0f, 0f, 0f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0f, 0f, 0f, 0f);

		this.fogAttenuation = 12f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;
		this.fogAlbedo = BASE_UNDERGROUND_FOG_COLOR;
		this.fogAmbientLight = 0.12f;

		this.wbTemperature = 0f;
		this.wbTint = 0f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 3f;
		this.rainSpawnRate = 0;

		this.isSurface = false;
	}
	public override float GetSunIntensity(float t){
		return this.lightIntensity;
	}
	public override float GetMoonIntensity(float t){
		return this.lightIntensity;
	}
	public override float GetSunDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
	public override float GetMoonDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
	public override float GetFogBaseHeight(float t){
		return this.fogBaseHeight;
	}
	public override float GetFogAttenuation(float t){
		return this.fogAttenuation;
	}
	public override int GetRainSpawnRate(WeatherCast ws){return 0;}
}