using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Deserializes DRVN (Draconic Revolution Voxel Notation) files
*/
public static class VoxelDeserializer {
	// Generic Voxel Placeholders
	private static List<VoxelBehaviour> onPlaceEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onBreakEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onInteractEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onBlockUpdateEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onLoadEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onVFXBuildEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onVFXChangeEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onVFXBreakEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onSFXPlayEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> placementRuleEvent = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onPlayerStepEnter = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onPlayerStepExit = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onPlayerBodyEnter = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onPlayerBodyExit = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onPlayerHeadEnter = new List<VoxelBehaviour>();
	private static List<VoxelBehaviour> onPlayerHeadExit = new List<VoxelBehaviour>();

	// Objects Placeholders
	private static ModelIdentityBehaviour modelIdentityEvent;
	private static VoxelBehaviour offsetVectorEvent;
	private static VoxelBehaviour rotationValueEvent;

	private static Dictionary<string, List<string>> behaviours = new Dictionary<string, List<string>>();
	private static Dictionary<string, VoxelBehaviour> nameToBehaviour = new Dictionary<string, VoxelBehaviour>();
	private static HashSet<string> assignedEvents = new HashSet<string>();


	public static Blocks DeserializeBlock(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson;

		Blocks block = JsonUtility.FromJson<Blocks>(JsonFormatter.RemoveComments(propertiesJson));

		if(HasBehaviours(json)){
			behaviourJson = GetBehaviours(json);
			FindBehaviours(behaviourJson);
			DeserializeAllBehaviours(json);
		}

		AssignEventsToBlock(block);
		Reset();

		return block;
	}

	public static BlocklikeObject DeserializeObject(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson;

		BlocklikeObject obj = JsonUtility.FromJson<BlocklikeObject>(JsonFormatter.RemoveComments(propertiesJson));

		if(HasBehaviours(json)){
			behaviourJson = GetBehaviours(json);
			FindBehaviours(behaviourJson);
			DeserializeAllBehaviours(json);
		}

		AssignEventsToObject(obj);
		Reset();

		return obj;
	}

	private static void Reset(){
		onPlaceEvent.Clear();
		onBreakEvent.Clear();
		onInteractEvent.Clear();
		onBlockUpdateEvent.Clear();
		onLoadEvent.Clear();
		onVFXBuildEvent.Clear();
		onVFXChangeEvent.Clear();
		onVFXBreakEvent.Clear();
		onSFXPlayEvent.Clear();
		placementRuleEvent.Clear();
		onPlayerStepEnter.Clear();
		onPlayerStepExit.Clear();
		onPlayerBodyEnter.Clear();
		onPlayerBodyExit.Clear();
		onPlayerHeadEnter.Clear();
		onPlayerHeadExit.Clear();

		behaviours.Clear();
		nameToBehaviour.Clear();
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
				case "onPlayerStepEnter":
					block.SetOnPlayerStepEnter(onPlayerStepEnter);
					break;
				case "onPlayerStepExit":
					block.SetOnPlayerStepExit(onPlayerStepExit);
					break;
				case "onPlayerBodyEnter":
					block.SetOnPlayerBodyEnter(onPlayerBodyEnter);
					break;
				case "onPlayerBodyExit":
					block.SetOnPlayerBodyExit(onPlayerBodyExit);
					break;
				case "onPlayerHeadEnter":
					block.SetOnPlayerHeadEnter(onPlayerHeadEnter);
					break;
				case "onPlayerHeadExit":
					block.SetOnPlayerHeadExit(onPlayerHeadExit);
					break;
				default:
					Debug.LogWarning("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + ev);
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
				case "onPlayerStepEnter":
					obj.SetOnPlayerStepEnter(onPlayerStepEnter);
					break;
				case "onPlayerStepExit":
					obj.SetOnPlayerStepExit(onPlayerStepExit);
					break;
				case "onPlayerBodyEnter":
					obj.SetOnPlayerBodyEnter(onPlayerBodyEnter);
					break;
				case "onPlayerBodyExit":
					obj.SetOnPlayerBodyExit(onPlayerBodyExit);
					break;
				case "onPlayerHeadEnter":
					obj.SetOnPlayerHeadEnter(onPlayerHeadEnter);
					break;
				case "onPlayerHeadExit":
					obj.SetOnPlayerHeadExit(onPlayerHeadExit);
					break;
				default:
					Debug.LogWarning("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + ev);
					break;
			}
		}
	}

	private static string GetProperties(string json){
		return json.Split("--->Behaviours")[0];
	}

	private static bool HasBehaviours(string json){
		int index = json.IndexOf("--->Behaviours");

		if(index == -1)
			return false;
		return true;
	}

	private static string GetBehaviours(string json){
		return json.Split("--->Behaviours")[1].Split("--->")[0];
	}

	private static string GetSection(string json, string section){
		return json.Split("--->" + section)[1].Split("--->")[0];
	}

	private static void FindBehaviours(string json){
		if(json == "")
			return;

		behaviours.Clear();
		string[] keyVal;

		json = json.Replace("{", "").Replace("}", "").Replace("\t", "").Replace(" ", "").Replace("\"", "").Replace("\r", "");

		foreach(string line in json.Split("\n")){
			if(line.Length <= 1)
				continue;
		
			keyVal = line.Split(':');

			behaviours.Add(keyVal[0], JsonFormatter.StringToList(keyVal[1]));

		}
	}

	/*
	Must be added whenever a new Behaviour is created
	*/
	private static void DeserializeAllBehaviours(string json){
		VoxelBehaviour vxb;

		foreach(string itemKey in behaviours.Keys){
			foreach(string itemValue in behaviours[itemKey]){
				// Skip event triggers that are already added
				if(assignedEvents.Contains(itemKey)){
					break;
				}

				if(nameToBehaviour.ContainsKey(itemValue)){
					vxb = nameToBehaviour[itemValue];
				}
				else{
					vxb = HandleBehaviourCreation(itemValue, json);
					nameToBehaviour.Add(itemValue, vxb);
				}

				AddToPlaceholder(itemKey, vxb);
			}

			assignedEvents.Add(itemKey);
		}

		assignedEvents.Clear();
	}

	private static VoxelBehaviour HandleBehaviourCreation(string val, string json){
		string jsonSerial = GetSection(json, val);

		switch(val){
			case "LiquidBehaviour":
				return JsonUtility.FromJson<LiquidBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "TreeBehaviour":
				return JsonUtility.FromJson<TreeBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "BreakDropItemBehaviour":
				return JsonUtility.FromJson<BreakDropItemBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "InteractChangeBlockBehaviour":
				return JsonUtility.FromJson<InteractChangeBlockBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "ModelIdentityBehaviour":
				return JsonUtility.FromJson<ModelIdentityBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "PlaceSetStateBehaviour":
				return JsonUtility.FromJson<PlaceSetStateBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "TorchBehaviour":
				return JsonUtility.FromJson<TorchBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "UpdateDecaySecondaryBlockBehaviour":
				return JsonUtility.FromJson<UpdateDecaySecondaryBlockBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "AuraCrystalBehaviour":
				return JsonUtility.FromJson<AuraCrystalBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "ConfigurablePositionBehaviour":
				return JsonUtility.FromJson<ConfigurablePositionBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "ConfigurableRotationBehaviour":
				return JsonUtility.FromJson<ConfigurableRotationBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "PlayerMovementEnterMultiplyBehaviour":
				return JsonUtility.FromJson<PlayerMovementEnterMultiplyBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "PlayerMovementExitMultiplyBehaviour":
				return JsonUtility.FromJson<PlayerMovementExitMultiplyBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "PlayerMovementStepMultiplyBehaviour":
				return JsonUtility.FromJson<PlayerMovementStepMultiplyBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "TestVoxelBehaviour":
				return JsonUtility.FromJson<TestVoxelBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			default:
				Debug.LogError("ERROR WHEN TRYING TO DE-SERIALIZE BEHAVIOUR: " + val);
				return new LiquidBehaviour();
		}
	}

	private static void AddToPlaceholder(string key, VoxelBehaviour vx){
		switch(key){
			case "onPlace":
				onPlaceEvent.Add(vx);
				break;
			case "onBreak":
				onBreakEvent.Add(vx);
				break;
			case "onInteract":
				onInteractEvent.Add(vx);
				break;
			case "onBlockUpdate":
				onBlockUpdateEvent.Add(vx);
				break;
			case "onLoad":
				onLoadEvent.Add(vx);
				break;
			case "onVFXBuild":
				onVFXBuildEvent.Add(vx);
				break;
			case "onVFXChange":
				onVFXChangeEvent.Add(vx);
				break;
			case "onVFXBreak":
				onVFXBreakEvent.Add(vx);
				break;
			case "onSFXPlay":
				onSFXPlayEvent.Add(vx);
				break;
			case "placementRule":
				placementRuleEvent.Add(vx);
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
			case "onPlayerStepEnter":
				onPlayerStepEnter.Add(vx);
				break;
			case "onPlayerStepExit":
				onPlayerStepExit.Add(vx);
				break;
			case "onPlayerBodyEnter":
				onPlayerBodyEnter.Add(vx);
				break;
			case "onPlayerBodyExit":
				onPlayerBodyExit.Add(vx);
				break;
			case "onPlayerHeadEnter":
				onPlayerHeadEnter.Add(vx);
				break;
			case "onPlayerHeadExit":
				onPlayerHeadExit.Add(vx);
				break;
			default:
				Debug.LogWarning("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + key);
				break;
		}
	}
}