using System;
using System.Collections.Generic;
using static System.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour {
	public GameObject prefabObjects;

	private bool isClient = true;
	private VoxelLoader voxelLoader;
	private ItemLoader itemLoader;
	private ShaderLoader shaderLoader;
	private AudioLoader audioLoader;
	private StructureLoader structureLoader;
	private AnimationLoader animationLoader;

	#if UNITY_EDITOR
	private AnimationControlBuilder animationControlBuilder;
	#endif

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
		this.audioLoader = new AudioLoader(this.isClient);
		this.structureLoader = new StructureLoader(this.isClient);

        #if UNITY_EDITOR
        	this.animationControlBuilder = new AnimationControlBuilder(this.isClient);
            this.animationControlBuilder.Build();
        #endif

        this.animationLoader = new AnimationLoader(this.isClient);
		
		this.shaderLoader.Load();
		this.itemLoader.Load();
		this.voxelLoader.Load();
		this.audioLoader.Load();
		this.structureLoader.Load();
		this.animationLoader.Load();


		this.itemLoader.RunPostDeserializationRoutine();
		this.voxelLoader.RunPostDeserializationRoutine();
		this.structureLoader.RunPostDeserializationRoutine();

		if(this.isClient)
			SceneManager.LoadScene("Menu");
		else
			SceneManager.LoadScene("Server");
	}
}