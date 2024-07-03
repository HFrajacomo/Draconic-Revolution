using System;
using UnityEngine;

public class StaticUnloader : MonoBehaviour{

	void Awake(){
		DontDestroyOnLoad(this.gameObject);
	}

	void OnApplicationQuit(){
		Compression.Destroy();
		VoxelLoader.Destroy();
	}
}