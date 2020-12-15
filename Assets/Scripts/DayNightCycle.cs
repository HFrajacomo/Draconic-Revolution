using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{

	public Transform skyboxLight;
	public Light skyDirectionalLight;
	public Transform counterLight;
	public Transform counterLight2;
	public Transform counterLight3;
	public Transform counterLight4;
	public TimeOfDay timer;

    // Update is called once per frame
    void Update()
    {
    	int time = timer.ToSeconds();

        skyboxLight.localRotation = Quaternion.Euler(RotationFunction(time), 270, 0);
        /*
        counterLight.localRotation = Quaternion.Euler(RotationAssist(time), 45, 45);
        counterLight2.localRotation = Quaternion.Euler(RotationAssist(time), 135, 135);
        counterLight3.localRotation = Quaternion.Euler(RotationAssist(time), 225, 225);
        counterLight4.localRotation = Quaternion.Euler(RotationAssist(time), 315, 315);
        */
		//skyDirectionalLight.intensity = LuminosityFunction(time);
		}

	// Rotation for main Skybox light
    private float RotationFunction(int x){
        if(x > 240 && x <= 720){
            return Mathf.Lerp(0f, 90f, ClampTime(x));
        }
        else if(x > 720 && x < 1200){
            return Mathf.Lerp(90f, 180f, ClampTime(x));
        }
        else{
            return Mathf.Lerp(180f, 360f, ClampTime(x));
        }
    }

    // Clamps the current time to a float[0,1]
    private float ClampTime(int x){
        // Zero Lerp if below 4h
        if(x <= 240){
            return (x+240)/480f;
        }
        // Zero Lerp after 20h
        else if(x >= 1200){
            return (x-1200)/480f;
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

    // Rotation for GI lights
    private float RotationAssist(int x){
    	if(x < 360 || x >= 1320)
    		return 272f;
    	if(x >= 360 && x < 420)
    		return 1.466666f*(x-360) + 272;
    	if(x >= 420 && x < 720)
    		return 0.166666f*(x-420);
    	if(x >= 720 && x < 960)
    		return 50f;
    	if(x >= 960 && x < 1260)
    		return 50-(0.166666f*(x-960));
    	if(x >= 1260 && x < 1320)
    		return 360 - (1.466666f*(x-1260));

    	return 0;
    }

    /*
    // TOO HEAVY ON CPU
    // Calculates Sunlight Luminosity based on time of day
    private float LuminosityFunction(int x){
    	if(x < 360 || x >= 1320)
    		return 0f;
    	if(x >= 360 && x < 390)
    		return 0.016666f*(x-360);
    	if(x >= 390 && x < 420)
    		return 0.01f*(x-390) + 0.5f;
    	if(x >= 420 && x < 720)
    		return 0.000666f*(x-420) + 0.8f;
    	if(x >= 720 && x < 960)
    		return 1.0f;
    	if(x >= 960 && x < 1260)
    		return 1-0.000666f*(x-960);
    	if(x >= 1260 && x < 1290)
    		return 0.8f-0.01f*(x-1260);
    	if(x >= 1290 && x < 1320)
    		return 0.5f-0.016666f*(x-1290);
    	return 0f;
    }
    */

}
