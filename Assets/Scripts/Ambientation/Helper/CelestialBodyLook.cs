using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/*
How a Directional Light is drawn as a celestial body in the Physically Based Sky (shading, moon phase, size and flare).
Lets Ambience Presets give the moon a look of their own. The disc size uses diameterOverride, so it doesn't depend on angularDiameter
*/
public class CelestialBodyLook{
	public HDAdditionalLightData.CelestialBodyShadingSource shadingSource;
	public float moonPhase;
	public float moonPhaseRotation;
	public float earthshine;
	public bool diameterMultiplierMode;
	public float diameterMultiplier;
	public float diameterOverride;
	public Color surfaceTint;
	public float flareSize;
	public float flareFalloff;
	public Color flareTint;
	public float flareMultiplier;

	public static CelestialBodyLook FromLight(HDAdditionalLightData light){
		return new CelestialBodyLook{
			shadingSource = light.celestialBodyShadingSource,
			moonPhase = light.moonPhase,
			moonPhaseRotation = light.moonPhaseRotation,
			earthshine = light.earthshine,
			diameterMultiplierMode = light.diameterMultiplerMode,
			diameterMultiplier = light.diameterMultiplier,
			diameterOverride = light.diameterOverride,
			surfaceTint = light.surfaceTint,
			flareSize = light.flareSize,
			flareFalloff = light.flareFalloff,
			flareTint = light.flareTint,
			flareMultiplier = light.flareMultiplier
		};
	}

	public void ApplyTo(HDAdditionalLightData light){
		light.celestialBodyShadingSource = this.shadingSource;
		light.moonPhase = this.moonPhase;
		light.moonPhaseRotation = this.moonPhaseRotation;
		light.earthshine = this.earthshine;
		light.diameterMultiplerMode = this.diameterMultiplierMode;
		light.diameterMultiplier = this.diameterMultiplier;
		light.diameterOverride = this.diameterOverride;
		light.surfaceTint = this.surfaceTint;
		light.flareSize = this.flareSize;
		light.flareFalloff = this.flareFalloff;
		light.flareTint = this.flareTint;
		light.flareMultiplier = this.flareMultiplier;
	}
}