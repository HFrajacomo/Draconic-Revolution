using UnityEngine;
using Unity.Mathematics;

public class DesertAmbientPreset: BaseAmbientPreset{
	public DesertAmbientPreset(){
		this.aerosolDensitySunrise = 0f;
		this.aerosolDensityDay = 0.2f;
		this.aerosolDensitySunrise = 0.1f;
		this.aerosolDensityNight = 0f;

		this.horizonTintSunrise = new Color(.83f, .24f, .01f);
		this.horizonTintDay = new Color(.93f, .54f, .15f);
		this.horizonTintSunset = new Color(.69f, .4f, .85f);
		this.horizonTintNight = new Color(.48f, .3f, .1f);

		this.zenithTintSunrise = new Color(.68f, .37f, .38f);
		this.zenithTintDay = new Color(0f, .87f, 1f);
		this.zenithTintSunset = new Color(.72f, .34f, .03f);
		this.zenithTintNight = new Color(.82f, .74f, .71f);

		this.fogAttenuation1 = 8f;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;

		this.cloudTintSunrise = new Color(.08f, .05f, .01f);
		this.cloudTintDay = new Color(.5f, .43f, .28f);
		this.cloudTintSunset = new Color(.7f, .28f, 0f);
		this.cloudTintNight = new Color(.09f, .08f, .04f);;

		this.wbTemperature = 20f;

		this.gainSunrise = new float4(.88f, .64f, .21f, 0f);
		this.gainDay = new float4(.6f, .56f, .45f, .16f);
		this.gainSunset = new float4(.76f, .37f, .32f, .14f);
		this.gainNight = new float4(0f, 0f, 0f, 0f);
	}

	public override float GetAerosolDensity(int t){
		return this.BehaviourLerp4(aerosolDensitySunrise, aerosolDensityDay, aerosolDensitySunset, aerosolDensityNight, t);
	}
	public override Color GetHorizonTint(int t){
		return this.BehaviourColor4(horizonTintSunrise, horizonTintDay, horizonTintSunset, horizonTintNight, t);
	}
	public override Color GetZenithTint(int t){
		return this.BehaviourColor4(zenithTintSunrise, zenithTintDay, zenithTintSunset, zenithTintNight, t);
	}
	public override Color GetCloudTint(int t){
		return this.BehaviourColor4(cloudTintSunrise, cloudTintDay, cloudTintSunset, cloudTintNight, t);
	}
	public override float4 GetGain(int t){
		return this.BehaviourFloat4(gainSunrise, gainDay, gainSunset, gainNight, t);
	}
	public override float GetSunIntensity(int t){
		return this.BehaviourFlipDayNight<float>(SURFACE_LIGHT_LUMINOSITY_DAY, SURFACE_LIGHT_LUMINOSITY_NIGHT, t);
	}
	public override float2 GetSunRotation(int t){
		return new float2(this.SunRotationX(t), this.SunRotationZ(t));
	}
	public override Color GetSunColor(int t){
		return this.BehaviourFlipDayNight<Color>(SURFACE_LIGHT_COLOR_DAY, SURFACE_LIGHT_COLOR_NIGHT, t);
	}
}