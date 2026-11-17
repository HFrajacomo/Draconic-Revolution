using UnityEngine;
using Unity.Mathematics;

public class CavernsAmbientPreset: BaseAmbientPreset{
	public CavernsAmbientPreset(){
		this.horizonTintDay = new Color(0.1724368f, 0.2021929f, 0.2358491f);
		this.zenithTintDay = new Color(0.3301887f, 0.3301887f, 0.3301887f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0.5f, 0.5f, 0.5f, 0f);

		this.fogAttenuation = 8f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;
		this.fogAlbedo = BASE_UNDERGROUND_FOG_COLOR;
		this.fogAmbientLight = FOG_AMBIENT_LIGHT_UNDERGROUND;
		this.fogAnisotropy = 0f;

		this.wbTemperature = 10f;
		this.wbTint = 0f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 0f; // No fake sun underground: caves are only lit by Voxel Light and light sources
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
	// Shafts to the surface bring the surface sky's light, and natural light underground only comes from them
	public override Color GetShaftLight(float t){return SurfaceShaftLight(t);}
	public override Vector3 GetShaftLightDirection(float t){return SurfaceShaftLightDirection(t);}
	public override Color GetShaftAmbient(float t){return SurfaceShaftAmbient(t);}
	public override float GetFloorLighting(float t){
		return this.BehaviourLerpDayNight(FLOOR_LIGHTING_DAY, FLOOR_LIGHTING_NIGHT, t);
	}
	public override float GetFogBaseHeight(float t){
		return this.fogBaseHeight;
	}
	public override float GetFogAttenuation(float t){
		return this.fogAttenuation;
	}
	public override int GetRainSpawnRate(WeatherCast ws){return 0;}
}