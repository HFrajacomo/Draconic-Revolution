using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This behaviour only supports Players so far. 
Animation and movement mechanics must be added to AI before this leveraging this class for AI.
*/

[Serializable]
public class AN_DestroyVFXBehaviour : AnimationBehaviour {
	public string vfxGameObjectName;
	public bool ifPlayerBuildOnlyFirstPerson;

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if((this.ifPlayerBuildOnlyFirstPerson && this.firstPerson) || !this.ifPlayerBuildOnlyFirstPerson || !isPlayer){
			cl.vfx.Remove(entityID, this.vfxGameObjectName);
		}
	}
}