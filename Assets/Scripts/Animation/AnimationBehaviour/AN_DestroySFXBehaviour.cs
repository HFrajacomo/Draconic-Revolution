using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Destroys an SFX previously created by AN_CreateSFXBehaviour in a Player entity

This behaviour only supports Players so far. 
Animation and movement mechanics must be added to AI before leveraging this class for AI.
*/

[Serializable]
public class AN_DestroySFXBehaviour : AnimationBehaviour {
	public string sfxName;
	public bool ifPlayerBuildOnlyFirstPerson;

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if((this.ifPlayerBuildOnlyFirstPerson && this.firstPerson) || !this.ifPlayerBuildOnlyFirstPerson || !isPlayer){
			cl.sfx.RemoveEntitySFX(new EntityID(EntityType.PLAYER, entityID), this.sfxName);
		}
	}
}
