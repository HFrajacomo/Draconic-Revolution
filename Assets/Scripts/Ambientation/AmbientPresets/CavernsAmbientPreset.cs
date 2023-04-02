using UnityEngine;
using Unity.Mathematics;

public class CavernsAmbientPreset: BaseAmbientPreset{
	public CavernsAmbientPreset(){
		this.horizonTintDay = new Color(0.1724368f, 0.2021929f, 0.2358491f);
		this.zenithTintDay = new Color(0.3301887f, 0.3301887f, 0.3301887f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0.5f, 0.5f, 0.5f, 0.3333333f);

		this.fogAttenuation1 = 8f;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;

		this.wbTemperature = 10f;
		this.wbTint = 0f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 2.5f;
		this.sunColor = Color.white;
		this.sunDiameter = 0f;
	}
	public override float GetSunDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
}