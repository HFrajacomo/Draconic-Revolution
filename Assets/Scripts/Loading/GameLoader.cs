using System;
using static System.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour {
	private bool isClient = true;
	private VoxelLoader voxelLoader;

	private static readonly string SERVER_SCENE = "Assets/Scenes/Server.unity";

	void Awake(){
		if(SceneUtility.GetScenePathByBuildIndex(1) == SERVER_SCENE){
			this.isClient = false;
		}
	}

	void Start(){
		this.voxelLoader = new VoxelLoader(this.isClient);
		this.voxelLoader.Load();

		if(this.isClient)
			SceneManager.LoadScene("Menu");
		else
			SceneManager.LoadScene("Server");
	}
}