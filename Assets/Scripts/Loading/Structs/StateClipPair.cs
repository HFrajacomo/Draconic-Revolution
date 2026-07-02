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
		if(this.clip == "")
			return null;

		AnimationClip animationClip = Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{this.clip}");
		AnimationBehaviour currentBehaviour;

		if(animationClip == null){
			throw new AnimationImportException($"[StateClipPair] Failed to load clip: {this.clip}");
		}

		if(animationClip.events.Length == 0 && this.events.Count > 0){
			for(int i=0; i < this.events.Count; i++){
				currentBehaviour = AnimationBehaviourDeserializer.Deserialize(this.events[i]);
				currentBehaviour.PostDeserializationSetup();
				currentBehaviour.ApplyToClip(animationClip, i);
			}
		}

		return animationClip;
	}

	public override string ToString(){
		return "{" + this.state.ToString() + ": " + this.clip.ToString() + "}";
	}
}