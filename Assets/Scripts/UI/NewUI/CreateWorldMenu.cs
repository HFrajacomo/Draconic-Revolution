using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using Random = UnityEngine.Random;

public class CreateWorldMenu : Menu{
	// UI Documents
	public StyleSheet style;

    // Visual Elements
    private VisualElement root;

	// Buttons
	private Button createButton;
	private Button backButton;

	// TextFields
	private TextField nameField;
	private TextField seedField;
	private readonly Color TEXT_FIELD_COLOR = new Color(0.31f, 0.31f, 0.31f, 1f);
	private readonly Color TEXT_COLOR = new Color(0.8f, 0.8f, 0.8f, 1f);


    public override void Disable(){
        this.mainDocument.panelSettings = null;
        this.nameField.value = "";
        this.seedField.value = "";
    }

	void Start(){
        this.root = this.mainDocument.rootVisualElement;
        this.root.styleSheets.Add(this.style);

        this.createButton = this.root.Query<Button>("create-button");
        this.backButton = this.root.Query<Button>("back-button");
        this.nameField = this.root.Query<TextField>("world-name-field");
        this.seedField = this.root.Query<TextField>("world-seed-field");

        List<VisualElement> textFieldList = this.root.Query<VisualElement>("unity-text-input").ToList();

        USSPreparer.SetInputFieldColors(textFieldList, TEXT_FIELD_COLOR, TEXT_COLOR);

        USSPreparer.SetTextFieldLimitation(this.nameField, InputFieldLimitation.CHARACTERS_ONLY);
        USSPreparer.SetTextFieldLimitation(this.seedField, InputFieldLimitation.NUMBERS_ONLY);

        InitClickEvents();
	}

    private void InitClickEvents(){
        this.createButton.clicked += () => CreateNewWorld();
        this.backButton.clicked += () => SendMessage("ChangeMenu", MenuID.SELECT_WORLD);
    }


    private void CreateNewWorld(){
        int rn;

        if(this.nameField.value == ""){
            return;
        }

        if(this.seedField.value == ""){
            Random.InitState((int)DateTime.Now.Ticks);
            rn = (int)Random.Range(0, int.MaxValue);
            World.SetWorldSeed(rn.ToString());
        }
        else{
            World.SetWorldSeed(this.seedField.value);
        }

        World.SetWorldName(this.nameField.value);
        
        if(RegionFileHandler.CreateWorldFile(World.worldName, World.worldSeed)){
            SendMessage("ChangeMenu", MenuID.SELECT_WORLD);
        }
    }

}