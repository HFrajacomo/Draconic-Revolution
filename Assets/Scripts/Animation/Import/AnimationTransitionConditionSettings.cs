using System;
using UnityEditor.Animations;

[Serializable]
public class AnimationTransitionConditionSettings {
	public string mode;
	public float threshold;
	public string parameter;
	private AnimatorConditionMode conditionMode;

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
	}
}