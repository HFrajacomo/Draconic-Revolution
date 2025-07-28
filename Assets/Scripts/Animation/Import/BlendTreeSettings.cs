#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[Serializable]
public class BlendTreeSettings {
	public string clipA;
	public string clipB;
	public float minThreshold;
	public float maxThreshold;
	public BlendingParameterSettings blendParameter;

	// Requires a dict of <string, Motion> to be created by the AnimationLoader first
	public BlendTree Build(string blendTreeName, Dictionary<string, Motion> clips){
		if(!clips.ContainsKey(this.clipA) || !clips.ContainsKey(this.clipB))
			throw new AnimationImportException($"BlendTreeSettings references clips that may not exist: {this.clipA} and {this.clipB}");

		BlendTree tree = new BlendTree();

		tree.name = blendTreeName;
		tree.useAutomaticThresholds = false;
		tree.minThreshold = this.minThreshold;
		tree.maxThreshold = this.maxThreshold;
		tree.AddChild(clips[this.clipA], this.minThreshold);
		tree.AddChild(clips[this.clipB], this.maxThreshold);
		tree.blendParameter = this.blendParameter.parameterName;
		tree.blendType = BlendTreeType.Simple1D;

		return tree;
	}

	public void PostDeserializationSetup(){
		this.blendParameter.PostDeserializationSetup();
	}
}
#endif