using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TimeOfDay : MonoBehaviour
{
	public float ticks = 0f;
	public byte minutes = 0;
	public byte hours = 6;
	public uint days = 0;
	public int DEBUGTIMEMULT=1;
    private byte[] timeArray = new byte[7];

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

    // Returns byte array representing current time for Chunk Header in RegionFile
    // Stored data is in format d:h:m:t
    public byte[] TimeHeader(){
        timeArray[0] = (byte)(this.days >> 24);
        timeArray[1] = (byte)(this.days >> 16);
        timeArray[2] = (byte)(this.days >> 8);
        timeArray[3] = (byte)this.days;
        timeArray[4] = this.hours;
        timeArray[5] = this.minutes;
        timeArray[6] = (byte)(this.ticks%2);

        return timeArray;
    }

    // Fake Sum to calculate schedule time
    public string FakeSum(float tick){
        float t = this.ticks + tick/10;
        byte m = this.minutes;
        byte h = this.hours;
        uint d = this.days;

        m = m + (int)(t/2);
        t = t%2;
        h = h + (int)(m/60);
        m = m%60;
        d = d + (int)(h/24);
        h = h%24;

        return d.ToString() + ":" + h.ToString("00") + ":" + m.ToString("00") + ":" +  ((int)(t*5)).ToString();

    }
}