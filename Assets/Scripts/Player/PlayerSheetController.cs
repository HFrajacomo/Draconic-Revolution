using System;
using UnityEngine;

public class PlayerSheetController : MonoBehaviour {
	private CharacterSheet sheet;
	private Light characterLight;

	void Start(){
		this.characterLight = this.gameObject.AddComponent<Light>();
		this.characterLight.enabled = false;
	}


	public void SetSheet(CharacterSheet sheet){
		this.sheet = sheet;
	}

	public CharacterSheet GetSheet(){return this.sheet;}
}