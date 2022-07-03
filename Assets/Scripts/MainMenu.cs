using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using static System.IO.Path;
using TMPro;

public class MainMenu : MonoBehaviour
{

	// Unity Reference
	public GameObject skybox;
	public GameObject mainMenu;
	public GameObject singleplayerMenu;
	public GameObject singleplayerNewMenu;
	public GameObject multiplayerMenu;
	public GameObject optionsMenu;

	// Input
	public Text single_nameField;
	public Text single_seedField;
	public Text single_renderField;
	public Text multi_IPField;
	public Text multi_renderField;
	public Text multi_accountField;
	public TextMeshProUGUI fullbrightText;
	public InputField single_nameInput;
	public InputField single_seedInput;
	public InputField single_renderInput;
	public InputField multi_ipInput;
	public InputField multi_accountInput;
	public InputField multi_renderInput;

	// Initial Button
	public Button singleplayerButton;
	public Button multiplayerPlayButton;

	// Sliders
	public ScrollRect singleplayer_sliderList;
	public GameObject worldButtonPrefab;
	private GameObject cacheObj;

	// Flags
	private static bool firstLoad = true;
	private static MenuCode code = MenuCode.MAIN;

	// Color Constants
	private Color GREEN = new Color(0.3f, 0.9f, 0.2f);
	private Color RED = new Color(0.3f, 0.0f, 0.0f);

	// Menu Maps
	private int currentSelection = 0;
	private Dictionary<int, Selectable> singlePlayerMap = new Dictionary<int, Selectable>();
	private Dictionary<int, Selectable> multiPlayerMap = new Dictionary<int, Selectable>();
	private Dictionary<int, Selectable> optionsMap = new Dictionary<int, Selectable>();

	// Directories
	private string[] worldNames;
	private string worldsDir;
	private List<string> worldsList = new List<string>();


	public void OnApplicationQuit(){
		BlockEncyclopediaECS.Destroy();
	}

	public void Start(){
		if(MainMenu.firstLoad)
			GameObject.DontDestroyOnLoad(this.skybox);
		else
			UnloadMemory();

		EnvironmentVariablesCentral.Start();
		World.SetGameSceneFlag(false);

		MainMenu.firstLoad = false;
		this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "\\Worlds\\";

		CreateSinglePlayerMap();
		CreateMultiPlayerMap();
		OpenMainMenu();
	}

	public void Update(){
		if(Input.GetKeyDown(KeyCode.Tab)){
			switch(MainMenu.code){
				case MenuCode.MAIN:
					break;
				case MenuCode.SINGLEPLAYER:
					if(IncrementTab())
						singlePlayerMap[this.currentSelection].Select();
					break;
				case MenuCode.MULTIPLAYER:
					if(IncrementTab())
						multiPlayerMap[this.currentSelection].Select();
					break;
				case MenuCode.OPTIONS:
					if(IncrementTab())
						optionsMap[this.currentSelection].Select();
					break;
				default:
					break;
			}
		}
	}



	public void UnloadMemory(){
		System.GC.Collect();
		Resources.UnloadUnusedAssets();		
	}

	public void StartGameSingleplayer(string world){
		if(single_renderField.text == ""){
			World.SetRenderDistance("5");
		}
		else{
			World.SetRenderDistance(single_renderField.text);
		}

		World.SetWorldName(world);
		World.SetWorldSeed(0);
		World.SetToClient();


		SceneManager.LoadScene(1);
	}

	
	public void CreateNewWorld(){
		int rn;

		if(single_nameField.text == ""){
			return;
		}

		if(single_seedField.text == ""){
			Random.InitState((int)DateTime.Now.Ticks);
			rn = (int)Random.Range(0, int.MaxValue);
			World.SetWorldSeed(rn.ToString());
		}
		else{
			World.SetWorldSeed(single_seedField.text);
		}

		World.SetWorldName(single_nameField.text);
		
		if(RegionFileHandler.CreateWorldFile(World.worldName, World.worldSeed))
			OpenSingleplayerMenu();
	}

	public void StartGameMultiplayer(){
		if(multi_IPField.text == "")
			return;
		if(multi_accountField.text == "")
			return;

		World.SetAccountID(multi_accountField.text);

		World.SetIP(multi_IPField.text);

		if(multi_renderField.text == ""){
			World.SetRenderDistance("5");
		}
		else{
			World.SetRenderDistance(multi_renderField.text);
		}

		World.SetToServer();

		SceneManager.LoadScene(1);
	}

	public void OpenMainMenu(){
		ChangeVisibleMenu(this.mainMenu);
		singleplayerButton.Select();
		MainMenu.code = MenuCode.MAIN;
	}

	public void OpenSingleplayerMenu(){
		ChangeVisibleMenu(this.singleplayerMenu);
		this.currentSelection = 0;
		single_nameInput.Select();
		MainMenu.code = MenuCode.SINGLEPLAYER;

		ListWorldFolders();
	}

	public void OpenSingleplayerNewMenu(){
		ChangeVisibleMenu(this.singleplayerNewMenu);
		single_nameInput.text = "";
		single_seedInput.text = "";
		this.currentSelection = 0;
		MainMenu.code = MenuCode.SINGLEPLAYER_NEW;
	}

	public void OpenMultiplayerMenu(){
		ChangeVisibleMenu(this.multiplayerMenu);
		this.currentSelection = 0;
		multi_ipInput.Select();
		MainMenu.code = MenuCode.MULTIPLAYER;
	}

	public void OpenOptionsMenu(){
		SetFullBrightColor();
		ChangeVisibleMenu(this.optionsMenu);
		this.currentSelection = 0;
		MainMenu.code = MenuCode.OPTIONS;
	}

	public void ExitGame(){
		Application.Quit();
	}

	private void ChangeVisibleMenu(GameObject go){
		this.mainMenu.SetActive(false);
		this.singleplayerMenu.SetActive(false);
		this.multiplayerMenu.SetActive(false);
		this.optionsMenu.SetActive(false);
		this.singleplayerNewMenu.SetActive(false);
		go.SetActive(true);
	}

	public void ToogleFullbright(){
		Configurations.FULLBRIGHT = !Configurations.FULLBRIGHT;
		SetFullBrightColor();
	}

	private void SetFullBrightColor(){
		if(Configurations.FULLBRIGHT)
			fullbrightText.color = GREEN;
		else
			fullbrightText.color = RED;		
	}

	private void CreateSinglePlayerMap(){
		this.singlePlayerMap.Add(0, single_nameInput);
		this.singlePlayerMap.Add(1, single_seedInput);
		this.singlePlayerMap.Add(2, single_renderInput);
	}

	private void CreateMultiPlayerMap(){
		this.multiPlayerMap.Add(0, multi_ipInput);
		this.multiPlayerMap.Add(1, multi_accountInput);
		this.multiPlayerMap.Add(2, multi_renderInput);
		this.multiPlayerMap.Add(3, multiplayerPlayButton);
	}

	private bool IncrementTab(){
		switch(MainMenu.code){
			case MenuCode.MAIN:
				return false;
			case MenuCode.SINGLEPLAYER:
				if(this.singlePlayerMap.Count == 0)
					return false;

				this.currentSelection++;
				if(this.currentSelection >= this.singlePlayerMap.Count)
					this.currentSelection = 0;
				return true;
			case MenuCode.MULTIPLAYER:
				if(this.multiPlayerMap.Count == 0)
					return false;

				this.currentSelection++;
				if(this.currentSelection >= this.multiPlayerMap.Count)
					this.currentSelection = 0;
				return true;
			case MenuCode.OPTIONS:
				if(this.optionsMap.Count == 0)
					return false;

				this.currentSelection++;
				if(this.currentSelection >= this.optionsMap.Count)
					this.currentSelection = 0;
				return true;
			case MenuCode.SINGLEPLAYER_NEW:
				return false;
			default:
				return false;
		}
	}

	private bool ListWorldFolders(){
		string worldName;

		this.worldsList.Clear();
		DeleteChildGameObjects(this.singleplayer_sliderList);

		this.worldNames = Directory.GetDirectories(this.worldsDir);

		foreach(string world in this.worldNames){
			worldName = GetDirectoryName(world);

			this.worldsList.Add(worldName);

			this.cacheObj = GameObject.Instantiate(this.worldButtonPrefab);
			this.cacheObj.transform.SetParent(this.singleplayer_sliderList.content.transform);
			this.cacheObj.GetComponentInChildren<TextMeshProUGUI>().text = worldName;
		}

		if(this.worldNames.Length > 0)
			return true;
		return false;
	}

	private void DeleteChildGameObjects(ScrollRect go){
		foreach(RectTransform child in go.content.transform){
			GameObject.Destroy(child.gameObject);
		}
	}

	private string GetDirectoryName(string path){
		string[] pathList = path.Split("\\");
		return pathList[pathList.Length-1];
	}
}

public enum MenuCode : byte{
	MAIN,
	SINGLEPLAYER,
	SINGLEPLAYER_NEW,
	MULTIPLAYER,
	OPTIONS
}
