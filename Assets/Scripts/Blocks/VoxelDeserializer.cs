using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Deserializes DRVN (Draconic Revolution Voxel Notation) files
*/
public static class VoxelDeserializer {
	// Generic Voxel Placeholders
	private static VoxelBehaviour onPlaceEvent;
	private static VoxelBehaviour onBreakEvent;
	private static VoxelBehaviour onInteractEvent;
	private static VoxelBehaviour onBlockUpdateEvent;
	private static VoxelBehaviour onLoadEvent;
	private static VoxelBehaviour onVFXBuildEvent;
	private static VoxelBehaviour onVFXChangeEvent;
	private static VoxelBehaviour onVFXBreakEvent;
	private static VoxelBehaviour onSFXPlayEvent;
	private static VoxelBehaviour placementRuleEvent;

	// Objects Placeholders
	private static ModelIdentityBehaviour modelIdentityEvent;
	private static VoxelBehaviour offsetVectorEvent;
	private static VoxelBehaviour rotationValueEvent;

	private static Dictionary<string, string> behaviours = new Dictionary<string, string>();
	private static HashSet<string> assignedEvents = new HashSet<string>();


	public static Blocks DeserializeBlock(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson = GetBehaviours(json);

		Blocks block = JsonUtility.FromJson<Blocks>(propertiesJson);
		FindBehaviours(behaviourJson);
		DeserializeAllBehaviours(json);

		behaviours.Clear();

		AssignEventsToBlock(block);

		return block;
	}

	public static BlocklikeObject DeserializeObject(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson = GetBehaviours(json);

		BlocklikeObject obj = JsonUtility.FromJson<BlocklikeObject>(propertiesJson);
		FindBehaviours(behaviourJson);
		DeserializeAllBehaviours(json);

		behaviours.Clear();

		AssignEventsToObject(obj);

		return obj;
	}

	private static void AssignEventsToBlock(Blocks block){
		foreach(string ev in behaviours.Keys){
			switch(ev){
				case "onPlace":
					block.SetOnPlace(onPlaceEvent);
					break;
				case "onBreak":
					block.SetOnBreak(onBreakEvent);
					break;
				case "onInteract":
					block.SetOnInteract(onInteractEvent);
					break;
				case "onBlockUpdate":
					block.SetOnBlockUpdate(onBlockUpdateEvent);
					break;
				case "onLoad":
					block.SetOnLoad(onLoadEvent);
					break;
				case "onVFXBuild":
					block.SetOnVFXBuild(onVFXBuildEvent);
					break;
				case "onVFXChange":
					block.SetOnVFXChange(onVFXChangeEvent);
					break;
				case "onVFXBreak":
					block.SetOnVFXBreak(onVFXBreakEvent);
					break;
				case "onSFXPlay":
					block.SetOnSFXPlay(onSFXPlayEvent);
					break;
				case "placementRule":
					block.SetPlacementRule(placementRuleEvent);
					break;
				default:
					Debug.Log("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + ev);
					break;
			}
		}
	}

	private static void AssignEventsToObject(BlocklikeObject obj){
		foreach(string ev in behaviours.Keys){
			switch(ev){
				case "onPlace":
					obj.SetOnPlace(onPlaceEvent);
					break;
				case "onBreak":
					obj.SetOnBreak(onBreakEvent);
					break;
				case "onInteract":
					obj.SetOnInteract(onInteractEvent);
					break;
				case "onBlockUpdate":
					obj.SetOnBlockUpdate(onBlockUpdateEvent);
					break;
				case "onLoad":
					obj.SetOnLoad(onLoadEvent);
					break;
				case "onVFXBuild":
					obj.SetOnVFXBuild(onVFXBuildEvent);
					break;
				case "onVFXChange":
					obj.SetOnVFXChange(onVFXChangeEvent);
					break;
				case "onVFXBreak":
					obj.SetOnVFXBreak(onVFXBreakEvent);
					break;
				case "onSFXPlay":
					obj.SetOnSFXPlay(onSFXPlayEvent);
					break;
				case "placementRule":
					obj.SetPlacementRule(placementRuleEvent);
					break;
				case "offsetVector":
					obj.SetOffsetVector(offsetVectorEvent);
					break;
				case "rotationValue":
					obj.SetRotationValue(rotationValueEvent);
					break;
				case "modelIdentity":
					obj.SetModelIdentity(modelIdentityEvent);
					break;
				default:
					Debug.Log("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + ev);
					break;
			}
		}
	}

	private static string GetProperties(string json){
		return json.Split("---Behaviours")[0];
	}

	private static string GetBehaviours(string json){
		return json.Split("---Behaviours")[1].Split("---")[0];
	}

	private static string GetSection(string json, string section){
		return json.Split("---" + section)[1].Split("---")[0];
	}

	private static void FindBehaviours(string json){
		behaviours.Clear();

		string[] keyVal;

		json = json.Replace("{", "").Replace("}", "").Replace("\t", "").Replace(" ", "");

		foreach(string line in json.Split("\n")){
			if(line.Length == 0)
				continue;
		
			keyVal = line.Split(":");

			behaviours.Add(keyVal[0], keyVal[1]);
		}
	}

	/*
	Must be added whenever a new Behaviour is created
	*/
	private static void DeserializeAllBehaviours(string json){
		VoxelBehaviour vxb;

		foreach(KeyValuePair<string, string> item in behaviours){
			if(assignedEvents.Contains(item.Key)){
				continue;
			}

			vxb = HandleBehaviourCreation(item.Value, json);

			foreach(KeyValuePair<string, string> insideItem in behaviours){
				if(assignedEvents.Contains(item.Key)){
					continue;
				}

				if(insideItem.Value == item.Value){
					assignedEvents.Add(insideItem.Key);
					AddToPlaceholder(insideItem.Key, vxb);
				}
			}
		}

		assignedEvents.Clear();
	}

	private static VoxelBehaviour HandleBehaviourCreation(string val, string json){
		string jsonSerial = GetSection(json, val.Replace("\"", ""));

		switch(val){
			case "LiquidBehaviour":
				return JsonUtility.FromJson<LiquidBehaviour>(jsonSerial);
			case "TreeBehaviour":
				return JsonUtility.FromJson<TreeBehaviour>(jsonSerial);
			case "BreakDropItemBehaviour":
				return JsonUtility.FromJson<BreakDropItemBehaviour>(jsonSerial);
			case "InteractChangeBlockBehaviour":
				return JsonUtility.FromJson<InteractChangeBlockBehaviour>(jsonSerial);
			case "ModelIdentityBehaviour":
				return JsonUtility.FromJson<ModelIdentityBehaviour>(jsonSerial);
			case "PlaceSetStateBehaviour":
				return JsonUtility.FromJson<PlaceSetStateBehaviour>(jsonSerial);
			case "TorchBehaviour":
				return JsonUtility.FromJson<TorchBehaviour>(jsonSerial);
			case "UpdateDecaySecondaryBlockBehaviour":
				return JsonUtility.FromJson<UpdateDecaySecondaryBlockBehaviour>(jsonSerial);
			case "VisCrystalBehaviour":
				return JsonUtility.FromJson<VisCrystalBehaviour>(jsonSerial);
			default:
				Debug.Log("ERROR WHEN TRYING TO DE-SERIALIZE BEHAVIOUR: " + val);
				return new LiquidBehaviour();
		}
	}

	private static void AddToPlaceholder(string key, VoxelBehaviour vx){
		switch(key){
			case "onPlace":
				onPlaceEvent = vx;
				break;
			case "onBreak":
				onBreakEvent = vx;
				break;
			case "onInteract":
				onInteractEvent = vx;
				break;
			case "onBlockUpdate":
				onBlockUpdateEvent = vx;
				break;
			case "onLoad":
				onLoadEvent = vx;
				break;
			case "onVFXBuild":
				onVFXBuildEvent = vx;
				break;
			case "onVFXChange":
				onVFXChangeEvent = vx;
				break;
			case "onVFXBreak":
				onVFXBreakEvent = vx;
				break;
			case "onSFXPlay":
				onSFXPlayEvent = vx;
				break;
			case "placementRule":
				placementRuleEvent = vx;
				break;
			case "modelIdentity":
				modelIdentityEvent = (ModelIdentityBehaviour)vx;
				break;
			case "offsetVector":
				offsetVectorEvent = vx;
				break;
			case "rotationValue":
				rotationValueEvent = vx;
				break;
			default:
				Debug.Log("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + key);
				break;
		}
	}

	/*

	private static VoxelBehaviour onInteractEvent;
	private static VoxelBehaviour onBlockUpdateEvent;
	private static VoxelBehaviour onLoadEvent;
	private static VoxelBehaviour onVFXBuildEvent;
	private static VoxelBehaviour onVFXChangeEvent;
	private static VoxelBehaviour onVFXBreakEvent;
	private static VoxelBehaviour onSFXPlayEvent;
	private static VoxelBehaviour placementRuleEvent;

	private static ModelIdentityBehaviour modelIdentityEvent;
	private static VoxelBehaviour offsetVectorEvent;
	private static VoxelBehaviour rotationValueEvent;
	 */
}