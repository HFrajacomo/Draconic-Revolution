using System;
using UnityEngine;

public static class AnimationBehaviourDeserializer {
	public static AnimationBehaviour Deserialize(AnimationEventData data){
		switch(data.type){
			case "TestPrintBehaviour":
				return JsonUtility.FromJson<TestPrintBehaviour>(data.json);
			case "AnimatorSetBehaviour":
				return JsonUtility.FromJson<AnimatorSetBehaviour>(data.json);
			case "SwitchAttachmentAnchorBehaviour":
				return JsonUtility.FromJson<SwitchAttachmentAnchorBehaviour>(data.json);
			default:
				return JsonUtility.FromJson<TestPrintBehaviour>(data.json);
		}
	}
}