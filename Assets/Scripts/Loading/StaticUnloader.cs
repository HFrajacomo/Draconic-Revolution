using System;
using UnityEngine;

public class StaticUnloader : MonoBehaviour{

	void Awake(){
		DontDestroyOnLoad(this.gameObject);
	}

	void OnApplicationQuit(){
		// Queued chunk saves need Compression, which is freed below
		RegionFileHandler.FlushAllHandlers();

		Compression.Destroy();
		VoxelLoader.Destroy();
	}
}