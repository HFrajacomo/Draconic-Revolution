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
	public Text multi_IPField;
	public TextMeshProUGUI fullbrightText;
	public InputField single_nameInput;
	public InputField single_seedInput;
	public InputField multi_ipInput;
	public TMP_InputField options_accountIDInput;
	public TextMeshProUGUI subtitlesText;

	// Initial Button
	public Button singleplayerButton;
	public Button multiplayerPlayButton;
	public Button singleplayerCreateButton;
	public Button singleplayerNewButton;

	// Sliders
	public ScrollRect singleplayer_sliderList;
	public GameObject worldButtonPrefab;
	private GameObject cacheObj;
	public Slider renderDistanceSlider;
	public Slider music2DMusicSlider;
	public Slider music3DMusicSlider;
	public Slider sfx2DMusicSlider;
	public Slider sfx3DMusicSlider;
	public Slider voice2DMusicSlider;
	public Slider voice3DMusicSlider;

	// Flags
	private static bool firstLoad = true;
	private static MenuCode code = MenuCode.MAIN;

	// Color Constants
	private Color GREEN = new Color(0.3f, 0.9f, 0.2f);
	private Color RED = new Color(0.3f, 0.0f, 0.0f);

	// Menu Maps
	private int currentSelection = 0;
	private Dictionary<int, Selectable> singlePlayerNewMap = new Dictionary<int, Selectable>();
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
		Configurations.LoadConfigFile();
		World.SetGameSceneFlag(false);

		MainMenu.firstLoad = false;
		this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "\\Worlds\\";

		CreateSinglePlayerNewMap();
		CreateMultiPlayerMap();
		OpenMainMenu();

		GameObject.Find("AudioManager").GetComponent<AudioManager>().RefreshVolume();
	}

	public void Update(){
		if(Input.GetKeyDown(KeyCode.Tab)){
			switch(MainMenu.code){
				case MenuCode.MAIN:
					break;
				case MenuCode.SINGLEPLAYER:
					break;
				case MenuCode.MULTIPLAYER:
					if(IncrementTab())
						multiPlayerMap[this.currentSelection].Select();
					break;
				case MenuCode.OPTIONS:
					if(IncrementTab())
						optionsMap[this.currentSelection].Select();
					break;
				case MenuCode.SINGLEPLAYER_NEW:
					if(IncrementTab())
						singlePlayerNewMap[this.currentSelection].Select();
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

		World.SetAccountID(Configurations.accountID);

		World.SetIP(multi_IPField.text);

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
		singleplayerNewButton.Select();
		this.currentSelection = 0;
		MainMenu.code = MenuCode.SINGLEPLAYER;

		ListWorldFolders();
	}

	public void OpenSingleplayerNewMenu(){
		ChangeVisibleMenu(this.singleplayerNewMenu);
		single_nameInput.text = "";
		single_seedInput.text = "";
		single_nameInput.Select();
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
		SetSubtitlesColor();
		ChangeVisibleMenu(this.optionsMenu);
		this.currentSelection = 0;
		this.renderDistanceSlider.value = World.renderDistance;
		this.options_accountIDInput.text = Configurations.accountID.ToString();
		this.music2DMusicSlider.value = Configurations.music2DVolume;
		this.music3DMusicSlider.value = Configurations.music3DVolume;
		this.sfx2DMusicSlider.value = Configurations.sfx2DVolume;
		this.sfx3DMusicSlider.value = Configurations.sfx3DVolume;
		this.voice2DMusicSlider.value = Configurations.voice2DVolume;
		this.voice3DMusicSlider.value = Configurations.voice3DVolume;
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

	public void ToggleSubtitles(){
		Configurations.subtitlesOn = !Configurations.subtitlesOn;
		SetSubtitlesColor();
	}

	public void SaveConfigs(){
		Configurations.SaveConfigFile();
		GameObject.Find("AudioManager").GetComponent<AudioManager>().RefreshVolume();
	}

	private void SetFullBrightColor(){
		if(Configurations.FULLBRIGHT)
			fullbrightText.color = GREEN;
		else
			fullbrightText.color = RED;
	}

	private void SetSubtitlesColor(){
		if(Configurations.subtitlesOn)
			subtitlesText.color = GREEN;
		else
			subtitlesText.color = RED;		
	}

	private void CreateSinglePlayerNewMap(){
		this.singlePlayerNewMap.Add(0, single_nameInput);
		this.singlePlayerNewMap.Add(1, single_seedInput);
		this.singlePlayerNewMap.Add(2, singleplayerCreateButton);
	}

	private void CreateMultiPlayerMap(){
		this.multiPlayerMap.Add(0, multi_ipInput);
		this.multiPlayerMap.Add(1, multiplayerPlayButton);
	}

	private bool IncrementTab(){
		switch(MainMenu.code){
			case MenuCode.MAIN:
				return false;
			case MenuCode.SINGLEPLAYER:
				return false;
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
				if(this.singlePlayerNewMap.Count == 0)
					return false;

				this.currentSelection++;
				if(this.currentSelection >= this.singlePlayerNewMap.Count)
					this.currentSelection = 0;
				return true;
			default:
				return false;
		}
	}

	private bool ListWorldFolders(){
		string worldName;

		this.worldsList.Clear();
		DeleteChildGameObjects(this.singleplayer_sliderList);

		if(!Directory.Exists(this.worldsDir))
			return false;

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
