using System;
using UnityEngine;

public static class AnimationBehaviourDeserializer {
	public static AnimationBehaviour Deserialize(AnimationEventData data){
		switch(data.type){
			case "TestPrintBehaviour":
				return JsonUtility.FromJson<TestPrintBehaviour>(JsonFormatter.RemoveComments(data.json));
			case "AnimatorSetBehaviour":
				return JsonUtility.FromJson<AnimatorSetBehaviour>(JsonFormatter.RemoveComments(data.json));
			case "SwitchAttachmentAnchorBehaviour":
				return JsonUtility.FromJson<SwitchAttachmentAnchorBehaviour>(JsonFormatter.RemoveComments(data.json));
			default:
				return JsonUtility.FromJson<TestPrintBehaviour>(JsonFormatter.RemoveComments(data.json));
		}
	}
}