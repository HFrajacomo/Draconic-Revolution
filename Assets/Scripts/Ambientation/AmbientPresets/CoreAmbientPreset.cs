using UnityEngine;
using Unity.Mathematics;

public class CoreAmbientPreset: BaseAmbientPreset{
	public CoreAmbientPreset(){
		this.horizonTintDay = new Color(1f, 1f, 1f);
		this.zenithTintDay = new Color(0f, 0f, 0f);
		this.cloudTintDay = new Color(0f, 0f, 0f);
		this.gainDay = new float4(0f, 0f, 0f, 0f);

		this.fogAttenuation1 = 12f;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = 0.12f;

		this.wbTemperature = 0f;
		this.wbTint = 0f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 3f;
		this.sunColor = Color.white;
	}
	public override float GetSunDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
}