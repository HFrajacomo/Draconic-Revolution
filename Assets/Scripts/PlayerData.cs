using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerData
{
	public ulong ID;
	public float posX, posY, posZ, dirX, dirY, dirZ;

	public PlayerData(ulong ID, float3 pos, float3 dir){
		this.ID = ID;
		this.posX = pos.x;
		this.posY = pos.y;
		this.posZ = pos.z;
		this.dirX = dir.x;
		this.dirY = dir.y;
		this.dirZ = dir.z;
	}

	public PlayerData(byte[] data){
		this.ID = NetDecoder.ReadUlong(data, 0);
		this.posX = NetDecoder.ReadFloat(data, 8);
		this.posY = NetDecoder.ReadFloat(data, 12);
		this.posZ = NetDecoder.ReadFloat(data, 16);
		this.dirX = NetDecoder.ReadFloat(data, 20);
		this.dirY = NetDecoder.ReadFloat(data, 24);
		this.dirZ = NetDecoder.ReadFloat(data, 28);
	}

	public byte[] ToByteArray(){
		byte[] data = new byte[RegionFileHandler.pdatEntrySize];

		NetDecoder.WriteLong(this.ID, data, 0);
		NetDecoder.WriteFloat(this.posX, data, 8);
		NetDecoder.WriteFloat(this.posY, data, 12);
		NetDecoder.WriteFloat(this.posZ, data, 16);
		NetDecoder.WriteFloat(this.dirX, data, 20);
		NetDecoder.WriteFloat(this.dirY, data, 24);
		NetDecoder.WriteFloat(this.dirZ, data, 28);

		return data;
	}

	public Vector3 GetPosition(){
		return new Vector3(this.posX, this.posY, this.posZ);
	}

	public Vector3 GetDirection(){
		return new Vector3(this.dirX, this.dirY, this.dirZ);
	}

	public void SetPosition(float x, float y, float z){
		this.posX = x;
		this.posY = y;
		this.posZ = z;
	}

	public void SetDirection(float x, float y, float z){
		this.dirX = x;
		this.dirY = y;
		this.dirZ = z;
	}

	public void SetID(ulong newID){
		this.ID = newID;
	}
}
