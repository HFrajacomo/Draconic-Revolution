using UnityEngine;
using Unity.Mathematics;

public class PlainsAmbientPreset: BaseAmbientPreset{
	public PlainsAmbientPreset(){
		this.horizonTintSunrise = new Color(.74f, .82f, .33f);
		this.horizonTintDay = new Color(.26f, .9f, .86f);
		this.horizonTintSunset = new Color(.57f, .07f, .35f);
		this.horizonTintNight = new Color(0f, .07f, .16f);

		this.zenithTintDay = new Color(0f, .57f, 1f);
		this.zenithTintSunrise = new Color(.81f, .34f, .07f);
		this.zenithTintSunset = new Color(.82f, .15f, .2f);
		this.zenithTintNight = new Color(.28f, .28f, .28f);
		
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;

		this.cloudTintDay = new Color(.79f, .79f, .79f);
		this.cloudTintSunrise = new Color(.23f, .11f, .07f);
		this.cloudTintSunset = new Color(.02f, .04f, .16f);
		this.cloudTintNight = new Color(.05f, .05f, .05f);

		this.wbTemperature = 17f;
		this.wbTint = -4f;

		this.gainSunrise = new float4(1f, .60f, .17f, .54f);
		this.gainDay = new float4(.1f, .28f, .39f, .14f);
		this.gainSunset = new float4(1f, .61f, .62f, .02f);
		this.gainNight = new float4(0f, 0f, 0f, 0f);

		this.isSurface = true;
		this.hasFlare = true;
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