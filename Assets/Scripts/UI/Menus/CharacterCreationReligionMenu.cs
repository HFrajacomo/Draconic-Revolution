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

	[Header("Objects")]
	public Text descriptionText;


	private GameObject selectedPrimaryToggle;
	private GameObject selectedSecondaryToggle;
	
	private SkillType? selectedAlignment = null; 

	private bool INIT = false;

	private Dictionary<string, string> religion_to_description = new Dictionary<string, string>();
	private Dictionary<string, string> alignment_to_description = new Dictionary<string, string>();

	private static readonly string DESCRIPTION_DIR = "Text/CharacterCreationDescriptions/";
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
		{"Cronology", Religion.CRONOLOGY},
		{"Existology", Religion.EXISTOLOGY},
		{"Tenebrocism", Religion.TENEBROCISM},
		{"No Religion", Religion.NATURALISM},
		{"No Religion", Religion.EIHLISM},
		{"No Religion", Religion.VARNACISM},
		{"No Religion", Religion.NECROCISM},
		{"No Religion", Religion.HEREGISM},
		{"No Religion", Religion.CAOLITISM},
		{"No Religion", Religion.PATRONISM},
		{"No Religion", Religion.LIBRETISM},
		{"No Religion", Religion.MONOCENTRISM},
		{"No Religion", Religion.INQUISITION},
		{"No Religion", Religion.REALISM},
		{"No Religion", Religion.SATANISM},
		{"No Religion", Religion.FRATERNISM},
		{"No Religion", Religion.PROFECISM},
		{"No Religion", Religion.CONTIGISM},
		{"No Religion", Religion.ACTOISM},
		{"No Religion", Religion.METALARMISM}
	};


	void Start(){
		Init();
	}


	public void HoverIn(GameObject go){
		string key = go.GetComponentInChildren<Text>().text;

		key = key.Replace("\n", "");

		this.descriptionText.text = this.religion_to_description[key];
	}

	public void HoverOut(){
		this.descriptionText.text = "";
	}

	private void Init(){
		if(!INIT){
		
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

}
