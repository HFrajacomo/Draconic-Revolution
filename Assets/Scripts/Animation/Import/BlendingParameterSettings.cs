using System;
using UnityEngine;
using UnityEditor.Animations;

[Serializable]
public class BlendingParameterSettings {
	public string name;
	public string parameterType;
	private AnimatorControllerParameterType type;

	public AnimatorControllerParameterType GetParameterType(){return this.type;}

	public void PostDeserializationSetup(){
		switch(parameterType){
			case "int":
				this.type = AnimatorControllerParameterType.Int;
				break;
			case "float":
				this.type = AnimatorControllerParameterType.Float;
				break;
			case "bool":
				this.type = AnimatorControllerParameterType.Bool;
				break;
			case "trigger":
				this.type = AnimatorControllerParameterType.Trigger;
				break;
			default:
				this.type = AnimatorControllerParameterType.Float;
				break;
		}
	}
}