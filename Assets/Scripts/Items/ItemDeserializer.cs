using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Deserializes Draconic Revolution Item Notation files
*/
public static class ItemDeserializer {
	// Generic Item Placeholders
	private static ItemBehaviour onHoldEvent;
	private static ItemBehaviour onUseClientEvent;
	private static ItemBehaviour onUseServerEvent;

	private static Dictionary<string, string> behaviours = new Dictionary<string, string>();
	private static HashSet<string> assignedEvents = new HashSet<string>();


	public static Item DeserializeItem(string json){
		string propertiesJson = GetProperties(json);
		string behaviourJson;

		Item item = JsonUtility.FromJson<Item>(propertiesJson);
		item.SetMemoryStorageType();

		if(HasBehaviours(json)){
			behaviourJson = GetBehaviours(json);
			FindBehaviours(behaviourJson);
			DeserializeAllBehaviours(json);
		}

		AssignEventsToItem(item);

		behaviours.Clear();

		return item;
	}

	private static void AssignEventsToItem(Item item){
		foreach(string ev in behaviours.Keys){
			switch(ev){
				case "onHold":
					item.SetOnHold(onHoldEvent);
					break;
				case "onUseClient":
					item.SetOnUseClient(onUseClientEvent);
					break;
				case "onUseServer":
					item.SetOnUseServer(onUseServerEvent);
					break;
				default:
					Debug.Log("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + ev);
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

			behaviours.Add(keyVal[0], keyVal[1].Replace("\n", "").Replace(",", ""));
		}
	}

	/*
	Must be added whenever a new Behaviour is created
	*/
	private static void DeserializeAllBehaviours(string json){
		ItemBehaviour ib;

		foreach(KeyValuePair<string, string> item in behaviours){
			if(assignedEvents.Contains(item.Key)){
				continue;
			}

			ib = HandleBehaviourCreation(item.Value, json);

			foreach(KeyValuePair<string, string> insideItem in behaviours){
				if(assignedEvents.Contains(item.Key)){
					continue;
				}

				if(insideItem.Value == item.Value){
					assignedEvents.Add(insideItem.Key);
					AddToPlaceholder(insideItem.Key, ib);
				}
			}
		}

		assignedEvents.Clear();
	}

	private static ItemBehaviour HandleBehaviourCreation(string val, string json){
		string jsonSerial = GetSection(json, val);

		switch(val){
			case "PlaceBlockBehaviour":
				return JsonUtility.FromJson<PlaceBlockBehaviour>(jsonSerial);
			default:
				Debug.Log("ERROR WHEN TRYING TO DE-SERIALIZE BEHAVIOUR: " + val);
				return new PlaceBlockBehaviour();
		}
	}

	private static void AddToPlaceholder(string key, ItemBehaviour ib){
		switch(key){
			case "onHold":
				onHoldEvent = ib;
				break;
			case "onUseClient":
				onUseClientEvent = ib;
				break;
			case "onUseServer":
				onUseServerEvent = ib;
				break;
			default:
				Debug.Log("ERROR WHILE TRYING TO DE-SERIALIZE AN EVENT: " + key);
				break;
		}
	}
}