using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class AmbientationTestingTool: MonoBehaviour{
	// What Biome is being tested?
	private BaseAmbientPreset preset = new DesertAmbientPreset();

	// Unity Reference
	public TimeOfDay timer;
	public VolumeProfile volume;
	public Light skyDirectionalLight;

	public int stopTimeAt = -1;
	private int lastTimeAt = -1;
	private int lastTime = 0;

    // Skybox Parameters
    private PhysicallyBasedSky pbsky;
    private CloudLayer clouds;
    private WhiteBalance whiteBalance;
    private Fog fog;
    private LiftGammaGain lgg;

	// Physically based sky
	public float aerosolDensity_sunrise;
	public float aerosolDensity_day;
	public float aerosolDensity_sunset;
	public float aerosolDensity_night;

	public Color horizonTint_sunrise;
	public Color horizonTint_day;
	public Color horizonTint_sunset;
	public Color horizonTint_night;

	public Color zenithTint_sunrise;
	public Color zenithTint_day;
	public Color zenithTint_sunset;
	public Color zenithTint_night;

	// Fog
	public float fogAttenuation1;
	public float fogAttenuation2;
	public Color fogAlbedo;
	public float fogAmbientLight;

	// Cloud Layer
	public Color cloudTint_sunrise;
	public Color cloudTint_day;
	public Color cloudTint_sunset;
	public Color cloudTint_night;

	// White Balance
	public float wbTemperature;
	public float wbTint;

	// Lift, Gamma, Gain
	public Color gain_sunrise;
	public Color gain_day;
	public Color gain_sunset;
	public Color gain_night;

	// Directional Light
	public float lightIntensity;
	public float2 sunRotation;
	public Color sunColor;

	void Start(){
        this.volume.TryGet<PhysicallyBasedSky>(out this.pbsky);
        this.volume.TryGet<CloudLayer>(out this.clouds);
        this.volume.TryGet<WhiteBalance>(out this.whiteBalance);
        this.volume.TryGet<Fog>(out this.fog);
        this.volume.TryGet<LiftGammaGain>(out this.lgg);

        SetValues(preset);
	}

	void Update(){
		int time = this.timer.ToSeconds();
		preset.SetValues(this);

		if(stopTimeAt >= 0){
			time = stopTimeAt;
		}

		SimulateNormally(time);
		lastTimeAt = stopTimeAt;
		lastTime = time;
	}

	private void SimulateNormally(int time){
		this.pbsky.horizonTint.value = preset.GetHorizonTint(time);
		this.pbsky.zenithTint.value = preset.GetZenithTint(time);
		this.pbsky.aerosolDensity.value = preset.GetAerosolDensity(time);
		this.fog.meanFreePath.value = preset.GetFogAttenuation(time);
		this.fog.albedo.value = preset.GetFogAlbedo(time);
		this.fog.globalLightProbeDimmer.value = preset.GetFogAmbientLight(time);
		this.clouds.layerA.tint.value = preset.GetCloudTint(time);
		this.whiteBalance.temperature.value = preset.GetWhiteBalanceTemperature();
		this.whiteBalance.tint.value = preset.GetWhiteBalanceTint();
		this.lgg.gain.value = preset.GetGain(time);
		this.skyDirectionalLight.intensity = preset.GetSunIntensity(time);
		this.skyDirectionalLight.color = preset.GetSunColor(time);
		this.skyDirectionalLight.transform.rotation = Quaternion.Euler(preset.GetSunRotation(time).x, 0, preset.GetSunRotation(time).y);
	}

	private void SetValues(BaseAmbientPreset p){
    	aerosolDensity_day = p._ad_d();
    	aerosolDensity_sunrise = p._ad_sr();
    	aerosolDensity_sunset = p._ad_ss();
    	aerosolDensity_night = p._ad_n();

    	horizonTint_day = p._ht_d();
    	horizonTint_sunrise = p._ht_sr();
    	horizonTint_sunset = p._ht_ss();
    	horizonTint_night = p._ht_n();

    	zenithTint_day = p._zt_d();
    	zenithTint_sunrise = p._zt_sr();
    	zenithTint_sunset = p._zt_ss();
    	zenithTint_night = p._zt_n();
    	
    	fogAttenuation1 = p._fa1();
    	fogAttenuation2 = p._fa2();
    	fogAlbedo = p._falb();
    	fogAmbientLight = p._fal();

    	cloudTint_day = p._ct_d();
    	cloudTint_sunrise = p._ct_sr();
    	cloudTint_sunset = p._ct_ss();
    	cloudTint_night = p._ct_n();

    	wbTemperature = p._wbte();
    	wbTint = p._wbti();

    	gain_day = p._g_d();
    	gain_sunrise = p._g_sr();
    	gain_sunset = p._g_ss();
    	gain_night = p._g_n();

    	lightIntensity = p._li();
    	sunRotation = p._sr();
    	sunColor = p._sc();
	}
}