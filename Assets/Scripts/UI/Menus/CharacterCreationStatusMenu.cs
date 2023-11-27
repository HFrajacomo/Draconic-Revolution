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

	[Header("Attribute Input Fields")]
	public AttributeInputField strengthField;
	public AttributeInputField precisionField;
	public AttributeInputField vitalityField;
	public AttributeInputField evasionField;
	public AttributeInputField magicField;
	public AttributeInputField charismaField;
	public AttributeInputField fireResField;
	public AttributeInputField iceResField;
	public AttributeInputField lightningResField;
	public AttributeInputField poisonResField;
	public AttributeInputField curseResField;
	public AttributeInputField speedField;

	[Header("Description")]
	public Text descriptionText;
	public InputField pointsPool;


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
	private static readonly Dictionary<string, AttributeName> NAME_TO_ATTRIBUTE = new Dictionary<string, AttributeName>(){
		{"Strength", AttributeName.STRENGTH},
		{"Precision", AttributeName.PRECISION},
		{"Vitality", AttributeName.VITALITY},
		{"Evasion", AttributeName.EVASION},
		{"Magic", AttributeName.MAGIC},
		{"Charisma", AttributeName.CHARISMA},
		{"Speed", AttributeName.SPEED},
	};
	private static readonly HashSet<string> RESISTANCES = new HashSet<string>(){
		"Fire Res.", "Ice Res.", "Lightning Res.", "Poison Res.", "Curse Res."
	};


	void Start(){
		Init();
		SetupShaders();
	}

	public override void Enable(){
		this.mainObject.SetActive(true);

		this.strengthField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.STRENGTH));
		this.precisionField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.PRECISION));
		this.vitalityField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.VITALITY));
		this.evasionField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.EVASION));
		this.magicField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.MAGIC));
		this.charismaField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.CHARISMA));
		this.fireResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.FIRE_RESISTANCE));
		this.iceResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.ICE_RESISTANCE));
		this.lightningResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.LIGHTNING_RESISTANCE));
		this.poisonResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.POISON_RESISTANCE));
		this.curseResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.CURSE_RESISTANCE));
		this.speedField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.SPEED));
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

	public void OpenCharacterCreationMenu(){
		this.RequestMenuChange(MenuID.CHARACTER_CREATION);
	}

	public void OpenCharacterCreationReligionMenu(){
		if(this.selectedPrimary == null || this.selectedSecondary == null || this.pointsPool.text != "0/10")
			return;

		CharacterCreationData.SetPrimarySkill((SkillType)this.selectedPrimary);
		CharacterCreationData.SetSecondarySkill((SkillType)this.selectedSecondary);

        CharacterCreationData.SetAttribute(AttributeName.STRENGTH, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.STRENGTH));
        CharacterCreationData.AddAttribute(AttributeName.STRENGTH, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.STRENGTH));
        CharacterCreationData.SetAttribute(AttributeName.PRECISION, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.PRECISION));
        CharacterCreationData.AddAttribute(AttributeName.PRECISION, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.PRECISION));
        CharacterCreationData.SetAttribute(AttributeName.VITALITY, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.VITALITY));
        CharacterCreationData.AddAttribute(AttributeName.VITALITY, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.VITALITY));
        CharacterCreationData.SetAttribute(AttributeName.EVASION, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.EVASION));
        CharacterCreationData.AddAttribute(AttributeName.EVASION, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.EVASION));
        CharacterCreationData.SetAttribute(AttributeName.MAGIC, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.MAGIC));
        CharacterCreationData.AddAttribute(AttributeName.MAGIC, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.MAGIC));
        CharacterCreationData.SetAttribute(AttributeName.CHARISMA, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.CHARISMA));
        CharacterCreationData.AddAttribute(AttributeName.CHARISMA, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.CHARISMA));
        CharacterCreationData.SetAttribute(AttributeName.SPEED, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.SPEED));
        CharacterCreationData.AddAttribute(AttributeName.SPEED, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.SPEED));

        CharacterCreationData.SetAttribute(AttributeName.FIRE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.FIRE_RESISTANCE));
        CharacterCreationData.AddAttribute(AttributeName.FIRE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.FIRE_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.ICE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.ICE_RESISTANCE));
        CharacterCreationData.AddAttribute(AttributeName.ICE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.ICE_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.LIGHTNING_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.LIGHTNING_RESISTANCE));
        CharacterCreationData.AddAttribute(AttributeName.LIGHTNING_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.LIGHTNING_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.POISON_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.POISON_RESISTANCE));
        CharacterCreationData.AddAttribute(AttributeName.POISON_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.POISON_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.CURSE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.CURSE_RESISTANCE));
        CharacterCreationData.AddAttribute(AttributeName.CURSE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.CURSE_RESISTANCE));

        CharacterCreationData.SetAttribute(AttributeName.STRENGTH, 2, this.strengthField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.PRECISION, 2, this.precisionField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.VITALITY, 2, this.vitalityField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.EVASION, 2, this.evasionField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.MAGIC, 2, this.magicField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.CHARISMA, 2, this.charismaField.GetExtra());

		this.RequestMenuChange(MenuID.CHARACTER_CREATION_RELIGION);
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
			this.skill_to_description.Add(key, Resources.Load<TextAsset>(DESCRIPTION_DIR + key.ToLower()).ToString());
		}

		foreach(string key in NAME_TO_ATTRIBUTE.Keys){
			this.skill_to_description.Add(key, Resources.Load<TextAsset>(DESCRIPTION_DIR + key.ToLower()).ToString());
		}

		foreach(string key in RESISTANCES){
			this.skill_to_description.Add(key, Resources.Load<TextAsset>(DESCRIPTION_DIR + "resistance").ToString());			
		}
	}

}
