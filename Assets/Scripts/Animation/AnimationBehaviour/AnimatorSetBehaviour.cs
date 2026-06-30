using System;
using UnityEngine;

[Serializable]
public class AnimatorSetBehaviour : AnimationBehaviour {
	public string type;
	public string fieldName;

	public bool boolValue;
	public int intValue;
	public float floatValue;

	public override void PostDeserializationSetup(){
		if(this.type != "bool" && this.type != "int" && this.type != "float"){
			Debug.LogError($"AnimatorSetBehaviour has invalid type: {this.type}. Only bool/int/float are supported");
		}
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, ulong entityID, bool isPlayer){
		Animator anim = animatorParent.GetComponent<Animator>();

		switch(this.type){
			case "bool":
				anim.SetBool(this.fieldName, this.boolValue);
				break;
			case "int":
				anim.SetInteger(this.fieldName, this.intValue);
				break;
			case "float":
				anim.SetFloat(this.fieldName, this.floatValue);
				break;
			default:
				break;
		}
	}
}