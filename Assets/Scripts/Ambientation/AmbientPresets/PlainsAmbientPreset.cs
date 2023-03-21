using UnityEngine;
using Unity.Mathematics;

public class PlainsAmbientPreset: BaseAmbientPreset{
	public PlainsAmbientPreset(){
		this.aerosolDensityDay = 0f;

		this.horizonTintDay = new Color(.26f, .9f, .86f);
		this.horizonTintSunrise = new Color(.74f, .82f, .33f);
		this.zenithTintDay = new Color(.29f, .58f, .97f);
		this.zenithTintSunrise = new Color(.81f, .34f, .07f);
		
		this.fogAttenuation1 = 8f;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;

		this.cloudTintDay = Color.white;
		this.cloudTintSunrise = new Color(.23f, .11f, .07f);

		this.wbTemperature = 17f;
		this.wbTint = -4f;

		this.gainSunrise = new float4(1f, .60f, .17f, .54f);
		this.gainDay = new float4(1f, 1f, 1f, 0f);
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