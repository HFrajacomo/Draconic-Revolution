#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

[Serializable]
public class AnimationStateSettings {
	public string name;
	public string clip;
	public string layer;
	public bool isBlendTree;
	public bool isDefaultState;
	public BlendTreeSettings blendTree;

	public AnimatorState Build(AnimatorController animatorController, Dictionary<string, Motion> animations){
		AnimatorState state = new AnimatorState();

		state.name = this.name;

		if(this.isBlendTree){
			BlendTree blendTree = this.blendTree.Build(this.name, animations);
			state.motion = blendTree;
			AssetDatabase.AddObjectToAsset(blendTree, animatorController);

			// Adding BlendTree Parameter
			AnimatorControllerParameter acp = this.blendTree.blendParameter.Build();

			if(!CheckControllerHasParameter(acp.name, animatorController)){
				animatorController.AddParameter(acp);
			}
		}
		else if(clip != ""){
			state.motion = animations[clip];
		}

		return state;
	}

	public void PostDeserializationSetup(){
		if(this.layer == "")
			this.layer = "Base Layer";

		if(this.isBlendTree)
			this.blendTree.PostDeserializationSetup();
	}

	private bool CheckControllerHasParameter(string name, AnimatorController controller){
		for(int i=0; i < controller.parameters.Length; i++){
			if(controller.parameters[i].name == name){
				return true;
			}
		}
		return false;
	}
}

#endif