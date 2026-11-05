using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Creates or destroys point light in a given bone attachment object
*/

[Serializable]
public class AN_PointLightBehaviour : AnimationBehaviour {
	public float voxelLightIntensity;
	public bool create;
	public bool ifPlayerBuildOnlyFirstPerson;

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if((this.ifPlayerBuildOnlyFirstPerson && this.firstPerson) || !this.ifPlayerBuildOnlyFirstPerson || !isPlayer){
			EntityID id = new EntityID(EntityType.PLAYER, entityID);

			if(this.create){
				cl.voxelLightHandler.Add(id, animatorParent.transform.position, this.voxelLightIntensity, priority:isPlayer);
			}
			else{
				cl.voxelLightHandler.Remove(id);
			}
		}
	}
}