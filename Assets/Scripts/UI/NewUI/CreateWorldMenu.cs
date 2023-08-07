using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class CreateWorldMenu : Menu{
    public Button createWorldButton;
    public Button backButton;
    public InputField nameField;
    public InputField seedField;
    public Text nameText;
    public Text seedText;

    // Cache
    private Text cacheText;
    private GameObject cacheObj;


    public override void Disable(){
        DeselectClickedButton();
        this.mainObject.SetActive(false);

        RebuildText(this.nameText, this.nameField);
        RebuildText(this.seedText, this.seedField);
    }

    public override void Enable(){
        this.mainObject.SetActive(true);
    }

	void Start(){
        nameField.onValidateInput += ValidateFilename;
        seedField.onValidateInput += ValidateSeedNumber;
	}

    public void RebuildText(Text text, InputField field){
    }

    private char ValidateSeedNumber(string text, int charIndex, char addedChar){
        if(text.Length >= 9)
            return '\0';
        if(char.IsDigit(addedChar))
            return addedChar;
        return '\0';
    }

    private char ValidateFilename(string text, int charIndex, char addedChar){
        if(char.IsLetter(addedChar))
            return addedChar;
        else if(text.Length > 0 && addedChar == ' ')
            return addedChar;
        return '\0';
    }    


    public void CreateNewWorld(){
        int rn;

        if(this.nameText.text == ""){
            return;
        }

        if(this.seedText.text == ""){
            Random.InitState((int)DateTime.Now.Ticks);
            rn = (int)Random.Range(0, int.MaxValue);
            World.SetWorldSeed(rn.ToString());
        }
        else{
            World.SetWorldSeed(this.seedText.text);
        }

        World.SetWorldName(this.nameText.text);
        
        if(RegionFileHandler.CreateWorldFile(World.worldName, World.worldSeed)){
            OpenSelectWorldMenu();
        }
    }

    public void OpenSelectWorldMenu(){
        this.RequestMenuChange(MenuID.SELECT_WORLD);
    }    
}