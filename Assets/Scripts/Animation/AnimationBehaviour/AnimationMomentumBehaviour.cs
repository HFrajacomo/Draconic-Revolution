using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This behaviour only supports Client Players so far. 
Animation and movement mechanics must be added to AI before this leveraging this class for AI.
*/

[Serializable]
public class AnimationMomentumBehaviour : AnimationBehaviour {
	public string direction;
	public float momentum;

	private static readonly HashSet<string> VALID_DIRECTIONS = new HashSet<string>{"directional", "forward", "backward"};

	public override void PostDeserializationSetup(){
		if(!VALID_DIRECTIONS.Contains(this.direction)){
			Debug.LogError($"AnimationMomentumBehaviour has invalid direction: {this.direction}. Only 'directional/forward/backward' are supported so far");
		}
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(isPlayer){
			switch(this.direction){
				case "directional":
					cl.playerMovement.AddKnockback(cl.playerMovement.GetForwardDirection(), momentum);
					break;
				case "forward":
					cl.playerMovement.AddKnockback(cl.playerMovement.GetLookDirection(), momentum);
					break;
				case "backward":
					cl.playerMovement.AddKnockback(-cl.playerMovement.GetLookDirection(), momentum);
					break;
				default:
					break;
			}
		}
	}
}