using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResetWorldMenu : Menu
{
	[Header("UI Elements")]
	public Text confirmText;

	private static string WORLD_NAME;
	private static readonly string MESSAGE = "Are you sure you want to reset world ";
	private static readonly string RICHTEXT_COLOR = "#ff0000ff";
	private List<string> filenames;


	public static void SetWorldName(string name){WORLD_NAME = name;}

	public override void Enable(){
		this.mainObject.SetActive(true);
		confirmText.text = MESSAGE + ApplyColor(WORLD_NAME);
	}

	private string ApplyColor(string text){return "<color=" + RICHTEXT_COLOR + ">" + text + "</color>";}

	public void OpenSelectWorldMenu(){this.RequestMenuChange(MenuID.SELECT_WORLD);}

	public void ResetWorld(){
		// Delete Regions
		filenames = EnvironmentVariablesCentral.ListFilesInWorldFolder(WORLD_NAME, firstLetterFilter:'r');

		foreach(string file in filenames){
			File.Delete(EnvironmentVariablesCentral.saveDir + WORLD_NAME + "/" + file);
		}

		// Delete Entities
		filenames = EnvironmentVariablesCentral.ListFilesInWorldFolder(WORLD_NAME, firstLetterFilter:'e');

		foreach(string file in filenames){
			File.Delete(EnvironmentVariablesCentral.saveDir + WORLD_NAME + "/" + file);
		}

		this.RequestMenuChange(MenuID.SELECT_WORLD);
	}
}