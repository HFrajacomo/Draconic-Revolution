using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;

[Serializable]
public class AnimationControllerSettings {
	public string controllerName;
	public string armatureName;
	public string fbxFile;
	public string layersFile;
	public string statesFile;
	public string transitionFile;
	public string[] animations;
	private HashSet<string> animationSet;


	public bool Contains(string animation){return this.animationSet.Contains(animation);}

	public void PostDeserializationSetup(){
		this.animationSet = new HashSet<string>();

		for(int i=0; i < this.animations.Length; i++){
			this.animationSet.Add($"{this.armatureName}|{this.animations[i]}");
		}
	}
}