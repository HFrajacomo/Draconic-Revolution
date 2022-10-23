using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{

    // Unity References
    public GameObject lightObject;
	public Transform skyboxLight;
	public Light skyDirectionalLight;
    public HDAdditionalLightData hdLight;
	public TimeOfDay timer;
    public LensFlareComponentSRP dayFlare;
    public LensFlareComponentSRP nightFlare;

    // Luminosity
    private float dayLuminosity = 3f;
    private float nightLuminosity = 0.2f;

    // Shadow Dimmer
    private float maxShadowDimmer = 1f;
    private float minShadowDimmer = 0.575f;

    // Shader Parameters
    private float lightMultiplier = 0f;
    private float lightValueAtDay = 1f;
    private float lightValueAtNight = 0.05f;

    // Tint and Fog
    private float normalTint = 0f;
    private float sunTint = 30f;
    private float minNaturalFog = 20f;
    private float maxNaturalFog = 60f;
    private float currentTint = 0f;
    private float currentFog = 0f;

    // Skybox Parameters
    public VolumeProfile volume;
    private PhysicallyBasedSky pbsky;
    private CloudLayer clouds;
    private WhiteBalance whiteBalance;
    private Fog fog;
    private Color horizonColor = new Color(0.26f, 0.89f, 0.9f);
    private Color horizonDay = new Color(0.26f, 0.89f, 0.9f);
    private Color horizonNight = new Color(1f, 1f, 1f);
    private Color horizonSunriseAndSet = new Color(0.97f, 0.57f, 0.33f);
    private Color cloudTintCurrent = new Color(0.66f, 0.94f, 1f);
    private Color cloudTintNormal = new Color(0.66f, 0.94f, 1f);
    private Color cloudTintSunrise = new Color(1f, 0.52f, 0.08f);
    private Color cloudTintSunset = new Color(1f, 0.19f, 0.32f);
    private float currentSaturation = 1f;


    // Update detectors
    public float delta = 0;
    public int previousFrameSeconds = 0;
    public bool UPDATELIGHT_FLAG = true;
    private static float FRAME_TIME_DIFF_MULTIPLIER = 0.7f;
    public int updateTimer = 10;
    private int timerFrameSize = 10;

    void OnDestroy(){
        GameObject.Destroy(this.lightObject);
        this.lightObject = null;
        this.skyDirectionalLight = null;
        this.hdLight = null;
        this.timer = null;
        this.dayFlare = null;
        this.nightFlare = null;
        this.volume = null;
        this.pbsky = null;
        this.clouds = null;
        this.whiteBalance = null;
    }

    void Start(){
        this.volume.TryGet<PhysicallyBasedSky>(out this.pbsky);
        this.volume.TryGet<CloudLayer>(out this.clouds);
        this.volume.TryGet<WhiteBalance>(out this.whiteBalance);
        this.volume.TryGet<Fog>(out this.fog);
        this.pbsky.horizonTint.value = this.horizonColor;
    }

    // Update is called once per frame
    void Update()
    {
    	int time = timer.ToSeconds();
        this.delta += Time.deltaTime;
        this.updateTimer--;

        if(this.previousFrameSeconds != time){
            this.delta = 0;
            this.previousFrameSeconds = time;
            this.SetLightColor(time);
            this.SetIntensity(time);
            this.SetFloorIntensity(time);
            this.SetHorizonColor(time);
            this.SetFlare(time);
            this.SetTintColor(time);
            this.SetFog(time);
            this.SetAlphaSaturation(time);
            this.SetCloudTint(time);
            this.ToggleClouds(time);
            this.SetLensFlareIntensity(time);
        }

        if(UPDATELIGHT_FLAG){
            this.SetShadowDimmer(time + (this.delta*DayNightCycle.FRAME_TIME_DIFF_MULTIPLIER));
            Shader.SetGlobalFloat("_SkyLightMultiplier", this.lightMultiplier);
            this.pbsky.horizonTint.value = this.horizonColor;
            this.pbsky.alphaSaturation.value = this.currentSaturation;
            this.whiteBalance.temperature.value = this.currentTint;
            this.fog.meanFreePath.value = this.currentFog;
            this.clouds.layerA.tint.value = this.cloudTintCurrent;
            this.UPDATELIGHT_FLAG = false;
        }
        else{
            this.UPDATELIGHT_FLAG = true;
        }

        if(this.updateTimer <= 0){
            skyboxLight.rotation = Quaternion.Euler(RotationFunction(time + (this.delta*DayNightCycle.FRAME_TIME_DIFF_MULTIPLIER)), 270, RotateZ(time + (this.delta*DayNightCycle.FRAME_TIME_DIFF_MULTIPLIER)));
            this.updateTimer = this.timerFrameSize;
        }


	}

	// Rotation for main Skybox light
    private float RotationFunction(float x){
        if(x > 240 && x <= 720){
            return Mathf.Lerp(0f, 90f, ClampTime(x));
        }
        else if(x > 720 && x < 1200){
            return Mathf.Lerp(90f, 180f, ClampTime(x));
        }
        else if(x <= 240){
            return Mathf.Lerp(90f, 180f, ClampTime(x));
        }
        else{
            return Mathf.Lerp(0f, 90f, ClampTime(x));
        }
    }

    // Rotation for Z component of Skybox Light
    private float RotateZ(float x){
        if(x > 240 && x <= 720)
            return Mathf.Lerp(0f, 30f, (x-240)/480);
        else if(x > 720 && x <= 1200)
            return Mathf.Lerp(30f, 0f, (x-720)/480);
        else if(x > 1200)
            return Mathf.Lerp(0f, 30f, (x-1200)/1440);
        else
            return Mathf.Lerp(30f, 0f, x/240);
    }

    // Clamps the current time to a float[0,1]
    private float ClampTime(float x){
        // Zero Lerp if below 4h
        if(x <= 240){
            return x/240f;
        }
        // Zero Lerp after 20h
        else if(x >= 1200){
            return (x-1200)/240f;
        }
        // Inclination until 12h
        else if(x <= 720){
            return (x-240)/480f;
        }
        // Declination until 20h
        else{
            return ((x-720)/480f);
        }
    }

    // Sets the ShadowDimmer of the main light
    private void SetShadowDimmer(float x){
        if(x >= 240 && x < 660){
            hdLight.volumetricShadowDimmer = Mathf.Lerp(this.minShadowDimmer, this.maxShadowDimmer, (x-240)/420);
        }
        else if(x >= 660 && x < 900){
            hdLight.volumetricShadowDimmer = this.maxShadowDimmer;
        }
        else if(x >= 900 && x < 1200){
            hdLight.volumetricShadowDimmer = Mathf.Lerp(this.maxShadowDimmer, this.minShadowDimmer, (x-900)/300);
        }
        else if(x >= 1200 || x < 240){
            hdLight.volumetricShadowDimmer = this.minShadowDimmer;
        }
    }

    // Sets color intensity based on current time
    private void SetIntensity(int x){
        // If day
        if(x >= 240 && x < 1080){
            skyDirectionalLight.intensity = this.dayLuminosity;
        }
        else if(x >= 1080 && x < 1200){
            skyDirectionalLight.intensity = Mathf.Lerp(this.dayLuminosity, 0f, Mathf.Pow((x-1080f)/120f, 2f));
        }
        else if(x >= 1200 && x < 1230){
            skyDirectionalLight.intensity = Mathf.Lerp(0f, this.nightLuminosity, (x-1200f)/30f);            
        }
        else if(x >= 1230){
            skyDirectionalLight.intensity = this.nightLuminosity;
        }
        else{
            skyDirectionalLight.intensity = Mathf.Lerp(this.nightLuminosity, this.dayLuminosity, Mathf.Pow(x/240f, 3f));
        }
    }

    // Sets floor intensity
    private void SetFloorIntensity(int x){
        // If day
        if(x >= 240 && x < 1080){
            lightMultiplier = this.lightValueAtDay;
        }
        else if(x >= 1080){
            lightMultiplier = Mathf.Lerp(this.lightValueAtDay, this.lightValueAtNight, Mathf.Pow((x-1080f)/360f, 1f/3f));
        }
        else{
            lightMultiplier = Mathf.Lerp(this.lightValueAtNight, this.lightValueAtDay, Mathf.Pow(x/240f, 3f));
        }
    }

    // Sets color of Directional Light given the time of the day it is now
    private void SetLightColor(int time){
        if(time < 240 || time > 1200){
            skyDirectionalLight.color = new Color(0.27f, 0.57f, 1f, 1f);
        }
        else{
            skyDirectionalLight.color = new Color(1f,1f,1f,1f);
        }
    }

    // Sets horizon color
    private void SetHorizonColor(int x){
        if(x <= 180){
            horizonColor = horizonNight;
        }
        else if(x > 180 && x <= 300){
            horizonColor = Color.Lerp(this.horizonNight, this.horizonSunriseAndSet, Mathf.Pow((x-180)/120f, 1/3f));
        }
        else if(x > 300 && x <= 360){
            horizonColor = Color.Lerp(this.horizonSunriseAndSet, this.horizonDay, (x-300)/60f);
        }
        else if(x > 360 && x <= 1080){
            horizonColor = horizonDay;
        }
        else if(x > 1080 && x < 1200){
            horizonColor = Color.Lerp(this.horizonDay, this.horizonSunriseAndSet, Mathf.Pow((x-1080f)/120f, 1f/2f));
        }
        else if(x == 1200){
            horizonColor = Color.black;
        }
        else if(x > 1200 && x <= 1260){
            horizonColor = Color.Lerp(this.horizonSunriseAndSet, this.horizonNight, Mathf.Pow((x-1200)/60f, 1f/2f));
        }
        else{
            horizonColor = horizonNight;
        }        
    }

    // Sets the Sun tint color
    private void SetTintColor(int x){
        if(x < 240){
            currentTint = normalTint;
        }
        else if(x >= 240 && x < 360){
            currentTint = sunTint;
        }
        else if(x >= 360 && x < 600){
            currentTint = Mathf.Lerp(sunTint, normalTint, (x-360)/240f);
        }
        else if(x >= 600 && x < 960){
            currentTint = normalTint;
        }
        else if(x >= 960 && x < 1200){
            currentTint = Mathf.Lerp(normalTint, sunTint, (x-960)/240f);
        }
        else{
            currentTint = normalTint;
        }
    }

    // Sets Fog distance
    private void SetFog(int x){
        if(x >= 360 && x < 480){
            currentFog = Mathf.Lerp(minNaturalFog, maxNaturalFog, (x-360)/120f);
        }
        else if(x >= 480 && x < 1080){
            currentFog = maxNaturalFog;
        }
        else if(x >= 1080 && x < 1200){
            currentFog = Mathf.Lerp(maxNaturalFog, minNaturalFog, (x-1080)/120f);
        }
        else{
            currentFog = minNaturalFog;
        }

    }

    // Sets the current Lens Flare
    private void SetFlare(int x){
        if(x >= 240 && x <= 1200){
            this.dayFlare.enabled = true;
            this.nightFlare.enabled = false;
        }
        else{
            this.dayFlare.enabled = false;
            this.nightFlare.enabled = true;
        }
    }

    // Sets the Alpha Saturation
    private void SetAlphaSaturation(int x){
        if(x < 240 || x >= 1200)
            currentSaturation = 1f;
        else if(x >= 240 && x < 420)
            currentSaturation = Mathf.Lerp(1f, 0f, Mathf.Pow((x-240)/180f, 3f));
        else if(x >= 1080 && x < 1200)
            currentSaturation = Mathf.Lerp(0f, 1f, Mathf.Pow((x-1080)/120f, 1.5f));
        else if(x == 1200)
            currentSaturation = 1f;
        else
            currentSaturation = 0f;
    }

    // Toggle Clouds
    private void ToggleClouds(int x){
        if(x == 1200)
            this.clouds.opacity.value = 0f;
        else
            this.clouds.opacity.value = 0.2f;
    }

    // Sets Clouds color
    private void SetCloudTint(int x){
        if(x >= 180 && x < 240)
            cloudTintCurrent = Color.Lerp(cloudTintNormal, cloudTintSunrise, (x-180)/60f);
        else if(x >= 240 && x < 360)
            cloudTintCurrent = cloudTintSunrise;
        else if(x >= 360  && x < 600)
            cloudTintCurrent = Color.Lerp(cloudTintSunrise, cloudTintNormal, (x-360)/240f);
        else if(x >= 1140 && x < 1200)
            cloudTintCurrent = Color.Lerp(cloudTintNormal, cloudTintSunset, (x-1140)/60f);
        else
            cloudTintCurrent = cloudTintNormal;
    }

    // Sets Lens Flare intensity
    private void SetLensFlareIntensity(int x){
        if(x >= 180 && x < 240){
            nightFlare.intensity = Mathf.Lerp(1f, 0.4f, (x-180)/60f);
        }
        else if(x >= 1200 && x < 1260){
            nightFlare.intensity = Mathf.Lerp(0.4f, 1f, (x-1200)/60f);
        }
        else{
            nightFlare.intensity = 1f;
        }

        if(x >= 240 && x <= 300){
            dayFlare.intensity = Mathf.Lerp(0.4f, 1f, (x-240)/60f);
        }
        else if(x >= 1140 && x < 1200){
            dayFlare.intensity = Mathf.Lerp(1f, 0.4f, (x-1140)/60f);
        }
        else{
            dayFlare.intensity = 1f;
        }
    }
}
