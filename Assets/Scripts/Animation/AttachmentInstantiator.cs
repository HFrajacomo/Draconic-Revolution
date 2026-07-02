using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Loads model objects and assigns them to "--- LoadedAttachments ---" organizer object.
Models are loaded in the main thread so far and are not unloaded (might change for better experience)
*/

public static class AttachmentInstantiator {
	private static readonly Transform organizerObject;
	private static readonly string ITEM_MODEL_RESPATH = "ItemModels/";
	private static Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
	private static Dictionary<string, int> instanceCounter = new Dictionary<string, int>();
	private static Vector3 SCALING = new Vector3(.01f, .01f, .01f);

	static AttachmentInstantiator(){
		organizerObject = GameObject.Find("----- LoadedAttachments -----").transform;
	}

	public static GameObject Instantiate(string name){
		GameObject instance;
		GameObject prefab;

		if(!prefabs.ContainsKey(name)){
			prefab = Resources.Load<GameObject>($"{ITEM_MODEL_RESPATH}{name}");
			prefabs.Add(name, prefab);
			instanceCounter.Add(name, 1);			
		}

		instance = GameObject.Instantiate(prefabs[name]);
		instance.transform.parent = organizerObject;
		instance.transform.localScale = SCALING;
		instance.name = $"{name}_{instanceCounter[name]}";
		instanceCounter[name] += 1;
		return instance;
	}

	public static void ApplyScaling(GameObject go){
		go.transform.localScale = SCALING;
	}
}