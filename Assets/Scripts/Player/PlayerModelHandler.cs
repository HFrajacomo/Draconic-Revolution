using System;
using UnityEngine;

public class PlayerModelHandler : MonoBehaviour {
	public GameObject parent;

	[Header("Animations")]
	public RuntimeAnimatorController maleAnimations;
	public RuntimeAnimatorController femaleAnimations;

	[Header("Materials")]
	public Material plainClothingMaterial;
	public Material dragonSkinMaterial;

	private CharacterBuilder characterBuilder;

	public void Start(){
		this.parent.AddComponent<Animator>();
	}

	public void BuildModel(CharacterAppearance app, bool isMale){
		if(this.characterBuilder == null){
			if(isMale)
				this.characterBuilder = new CharacterBuilder(this.parent, this.maleAnimations, app, this.plainClothingMaterial, this.dragonSkinMaterial, isMale);
			else
				this.characterBuilder = new CharacterBuilder(this.parent, this.femaleAnimations, app, this.plainClothingMaterial, this.dragonSkinMaterial, isMale);

			this.characterBuilder.Build();			
		}
		else{
			this.characterBuilder.ChangeAppearaceAndBuild(app);
		}
	}
}