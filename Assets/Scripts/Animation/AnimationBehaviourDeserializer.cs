using System;
using UnityEngine;

public static class AnimationBehaviourDeserializer {
	public static AnimationBehaviour Deserialize(AnimationEventData data, bool firstPerson){
		switch(data.type){
			case "AN_TestPrintBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<AN_TestPrintBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			case "AN_AnimatorSetBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<AN_AnimatorSetBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			case "AN_SwitchAttachmentAnchorBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<AN_SwitchAttachmentAnchorBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			case "AN_AnimationMomentumBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<AN_AnimationMomentumBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			default:
				return SetAnimatorType(JsonUtility.FromJson<AN_TestPrintBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
		}
	}

	private static AnimationBehaviour SetAnimatorType(AnimationBehaviour beh, bool firstPerson){
		beh.SetFirstPerson(firstPerson);
		return beh;
	}
}