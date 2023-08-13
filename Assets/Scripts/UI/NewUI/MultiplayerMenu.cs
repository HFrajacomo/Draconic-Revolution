using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MultiplayerMenu : Menu
{
	// GameObjects
	public Button playButton;
	public Button backButton;
	public Button localButton;
	public InputField ipField;
	public Text ipText;

	private static readonly HashSet<char> VALID_HEX_IP = new HashSet<char>(){'a', 'b', 'c', 'd', 'e', 'f'};
	private static readonly string LOCALHOST = "127.0.0.1";

	public override void Disable(){
        DeselectClickedButton();
        this.mainObject.SetActive(false);

        RebuildText(this.ipField);
	}

	void Start(){
		ipField.onValidateInput += ValidateIP;
	}


	public void OpenInitialMenu(){this.RequestMenuChange(MenuID.INITIAL_MENU);}

    public void RebuildText(InputField parent){
        parent.ProcessEvent(Event.KeyboardEvent("^a"));
        parent.ProcessEvent(Event.KeyboardEvent("backspace"));
        parent.ForceLabelUpdate();
    }

	public void StartGameMultiplayer(){
		if(this.ipText.text == "")
			return;

		World.SetAccountID(Configurations.accountID);

		World.SetIP(this.ipText.text);

		World.SetToServer();

		SceneManager.LoadScene(1);
	}

	public void StartLocalGame(){
		World.SetAccountID(Configurations.accountID);

		World.SetIP(LOCALHOST);

		World.SetToServer();

		SceneManager.LoadScene(1);
	}

    private char ValidateIP(string text, int charIndex, char addedChar){
        if(char.IsDigit(addedChar))
            return addedChar;
        else if(addedChar == '.' || addedChar == ':')
        	return addedChar;
        else if(VALID_HEX_IP.Contains(addedChar))
        	return addedChar;
        return '\0';
    }
}