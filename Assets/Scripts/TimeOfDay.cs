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

    // Gets full day description with Ticks
    public string GetBUDTime(){
        return days.ToString() + ":" + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + ((int)(ticks*5)).ToString();
    }

    // Fake Sum to calculate schedule time
    public string FakeSum(float tick){
        float t = this.ticks + tick/10;
        int m = this.minutes;
        int h = this.hours;
        int d = this.days;

        m = m + (int)(t/2);
        t = t%2;
        h = h + (int)(m/60);
        m = m%60;
        d = d + (int)(h/24);
        h = h%24;

        return d.ToString() + ":" + h.ToString("00") + ":" + m.ToString("00") + ":" +  ((int)(t*5)).ToString();

    }
}