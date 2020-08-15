using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeOfDay : MonoBehaviour
{
	public float ticks = 0f;
	public int minutes = 0;
	public int hours = 6;

	public int days = 0;

	public int DEBUGTIMEMULT=1;

    void Update()
    {
        ticks += Time.deltaTime * DEBUGTIMEMULT;

        if(ticks >= 2){
        	ticks = 0f;
        	minutes++;
        }

        if(minutes >= 60){
        	minutes = 0;
        	hours++;
        }

        if(hours >= 24){
        	hours = 0;
        	days++;
        }

    }

    // Gets formatted h:m string
    public override string ToString(){
    	return hours.ToString("00") + ":" + minutes.ToString("00");
    }

    // Gets current passed seconds in a day
    public int ToSeconds(){
    	return hours*60 + minutes;
    }
}
