using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DefragmentWorldMenu : Menu
{
	[Header("UI Elements")]
	public Button defragmentButton;
	public Button cancelButton;
	public Button backButton;
	public Slider progressBar;

	[Header("Text Elements")]
	public Text currentRegionText;
	public Text progressText;
	public Text totalWorldSize;
	public Text defragedWorldSize;

	[Header("Material")]
	public Material progressBarMaterial;

	// Defragmenter
	private RegionDefragmenter defrag;

	// Defragment operations
	private static string WORLD_NAME;
	private bool isDefraging;
	private List<string> regionFilepath = new List<string>();
	private int amountOfRegions;

	// Directories
	private string saveDir;
	private string worldDir;


	void Update(){

	}

	public static void SetWorldName(string name){WORLD_NAME = name;}

	public void StartDefragment(){
		GetAllRegionFilePaths();
		this.amountOfRegions = this.regionFilepath.Count;
	}

	public void OpenSelectWorldMenu(){
		if(!this.isDefraging){
			this.RequestMenuChange(MenuID.SELECT_WORLD);
		}
	}

	public void GetAllRegionFilePaths(){
		#if UNITY_EDITOR
			this.saveDir = "Worlds/";
			this.worldDir = this.saveDir + WORLD_NAME + "/";
		#else
			// If is in Dedicated Server
			if(!World.isClient){
				this.saveDir = "Worlds/";
				this.worldDir = this.saveDir + WORLD_NAME + "/";
			}
			// If it's a Local Server
			else{
				this.saveDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";
				this.worldDir = this.saveDir + WORLD_NAME + "/";			
			}
		#endif


		string[] files = Directory.GetFiles(this.worldDir);
		string[] splitted;
		string fileName = "";

        foreach (string file in files){
        	if(file.Split(".")[1] == "rdf"){
        		splitted = file.Split("/");
        		fileName = splitted[splitted.Length - 1].Split(".")[0];

        		this.regionFilepath.Add(fileName);
        	}
        }
	}
}