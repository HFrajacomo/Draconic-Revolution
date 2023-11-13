using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Animations;

using Random = UnityEngine.Random;
using Object = System.Object;

public class CharacterCreationStatusMenu : Menu{
	private GameObject selectedPrimaryToggle;
	private GameObject selectedSecondaryToggle;
	
	private SkillType? selectedPrimary = null;
	private SkillType? selectedSecondary = null;

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
		{"Loeadership", SkillType.LEADERSHIP},
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

	public void SelectPrimary(GameObject go){
		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			this.selectedPrimary = null;
			this.selectedPrimaryToggle = null;
			return;
		}

		this.selectedPrimaryToggle = go;
		this.selectedPrimary = NAME_TO_SKILLTYPE[go.GetComponentInParent<Text>().text];
	}

	public void SelectSecondary(GameObject go){
		bool current = go.GetComponent<Toggle>().isOn;

		// If has been toggled off
		if(!current){
			this.selectedSecondary = null;
			this.selectedSecondaryToggle = null;
			return;
		}

		this.selectedSecondaryToggle = go;
		this.selectedSecondary = NAME_TO_SKILLTYPE[go.GetComponentInParent<Text>().text];
	}


}
