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

		this.pointsPool.text = CharacterCreationData.GetRemainingPoints();
		this.strengthField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.STRENGTH), CharacterCreationData.GetAttributeNoBonus(AttributeName.STRENGTH));
		this.precisionField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.PRECISION), CharacterCreationData.GetAttributeNoBonus(AttributeName.PRECISION));
		this.vitalityField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.VITALITY), CharacterCreationData.GetAttributeNoBonus(AttributeName.VITALITY));
		this.evasionField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.EVASION), CharacterCreationData.GetAttributeNoBonus(AttributeName.EVASION));
		this.magicField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.MAGIC), CharacterCreationData.GetAttributeNoBonus(AttributeName.MAGIC));
		this.charismaField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.CHARISMA), CharacterCreationData.GetAttributeNoBonus(AttributeName.CHARISMA));
		this.fireResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.FIRE_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.FIRE_RESISTANCE));
		this.iceResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.ICE_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.ICE_RESISTANCE));
		this.lightningResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.LIGHTNING_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.LIGHTNING_RESISTANCE));
		this.poisonResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.POISON_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.POISON_RESISTANCE));
		this.curseResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.CURSE_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.CURSE_RESISTANCE));
		this.speedField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.SPEED), CharacterCreationData.GetAttributeNoBonus(AttributeName.SPEED));

		this.pointsPool.GetComponentInChildren<Text>().Rebuild(CanvasUpdate.Layout);
		ResetInputField(this.pointsPool);
		this.strengthField.UpdateGeometry();
		this.precisionField.UpdateGeometry();
		this.vitalityField.UpdateGeometry();
		this.evasionField.UpdateGeometry();
		this.magicField.UpdateGeometry();
		this.charismaField.UpdateGeometry();
		this.fireResField.UpdateGeometry();
		this.iceResField.UpdateGeometry();
		this.lightningResField.UpdateGeometry();
		this.poisonResField.UpdateGeometry();
		this.curseResField.UpdateGeometry();
		this.speedField.UpdateGeometry();
	}


	public void SelectPrimary(GameObject go){
		if(!INIT)
			return;

		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			AddToSkill((SkillType)this.selectedPrimary, multiplier:-2);

			this.selectedPrimary = null;
			this.selectedPrimaryToggle = null;
			return;
		}

		if(this.selectedPrimaryToggle != null)
			this.selectedPrimaryToggle.GetComponent<Toggle>().isOn = false;

		this.selectedPrimaryToggle = go;
		this.selectedPrimary = NAME_TO_SKILLTYPE[go.transform.parent.name];
		AddToSkill((SkillType)this.selectedPrimary, multiplier:2);

		if(CheckSameNameParent(go, this.selectedSecondaryToggle)){
			this.selectedSecondaryToggle.GetComponent<Toggle>().isOn = false;
			this.selectedSecondaryToggle = null;
			this.selectedSecondary = null;
		}

	}

	public void SelectSecondary(GameObject go){
		if(!INIT)
			return;

		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			AddToSkill((SkillType)this.selectedSecondary, multiplier:-1);
			this.selectedSecondary = null;
			this.selectedSecondaryToggle = null;
			return;
		}

		if(this.selectedSecondaryToggle != null)
			this.selectedSecondaryToggle.GetComponent<Toggle>().isOn = false;

		this.selectedSecondaryToggle = go;
		this.selectedSecondary = NAME_TO_SKILLTYPE[go.transform.parent.name];
		AddToSkill((SkillType)this.selectedSecondary, multiplier:1);

		if(CheckSameNameParent(go, this.selectedPrimaryToggle)){
			this.selectedPrimaryToggle.GetComponent<Toggle>().isOn = false;
			this.selectedPrimaryToggle = null;
			this.selectedPrimary = null;
		}
	}

	public void AddToExtra(string att){
		if(Convert.ToInt32(this.pointsPool.text.Split("/")[0]) == 0)
			return;

		AttributeName attribute = NAME_TO_ATTRIBUTE[att];
		CharacterCreationData.AddAttribute(attribute, 2, 1);
	}

	public void SubToExtra(string att){
		AttributeName attribute = NAME_TO_ATTRIBUTE[att];

		if(GetField(attribute).IsAtBase())
			return;

		CharacterCreationData.AddAttribute(attribute, 2, -1);
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
		SetupDataValues();

		this.RequestMenuChange(MenuID.CHARACTER_CREATION);
	}

	public void OpenCharacterCreationReligionMenu(){
		if(SetupDataValues())
			this.RequestMenuChange(MenuID.CHARACTER_CREATION_RELIGION);
	}

	public void Reset(){
		if(this.selectedPrimaryToggle != null)
			this.selectedPrimaryToggle.GetComponent<Toggle>().isOn = false;
		if(this.selectedSecondaryToggle != null)
			this.selectedSecondaryToggle.GetComponent<Toggle>().isOn = false;

		this.selectedPrimaryToggle = null;
		this.selectedSecondaryToggle = null;
		this.selectedPrimary = null;
		this.selectedSecondary = null;

		CharacterCreationData.ResetAttributes();
	}

	private bool SetupDataValues(){
		bool returnCode = true;

		CharacterCreationData.SetRemainingPoints(this.pointsPool.text);

		if(this.selectedPrimary != null){
			CharacterCreationData.SetPrimarySkill((SkillType)this.selectedPrimary);
	        CharacterCreationData.SetAttribute(AttributeName.STRENGTH, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.STRENGTH) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.PRECISION, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.PRECISION) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.VITALITY, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.VITALITY) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.EVASION, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.EVASION) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.MAGIC, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.MAGIC) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.CHARISMA, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.CHARISMA) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.SPEED, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.SPEED) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.FIRE_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.FIRE_RESISTANCE) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.ICE_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.ICE_RESISTANCE) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.LIGHTNING_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.LIGHTNING_RESISTANCE) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.POISON_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.POISON_RESISTANCE) * 2));
	        CharacterCreationData.SetAttribute(AttributeName.CURSE_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedPrimary, AttributeName.CURSE_RESISTANCE) * 2));
		}
		else
			returnCode = false;

		if(this.selectedSecondary != null){
			CharacterCreationData.SetSecondarySkill((SkillType)this.selectedSecondary);
	        CharacterCreationData.AddAttribute(AttributeName.STRENGTH, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.STRENGTH));
	        CharacterCreationData.AddAttribute(AttributeName.PRECISION, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.PRECISION));
	        CharacterCreationData.AddAttribute(AttributeName.VITALITY, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.VITALITY));
	        CharacterCreationData.AddAttribute(AttributeName.EVASION, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.EVASION));
	        CharacterCreationData.AddAttribute(AttributeName.MAGIC, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.MAGIC));
	        CharacterCreationData.AddAttribute(AttributeName.CHARISMA, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.CHARISMA));
	        CharacterCreationData.AddAttribute(AttributeName.SPEED, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.SPEED));
	        CharacterCreationData.AddAttribute(AttributeName.FIRE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.FIRE_RESISTANCE));
	        CharacterCreationData.AddAttribute(AttributeName.ICE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.ICE_RESISTANCE));
	        CharacterCreationData.AddAttribute(AttributeName.LIGHTNING_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.LIGHTNING_RESISTANCE));
	        CharacterCreationData.AddAttribute(AttributeName.POISON_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.POISON_RESISTANCE));
	        CharacterCreationData.AddAttribute(AttributeName.CURSE_RESISTANCE, 1, AttributeIncreaseTable.GetAttributeIncrease((SkillType)this.selectedSecondary, AttributeName.CURSE_RESISTANCE));
    	}
    	else
    		returnCode = false;

    	if(this.pointsPool.text != "0/10")
    		returnCode = false;

        CharacterCreationData.SetAttribute(AttributeName.STRENGTH, 2, this.strengthField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.PRECISION, 2, this.precisionField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.VITALITY, 2, this.vitalityField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.EVASION, 2, this.evasionField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.MAGIC, 2, this.magicField.GetExtra());
        CharacterCreationData.SetAttribute(AttributeName.CHARISMA, 2, this.charismaField.GetExtra());


        return returnCode;
	}

	private bool CheckSameNameParent(GameObject obj, GameObject tgt){
		if(tgt == null)
			return false;

		return obj.transform.parent.name == tgt.transform.parent.name;
	}

	private void ResetInputField(InputField ipf){
		string txt = ipf.text;

		ipf.textComponent.text = "";
		ipf.textComponent.text = txt;
	}

	private void Init(){
		if(!INIT){
			foreach(Toggle t in allToggles){
				t.GetComponent<Image>().material = Instantiate(toggleMaterial);
				t.isOn = false;
				t.GetComponent<ShaderBorderFillToggle>().RefreshToggle(false);
			}		
			LoadDescriptions();
		}

		Reset();

		INIT = true;
	}

	private AttributeInputField GetField(AttributeName att){
		switch(att){
			case AttributeName.STRENGTH:
				return this.strengthField;
			case AttributeName.PRECISION:
				return this.precisionField;
			case AttributeName.VITALITY:
				return this.vitalityField;
			case AttributeName.EVASION:
				return this.evasionField;
			case AttributeName.MAGIC:
				return this.magicField;
			case AttributeName.CHARISMA:
				return this.charismaField;
			default:
				return this.strengthField;
		}
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

	private void AddToSkill(SkillType skill, int multiplier=1){
		CharacterCreationData.AddAttribute(AttributeName.STRENGTH, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.STRENGTH) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.PRECISION, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.PRECISION) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.VITALITY, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.VITALITY) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.EVASION, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.EVASION) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.MAGIC, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.MAGIC) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.CHARISMA, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.CHARISMA) * multiplier));
	
		CharacterCreationData.AddAttribute(AttributeName.FIRE_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.FIRE_RESISTANCE) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.ICE_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.ICE_RESISTANCE) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.LIGHTNING_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.LIGHTNING_RESISTANCE) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.POISON_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.POISON_RESISTANCE) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.CURSE_RESISTANCE, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.CURSE_RESISTANCE) * multiplier));
		CharacterCreationData.AddAttribute(AttributeName.SPEED, 1, (short)(AttributeIncreaseTable.GetAttributeIncrease(skill, AttributeName.SPEED) * multiplier));
	
		this.strengthField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.STRENGTH), CharacterCreationData.GetAttributeNoBonus(AttributeName.STRENGTH));
		this.precisionField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.PRECISION), CharacterCreationData.GetAttributeNoBonus(AttributeName.PRECISION));
		this.vitalityField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.VITALITY), CharacterCreationData.GetAttributeNoBonus(AttributeName.VITALITY));
		this.evasionField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.EVASION), CharacterCreationData.GetAttributeNoBonus(AttributeName.EVASION));
		this.magicField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.MAGIC), CharacterCreationData.GetAttributeNoBonus(AttributeName.MAGIC));
		this.charismaField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.CHARISMA), CharacterCreationData.GetAttributeNoBonus(AttributeName.CHARISMA));
		this.speedField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.SPEED), CharacterCreationData.GetAttributeNoBonus(AttributeName.SPEED));
		this.fireResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.FIRE_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.FIRE_RESISTANCE));
		this.iceResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.ICE_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.ICE_RESISTANCE));
		this.lightningResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.LIGHTNING_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.LIGHTNING_RESISTANCE));
		this.poisonResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.POISON_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.POISON_RESISTANCE));
		this.curseResField.SetFieldValue(CharacterCreationData.GetAttribute(AttributeName.CURSE_RESISTANCE), CharacterCreationData.GetAttributeNoBonus(AttributeName.CURSE_RESISTANCE));
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
