using UnityEngine;
using Unity.Mathematics;

public class ForestAmbientPreset: BaseAmbientPreset{
	public ForestAmbientPreset(){
		this.horizonTintSunrise = new Color(0.8396226f, 0.6757423f, 0.2970363f);
		this.horizonTintDay = new Color(0.3850125f, 0.7721011f, 0.7924528f);
		this.horizonTintSunset = new Color(0.7735849f, 0.6213959f, 0.3028658f);
		this.horizonTintNight = new Color(0.1523674f, 0.1828009f, 0.3018868f);

		this.zenithTintSunrise = new Color(0.7562377f, 0.9339623f, 0.2775454f);
		this.zenithTintDay = new Color(1f, 1f, 1f);
		this.zenithTintSunset = new Color(0.735849f, 0f, 0.3203108f);
		this.zenithTintNight = new Color(0.1854943f, 0.3584906f, 0.04565682f);

		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;

		this.cloudTintSunrise = new Color(0.3207547f, 0.3207547f, 0.3207547f);
		this.cloudTintDay = new Color(0.7830189f, 0.7793254f, 0.7793254f);
		this.cloudTintSunset = new Color(0.09623479f, 0.06652723f, 0.1226415f);
		this.cloudTintNight = new Color(0.0764062f, 0.1396128f, 0.1603774f);

		this.wbTemperature = -15f;
		this.wbTint = -10f;
		this.gainSunrise = new float4(0.7924528f, 0.5688225f, 0.09344961f, 0f);
		this.gainDay = new float4(0.6959772f, 0.754717f, 0.6799573f, 0f);
		this.gainSunset = new float4(0.8679245f, 0.2894512f, 0.1596654f, 0f);
		this.gainNight = new float4(0.1665321f, 0.2358491f, 0.08343717f, 0f);

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
}