using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{

    public GameObject lightObject;
	public Transform skyboxLight;
	public Light skyDirectionalLight;
    public HDAdditionalLightData hdLight;
	public TimeOfDay timer;

    // Luminosity
    private float dayLuminosity = 3f;
    private float nightLuminosity = 0.5f;

    // Shadow Dimmer
    private float maxShadowDimmer = 1f;
    private float minShadowDimmer = 0.575f;

    // Shader Parameters
    private float lightMultiplier = 0f;
    private float lightValueAtDay = 1f;
    private float lightValueAtNight = 0.3f;

    // Skybox Parameters
    public VolumeProfile volume;
    private PhysicallyBasedSky pbsky;
    private CloudLayer clouds;
    private Color horizonColor = new Color(0.26f, 0.89f, 0.9f);
    private Color horizonDay = new Color(0.26f, 0.89f, 0.9f);
    private Color horizonSunset = new Color(0.42f, 0f, 1f);
    private Color horizonNight = new Color(1f, 1f, 1f);
    private Color horizonSunrise = new Color(0.97f, 0.57f, 0.33f);
    private float currentSaturation = 1f;

    // Update detectors
    public float delta = 0;
    public int previousFrameSeconds = 0;
    public bool UPDATELIGHT_FLAG = true;
    private static float FRAME_TIME_DIFF_MULTIPLIER = 0.7f;

    void Start(){
        this.volume.TryGet<PhysicallyBasedSky>(out this.pbsky);
        this.volume.TryGet<CloudLayer>(out this.clouds);
        this.pbsky.horizonTint.value = this.horizonColor;
    }

    // Update is called once per frame
    void Update()
    {
    	int time = timer.ToSeconds();
        this.delta += Time.deltaTime;

        if(this.previousFrameSeconds != time){
            this.delta = 0;
            this.previousFrameSeconds = time;
            this.SetLightColor(time);
            this.SetIntensity(time);
            this.SetFloorIntensity(time);
            this.SetHorizonColor(time);
            this.SetAlphaSaturation(time);
            this.ToggleClouds(time);
        }

        if(UPDATELIGHT_FLAG){
            skyboxLight.localRotation = Quaternion.Euler(RotationFunction(time + (this.delta*DayNightCycle.FRAME_TIME_DIFF_MULTIPLIER)), 270, 0);
            this.SetShadowDimmer(time + (this.delta*DayNightCycle.FRAME_TIME_DIFF_MULTIPLIER));
            Shader.SetGlobalFloat("_SkyLightMultiplier", this.lightMultiplier);
            this.pbsky.horizonTint.value = this.horizonColor;
            this.pbsky.alphaSaturation.value = this.currentSaturation;
            this.UPDATELIGHT_FLAG = false;
        }
        else{
            this.UPDATELIGHT_FLAG = true;
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
        if(x > 240 && x < 660){
            hdLight.volumetricShadowDimmer = Mathf.Lerp(this.minShadowDimmer, this.maxShadowDimmer, (x-240)/420);
        }
        else if(x >= 660 && x < 900){
            hdLight.volumetricShadowDimmer = this.maxShadowDimmer;
        }
        else if(x >= 900 && x < 1200){
            hdLight.volumetricShadowDimmer = Mathf.Lerp(this.maxShadowDimmer, this.minShadowDimmer, (x-900)/300);
        }
        else if(x >= 1200 || x <= 240){
            hdLight.volumetricShadowDimmer = this.minShadowDimmer;
        }
    }

    // Sets color intensity based on current time
    private void SetIntensity(int x){
        // If day
        if(x > 240 && x < 1080){
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
        if(x > 240 && x < 1080){
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
        if(time <= 240 || time > 1200){
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
            horizonColor = Color.Lerp(this.horizonNight, this.horizonSunrise, Mathf.Pow((x-180)/120f, 1/3f));
        }
        else if(x > 300 && x <= 360){
            horizonColor = Color.Lerp(this.horizonSunrise, this.horizonDay, (x-300)/60f);
        }
        else if(x > 360 && x <= 1080){
            horizonColor = horizonDay;
        }
        else if(x > 1080 && x < 1200){
            horizonColor = Color.Lerp(this.horizonDay, this.horizonSunset, Mathf.Pow((x-1080f)/120f, 1f/2f));
        }
        else if(x == 1200){
            horizonColor = Color.black;
        }
        else if(x > 1200 && x <= 1260){
            horizonColor = Color.Lerp(this.horizonSunset, this.horizonNight, Mathf.Pow((x-1200)/60f, 1f/2f));
        }
        else{
            horizonColor = horizonNight;
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
}
