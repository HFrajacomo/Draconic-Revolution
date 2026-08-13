using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Deserializes Draconic Revolution Item Notation files
*/
public static class EntityActionDeserializer {
	private static List<EntityActionBehaviour> onIconDrawEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onStackDrawEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onHoldPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onHoldClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onHoldServerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onUnholdPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onUnholdClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onUnholdServerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onPrimaryPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onPrimaryClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onPrimaryServerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onPrimaryHoldPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onPrimaryHoldClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onPrimaryHoldServerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onSecondaryPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onSecondaryClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onSecondaryServerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onSecondaryHoldPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onSecondaryHoldClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onSecondaryHoldServerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onTerciaryPlayerEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onTerciaryClientEvent = new List<EntityActionBehaviour>();
	private static List<EntityActionBehaviour> onTerciaryServerEvent = new List<EntityActionBehaviour>();

	private static Dictionary<string, List<string>> behaviours = new Dictionary<string, List<string>>();
	private static HashSet<string> assignedEvents = new HashSet<string>();
	private static Dictionary<string, EntityActionBehaviour> nameToBehaviour = new Dictionary<string, EntityActionBehaviour>();

	public static EntityAction DeserializeAction(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson;

		EntityAction action = JsonUtility.FromJson<EntityAction>(JsonFormatter.RemoveComments(propertiesJson));

		if(HasBehaviours(json)){
			behaviourJson = GetBehaviours(json);
			FindBehaviours(behaviourJson);
			DeserializeAllBehaviours(json);
		}

		AssignEventsToAction(action);
		Reset();

		return action;
	}

	private static void Reset(){
		onIconDrawEvent = new List<EntityActionBehaviour>();
		onStackDrawEvent = new List<EntityActionBehaviour>();
		onHoldPlayerEvent = new List<EntityActionBehaviour>();
		onHoldClientEvent = new List<EntityActionBehaviour>();
		onHoldServerEvent = new List<EntityActionBehaviour>();
		onUnholdPlayerEvent = new List<EntityActionBehaviour>();
		onUnholdClientEvent = new List<EntityActionBehaviour>();
		onUnholdServerEvent = new List<EntityActionBehaviour>();
		onPrimaryPlayerEvent = new List<EntityActionBehaviour>();
		onPrimaryClientEvent = new List<EntityActionBehaviour>();
		onPrimaryServerEvent = new List<EntityActionBehaviour>();
		onPrimaryHoldPlayerEvent = new List<EntityActionBehaviour>();
		onPrimaryHoldClientEvent = new List<EntityActionBehaviour>();
		onPrimaryHoldServerEvent = new List<EntityActionBehaviour>();
		onSecondaryPlayerEvent = new List<EntityActionBehaviour>();
		onSecondaryClientEvent = new List<EntityActionBehaviour>();
		onSecondaryServerEvent = new List<EntityActionBehaviour>();
		onSecondaryHoldPlayerEvent = new List<EntityActionBehaviour>();
		onSecondaryHoldClientEvent = new List<EntityActionBehaviour>();
		onSecondaryHoldServerEvent = new List<EntityActionBehaviour>();
		onTerciaryPlayerEvent = new List<EntityActionBehaviour>();
		onTerciaryClientEvent = new List<EntityActionBehaviour>();
		onTerciaryServerEvent = new List<EntityActionBehaviour>();

		behaviours.Clear();
		nameToBehaviour.Clear();
	}

	private static void AssignEventsToAction(EntityAction action){
		foreach(string ev in behaviours.Keys){
			switch(ev){
				case "onIconDraw":
					action.SetOnIconDraw(onIconDrawEvent);
					break;
				case "onStackDraw":
					action.SetOnStackDraw(onStackDrawEvent);
					break;
				case "onHoldPlayer":
					action.SetOnHoldPlayer(onHoldPlayerEvent);
					break;
				case "onHoldClient":
					action.SetOnHoldClient(onHoldClientEvent);
					break;
				case "onHoldServer":
					action.SetOnHoldServer(onHoldServerEvent);
					break;
				case "onUnholdPlayer":
					action.SetOnUnholdPlayer(onUnholdPlayerEvent);
					break;
				case "onUnholdClient":
					action.SetOnUnholdClient(onUnholdClientEvent);
					break;
				case "onUnholdServer":
					action.SetOnUnholdServer(onUnholdServerEvent);
					break;
				case "onPrimaryPlayer":
					action.SetOnPrimaryPlayer(onPrimaryPlayerEvent);
					break;
				case "onPrimaryClient":
					action.SetOnPrimaryClient(onPrimaryClientEvent);
					break;
				case "onPrimaryServer":
					action.SetOnPrimaryServer(onPrimaryServerEvent);
					break;
				case "onPrimaryHoldPlayer":
					action.SetOnPrimaryHoldPlayer(onPrimaryHoldPlayerEvent);
					break;
				case "onPrimaryHoldClient":
					action.SetOnPrimaryHoldClient(onPrimaryHoldClientEvent);
					break;
				case "onPrimaryHoldServer":
					action.SetOnPrimaryHoldServer(onPrimaryHoldServerEvent);
					break;
				case "onSecondaryPlayer":
					action.SetOnSecondaryPlayer(onSecondaryPlayerEvent);
					break;
				case "onSecondaryClient":
					action.SetOnSecondaryClient(onSecondaryClientEvent);
					break;
				case "onSecondaryServer":
					action.SetOnSecondaryServer(onSecondaryServerEvent);
					break;
				case "onSecondaryHoldPlayer":
					action.SetOnSecondaryHoldPlayer(onSecondaryHoldPlayerEvent);
					break;
				case "onSecondaryHoldClient":
					action.SetOnSecondaryHoldClient(onSecondaryHoldClientEvent);
					break;
				case "onSecondaryHoldServer":
					action.SetOnSecondaryHoldServer(onSecondaryHoldServerEvent);
					break;
				case "onTerciaryPlayer":
					action.SetOnTerciaryPlayer(onTerciaryPlayerEvent);
					break;
				case "onTerciaryClient":
					action.SetOnTerciaryClient(onTerciaryClientEvent);
					break;
				case "onTerciaryServer":
					action.SetOnTerciaryServer(onTerciaryServerEvent);
					break;
				default:
					throw new DeserializationErrorException($"[EntityActionDeserializer] ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: {ev}");
			}
		}
	}

	private static EntityActionBehaviour HandleBehaviourCreation(string val, string json){
		string jsonSerial = GetSection(json, val);

		switch(val){
			case "EAItemUIBehaviour":
				return JsonUtility.FromJson<EAItemUIBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "EAPlaceBlockBehaviour":
				return JsonUtility.FromJson<EAPlaceBlockBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			default:
				throw new DeserializationErrorException($"[EntityActionDeserializer] ERROR WHEN TRYING TO DE-SERIALIZE BEHAVIOUR: {val}");
		}
	}

	private static string GetProperties(string json){
		if(json.Contains("--->Behaviours"))
			return json.Split("--->Behaviours")[0];
		else
			return json.Split("--->Type")[0];
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

	private static string GetTypeSection(string json){
		return json.Split("--->Type")[1].Replace("\r", "").Replace("\n", "");
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

	private static void DeserializeAllBehaviours(string json){
		EntityActionBehaviour eab;

		foreach(string itemKey in behaviours.Keys){
			foreach(string itemValue in behaviours[itemKey]){
				// Skip event triggers that are already added
				if(assignedEvents.Contains(itemKey)){
					break;
				}

				if(nameToBehaviour.ContainsKey(itemValue)){
					eab = nameToBehaviour[itemValue];
				}
				else{
					eab = HandleBehaviourCreation(itemValue, json);
					nameToBehaviour.Add(itemValue, eab);
				}

				AddToPlaceholder(itemKey, eab);
			}

			assignedEvents.Add(itemKey);
		}

		assignedEvents.Clear();
	}

	private static void AddToPlaceholder(string key, EntityActionBehaviour eab){
		switch(key){
			case "onIconDraw":
				onIconDrawEvent.Add(eab);
				break;
			case "onStackDraw":
				onStackDrawEvent.Add(eab);
				break;
			case "onHoldPlayer":
				onHoldPlayerEvent.Add(eab);
				break;
			case "onHoldClient":
				onHoldClientEvent.Add(eab);
				break;
			case "onHoldServer":
				onHoldServerEvent.Add(eab);
				break;
			case "onUnholdPlayer":
				onUnholdPlayerEvent.Add(eab);
				break;
			case "onUnholdClient":
				onUnholdClientEvent.Add(eab);
				break;
			case "onUnholdServer":
				onUnholdServerEvent.Add(eab);
				break;
			case "onPrimaryPlayer":
				onPrimaryPlayerEvent.Add(eab);
				break;
			case "onPrimaryClient":
				onPrimaryClientEvent.Add(eab);
				break;
			case "onPrimaryServer":
				onPrimaryServerEvent.Add(eab);
				break;
			case "onPrimaryHoldPlayer":
				onPrimaryHoldPlayerEvent.Add(eab);
				break;
			case "onPrimaryHoldClient":
				onPrimaryHoldClientEvent.Add(eab);
				break;
			case "onPrimaryHoldServer":
				onPrimaryHoldServerEvent.Add(eab);
				break;
			case "onSecondaryPlayer":
				onSecondaryPlayerEvent.Add(eab);
				break;
			case "onSecondaryClient":
				onSecondaryClientEvent.Add(eab);
				break;
			case "onSecondaryServer":
				onSecondaryServerEvent.Add(eab);
				break;
			case "onSecondaryHoldPlayer":
				onSecondaryHoldPlayerEvent.Add(eab);
				break;
			case "onSecondaryHoldClient":
				onSecondaryHoldClientEvent.Add(eab);
				break;
			case "onSecondaryHoldServer":
				onSecondaryHoldServerEvent.Add(eab);
				break;
			case "onTerciaryPlayer":
				onTerciaryPlayerEvent.Add(eab);
				break;
			case "onTerciaryClient":
				onTerciaryClientEvent.Add(eab);
				break;
			case "onTerciaryServer":
				onTerciaryServerEvent.Add(eab);
				break;
			default:
				throw new DeserializationErrorException($"ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: {key}");
		}
	}
}