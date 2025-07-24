using System;
using UnityEditor.Animations;

[Serializable]
public class AnimationStateSettings {
	public string name;
	public string clip;
	public string layer;
	public bool isBlendTree;
	public bool isDefaultState;
	public BlendTreeSettings blendTree;
}