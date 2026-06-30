using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct StateClipPair{
	public string state;
	public string clip;
	public string direction;
	public float momentum;
	public List<AnimationEventData> events;

	public AnimationClip FetchStateClip(){
		return Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{this.state}");
	}

	public AnimationClip FetchFinalClip(){
		AnimationClip clip = Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{this.clip}");
		AnimationBehaviour currentBehaviour;

		for(int i=0; i < this.events.Count; i++){
			currentBehaviour = AnimationBehaviourDeserializer.Deserialize(this.events[i]);
			currentBehaviour.ApplyToClip(clip, i);
		}

		return clip;
	}

	public override string ToString(){
		return "{" + this.state.ToString() + ": " + this.clip.ToString() + "}";
	}
}