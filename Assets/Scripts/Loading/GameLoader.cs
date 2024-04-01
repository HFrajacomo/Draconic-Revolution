using System;
using static System.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour {
	private bool isClient = false;
	private VoxelLoader voxelLoader;

	void Awake(){
		string[] args = GetCommandLineArgs();

		foreach(string arg in args){
			switch(arg){
				case "-Local":
					this.isClient = true;
					break;
				default:
					break;
			}
		}
	}

	void Start(){
		this.voxelLoader = new VoxelLoader(this.isClient);
		this.voxelLoader.Load();

		SceneManager.LoadScene("Menu");
	}
}