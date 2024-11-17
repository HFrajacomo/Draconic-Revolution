using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Light))]
public class RealisticLight : MonoBehaviour
{
	public Light lightComponent;
	public float maxLightLevel;
	public float minLightLevel;
	public float minLightPercentage = 0.85f;
	public float deltaLight;
	public bool decreasing = true;
	public int steps = 150;

	void Start(){
		this.lightComponent = this.GetComponent<Light>();
		this.maxLightLevel = lightComponent.intensity;
		this.minLightLevel = lightComponent.intensity * minLightPercentage;
		this.deltaLight = (this.maxLightLevel - this.minLightLevel)/this.steps;
	}

    // Update is called once per frame
    void Update()
    {
    	if(this.decreasing)
    		lightComponent.intensity -= this.deltaLight;
    	else
    		lightComponent.intensity += this.deltaLight;


		if(lightComponent.intensity <= this.minLightLevel)
			this.decreasing = false;
		else if(lightComponent.intensity >= this.maxLightLevel)
			this.decreasing = true;  
    }
}
