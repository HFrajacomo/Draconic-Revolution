using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class DebugRaycast {
	private static Dictionary<ulong, Vector3> positions = new Dictionary<ulong, Vector3>();
	private static Dictionary<ulong, Vector3> directions = new Dictionary<ulong, Vector3>();

	public static void Draw(){
		foreach(ulong code in positions.Keys){
			Debug.DrawRay(positions[code], directions[code].normalized, Color.green, duration:0f);
		}
	} 

	public static void Register(ulong code, Vector3 pos, Vector3 dir){
		if(positions.ContainsKey(code))
			return;

		positions.Add(code, pos);
		directions.Add(code, dir);
	}

	public static void Register(ulong code, float3 pos, float3 dir){
		Register(code, new Vector3(pos.x, pos.y, pos.z), new Vector3(dir.x, dir.y, dir.z));
	}

	public static void Unregister(ulong code){
		if(!positions.ContainsKey(code))
			return;

		positions.Remove(code);
		directions.Remove(code);
	}
}