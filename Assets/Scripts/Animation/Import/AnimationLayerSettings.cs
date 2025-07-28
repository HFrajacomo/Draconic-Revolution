#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;


[Serializable]
public class AnimationLayerSettings {
	public string name;
	public float defaultWeight;
	public string[] avatarMask;
	public int blendingMode;
	private AnimatorLayerBlendingMode layerBlending;


	public AnimatorControllerLayer Build(){
		AnimatorControllerLayer layer = new AnimatorControllerLayer();

		layer.name = this.name;
		layer.defaultWeight = this.defaultWeight;
		layer.blendingMode = this.layerBlending;
		layer.stateMachine = new AnimatorStateMachine();
		layer.stateMachine.name = this.name;
		layer.avatarMask = new AvatarMask();
		layer.avatarMask.name = $"{this.name}_Mask";

		layer.avatarMask.transformCount = this.avatarMask.Length;

		for(int i=0; i < this.avatarMask.Length; i++){
			layer.avatarMask.SetTransformPath(i, this.avatarMask[i]);
			layer.avatarMask.SetTransformActive(i, true);
		}

		return layer;
	}

	public void PostDeserializationSetup(){
		if(blendingMode == 0)
			this.layerBlending = AnimatorLayerBlendingMode.Override;
		else
			this.layerBlending = AnimatorLayerBlendingMode.Additive;
	}
}
#endif