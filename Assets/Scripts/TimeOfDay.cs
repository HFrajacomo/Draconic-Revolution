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
    public bool LOCKTIME = true;

    void Update()
    {
        if(!this.LOCKTIME){
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

    }

    public void SetLock(bool flag){
        this.LOCKTIME = flag;
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

    // Returns byte array representing current time for Chunk Header in RegionFile
    // Stored data is in format d:h:m:t
    public void TimeHeader(byte[] b){
        b[0] = (byte)(this.days >> 24);
        b[1] = (byte)(this.days >> 16);
        b[2] = (byte)(this.days >> 8);
        b[3] = (byte)this.days;
        b[4] = this.hours;
        b[5] = this.minutes;
        b[6] = (byte)(this.ticks%2);
    }

    // Reconstructs byte array read from RegionFile to a date string
    public string DateBytes(byte[] b){
        uint days;

        days = b[0];
        days = days << 8;
        days = days + b[1];
        days = days << 8;
        days = days + b[2];
        days = days << 8;
        days = days + b[3];

        return days.ToString() + ":" + b[4].ToString("00") + ":" + b[5].ToString("00") + ":" + b[6].ToString();
    }

    // Sets time based on byte[] read from WDAT file
    public void SetTime(byte[] byteArray){
        uint days;

        days = byteArray[0];
        days = days << 8;
        days = days + byteArray[1];
        days = days << 8;
        days = days + byteArray[2];
        days = days << 8;
        days = days + byteArray[3];

        this.days = days;
        this.hours = byteArray[4];
        this.minutes = byteArray[5];
        this.ticks = (float)(byteArray[6]);

    }

    // Fake Sum to calculate schedule time
    public string FakeSum(float tick){
        float t = this.ticks + tick/10;
        byte m = this.minutes;
        byte h = this.hours;
        uint d = this.days;

        m = (byte)(m + (t/2));
        t = t%2;
        h = (byte)(h + (m/60));
        m = (byte)(m%60);
        d = (uint)(d + (h/24));
        h = (byte)(h%24);

        return d.ToString() + ":" + h.ToString("00") + ":" + m.ToString("00") + ":" +  ((int)(t*5)).ToString();

    }
}