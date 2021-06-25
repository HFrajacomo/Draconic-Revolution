using System;
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
    public bool isClient;
    public Server server;

    private bool SENDTIMEFLAG = false;

    void Update()
    {
        if(!this.LOCKTIME){
            ticks += Time.deltaTime * 10 * DEBUGTIMEMULT;

            if(ticks >= 20){
            	ticks = 0f;
            	minutes++;
                this.SENDTIMEFLAG = true;
            }

            if(minutes >= 60){
            	minutes = 0;
            	hours++;
            }

            if(hours >= 24){
            	hours = 0;
            	days++;
            }

            if(this.SENDTIMEFLAG && !this.isClient){
                NetMessage message = new NetMessage(NetCode.SENDGAMETIME);
                message.SendGameTime(this.days, this.hours, this.minutes);
                this.server.SendAll(message.GetMessage(), message.size);

                this.SENDTIMEFLAG = false;
            }
        }

        // Debug to advance time
        if(MainControllerManager.debugKey){
            this.hours++;
            MainControllerManager.debugKey = false;
        }

    }

    public void SetLock(bool flag){
        this.LOCKTIME = flag;
    }

    // Sets current time. Used to set time in client through a server force message
    // Currently sets ticks to 0 in client
    public void SetTime(uint days, byte hours, byte minutes){
        this.days = days;
        this.hours = hours;
        this.minutes = minutes;
        this.ticks = 0;
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
        return days.ToString() + ":" + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + ((int)this.ticks).ToString();
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
        timeArray[6] = (byte)((int)this.ticks);

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
        b[6] = (byte)((int)this.ticks);
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

    // Checks if a given string-formatted time is past compared to current time
    public bool IsPast(string givenTime){
        string[] given_split = new string[4];
        string[] current_split = new string[4];

        float given_t, current_t;
        byte given_m, current_m, given_h, current_h;
        uint given_d, current_d;

        given_split = givenTime.Split(':');
        current_split = this.Serialize().Split(':');

        given_t = (float)Convert.ToDouble(given_split[3]);
        given_m = (byte)Convert.ToByte(given_split[2]);
        given_h = (byte)Convert.ToByte(given_split[1]);
        given_d = (uint)Convert.ToInt32(given_split[0]);

        current_t = (float)Convert.ToDouble(current_split[3]);
        current_m = (byte)Convert.ToByte(current_split[2]);
        current_h = (byte)Convert.ToByte(current_split[1]);
        current_d = (uint)Convert.ToInt32(current_split[0]);

        if(given_d < current_d)
            return false;

        if(given_h < current_h)
            return false;

        if(given_m < current_m)
            return false;

        if(given_t < current_t)
            return false;

        return true;
    }

    // Fake Sum to calculate schedule time
    public string FakeSum(int tick){
        float t = this.ticks + tick;
        byte m = this.minutes;
        byte h = this.hours;
        uint d = this.days;

        m = (byte)(m + (t/20));
        t = t%20;
        h = (byte)(h + (m/60));
        m = (byte)(m%60);
        d = (uint)(d + (h/24));
        h = (byte)(h%24);

        string returnString = d.ToString() + ":" + h.ToString("00") + ":" + m.ToString("00") + ":" +  (Mathf.FloorToInt(t)).ToString();

        return returnString;
    }

    // Gets currentTime as string
    public string Serialize(){
        string returnString = this.days.ToString() + ":" + this.hours.ToString("00") + ":" + this.minutes.ToString("00") + ":" + Mathf.FloorToInt(this.ticks).ToString();
        return returnString;
    }
}