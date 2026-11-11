using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering.HighDefinition;

public class CoreAmbientPreset: BaseAmbientPreset{
	/*
	New Moon: a big eclipse hanging still in the north. Its lower edge sits where block CORE_MOON_HEIGHT would be seen CORE_MOON_DISTANCE blocks away,
	so it rises in the sky as the player goes deeper. The lower edge never goes below the horizon, so the disc is never cut
	*/
	private static readonly float CORE_MOON_HEIGHT = 128f;
	private static readonly float CORE_MOON_DISTANCE = 1024f;
	private static readonly float CORE_MOON_DIAMETER = 14f;
	// Shadowless light of the New Moon, in lux. It comes from low in the north, so it mostly lights walls facing north
	private static readonly float CORE_MOON_LIGHT = .4f;
	// The sun lights tops and the faces looking south and east. Every face also gets CORE_VOXEL_AMBIENT
	private static readonly float CORE_SUN_ELEVATION = 55f;
	private static readonly float CORE_SUN_AZIMUTH = 135f;
	// Uniform glow of every surface (nits), so faces no light reaches are about 65% as bright as tops
	private static readonly float CORE_VOXEL_AMBIENT = 0.015f;
	// Manual shading at moon phase 0 shows the dark side, lit only by earthshine. The flare is brightest at the disc's edge, drawing a corona
	private static readonly CelestialBodyLook CORE_MOON_LOOK = new CelestialBodyLook{
		shadingSource = HDAdditionalLightData.CelestialBodyShadingSource.Manual,
		moonPhase = 0f,
		moonPhaseRotation = 0f,
		earthshine = 2f,
		diameterMultiplierMode = false,
		diameterMultiplier = 1f,
		diameterOverride = CORE_MOON_DIAMETER,
		surfaceTint = new Color(.35f, .42f, .55f),
		flareSize = 8f,
		flareFalloff = 5f,
		flareTint = new Color(.73f, .34f, .07f),
		flareMultiplier = .8f
	};

	public CoreAmbientPreset(){
		// Black sky, like deep space. Tints multiply the sky's scattering
		this.horizonTintDay = Color.black;
		this.zenithTintDay = Color.black;
		this.cloudTintDay = new Color(1f, 1f, 1f);
		this.gainDay = new float4(0f, 0f, 0f, 0f);

		this.fogAttenuation = 1000f;
		this.fogBaseHeight = BASE_FOG_HEIGHT_UNDERGROUND;
		this.fogMaximumHeight = FOG_MAX_HEIGHT_SURFACE;
		this.fogAlbedo = BASE_UNDERGROUND_FOG_COLOR;
		this.fogAmbientLight = 0.3f;
		this.fogAnisotropy = FOG_BASE_ANISOTROPY;

		this.wbTemperature = 0f;
		this.wbTint = 0f;

		this.expOffset = -0.5f;

		this.sunRotation = new float2(90f, 0f);
		this.lightIntensity = .8f;
		this.rainSpawnRate = 0;

		this.isSurface = false;
	}
	public override float GetSunIntensity(float t){
		return this.lightIntensity;
	}
	// Azimuth is measured clockwise from north (+Z), so 135 is the south-east
	public override Quaternion GetSunOrientation(float t){
		float elevation = CORE_SUN_ELEVATION * Mathf.Deg2Rad;
		float azimuth = CORE_SUN_AZIMUTH * Mathf.Deg2Rad;
		Vector3 towardsSun = new Vector3(Mathf.Cos(elevation) * Mathf.Sin(azimuth), Mathf.Sin(elevation), Mathf.Cos(elevation) * Mathf.Cos(azimuth));

		return Quaternion.LookRotation(-towardsSun, Vector3.up);
	}
	public override float GetVoxelAmbient(){return CORE_VOXEL_AMBIENT;}
	public override float GetMoonIntensity(float t){
		return CORE_MOON_LIGHT;
	}
	// North is +Z. Core is the lowest layer, so the camera's world height is its height inside the Core chunk
	public override Quaternion GetMoonOrientation(float t, Vector3 cameraPosition){
		float lowerEdge = Mathf.Max(Mathf.Atan2(CORE_MOON_HEIGHT - cameraPosition.y, CORE_MOON_DISTANCE) * Mathf.Rad2Deg, 0f);
		return Quaternion.Euler(lowerEdge + CORE_MOON_DIAMETER/2f, 180f, 0f);
	}
	// Stars are always out in Core's deep space sky
	public override float GetStarMapMultiplier(float t){return STAR_MAP_MULTIPLIER_NIGHT;}
	// Below the horizon is black as well
	public override Color GetGroundTint(){return Color.black;}
	public override CelestialBodyLook GetMoonLook(){return CORE_MOON_LOOK;}
	// The sun is Core's ambient light, so it must light everything regardless of the terrain above
	public override bool HasCelestialShadows(){return false;}
	public override float GetSunDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
	public override float GetMoonDiameter(float t){
		return SUN_DIAMETER_UNDERGROUND;
	}
	public override float GetFogBaseHeight(float t){
		return this.fogBaseHeight;
	}
	public override float GetFogAttenuation(float t){
		return this.fogAttenuation;
	}

	public override float GetIndirectDiffuse(float t){return .8f;}
	public override float GetCaveDarkness(){return 0f;}
	public override bool HasClouds(){return false;}
	public override int GetRainSpawnRate(WeatherCast ws){return 0;}
}