using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler
{
	private Dictionary<ulong, GameObject> playerObject;
	private Dictionary<ulong, DeltaMove> playerCurrentPositions;
	private Dictionary<ulong, ItemEntity> dropObject;
	private Dictionary<ulong, DeltaMove> dropCurrentPositions;
	private GameObject droppedItemBase = GameObject.Find("----- PrefabModels -----/DroppedItemBase");


	public EntityHandler(){
		this.playerObject = new Dictionary<ulong, GameObject>();
		this.playerCurrentPositions = new Dictionary<ulong, DeltaMove>();
		this.dropObject = new Dictionary<ulong, ItemEntity>();
		this.dropCurrentPositions = new Dictionary<ulong, DeltaMove>();
	}

	// Only works while there is no other EntityTypes here other than player
	public bool Contains(EntityType type, ulong code){
		if(type == EntityType.PLAYER)
			return this.playerObject.ContainsKey(code);
		else if(type == EntityType.DROP)
			return this.dropObject.ContainsKey(code);
		return false;
	}

	public void AddPlayer(ulong code, float3 pos, float3 dir){
		GameObject go = GameObject.Instantiate(GameObject.Find("----- PrefabModels -----/PlayerModel"), new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(dir.x, dir.y, dir.z));
		go.name = "Player_" + code;
		this.playerObject.Add(code, go);
		this.playerCurrentPositions.Add(code, new DeltaMove(pos, dir));
	}

	public void AddItem(ulong code, float3 pos, float3 dir, ItemStack its){
		ItemEntity newItemEntity = GameObject.Instantiate(this.droppedItemBase, pos, Quaternion.Euler(dir.x, dir.y, dir.z)).GetComponent<ItemEntity>();
		newItemEntity.SetItemStack(its);

		this.dropObject.Add(code, newItemEntity);
		this.dropCurrentPositions.Add(code, new DeltaMove(pos, dir));
	}

	// Triggers whenever a t(x) position is received. Moves entity to t(x-1) position received
	public void NudgeLastPos(EntityType type, ulong code, float3 pos, float3 dir){
		if(type == EntityType.PLAYER){	
			this.playerObject[code].transform.position = this.playerCurrentPositions[code].deltaPos;
			this.playerObject[code].transform.eulerAngles = this.playerCurrentPositions[code].deltaRot;
		
			this.playerCurrentPositions[code] = new DeltaMove(pos, dir);
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].go.transform.position = this.dropCurrentPositions[code].deltaPos;
			this.dropObject[code].go.transform.eulerAngles = this.dropCurrentPositions[code].deltaRot;
		
			this.dropCurrentPositions[code] = new DeltaMove(pos, dir);			
		}

	}

	// Fine movement of entity in frame deltas
	public void Nudge(EntityType type, ulong code, Vector3 dPos, Vector3 dRot){
		if(type == EntityType.PLAYER){
			this.playerObject[code].transform.position += (dPos * (Time.deltaTime / TimeOfDay.timeRate));
			this.playerObject[code].transform.eulerAngles += (dRot * (Time.deltaTime / TimeOfDay.timeRate));
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].go.transform.position += (dPos * (Time.deltaTime / TimeOfDay.timeRate));
			this.dropObject[code].go.transform.eulerAngles += (dRot * (Time.deltaTime / TimeOfDay.timeRate));			
		}
	}

	// ...
	public void Remove(EntityType type, ulong code){
		if(type == EntityType.PLAYER){
			this.playerObject[code].SetActive(false);
			GameObject.Destroy(this.playerObject[code]);
			this.playerObject.Remove(code);
			this.playerCurrentPositions.Remove(code);
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].go.SetActive(false);
			GameObject.Destroy(this.dropObject[code]);
			this.dropObject.Remove(code);
			this.dropCurrentPositions.Remove(code);
		}
	}

	public Vector3 GetLastPosition(EntityType type, ulong code){
		if(type == EntityType.PLAYER)
			return this.playerCurrentPositions[code].deltaPos;
		else
			return this.dropCurrentPositions[code].deltaPos;
	}

	public Vector3 GetLastRotation(EntityType type, ulong code){
		if(type == EntityType.PLAYER)
			return this.playerCurrentPositions[code].deltaRot;
		else
			return this.dropCurrentPositions[code].deltaRot;
	}
}
