#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public static class FindMissingMonobehaviour {
	[MenuItem("Editor Tools/Find Missing Scripts in Project")]
	static void FindMissingScripts(){
		ScanProject();
	}

	private static void ScanProject(){
			int missingCount = 0;

			// Scan scene objects
			GameObject[] allSceneObjects = GameObject.FindObjectsByType<GameObject>();
			foreach (GameObject obj in allSceneObjects)
			{
				missingCount += ScanGameObject(obj, obj.scene.name);
			}

			// Scan prefabs
			string[] allPrefabPaths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
			foreach (string prefabPath in allPrefabPaths)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
				if (prefab != null)
				{
					missingCount += ScanGameObject(prefab, prefabPath);
				}
			}

			Debug.Log($"Scan complete. Found {missingCount} GameObjects with missing MonoBehaviours.");
	}


	private static int ScanGameObject(GameObject obj, string source){
		int count = 0;
		Component[] components = obj.GetComponents<Component>();
		for (int i = 0; i < components.Length; i++)
		{
			if (components[i] == null)
			{
				Debug.LogWarning($"Missing script in GameObject '{obj.name}' (Source: {source})", obj);
				count++;
			}
		}

		// Recursively scan children
		foreach (Transform child in obj.transform)
		{
			count += ScanGameObject(child.gameObject, source);
		}

		return count;
	}
}
#endif