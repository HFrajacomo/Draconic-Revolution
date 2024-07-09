using UnityEngine;
using Unity.Mathematics;

public class OceanAmbientPreset: BaseAmbientPreset{
	public OceanAmbientPreset(){
		this.horizonTintSunrise = new Color(0.7830189f, 0.6529611f, 0.1440459f);
		this.horizonTintDay = new Color(0.1745283f, 0.9856023f, 1f);
		this.horizonTintSunset = new Color(1f, 0.02020948f, 0f);
		this.horizonTintNight = new Color(0.1585084f, 0.1678471f, 0.245283f);

		this.zenithTintSunrise = new Color(0.1921569f, 0.7647059f, 0.6185005f);
		this.zenithTintDay = new Color(0.08962262f, 0.6109502f, 1f);
		this.zenithTintSunset = new Color(1f, 0.6462264f, 0.8220341f);
		this.zenithTintNight = new Color(0.08250267f, 0.1389117f, 0.1698113f);

		this.fogAlbedo = BASE_FOG_COLOR;
		this.fogAmbientLight = .25f;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;

		this.cloudTintSunrise = new Color(0.08735315f, 0.1189269f, 0.1226415f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.cloudTintSunset = new Color(0.2264151f, 0.1035956f, 0.1271085f);
		this.cloudTintNight = new Color(0.06857421f, 0.1270456f, 0.2169811f);

		this.wbTemperature = 0f;
		this.wbTint = 0f;
		this.gainSunrise = new float4(0.745283f, 0.57064f, 0.3339711f, 0.4f);
		this.gainDay = new float4(1f, 1f, 1f, 0.4f);
		this.gainSunset = new float4(0.8113208f, 0.4581941f, 0.1415984f, 0.4f);
		this.gainNight = new float4(0f, 0f, 0f, 0f);

		this.sunDiameter = SUN_DIAMETER_DAY;
		this.moonDiameter = SUN_DIAMETER_NIGHT;
		this.isSurface = true;
		this.hasFlare = true;
		this.rainSpawnRate = 2500;
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