using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RenameWorldMenu : Menu
{
	[Header("UI Elements")]
	public InputField nameField;
	public Text placeholderText;

	// Directory information
	private static string WORLD_NAME;
	private string saveDir;


	void Awake(){this.nameField.onValidateInput += ValidateFilename;}

	public override void Enable(){
		this.placeholderText.text = WORLD_NAME;
		this.mainObject.SetActive(true);
	}

    public override void Disable(){
        DeselectClickedButton();
        this.mainObject.SetActive(false);

        this.nameField.text = "";
    }

	public static void SetWorldName(string newName){WORLD_NAME = newName;}

	public void RenameWorld(){
		if(this.nameField.text == "")
			return;

		this.saveDir = EnvironmentVariablesCentral.saveDir;

		string fullpath = this.saveDir + WORLD_NAME + "/";

		if(Directory.Exists(fullpath)){
			Directory.Move(fullpath, this.saveDir + this.nameField.text + "/");
			OpenSelectWorldMenu();
		}
	}

	public void OpenSelectWorldMenu(){this.RequestMenuChange(MenuID.SELECT_WORLD);}

    private char ValidateFilename(string text, int charIndex, char addedChar){
        if(char.IsLetter(addedChar))
            return addedChar;
        else if(text.Length > 0 && addedChar == ' ')
            return addedChar;
        return '\0';
    }  
}