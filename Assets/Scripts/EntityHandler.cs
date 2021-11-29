using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler
{
	private Dictionary<ulong, GameObject> playerObject;
	private Dictionary<ulong, DeltaMove> playerCurrentPositions;

	public EntityHandler(){
		this.playerObject = new Dictionary<ulong, GameObject>();
		this.playerCurrentPositions = new Dictionary<ulong, DeltaMove>();
	}

	// Only works while there is no other EntityTypes here other than player
	public bool Contains(EntityType type, ulong code){
		return this.playerObject.ContainsKey(code);
	}

	// Only works while there is no other EntityType
	public void Add(EntityType type, ulong code, float3 pos, float3 dir){
		GameObject go = GameObject.Instantiate(GameObject.Find("----- PrefabModels -----/PlayerModel"), new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(dir.x, dir.y, dir.z));
		go.name = "Player_" + code;
		this.playerObject.Add(code, go);
		this.playerCurrentPositions.Add(code, new DeltaMove(pos, dir));
	}

	// Triggers whenever a t(x) position is received. Moves entity to t(x-1) position received
	public void NudgeLastPos(EntityType type, ulong code, float3 pos, float3 dir){		
		this.playerObject[code].transform.position = this.playerCurrentPositions[code].deltaPos;
		this.playerObject[code].transform.eulerAngles = this.playerCurrentPositions[code].deltaRot;
	
		this.playerCurrentPositions[code] = new DeltaMove(pos, dir);

	}

	// Fine movement of entity in frame deltas
	public void Nudge(EntityType type, ulong code, Vector3 dPos, Vector3 dRot){
		this.playerObject[code].transform.position = this.playerObject[code].transform.position + (dPos * (Time.deltaTime * TimeOfDay.tickRate));
		this.playerObject[code].transform.eulerAngles = this.playerObject[code].transform.eulerAngles + (dRot * (Time.deltaTime * TimeOfDay.tickRate));
	}

	// ...
	public void Remove(EntityType type, ulong code){
		this.playerObject[code].SetActive(false);
		GameObject.Destroy(this.playerObject[code]);
		this.playerObject.Remove(code);
		this.playerCurrentPositions.Remove(code);
	}

	public Vector3 GetLastPosition(ulong code){
		return this.playerCurrentPositions[code].deltaPos;
	}

	public Vector3 GetLastRotation(ulong code){
		return this.playerCurrentPositions[code].deltaRot;
	}
}
