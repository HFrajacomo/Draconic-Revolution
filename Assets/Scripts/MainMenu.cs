using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

	public Text nameField;
	public Text seedField;


	public void StartGame(){
		int rn;

		if(nameField.text == ""){
			return;
		}

		if(seedField.text == ""){
			Random.InitState((int)DateTime.Now.Ticks);
			rn = (int)Random.Range(0, 999999);
			World.SetWorldSeed(rn.ToString());
		}
		else{
			World.SetWorldSeed(seedField.text);
		}

		World.SetWorldName(nameField.text);

		SceneManager.LoadScene(1);
	}

}
