using System;
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
}