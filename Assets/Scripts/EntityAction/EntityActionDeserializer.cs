using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Deserializes Draconic Revolution Item Notation files
*/
public static class EntityActionDeserializer {
	private static List<EntityActionBehaviour> onIconDrawEvent;
	private static List<EntityActionBehaviour> onStackDrawEvent;
	private static List<EntityActionBehaviour> onHoldPlayerEvent;
	private static List<EntityActionBehaviour> onHoldClientEvent;
	private static List<EntityActionBehaviour> onHoldServerEvent;
	private static List<EntityActionBehaviour> onUnholdPlayerEvent;
	private static List<EntityActionBehaviour> onUnholdClientEvent;
	private static List<EntityActionBehaviour> onUnholdServerEvent;
	private static List<EntityActionBehaviour> onPrimaryPlayerEvent;
	private static List<EntityActionBehaviour> onPrimaryClientEvent;
	private static List<EntityActionBehaviour> onPrimaryServerEvent;
	private static List<EntityActionBehaviour> onPrimaryHoldPlayerEvent;
	private static List<EntityActionBehaviour> onPrimaryHoldClientEvent;
	private static List<EntityActionBehaviour> onPrimaryHoldServerEvent;
	private static List<EntityActionBehaviour> onSecondaryPlayerEvent;
	private static List<EntityActionBehaviour> onSecondaryClientEvent;
	private static List<EntityActionBehaviour> onSecondaryServerEvent;
	private static List<EntityActionBehaviour> onSecondaryHoldPlayerEvent;
	private static List<EntityActionBehaviour> onSecondaryHoldClientEvent;
	private static List<EntityActionBehaviour> onSecondaryHoldServerEvent;
	private static List<EntityActionBehaviour> onTerciaryPlayerEvent;
	private static List<EntityActionBehaviour> onTerciaryClientEvent;
	private static List<EntityActionBehaviour> onTerciaryServerEvent;

	private static Dictionary<string, List<string>> behaviours = new Dictionary<string, List<string>>();
	private static HashSet<string> assignedEvents = new HashSet<string>();
	private static Dictionary<string, EntityActionBehaviour> nameToBehaviour = new Dictionary<string, EntityActionBehaviour>();

	public static EntityAction DeserializeAction(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson;

		EntityAction action = JsonUtility.FromJson<EntityAction>(JsonFormatter.RemoveComments(json));

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
					action.SetOnIconDraw(onIconDrawEvent);
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
			// Add Behaviours here
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
			case "onPrimaryPlayerBehaviour":
				onPrimaryPlayerEvent.Add(eab);
				break;
			case "onPrimaryClientBehaviour":
				onPrimaryClientEvent.Add(eab);
				break;
			case "onPrimaryServerBehaviour":
				onPrimaryServerEvent.Add(eab);
				break;
			case "onPrimaryHoldPlayerBehaviour":
				onPrimaryHoldPlayerEvent.Add(eab);
				break;
			case "onPrimaryHoldClientBehaviour":
				onPrimaryHoldClientEvent.Add(eab);
				break;
			case "onPrimaryHoldServerBehaviour":
				onPrimaryHoldServerEvent.Add(eab);
				break;
			case "onSecondaryPlayerBehaviour":
				onSecondaryPlayerEvent.Add(eab);
				break;
			case "onSecondaryClientBehaviour":
				onSecondaryClientEvent.Add(eab);
				break;
			case "onSecondaryServerBehaviour":
				onSecondaryServerEvent.Add(eab);
				break;
			case "onSecondaryHoldPlayerBehaviour":
				onSecondaryHoldPlayerEvent.Add(eab);
				break;
			case "onSecondaryHoldClientBehaviour":
				onSecondaryHoldClientEvent.Add(eab);
				break;
			case "onSecondaryHoldServerBehaviour":
				onSecondaryHoldServerEvent.Add(eab);
				break;
			case "onTerciaryPlayerBehaviour":
				onTerciaryPlayerEvent.Add(eab);
				break;
			case "onTerciaryClientBehaviour":
				onTerciaryClientEvent.Add(eab);
				break;
			case "onTerciaryServerBehaviour":
				onTerciaryServerEvent.Add(eab);
				break;
			default:
				throw new DeserializationErrorException($"ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: {key}");
		}
	}
}