using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

[Serializable]
public abstract class AnimationBehaviour {
	public float triggerTime;
	private static Dictionary<string, List<AnimationBehaviour>> behaviours;

	public void ApplyToClip(AnimationClip clip, int index){
		if(behaviours == null)
			behaviours = new Dictionary<string, List<AnimationBehaviour>>();

		if(!behaviours.ContainsKey(clip.name))
			behaviours.Add(clip.name, new List<AnimationBehaviour>());

        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = this.triggerTime;
        animEvent.stringParameter = clip.name;
        animEvent.functionName = "DispatchAnimationBehaviour";
        animEvent.intParameter = index;
        clip.AddEvent(animEvent);

        behaviours[clip.name].Add(this);
	}

	public static List<AnimationBehaviour> Get(string clipName){return behaviours[clipName];}

	public abstract void Run(ChunkLoader cl, GameObject animatorParent, ulong entityID, bool isPlayer);
}