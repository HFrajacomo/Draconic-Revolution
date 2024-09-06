using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler
{
	private ChunkLoader cl;

	private Dictionary<ulong, CharacterSheet> playerSheet;
	private Dictionary<ulong, GameObject> playerObject;
	private Dictionary<ulong, MeshRenderer> playerRenderer;
	private Dictionary<ulong, DeltaMove> playerCurrentPositions;
	private Dictionary<ulong, ItemEntity> dropObject;
	private Dictionary<ulong, DeltaMove> dropCurrentPositions;
	private GameObject droppedItemBase = GameObject.Find("----- PrefabModels -----/DroppedItemBase");

	private int currentTogglingCounter;
	private static readonly int MAX_COUNTER_VALUE = 4;


	public EntityHandler(ChunkLoader cl){
		this.playerSheet = new Dictionary<ulong, CharacterSheet>();
		this.playerObject = new Dictionary<ulong, GameObject>();
		this.playerRenderer = new Dictionary<ulong, MeshRenderer>();
		this.playerCurrentPositions = new Dictionary<ulong, DeltaMove>();
		this.dropObject = new Dictionary<ulong, ItemEntity>();
		this.dropCurrentPositions = new Dictionary<ulong, DeltaMove>();
		this.cl = cl;
	}

	/*
	Runs through all entities in Handler to verify which ones are bordering RenderDistance and cannot be rendered
	*/
	public void RunEntityActivation(){
		currentTogglingCounter = currentTogglingCounter % MAX_COUNTER_VALUE;

		if(currentTogglingCounter == 0){
			foreach(ulong u in playerObject.Keys){
				this.playerRenderer[u].enabled = this.cl.playerPositionHandler.IsInPlayerRenderDistance(this.playerObject[u].transform.position);
			}
		}
		else if(currentTogglingCounter == 1){
			foreach(ItemEntity ie in dropObject.Values){
				ie.SetVisible(this.cl.playerPositionHandler.IsInPlayerRenderDistance(ie.transform.position));
			}
		}

		currentTogglingCounter++;
	}

	public void RunSingleActivation(EntityType type, ulong code, float3 pos){
		if(type == EntityType.PLAYER){
			this.playerRenderer[code].enabled = this.cl.playerPositionHandler.IsInPlayerRenderDistance(pos);
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].SetVisible(this.cl.playerPositionHandler.IsInPlayerRenderDistance(pos));
		}
	}


	// Only works while there is no other EntityTypes here other than player
	public bool Contains(EntityType type, ulong code){
		if(type == EntityType.PLAYER)
			return this.playerObject.ContainsKey(code);
		else if(type == EntityType.DROP)
			return this.dropObject.ContainsKey(code);
		return false;
	}

	public void AddPlayer(ulong code, float3 pos, float3 dir, CharacterSheet sheet){
		GameObject go = GameObject.Instantiate(GameObject.Find("----- PrefabModels -----/PlayerModel"), new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(dir.x, dir.y, dir.z));
		go.name = "Player_" + code;
		this.playerObject.Add(code, go);
		this.playerRenderer.Add(code, go.GetComponent<MeshRenderer>());
		this.playerCurrentPositions.Add(code, new DeltaMove(pos, dir));
		this.playerSheet.Add(code, sheet);
		RunSingleActivation(EntityType.PLAYER, code, pos);
	}

	public void UpdatePlayerModel(ulong code, CharacterAppearance app){
		// TODO
	}

	public void AddItem(ulong code, float3 pos, float3 dir, ItemStack its){
		ItemEntity newItemEntity = GameObject.Instantiate(this.droppedItemBase, pos, Quaternion.Euler(dir.x, dir.y, dir.z)).GetComponent<ItemEntity>();
		newItemEntity.SetItemStack(its);

		this.dropObject.Add(code, newItemEntity);
		this.dropCurrentPositions.Add(code, new DeltaMove(pos, dir));
		RunSingleActivation(EntityType.DROP, code, pos);
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
		}
	}

	// Should only be used to hard set item rotation animation position
	public void SetItemPosition(ulong code, Vector3 newPos){
		if(this.dropObject.ContainsKey(code)){
			this.dropObject[code].go.transform.position = newPos;
		}
	}

	// ...
	public void Remove(EntityType type, ulong code){
		if(type == EntityType.PLAYER){
			this.playerObject[code].SetActive(false);
			GameObject.Destroy(this.playerObject[code]);
			this.playerObject.Remove(code);
			this.playerRenderer.Remove(code);
			this.playerCurrentPositions.Remove(code);
			this.playerSheet.Remove(code);
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].go.SetActive(false);
			GameObject.Destroy(this.dropObject[code]);
			this.dropObject.Remove(code);
			this.dropCurrentPositions.Remove(code);
		}
	}

	public void ToggleItemAnimation(EntityType type, ulong code, float f){
		if(type == EntityType.DROP){
			if(this.dropObject.ContainsKey(code)){
				if(f == 1f){
					this.dropObject[code].PlayRotationAnimation();
				}
				else{
					this.dropObject[code].PlaySpinAnimation();
				}
			}
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

	public CharacterSheet GetPlayerSheet(ulong code){
		return this.playerSheet[code];
	}
}
