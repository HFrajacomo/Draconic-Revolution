using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Animations;

using Random = UnityEngine.Random;
using Object = System.Object;

public class CharacterCreationReligionMenu : Menu{
	[Header("Material")]
	public Material borderMaterial;
	public Material toggleMaterial;

	[Header("Objects")]
	public Text descriptionText;

	[Header("Divs")]
	public Image religionsDiv;
	public Image topDiv;
	public Image alignmentDiv;
	public Image descriptionDiv;

	[Header("Toggles")]
	public Toggle[] allToggles;
	public Button[] allAlignmentButtons;

	[Header("Exclusion Nodes")]
	public GameObject inquisitionGO;
	public GameObject necrocismGO;
	public Button[] lawfulsGO;
	public Button[] neutralsGO;
	public Button[] chaoticsGO;

	private GameObject selectedReligionToggle;
	private GameObject selectedAlignmentButton;
	
	private Religion? selectedReligion = null;
	private Alignment? selectedAlignment = null;

	private bool INIT = false;
	private ColorBlock colorBlock = new ColorBlock();


	private Dictionary<string, string> religion_to_description = new Dictionary<string, string>();
	private Dictionary<string, string> alignment_to_description = new Dictionary<string, string>();

	private static readonly string DESCRIPTION_DIR = "Text/CharacterCreationReligion/";
	private static readonly Dictionary<string, Alignment> NAME_TO_ALIGNMENT = new Dictionary<string, Alignment>(){
		{"Lawful Good", Alignment.LAWFUL_GOOD},
		{"Lawful Neutral", Alignment.LAWFUL_NEUTRAL},
		{"Lawful Evil", Alignment.LAWFUL_EVIL},
		{"Neutral Good", Alignment.NEUTRAL_GOOD},
		{"True Neutral", Alignment.TRUE_NEUTRAL},
		{"Neutral Evil", Alignment.NEUTRAL_EVIL},
		{"Chaotic Good", Alignment.CHAOTIC_GOOD},
		{"Chaotic Neutral", Alignment.CHAOTIC_NEUTRAL},
		{"Chaotic Evil", Alignment.CHAOTIC_EVIL}
	};

	private static readonly Dictionary<string, Religion> NAME_TO_RELIGION = new Dictionary<string, Religion>(){
		{"No Religion", Religion.NONE},
		{"Tenebrocism", Religion.TENEBROCISM},
		{"Naturalism", Religion.NATURALISM},
		{"Eihlism", Religion.EIHLISM},
		{"Varnacism", Religion.VARNACISM},
		{"Necrocism", Religion.NECROCISM},
		{"Heregism", Religion.HEREGISM},
		{"Caolitism", Religion.CAOLITISM},
		{"Patronism", Religion.PATRONISM},
		{"Libretism", Religion.LIBRETISM},
		{"Monocentrism", Religion.MONOCENTRISM},
		{"Inquisition", Religion.INQUISITION},
		{"Realism", Religion.REALISM},
		{"Satanism", Religion.SATANISM},
		{"Fraternism", Religion.FRATERNISM},
		{"Profecism", Religion.PROFECISM},
		{"Contigism", Religion.CONTIGISM},
		{"Actoism", Religion.ACTOISM},
		{"Metalarmism", Religion.METALARMISM}
	};


	private static readonly float HORIZONTAL_ADJUSTMENT = 1f;
	private static readonly float BORDER_SIZE_SKILLS = 0.001f;
	private static readonly float BORDER_SIZE_DESC_ATT = 0.002f;
	private static readonly float BORDER_SIZE_TOP = 0.016f;
	private static readonly Color BORDER_ENABLED = new Color(.43f, .9f, .68f);

	private static readonly Color BUTTON_DEFAULT_COLOR = new Color(.14f, .35f, .45f);
	private static readonly Color BUTTON_SELECTED_COLOR = new Color(.05f, .05f, .34f);
	private static readonly Color BUTTON_HIGHLIGHTED_COLOR = new Color(.02f, .22f, .42f);
	private static readonly Color BUTTON_DISABLED_COLOR = new Color(.3f, .3f, .3f);
	private static readonly Color DARKER_GREY = new Color(.27f, .27f, .27f);

	void Start(){
		Init();
		SetupShaders();
		LoadDescriptions();
	}

	public void SelectReligion(GameObject go){
		if(!INIT)
			return;

		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			this.selectedReligion = null;
			this.selectedReligionToggle = null;
			SetDisabilities();
			return;
		}

		if(this.selectedReligionToggle != null)
			this.selectedReligionToggle.GetComponent<Toggle>().isOn = false;

		this.selectedReligionToggle = go;
		this.selectedReligion = NAME_TO_RELIGION[go.transform.parent.name];

		SetDisabilities();
	}

	public void SelectAlignment(GameObject go){
		if(!INIT)
			return;

		if(go == this.selectedAlignmentButton){
			this.colorBlock.normalColor = BUTTON_DEFAULT_COLOR;
			this.selectedAlignmentButton.GetComponent<Button>().colors = this.colorBlock;

			this.selectedAlignment = null;
			this.selectedAlignmentButton = null;
			DeselectClickedButton();
			SetDisabilities();
			return;
		}

		if(this.selectedAlignmentButton != null){
			this.colorBlock.normalColor = BUTTON_DEFAULT_COLOR;
			this.selectedAlignmentButton.GetComponent<Button>().colors = this.colorBlock;
		}

		this.selectedAlignmentButton = go;
		this.colorBlock.normalColor = BUTTON_SELECTED_COLOR;
		this.selectedAlignmentButton.GetComponent<Button>().colors = this.colorBlock;
		this.selectedAlignment = NAME_TO_ALIGNMENT[go.transform.name];

		SetDisabilities();
	}

	public void HoverIn(GameObject go){
		string key = go.GetComponentInChildren<Text>().text;

		key = key.Replace("\n", "");

		this.descriptionText.text = this.religion_to_description[key];
	}

	public void HoverOut(){
		this.descriptionText.text = "";
	}

	public void HoverInAlignment(GameObject go){
		string key = go.GetComponentInChildren<Text>().text;

		key = key.Replace("\n", "");

		this.descriptionText.text = this.alignment_to_description[key];		
	}

	public void OpenCharacterCreationDataMenu(){
		if(this.selectedAlignment == null || this.selectedReligion == null)
			return;

		CharacterCreationData.SetAlignment((Alignment)this.selectedAlignment);
		CharacterCreationData.SetReligion((Religion)this.selectedReligion);

		this.RequestMenuChange(MenuID.CHARACTER_CREATION_DATA);
	}


	private void Init(){
		if(!INIT){
			foreach(Toggle t in allToggles){
				t.GetComponent<Image>().material = Instantiate(this.toggleMaterial);
				t.isOn = false;
				t.GetComponent<ShaderBorderFillToggle>().RefreshToggle(false);
			}	
		}

		INIT = true;
	}

	private void LoadDescriptions(){
		if(this.religion_to_description.Count > 0)
			return;

		foreach(string key in NAME_TO_ALIGNMENT.Keys){
			this.alignment_to_description.Add(key, Resources.Load<TextAsset>(DESCRIPTION_DIR + key.ToLower()).ToString());
		}
		foreach(string key in NAME_TO_RELIGION.Keys){
			this.religion_to_description.Add(key, Resources.Load<TextAsset>(DESCRIPTION_DIR + key.ToLower()).ToString());
		}
	}

	private void SetupShaders(){
		this.colorBlock.colorMultiplier = 1f;
		this.colorBlock.normalColor = BUTTON_DEFAULT_COLOR;
		this.colorBlock.selectedColor = BUTTON_SELECTED_COLOR;
		this.colorBlock.highlightedColor = BUTTON_HIGHLIGHTED_COLOR;
		this.colorBlock.disabledColor = BUTTON_DISABLED_COLOR;

		foreach(Button b in allAlignmentButtons){
			b.colors = this.colorBlock;
		}

		this.religionsDiv.material = Instantiate(this.borderMaterial);
		this.topDiv.material = Instantiate(this.borderMaterial);
		this.alignmentDiv.material = Instantiate(this.borderMaterial);
		this.descriptionDiv.material = Instantiate(this.borderMaterial);

		this.religionsDiv.material.SetFloat("_BorderSize", BORDER_SIZE_SKILLS);
		this.religionsDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
		this.topDiv.material.SetFloat("_BorderSize", BORDER_SIZE_TOP);
		this.topDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
		this.descriptionDiv.material.SetFloat("_BorderSize", BORDER_SIZE_DESC_ATT);
		this.descriptionDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
		this.alignmentDiv.material.SetFloat("_BorderSize", BORDER_SIZE_DESC_ATT);
		this.alignmentDiv.material.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT);
	}

	private void SetDisabilities(){
		if(CharacterCreationData.GetRace() != Race.UNDEAD)
			return;

		bool isExclusion = false;

		if(this.selectedAlignment != null){
			if(AlignmentTools.IsChaotic((Alignment)this.selectedAlignment)){
				if(this.selectedReligion != Religion.NECROCISM){
					this.selectedReligion = null;

					if(this.selectedReligionToggle != null)
						this.selectedReligionToggle.GetComponent<Toggle>().isOn = false;

					this.selectedReligionToggle = null;
				}

				isExclusion = true;

				RunToggles(this.necrocismGO);
			}
			else if(AlignmentTools.IsLawful((Alignment)this.selectedAlignment)){
				if(this.selectedReligion != Religion.INQUISITION){
					this.selectedReligion = null;

					if(this.selectedReligionToggle != null)
						this.selectedReligionToggle.GetComponent<Toggle>().isOn = false;

					this.selectedReligionToggle = null;
				}

				isExclusion = true;

				RunToggles(this.inquisitionGO);
			}
		}
		if(this.selectedReligion != null){
			if(this.selectedReligion == Religion.INQUISITION){
				if(this.selectedAlignment != null){
					if(!AlignmentTools.IsLawful((Alignment)this.selectedAlignment)){
						this.selectedAlignment = null;
						this.colorBlock.normalColor = BUTTON_DEFAULT_COLOR;
						this.selectedAlignmentButton.GetComponent<Button>().colors = this.colorBlock;
						this.selectedAlignmentButton = null;					
					}
				}

				isExclusion = true;

				ToggleButtons(this.lawfulsGO, true);
				ToggleButtons(this.neutralsGO, false);
				ToggleButtons(this.chaoticsGO, false);
			}
			else if(this.selectedReligion == Religion.NECROCISM){
				if(this.selectedAlignment != null){
					if(!AlignmentTools.IsChaotic((Alignment)this.selectedAlignment)){
						this.selectedAlignment = null;
						this.colorBlock.normalColor = BUTTON_DEFAULT_COLOR;
						this.selectedAlignmentButton.GetComponent<Button>().colors = this.colorBlock;
						this.selectedAlignmentButton = null;					
					}
				}

				isExclusion = true;

				ToggleButtons(this.chaoticsGO, true);
				ToggleButtons(this.neutralsGO, false);
				ToggleButtons(this.lawfulsGO, false);				
			}
		}

		if(!isExclusion){
			RunToggles(null);

			ToggleButtons(this.neutralsGO, true);
			ToggleButtons(this.lawfulsGO, true);
			ToggleButtons(this.chaoticsGO, true);
		}
	}

	private void ToggleButtons(Button[] array, bool toggle){
		foreach(Button b in array){
			b.interactable = toggle;

			if(toggle)
				b.GetComponentInChildren<Text>().color = Color.white;
			else
				b.GetComponentInChildren<Text>().color = DARKER_GREY;	
		}
	}

	#nullable enable
	private void RunToggles(GameObject? excluded){
		if(excluded == null){
			foreach(Toggle t in allToggles){
				t.interactable = true;
				t.GetComponent<Image>().material.SetColor("_BorderColor", BORDER_ENABLED);
				t.transform.parent.gameObject.GetComponentInChildren<Text>().color = Color.white;
			}
		}
		else{
			foreach(Toggle t in allToggles){
				if(t.transform.parent.name != excluded.name){
					t.interactable = false;
					t.GetComponent<Image>().material.SetColor("_BorderColor", DARKER_GREY);
					t.transform.parent.gameObject.GetComponentInChildren<Text>().color = DARKER_GREY;
				}
			}
		}
	}
	#nullable disable
}
