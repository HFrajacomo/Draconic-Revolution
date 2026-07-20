using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

	public static GameObject Instantiate(string name, bool castShadows){
		GameObject instance;
		GameObject prefab;
		MeshRenderer renderer;

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

		if(!castShadows){
			renderer = instance.GetComponent<MeshRenderer>();

			if(renderer != null){
				renderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}

		return instance;
	}

	public static void ApplyTransform(GameObject go, bool flip, float heightOffset=0f){
		go.transform.localPosition = Vector3.up * heightOffset;
		go.transform.localScale = SCALING;
		
		if(flip){
			go.transform.localRotation = Quaternion.Euler(Vector3.right * 180f);
		}
		else{
			go.transform.localRotation = Quaternion.Euler(Vector3.zero);

		}
	}
}