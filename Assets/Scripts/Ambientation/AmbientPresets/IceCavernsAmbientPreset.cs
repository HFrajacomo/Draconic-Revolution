using UnityEngine;
using Unity.Mathematics;

public class IceCavernsAmbientPreset: BaseAmbientPreset{
	public IceCavernsAmbientPreset(){
		this.horizonTintDay = new Color(0f, .87f, 1f);
		this.zenithTintDay = Color.white;
		this.cloudTintDay = Color.white;
		this.gainDay = new float4(.6f, .65f, .68f, 0f);

		this.fogAttenuation1 = 8f;
		this.fogAlbedo = Color.white;
		this.fogAmbientLight = .25f;

		this.wbTemperature = -7f;
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