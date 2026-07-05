using System;
using UnityEngine;

public static class AnimationBehaviourDeserializer {
	public static AnimationBehaviour Deserialize(AnimationEventData data, bool firstPerson){
		switch(data.type){
			case "TestPrintBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<TestPrintBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			case "AnimatorSetBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<AnimatorSetBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			case "SwitchAttachmentAnchorBehaviour":
				return SetAnimatorType(JsonUtility.FromJson<SwitchAttachmentAnchorBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
			default:
				return SetAnimatorType(JsonUtility.FromJson<TestPrintBehaviour>(JsonFormatter.RemoveComments(data.json)), firstPerson);
		}
	}

	private static AnimationBehaviour SetAnimatorType(AnimationBehaviour beh, bool firstPerson){
		beh.SetFirstPerson(firstPerson);
		return beh;
	}
}