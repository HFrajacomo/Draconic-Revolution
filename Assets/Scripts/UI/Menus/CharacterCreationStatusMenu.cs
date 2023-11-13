using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Animations;

using Random = UnityEngine.Random;
using Object = System.Object;

public class CharacterCreationStatusMenu : Menu{
	[Header("Material")]
	public Material borderMaterial;
	public Material toggleMaterial;

	[Header("Divs")]
	public Image skillsDiv;
	public Image topDiv;
	public Image attributesDiv;
	public Image descriptionDiv;

	[Header("Toggles")]
	public Toggle[] allToggles;

	[Header("Description")]
	public Text descriptionText;


	private GameObject selectedPrimaryToggle;
	private GameObject selectedSecondaryToggle;
	
	private SkillType? selectedPrimary = null;
	private SkillType? selectedSecondary = null;

	private bool INIT = false;

	private static readonly float HORIZONTAL_ADJUSTMENT = 1f;
	private static readonly float BORDER_SIZE_SKILLS = 0.001f;
	private static readonly float BORDER_SIZE_DESC_ATT = 0.002f;
	private static readonly float BORDER_SIZE_TOP = 0.016f;

	private Dictionary<string, string> skill_to_description = new Dictionary<string, string>();

	private static readonly string DESCRIPTION_DIR = "Text/CharacterCreationDescriptions/";
	private static readonly Dictionary<string, SkillType> NAME_TO_SKILLTYPE = new Dictionary<string, SkillType>(){
		{"Alchemy", SkillType.ALCHEMY},
		{"Bloodmancy", SkillType.BLOODMANCY},
		{"Crafting", SkillType.CRAFTING},
		{"Combat", SkillType.COMBAT},
		{"Construction", SkillType.CONSTRUCTION},
		{"Cooking", SkillType.COOKING},
		{"Enchanting", SkillType.ENCHANTING},
		{"Farming", SkillType.FARMING},
		{"Fishing", SkillType.FISHING},
		{"Leadership", SkillType.LEADERSHIP},
		{"Mining", SkillType.MINING},
		{"Mounting", SkillType.MOUNTING},
		{"Musicality", SkillType.MUSICALITY},
		{"Naturalism", SkillType.NATURALISM},
		{"Smithing", SkillType.SMITHING},
		{"Sorcery", SkillType.SORCERY},
		{"Thievery", SkillType.THIEVERY},
		{"Technology", SkillType.TECHNOLOGY},
		{"Thaumaturgy", SkillType.THAUMATURGY},
		{"Transmuting", SkillType.TRANSMUTING},
		{"Witchcraft", SkillType.WITCHCRAFT}
	};


	void Start(){
		Init();
		SetupShaders();
	}


	public void SelectPrimary(GameObject go){
		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			this.selectedPrimary = null;
			this.selectedPrimaryToggle = null;
			return;
		}

		if(this.selectedPrimaryToggle != null)
			this.selectedPrimaryToggle.GetComponent<Toggle>().isOn = false;

		this.selectedPrimaryToggle = go;
		this.selectedPrimary = NAME_TO_SKILLTYPE[go.transform.parent.name];

		if(CheckSameNameParent(go, this.selectedSecondaryToggle)){
			this.selectedSecondaryToggle.GetComponent<Toggle>().isOn = false;
			this.selectedSecondaryToggle = null;
			this.selectedSecondary = null;
		}

	}

	public void SelectSecondary(GameObject go){
		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			this.selectedSecondary = null;
			this.selectedSecondaryToggle = null;
			return;
		}

		if(this.selectedSecondaryToggle != null)
			this.selectedSecondaryToggle.GetComponent<Toggle>().isOn = false;

		this.selectedSecondaryToggle = go;
		this.selectedSecondary = NAME_TO_SKILLTYPE[go.transform.parent.name];

		if(CheckSameNameParent(go, this.selectedPrimaryToggle)){
			this.selectedPrimaryToggle.GetComponent<Toggle>().isOn = false;
			this.selectedPrimaryToggle = null;
			this.selectedPrimary = null;
		}
	}

	public void HoverIn(GameObject go){
		string key = go.GetComponentInChildren<Text>().text;

		key = key.Replace("\n", "");

		this.descriptionText.text = this.skill_to_description[key];
	}

	public void HoverOut(){
		this.descriptionText.text = "";
	}

	private bool CheckSameNameParent(GameObject obj, GameObject tgt){
		if(tgt == null)
			return false;

		return obj.transform.parent.name == tgt.transform.parent.name;
	}

	private void Init(){
		if(!INIT){
			foreach(Toggle t in allToggles){
				t.GetComponent<Image>().material = Instantiate(toggleMaterial);
				t.isOn = false;
				t.GetComponent<ShaderBorderFillToggle>().RefreshToggle(false);
				LoadDescriptions();
			}			
		}

		INIT = true;
	}

	private void SetupShaders(){
		this.skillsDiv.material = Instantiate(this.borderMaterial);
		this.topDiv.material = Instantiate(this.borderMaterial);
		this.attributesDiv.material = Instantiate(this.borderMaterial);
		this.descriptionDiv.material = Instantiate(this.borderMaterial);

		this.skillsDiv.material.SetFloat("_BorderSize", BORDER_SIZE_SKILLS);
		this.skillsDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
		this.topDiv.material.SetFloat("_BorderSize", BORDER_SIZE_TOP);
		this.topDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
		this.descriptionDiv.material.SetFloat("_BorderSize", BORDER_SIZE_DESC_ATT);
		this.descriptionDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
		this.attributesDiv.material.SetFloat("_BorderSize", BORDER_SIZE_DESC_ATT);
		this.attributesDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
	}

	private void LoadDescriptions(){
		if(this.skill_to_description.Count > 0)
			return;

		foreach(string key in NAME_TO_SKILLTYPE.Keys){
			Debug.Log("ADDED: " + key);
			this.skill_to_description.Add(key, Resources.Load<TextAsset>(DESCRIPTION_DIR + key.ToLower()).ToString());
		}
	}
}
