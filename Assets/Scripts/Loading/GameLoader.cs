using System;
using static System.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour {
	public GameObject prefabObjects;

	private bool isClient = true;
	private VoxelLoader voxelLoader;
	private ItemLoader itemLoader;
	private ShaderLoader shaderLoader;

	private static readonly string SERVER_SCENE = "Assets/Scenes/Server.unity";

	void Awake(){
		if(SceneUtility.GetScenePathByBuildIndex(1) == SERVER_SCENE){
			this.isClient = false;
		}
	}

	void Start(){
		this.shaderLoader = new ShaderLoader(this.isClient);
		this.itemLoader = new ItemLoader(this.isClient);
		this.voxelLoader = new VoxelLoader(this.isClient, this.prefabObjects);

		this.shaderLoader.Load();
		this.itemLoader.Load();
		this.voxelLoader.Load();

		this.itemLoader.RunPostDeserializationRoutine();
		this.voxelLoader.RunPostDeserializationRoutine();

		if(this.isClient)
			SceneManager.LoadScene("Menu");
		else
			SceneManager.LoadScene("Server");
	}
}