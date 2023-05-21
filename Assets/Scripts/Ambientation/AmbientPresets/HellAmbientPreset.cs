using UnityEngine;
using Unity.Mathematics;

public class HellAmbientPreset: BaseAmbientPreset{
	public HellAmbientPreset(){
		this.horizonTintDay = new Color(1f, 0f, 0f);
		this.zenithTintDay = new Color(0f, 0f, 0f);
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0.6981132f, 0.3062477f, 0.3062477f, 0.6392157f);

		this.fogAttenuation1 = 2.8f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogAlbedo = new Color(.51f, .08f, .08f);
		this.fogAmbientLight = 0f;

		this.wbTemperature = 40f;
		this.wbTint = 0f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = 5f;
		this.sunColor = Color.white;
		this.sunDiameter = 0f;

		this.isSurface = false;
	}
	public override float GetSunDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}

	public override float GetBaseFogHeight(float t){
		return this.fogBaseHeight;
	}
}