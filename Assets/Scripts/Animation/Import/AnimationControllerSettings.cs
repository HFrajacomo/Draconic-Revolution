using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

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


	public bool Contains(string animation){
		if(SelectBeforePipe(animation) != this.armatureName)
			return false;

		return this.animationSet.Contains(SelectAfterPipe(animation));
	}

	public void PostDeserializationSetup(){
		this.animationSet = new HashSet<string>();

		for(int i=0; i < this.animations.Length; i++){
			this.animationSet.Add(this.animations[i]);
		}
	}

	private string SelectBeforePipe(string input){
	    int index = input.IndexOf('|');

	    if(index >= 0)
	    	return input.Substring(0, index);
	    return input;
	}

	private string SelectAfterPipe(string input){
	    int index = input.IndexOf('|');

	    if(index >= 0)
	    	return input.Substring(index + 1);
	    return input;
	}
}