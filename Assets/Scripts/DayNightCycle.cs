using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{

	public Transform daynight;
	public TimeOfDay timer;

    // Start is called before the first frame update
    void Start()
    {
		daynight = GetComponent<Transform>();     
    }

    // Update is called once per frame
    void Update()
    {

        daynight.localRotation = Quaternion.Euler(RotationFunction(timer.ToSeconds()), 0, 0);
    }

    private float RotationFunction(int x){
    	if(x == 0){
    		return 240f;
    	}
    	if(x < 360){
    		return 0.33333333f*x + 240;
    	}
    	if(x >= 360 && x < 1320){
    		return 0.20833333f*(x-360);
    	}
    	if(x >= 1320 && x < 1440){
    		return 0.33333333f*(x-1320) + 200;
    	}
    	return 0;
    }

}
