using System;
using UnityEngine;

public class PlayerModelHandler : MonoBehaviour {
	public ChunkLoader cl;
	public GameObject parent;
	private CharacterController controller;
	private AnimationHandler animationHandler;
	private PlayerActionController playerActionController;
	private bool isMale;
	private bool INIT = false;

	[Header("Materials")]
	public Material plainClothingMaterial;
	public Material dragonSkinMaterial;
	public Material faceMaterial;
	public Material dragonHornMaterial;

	private CharacterBuilder characterBuilder;


	public void Awake(){
		this.animationHandler = this.parent.AddComponent<AnimationHandler>();
		this.playerActionController = this.parent.GetComponent<PlayerActionController>();

		this.controller = this.parent.GetComponent<CharacterController>();

		if(this.controller == null){
			this.controller = this.parent.AddComponent<CharacterController>();
		}
	}

	public void DeleteModel(GameObject parent){
		if(this.parent == null)
			return;
		Transform t = parent.transform.Find("TP-Rig");

		if(t == null){
			return;
		}

		GameObject.DestroyImmediate(t.gameObject);
	}

	// Builds any character other than Player
	public GameObject BuildModel(GameObject go, CharacterAppearance app, bool isMale, ulong entityID){
		CharacterBuilder builder;
		AnimationHandler anim;

		builder = new CharacterBuilder(go, AnimationLoader.GetController("BASE_Character"), AnimationLoader.GetController("BASE_Character_FP"), app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.faceMaterial, isMale, false);

		builder.Build();
		Rescale(app.race, go);

		if(INIT){
			anim = go.GetComponent<AnimationHandler>();
		}
		else{
			anim = go.AddComponent<AnimationHandler>();			
		}

		AnimationEventDispatcher dispatcher = builder.GetThirdPersonAnimatorObject().AddComponent<AnimationEventDispatcher>();
		dispatcher.Init(this.cl, anim, entityID);

		anim.Init("BASE_Character", this.characterBuilder, isUserCharacter:false);

		INIT = true;
		return go;
	}

	// Builds player character
	public void BuildModel(CharacterAppearance app, bool isMale, bool isPlayerCharacter){
		this.isMale = isMale;

		if(this.characterBuilder == null){
			this.characterBuilder = new CharacterBuilder(this.parent, AnimationLoader.GetController("BASE_Character"), AnimationLoader.GetController("BASE_Character_FP"), app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.faceMaterial, isMale, isPlayerCharacter);
			this.animationHandler.Init("BASE_Character", this.characterBuilder, isUserCharacter:true);
			this.playerActionController.UseStyle("BASE_Sword");

			this.characterBuilder.Build();
		}
		else{
			this.characterBuilder.ChangeAppearanceAndBuild(app);
		}


		AnimationEventDispatcher dispatcher = this.characterBuilder.GetThirdPersonAnimatorObject().AddComponent<AnimationEventDispatcher>();
		dispatcher.Init(this.cl, this.animationHandler, Configurations.accountID);

		Rescale(app.race, this.parent);

		//this.characterBuilder.StartAnimation();
	}

	public AnimationHandler GetAnimationHandler(){return this.animationHandler;}

	private void Rescale(Race r, GameObject go){
		switch(r){
			case Race.DWARF:
				go.transform.localScale = RaceManager.GetSettings(Race.DWARF).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
			case Race.HALFLING:
				go.transform.localScale = RaceManager.GetSettings(Race.HALFLING).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
			default:
				go.transform.localScale = RaceManager.GetSettings(Race.HUMAN).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
		}
	}
}