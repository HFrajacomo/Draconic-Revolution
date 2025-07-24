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
	public void Build(Dictionary<string, Motion> clips){
		if(!clips.ContainsKey(this.clipA) || !clips.ContainsKey(this.clipB))
			throw new AnimationImportException($"BlendTreeSettings references clips that may not exist: {this.clipA} and {this.clipB}");

		BlendTree tree = new BlendTree();

		tree.AddChild(clips[this.clipA], this.minThreshold);
		tree.AddChild(clips[this.clipB], this.maxThreshold);
		tree.blendParameter = this.blendParameter.name;
		tree.blendType = BlendTreeType.Simple1D;
	}

	public void PostDeserializationSetup(){
		this.blendParameter.PostDeserializationSetup();
	}
}