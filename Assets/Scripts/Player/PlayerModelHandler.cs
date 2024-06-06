using System;
using UnityEngine;

public class PlayerModelHandler : MonoBehaviour {
	public GameObject parent;
	private Animator animator;
	private CharacterController controller;

	[Header("Animations")]
	public RuntimeAnimatorController maleAnimations;
	public RuntimeAnimatorController femaleAnimations;

	[Header("Materials")]
	public Material plainClothingMaterial;
	public Material dragonSkinMaterial;

	private CharacterBuilder characterBuilder;


	public void Awake(){
		this.animator = this.parent.AddComponent<Animator>();

		this.controller = this.parent.GetComponent<CharacterController>();

		if(this.controller == null){
			// Will need to configure it for new entities later
			this.controller = this.parent.AddComponent<CharacterController>();
		}
	}

	public void ChangeParent(GameObject go){
		this.parent = go;
		this.animator = this.parent.AddComponent<Animator>();
		this.controller = this.parent.AddComponent<CharacterController>();
	}

	public void BuildModel(CharacterAppearance app, bool isMale, bool isPlayerCharacter){
		if(this.characterBuilder == null){
			if(isMale)
				this.characterBuilder = new CharacterBuilder(this.parent, this.maleAnimations, app, this.plainClothingMaterial, this.dragonSkinMaterial, isMale, isPlayerCharacter);
			else
				this.characterBuilder = new CharacterBuilder(this.parent, this.femaleAnimations, app, this.plainClothingMaterial, this.dragonSkinMaterial, isMale, isPlayerCharacter);

			this.characterBuilder.Build();
		}
		else{
			this.characterBuilder.ChangeAppearaceAndBuild(app);
		}

		Rescale(app.race);
	}

	private void Rescale(Race r){
		switch(r){
			case Race.DWARF:
				this.parent.transform.localScale = RaceManager.GetSettings(Race.DWARF).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
			case Race.HALFLING:
				this.parent.transform.localScale = RaceManager.GetSettings(Race.HALFLING).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
			default:
				this.parent.transform.localScale = RaceManager.GetSettings(Race.HUMAN).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
		}
	}
}