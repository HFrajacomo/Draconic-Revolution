using UnityEngine;
using Unity.Mathematics;

public class HellAmbientPreset: BaseAmbientPreset{
	public HellAmbientPreset(){
		this.horizonTintDay = new Color(1f, 0f, 0f);
		this.zenithTintDay = new Color(0f, 0f, 0f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0.6981132f, 0.3062477f, 0.3062477f, 0.6392157f);

		this.fogAttenuation = 2.8f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogAlbedo = new Color(.51f, .08f, .08f);
		this.fogAmbientLight = 0f;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;
		this.fogAnisotropy = FOG_BASE_ANISOTROPY;

		this.wbTemperature = 40f;
		this.wbTint = 0f;

		this.expCompensation = 0.2f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 5f;
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
	public override float GetExposureCompensation(){return this.expCompensation;}
}