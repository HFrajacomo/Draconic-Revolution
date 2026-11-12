using System.Collections.Generic;
using UnityEngine;

/*
Network-transferable description of a VFX attached to a BattleStyle attachment of an Entity
	The VFX is an instance of the prefab found in prefabPath, parented to the attachment in the given anchor
*/
public struct EntityVFXData{
	public string name;
	public BoneAnchorType anchor;
	public float offsetX, offsetY, offsetZ;
	public float rotationX, rotationY, rotationZ;
	public string prefabPath;

	// Prefabs live in the Game scene and are destroyed whenever the scene is reloaded
	private static Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

	public EntityVFXData(string name, BoneAnchorType anchor, float offsetX, float offsetY, float offsetZ, float rotationX, float rotationY, float rotationZ, string prefabPath){
		this.name = name;
		this.anchor = anchor;
		this.offsetX = offsetX;
		this.offsetY = offsetY;
		this.offsetZ = offsetZ;
		this.rotationX = rotationX;
		this.rotationY = rotationY;
		this.rotationZ = rotationZ;
		this.prefabPath = prefabPath;
	}

	public EntityVFXData(string name){
		this.name = name;
		this.anchor = (BoneAnchorType)0;
		this.offsetX = 0f;
		this.offsetY = 0f;
		this.offsetZ = 0f;
		this.rotationX = 0f;
		this.rotationY = 0f;
		this.rotationZ = 0f;
		this.prefabPath = "";
	}

	// Instantiates the VFX as a child of the attachment. Returns null if the attachment or the prefab doesn't exist
	public GameObject Build(GameObject attachment, bool firstPerson){
		if(attachment == null)
			return null;

		GameObject prefab = GetPrefab(this.prefabPath);

		if(prefab == null)
			return null;

		GameObject visualEffect = GameObject.Instantiate(prefab);
		visualEffect.name = this.name;
		visualEffect.transform.parent = attachment.transform;
		visualEffect.transform.localPosition = new Vector3(this.offsetX, this.offsetY, this.offsetZ);
		visualEffect.transform.localEulerAngles = new Vector3(this.rotationX, this.rotationY, this.rotationZ);
		visualEffect.transform.localScale = Vector3.one;

		if(firstPerson)
			visualEffect.layer = 12;

		return visualEffect;
	}

	private static GameObject GetPrefab(string path){
		GameObject prefab;

		if(prefabs.TryGetValue(path, out prefab) && prefab != null)
			return prefab;

		prefab = GameObject.Find(path);

		if(prefab == null){
			Debug.LogError($"[EntityVFXData] Could not find VFX prefab: {path}");
			return null;
		}

		prefabs[path] = prefab;
		return prefab;
	}
}
