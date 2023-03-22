using UnityEngine;
using Unity.Mathematics;

public class SnowAmbientPreset: BaseAmbientPreset{
	public SnowAmbientPreset(){
		this.horizonTintSunrise = new Color(.96f, .74f, .43f);
		this.horizonTintDay = new Color(0f, .87f, 1f);
		this.horizonTintSunset = new Color(.47f, .18f, 1f);
		this.horizonTintNight = new Color(.66f, .66f, .66f);

		this.zenithTintSunrise = Color.white;
		this.zenithTintDay = Color.white;
		this.zenithTintSunset = new Color(.42f, .15f, .05f);
		this.zenithTintNight = new Color(.13f, .13f, .13f);

		this.fogAttenuation1 = 8f;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;

		this.cloudTintSunrise = new Color(.27f, .27f, .27f);
		this.cloudTintDay = Color.white;
		this.cloudTintSunset = new Color(.16f, .04f, .02f);
		this.cloudTintNight = Color.black;

		this.wbTemperature = -7f;

		this.gainSunrise = new float4(.82f, .6f, .32f, .007f);
		this.gainDay = new float4(.6f, .65f, .68f, 0f);
		this.gainSunset = new float4(.68f, .3f, .53f, 0f);
		this.gainNight = new float4(.20f, .23f, .29f, .31f);
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
	public override float GetSunIntensity(float t){
		return this.BehaviourFlipDayNight<float>(SURFACE_LIGHT_LUMINOSITY_DAY, SURFACE_LIGHT_LUMINOSITY_NIGHT, t);
	}
	public override float2 GetSunRotation(float t){
		return new float2(this.SunRotationX(t), this.SunRotationZ(t));
	}
	public override Color GetSunColor(float t){
		return this.BehaviourFlipDayNight<Color>(SURFACE_LIGHT_COLOR_DAY, SURFACE_LIGHT_COLOR_NIGHT, t);
	}
	public override float GetSunDiameter(float t){
		return this.BehaviourFlipDayNight<float>(SUN_DIAMETER_DAY, SUN_DIAMETER_NIGHT, t);
	}
	public override float GetFloorLighting(float t){
		return this.BehaviourFlipDayNight(FLOOR_LIGHTING_DAY, FLOOR_LIGHTING_NIGHT, t);
	}
}