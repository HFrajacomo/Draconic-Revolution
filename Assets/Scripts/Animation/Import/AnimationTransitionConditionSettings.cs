#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor.Animations;

[Serializable]
public class AnimationTransitionConditionSettings {
	public string mode;
	public float threshold;
	public string parameter;
	public string parameterType;
	private AnimatorConditionMode conditionMode;
	private AnimatorControllerParameterType dataType;

	public AnimatorControllerParameter BuildParameter(){
		return new AnimatorControllerParameter{
			name = this.parameter,
			type = this.dataType
		};
	}

	public AnimatorConditionMode GetMode(){return this.conditionMode;}

	public void PostDeserializationSetup(){
		switch(mode){
			case "if":
				this.conditionMode = AnimatorConditionMode.If;
				break;
			case "if not":
				this.conditionMode = AnimatorConditionMode.IfNot;
				break;
			case "greater":
				this.conditionMode = AnimatorConditionMode.Greater;
				break;
			case "less":
				this.conditionMode = AnimatorConditionMode.Less;
				break;
			case "equals":
				this.conditionMode = AnimatorConditionMode.Equals;
				break;
			case "not equals":
				this.conditionMode = AnimatorConditionMode.NotEqual;
				break;
			default:
				this.conditionMode = AnimatorConditionMode.Equals;
				break;
		}

		switch(parameterType){
			case "int":
				this.dataType = AnimatorControllerParameterType.Int;
				break;
			case "float":
				this.dataType = AnimatorControllerParameterType.Float;
				break;
			case "bool":
				this.dataType = AnimatorControllerParameterType.Bool;
				break;
			case "trigger":
				this.dataType = AnimatorControllerParameterType.Trigger;
				break;
			default:
				this.dataType = AnimatorControllerParameterType.Float;
				break;
		}
	}
}

#endif