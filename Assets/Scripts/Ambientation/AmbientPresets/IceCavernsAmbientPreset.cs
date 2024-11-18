using UnityEngine;
using Unity.Mathematics;

public class IceCavernsAmbientPreset: BaseAmbientPreset{
	public IceCavernsAmbientPreset(){
		this.horizonTintDay = new Color(0f, .87f, 1f);
		this.zenithTintDay = Color.white;
		this.cloudTintDay = Color.white;
		this.gainDay = new float4(.6f, .65f, .68f, 0f);

		this.fogAttenuation = 8f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogAlbedo = BASE_UNDERGROUND_FOG_COLOR;
		this.fogAmbientLight = .2f;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;
		this.fogAnisotropy = 0f;

		this.wbTemperature = -7f;
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
	public override float GetFogBaseHeight(float t){
		return this.fogBaseHeight;
	}
	public override float GetFogAttenuation(float t){
		return this.fogAttenuation;
	}
	public override float GetSunIntensity(float t){
		return this.lightIntensity;
	}
	public override float GetMoonIntensity(float t){
		return this.lightIntensity;
	}
	public override int GetRainSpawnRate(WeatherCast ws){return 0;}
}