using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
			rn = (int)Random.Range(0, 1000000);
			seedField.text = rn.ToString();
		}

		World.SetWorldName(nameField.text);
		World.SetWorldSeed(seedField.text);

		SceneManager.LoadScene(1);
	}

}
