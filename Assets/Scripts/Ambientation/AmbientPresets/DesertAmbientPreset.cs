using UnityEngine;
using Unity.Mathematics;

public class DesertAmbientPreset: BaseAmbientPreset{
	public DesertAmbientPreset(){
		this.horizonTintSunrise = new Color(.83f, .24f, .01f);
		this.horizonTintDay = new Color(.93f, .54f, .15f);
		this.horizonTintSunset = new Color(.69f, .4f, .85f);
		this.horizonTintNight = new Color(.48f, .3f, .1f);

		this.zenithTintSunrise = new Color(.68f, .37f, .38f);
		this.zenithTintDay = new Color(0f, .87f, 1f);
		this.zenithTintSunset = new Color(.72f, .34f, .03f);
		this.zenithTintNight = new Color(.82f, .74f, .71f);

		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;

		this.cloudTintSunrise = new Color(.08f, .05f, .01f);
		this.cloudTintDay = new Color(.5f, .43f, .28f);
		this.cloudTintSunset = new Color(.7f, .28f, 0f);
		this.cloudTintNight = new Color(.09f, .08f, .04f);

		this.wbTemperature = 20f;

		this.gainSunrise = new float4(.88f, .64f, .21f, 0f);
		this.gainDay = new float4(.6f, .56f, .45f, .16f);
		this.gainSunset = new float4(.76f, .37f, .32f, .14f);
		this.gainNight = new float4(0f, 0f, 0f, 0f);

		this.rainSpawnRate = 0;

		this.sunDiameter = SUN_DIAMETER_DAY;
		this.moonDiameter = SUN_DIAMETER_NIGHT;
		this.hasFlare = true;
		this.isSurface = true;
	}

	public override Color GetHorizonTint(float t){
		return this.BehaviourColor4(horizonTintSunrise, horizonTintDay, horizonTintSunset, horizonTintNight, t);
	}
	public override Color GetZenithTint(float t){
		return this.BehaviourColor4(zenithTintSunrise, zenithTintDay, zenithTintSunset, zenithTintNight, t);
	}
	public override Color GetCloudTint(float t){
		return this.BehaviourColor4(cloudTintSunrise, cloudTintDay, cloudTintSunset, cloudTintNight, t);
	}
	public override float4 GetGain(float t){
		return this.BehaviourFloat4(gainSunrise, gainDay, gainSunset, gainNight, t);
	}
	public override float2 GetSunRotation(float t){
		return new float2(this.SunRotationX(t), this.SunRotationZ(t));
	}
	public override float2 GetMoonRotation(float t){
		return new float2(this.MoonRotationX(t), this.MoonRotationZ(t));
	}
	public override float GetFloorLighting(float t){
		return this.BehaviourLerpDayNight(FLOOR_LIGHTING_DAY, FLOOR_LIGHTING_NIGHT, t);
	}
	public override int GetRainSpawnRate(WeatherCast ws){return 0;}
}