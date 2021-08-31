using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler
{
	private Dictionary<ulong, GameObject> playerObject;

	public EntityHandler(){
		this.playerObject = new Dictionary<ulong, GameObject>();
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
	}

	// Only works while.... you get it
	public void Move(EntityType type, ulong code, float3 pos, float3 dir){
		this.playerObject[code].transform.position = new Vector3(pos.x, pos.y, pos.z);
		this.playerObject[code].transform.eulerAngles = new Vector3(dir.x, dir.y, dir.z);
	}

	// ...
	public void Remove(EntityType type, ulong code){
		this.playerObject[code].SetActive(false);
		GameObject.Destroy(this.playerObject[code]);
		this.playerObject.Remove(code);
	}
}
