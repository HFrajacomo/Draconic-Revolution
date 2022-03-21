using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

using UnityEngine.UI;
using UnityEngine.SceneManagement;

using static System.IO.Path;

public class MainMenu : MonoBehaviour
{

	// Unity Reference
	public GameObject mainMenu;
	public GameObject singleplayerMenu;
	public GameObject multiplayerMenu;
	public GameObject optionsMenu;

	// Input
	public Text single_nameField;
	public Text single_seedField;
	public Text single_renderField;
	public Text multi_IPField;
	public Text multi_renderField;
	public Text multi_accountField;

	public void Start(){
		Resources.UnloadUnusedAssets();
		System.GC.Collect();
		EnvironmentVariablesCentral.Start();
		OpenMainMenu();
	}

	public void StartGameSingleplayer(){
		int rn;

		if(single_nameField.text == ""){
			return;
		}

		if(single_seedField.text == ""){
			Random.InitState((int)DateTime.Now.Ticks);
			rn = (int)Random.Range(0, 999999);
			World.SetWorldSeed(rn.ToString());
		}
		else{
			World.SetWorldSeed(single_seedField.text);
		}

		if(single_renderField.text == ""){
			World.SetRenderDistance("5");
		}
		else{
			World.SetRenderDistance(single_renderField.text);
		}

		World.SetWorldName(single_nameField.text);
		World.SetToClient();


		SceneManager.LoadScene(1);
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
	}

	public void OpenSingleplayerMenu(){
		ChangeVisibleMenu(this.singleplayerMenu);
	}

	public void OpenMultiplayerMenu(){
		ChangeVisibleMenu(this.multiplayerMenu);
	}

	public void OpenOptionsMenu(){
		ChangeVisibleMenu(this.optionsMenu);
	}

	public void ExitGame(){
		Application.Quit();
	}

	private void ChangeVisibleMenu(GameObject go){
		this.mainMenu.SetActive(false);
		this.singleplayerMenu.SetActive(false);
		this.multiplayerMenu.SetActive(false);
		this.optionsMenu.SetActive(false);
		go.SetActive(true);
	}
}
