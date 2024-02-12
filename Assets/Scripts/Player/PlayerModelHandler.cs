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

	// Scaling Vectors
	private Vector3 halflingScale = new Vector3(.42f, .42f, .42f);
	private Vector3 dwarfScale = new Vector3(.55f, .55f, .55f);
	private Vector3 basicScale = new Vector3(.6f, .6f, .6f);

	// Character Controller Settings
	private float basicHeight = 1.84f;
	private float dwarfHeight = 1.44f;
	private float halflingHeight = .67f;
	private float basicCenterY = -0.32f;
	private float dwarfCenterY = 0f;
	private float halflingCenterY = 0f;
	private float basicRadius = .35f;
	private float dwarfRadius = .3f;
	private float halflingRadius = .18f;
	private float basicStep = .5f;
	private float dwardStep = .5f;
	private float halflingStep = .26f;


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
		SetControllerSettings(app.race);
	}

	private void Rescale(Race r){
		switch(r){
			case Race.DWARF:
				this.parent.transform.localScale = dwarfScale;
				break;
			case Race.HALFLING:
				this.parent.transform.localScale = halflingScale;
				break;
			default:
				this.parent.transform.localScale = basicScale;
				break;
		}
	}

	private void SetControllerSettings(Race r){
		switch(r){
			case Race.DWARF:
				this.controller.height = dwarfHeight;
				this.controller.center = new Vector3(0f, dwarfCenterY, 0f);
				this.controller.radius = dwarfRadius;
				this.controller.stepOffset = dwardStep;
				break;
			case Race.HALFLING:
				this.controller.height = halflingHeight;
				this.controller.center = new Vector3(0f, halflingCenterY, 0f);
				this.controller.radius = halflingRadius;
				this.controller.stepOffset = halflingStep;
				break;
			default:
				this.controller.height = basicHeight;
				this.controller.center = new Vector3(0f, basicCenterY, 0f);
				this.controller.radius = basicRadius;
				this.controller.stepOffset = basicStep;
				break;
		}
	}
}