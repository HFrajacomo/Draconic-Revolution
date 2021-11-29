using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TimeOfDay : MonoBehaviour
{
    public Server server;
    public Client client;
    public static byte tickRate = 20;
    private static float timeRate = 1f/TimeOfDay.tickRate;
    private static byte ticksForMinute = (byte)(TimeOfDay.tickRate << 1); // two seconds worth of ticks

    private float faketicks = 0f;
	public float ticks = 0f;
	public byte minutes = 0;
	public byte hours = 6;
	public uint days = 0;
    private byte[] timeArray = new byte[7];
    public bool LOCKTIME = true;
    public bool isClient;

    private PlayerMovement player;

    private bool SENDTIMEFLAG = false;
    private bool CHECKTIMEOUT = false;

    // Position properties
    private NetMessage movementMessage;
    private Vector3 position;
    private Vector3 rotation;
    private ChunkPos currentPos;
    private ChunkPos lastPos;
    private CastCoord cacheCoord;
    private byte sendZeroNotification = 2;

    void Update()
    {
        if(!this.LOCKTIME && !isClient){

            // If there are no players in the server, don't run
            if(this.server.connections.Count == 0)
                return;

            ticks += Time.deltaTime * TimeOfDay.tickRate;

            if(ticks >= TimeOfDay.ticksForMinute){
            	ticks = 0f;
            	minutes++;
                this.SENDTIMEFLAG = true;
                this.CHECKTIMEOUT = true;
            }

            if(minutes >= 60){
            	minutes = 0;
            	hours++;
            }

            if(hours >= 24){
            	hours = 0;
            	days++;
            }

            // If server, sends time update to client
            if(this.SENDTIMEFLAG && !this.isClient){
                NetMessage message = new NetMessage(NetCode.SENDGAMETIME);
                message.SendGameTime(this.days, this.hours, this.minutes);
                this.server.SendAll(message.GetMessage(), message.size);

                this.SENDTIMEFLAG = false;
            }

            // If server, checks for timeouts
            if(this.CHECKTIMEOUT && !this.isClient){
                this.server.CheckTimeout();
                this.CHECKTIMEOUT = false;
            }

        }
        else if(isClient){
            faketicks += Time.deltaTime;

            if(faketicks >= TimeOfDay.timeRate){
                SendPlayerPosition();
                faketicks = 0f;
            }
        }
    }

    public void SetLock(bool flag){
        this.LOCKTIME = flag;
    }

    public void SetServer(Server sv){
        this.server = sv;
    }

    public void SetClient(Client cli){
        this.client = cli;
    }

    public void SetPlayer(PlayerMovement player){
        this.player = player;
    }

    // When on client, sends player position to Server whenever there's movement
    private void SendPlayerPosition(){
        if(this.player == null)
            return;
        if(position == null)
            position = this.player.controller.transform.position;
        if(rotation == null)
            rotation = this.player.controller.transform.eulerAngles;

        // If has moved
        if(this.player.controller.transform.position != position || this.player.controller.transform.eulerAngles != rotation){
            SendPositionMessage();
            position = this.player.controller.transform.position;
            rotation = this.player.controller.transform.eulerAngles;

            this.cacheCoord = new CastCoord(this.position);
            if(this.currentPos == null){
                this.currentPos = this.cacheCoord.GetChunkPos();
                this.lastPos = this.cacheCoord.GetChunkPos();
            }
            else if(this.currentPos != this.cacheCoord.GetChunkPos()){
                this.currentPos = this.cacheCoord.GetChunkPos();
                SendChunkPosMessage();
                this.lastPos = this.currentPos;
            }

            this.sendZeroNotification = 2;            
        }
        // If hasn't moved but notification must be sent
        else if(this.sendZeroNotification > 0){
            this.sendZeroNotification -= 1;
            SendPositionMessage();
        }
    }

    public void SetCurrentChunkPos(ChunkPos pos){
        this.currentPos = pos;
        this.lastPos = pos;
    }

    private void SendPositionMessage(){
        this.movementMessage = new NetMessage(NetCode.CLIENTPLAYERPOSITION);
        this.movementMessage.ClientPlayerPosition(this.position.x, this.position.y, this.position.z, this.rotation.x, this.rotation.y, this.rotation.z);
        this.client.Send(this.movementMessage.GetMessage(), this.movementMessage.size);
    }

    public void SendChunkPosMessage(){
        NetMessage message = new NetMessage(NetCode.CLIENTCHUNK);
        message.ClientChunk(this.lastPos, this.currentPos);
        this.client.Send(message.GetMessage(), message.size);
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
            return true;
        else if(given_h < current_h)
            return true;
        else if(given_m < current_m)
            return true;
        else if(given_t < current_t)
            return true;

        return false;
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