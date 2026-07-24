using System;
using System.Collections.Generic;
using static System.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour {
	public GameObject prefabObjects;
	public bool testScene = false;

	private bool isClient = true;
	private VoxelLoader voxelLoader;
	private ItemLoader itemLoader;
	private ShaderLoader shaderLoader;
	private AudioLoader audioLoader;
	private StructureLoader structureLoader;
	private AnimationLoader animationLoader;
	private InventoryLoader inventoryLoader;

	private static readonly string SERVER_SCENE = "Assets/Scenes/Server.unity";

	void Awake(){
		if(!testScene){
			if(SceneUtility.GetScenePathByBuildIndex(1) == SERVER_SCENE){
				this.isClient = false;
			}
		}
	}

	void Start(){
		this.shaderLoader = new ShaderLoader(this.isClient);
		this.itemLoader = new ItemLoader(this.isClient);
		this.voxelLoader = new VoxelLoader(this.isClient, this.prefabObjects);
		this.audioLoader = new AudioLoader(this.isClient);
		this.structureLoader = new StructureLoader(this.isClient);
        this.animationLoader = new AnimationLoader(this.isClient);
        this.inventoryLoader = new InventoryLoader(this.isClient);
		
		this.shaderLoader.Load();
		this.itemLoader.Load();
		this.voxelLoader.Load();
		this.audioLoader.Load();
		this.structureLoader.Load();
		this.animationLoader.Load();
		this.inventoryLoader.Load();


		this.itemLoader.RunPostDeserializationRoutine();
		this.voxelLoader.RunPostDeserializationRoutine();
		this.structureLoader.RunPostDeserializationRoutine();

		if(!testScene){
			if(this.isClient)
				SceneManager.LoadScene("Menu");
			else
				SceneManager.LoadScene("Server");
		}
	}
}