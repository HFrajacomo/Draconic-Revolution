using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class DayNightCycle : MonoBehaviour
{

	public Transform skyboxLight;
	public Light skyDirectionalLight;
	public TimeOfDay timer;

    public float dayLuminosity = 4f;
    public float nightLuminosity = 0.5f;

    public float delta = 0;
    public int previousFrameSeconds = 0;

    public bool UPDATELIGHT_FLAG = true;
    private static float FRAME_TIME_DIFF_MULTIPLIER = 0.7f;

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
        }

        if(UPDATELIGHT_FLAG){
            skyboxLight.localRotation = Quaternion.Euler(RotationFunction(time + (this.delta*DayNightCycle.FRAME_TIME_DIFF_MULTIPLIER)), 270, 0);
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

    // Sets color intensity based on current time
    private void SetIntensity(int x){
        // If day
        if(x > 240 && x < 1200){
            skyDirectionalLight.intensity = this.dayLuminosity;
        }
        else if(x >= 1200){
            skyDirectionalLight.intensity = Mathf.Lerp(this.dayLuminosity, this.nightLuminosity, Mathf.Pow((x-1200f)/240f, 1f/3f));
        }
        else{
            skyDirectionalLight.intensity = Mathf.Lerp(this.nightLuminosity, this.dayLuminosity, Mathf.Pow(x/240f, 3f));
        }
    }

    // Sets color of Directional Light given the time of the day it is now
    private void SetLightColor(int time){
        if(time <= 240 || time >= 1200){
            skyDirectionalLight.color = new Color(0.27f, 0.57f, 1f, 1f);
        }
        else{
            skyDirectionalLight.color = new Color(1f,1f,1f,1f);
        }
    }

}
