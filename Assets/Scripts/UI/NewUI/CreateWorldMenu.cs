using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        this.createButton.clicked += () => Debug.Log("a");
        this.backButton.clicked += () => Debug.Log("b");
    }


}