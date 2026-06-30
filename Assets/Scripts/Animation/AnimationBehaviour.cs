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
        animEvent.stringParameter = SerializeDispatchParameter(clip.name, index);
        animEvent.functionName = "DispatchAnimationBehaviour";
        clip.AddEvent(animEvent);

        behaviours[clip.name].Add(this);
	}

	public static List<AnimationBehaviour> Get(string clipName){return behaviours[clipName];}

	public virtual void PostDeserializationSetup(){return;}

	public abstract void Run(ChunkLoader cl, GameObject animatorParent, ulong entityID, bool isPlayer);

	protected string SerializeDispatchParameter(string name, int index){return $"{{\"clip\": \"{name}\", \"index\": {index}}}";}
}