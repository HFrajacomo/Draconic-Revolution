using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Deserializes Draconic Revolution Item Notation files
*/
public static class ItemDeserializer {
	// Generic Item Placeholders
	private static List<ItemBehaviour> onHoldPlayerEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onHoldClientEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onHoldServerEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onUnholdPlayerEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onUnholdClientEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onUnholdServerEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onUseClientEvent = new List<ItemBehaviour>();
	private static List<ItemBehaviour> onUseServerEvent = new List<ItemBehaviour>();

	private static Dictionary<string, List<string>> behaviours = new Dictionary<string, List<string>>();
	private static HashSet<string> assignedEvents = new HashSet<string>();
	private static Dictionary<string, ItemBehaviour> nameToBehaviour = new Dictionary<string, ItemBehaviour>();



	public static Item DeserializeItem(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson;

		Item item = JsonUtility.FromJson<Item>(JsonFormatter.RemoveComments(propertiesJson));
		item.SetMemoryStorageType();

		if(HasBehaviours(json)){
			behaviourJson = GetBehaviours(json);
			FindBehaviours(behaviourJson);
			DeserializeAllBehaviours(json);
		}

		AssignEventsToItem(item);
		Reset();

		return item;
	}

	private static void Reset(){
		onHoldPlayerEvent.Clear();
		onHoldClientEvent.Clear();
		onHoldServerEvent.Clear();
		onUnholdPlayerEvent.Clear();
		onUnholdClientEvent.Clear();
		onUnholdServerEvent.Clear();
		onUseClientEvent.Clear();
		onUseServerEvent.Clear();

		behaviours.Clear();
		nameToBehaviour.Clear();
	}

	private static void AssignEventsToItem(Item item){
		foreach(string ev in behaviours.Keys){
			switch(ev){
				case "onHoldPlayer":
					item.SetOnHoldPlayer(onHoldPlayerEvent);
					break;
				case "onHoldClient":
					item.SetOnHoldClient(onHoldClientEvent);
					break;
				case "onHoldServer":
					item.SetOnHoldServer(onHoldServerEvent);
					break;
				case "onUnholdPlayer":
					item.SetOnUnholdPlayer(onUnholdPlayerEvent);
					break;
				case "onUnholdClient":
					item.SetOnUnholdClient(onUnholdClientEvent);
					break;
				case "onUnholdServer":
					item.SetOnUnholdServer(onUnholdServerEvent);
					break;
				case "onUseClient":
					item.SetOnUseClient(onUseClientEvent);
					break;
				case "onUseServer":
					item.SetOnUseServer(onUseServerEvent);
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
		ItemBehaviour ib;

		foreach(string itemKey in behaviours.Keys){
			foreach(string itemValue in behaviours[itemKey]){
				// Skip event triggers that are already added
				if(assignedEvents.Contains(itemKey)){
					break;
				}

				if(nameToBehaviour.ContainsKey(itemValue)){
					ib = nameToBehaviour[itemValue];
				}
				else{
					ib = HandleBehaviourCreation(itemValue, json);
					nameToBehaviour.Add(itemValue, ib);
				}

				AddToPlaceholder(itemKey, ib);
			}

			assignedEvents.Add(itemKey);
		}

		assignedEvents.Clear();
	}

	private static ItemBehaviour HandleBehaviourCreation(string val, string json){
		string jsonSerial = GetSection(json, val);

		switch(val){
			case "PlaceBlockBehaviour":
				return JsonUtility.FromJson<PlaceBlockBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			case "CreatePointLightBehaviour":
				return JsonUtility.FromJson<CreatePointLightBehaviour>(JsonFormatter.RemoveComments(jsonSerial));
			default:
				Debug.LogError("ERROR WHEN TRYING TO DE-SERIALIZE BEHAVIOUR: " + val);
				return new PlaceBlockBehaviour();
		}
	}

	private static void AddToPlaceholder(string key, ItemBehaviour ib){
		switch(key){
			case "onHoldPlayer":
				onHoldPlayerEvent.Add(ib);
				break;
			case "onHoldClient":
				onHoldClientEvent.Add(ib);
				break;
			case "onHoldServer":
				onHoldServerEvent.Add(ib);
				break;
			case "onUnholdPlayer":
				onUnholdPlayerEvent.Add(ib);
				break;
			case "onUnholdClient":
				onUnholdClientEvent.Add(ib);
				break;
			case "onUnholdServer":
				onUnholdServerEvent.Add(ib);
				break;
			case "onUseClient":
				onUseClientEvent.Add(ib);
				break;
			case "onUseServer":
				onUseServerEvent.Add(ib);
				break;
			default:
				Debug.LogWarning("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + key);
				break;
		}
	}
}