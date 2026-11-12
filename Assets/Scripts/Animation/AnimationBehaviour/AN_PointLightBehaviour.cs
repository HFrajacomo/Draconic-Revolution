using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Creates or destroys the moving voxel light of a Player entity

Voxel lights are owner-authoritative: only the client that owns the player runs this behaviour.
	ifPlayerBuildOnlyFirstPerson = true  -> only the First Person event changes the light locally
	ifPlayerBuildOnlyFirstPerson = false -> both events change the light locally
The Third Person event also sends an ENTITYVOXELLIGHT message, so the Server can track and relay it to other clients.
Animations replayed on other players' models do nothing.
*/

[Serializable]
public class AN_PointLightBehaviour : AnimationBehaviour {
	public float voxelLightIntensity;
	public bool create;
	public bool ifPlayerBuildOnlyFirstPerson;

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(!isPlayer)
			return;

		EntityEffectOperation operation = this.create ? EntityEffectOperation.CREATE : EntityEffectOperation.DESTROY;

		if(this.firstPerson || !this.ifPlayerBuildOnlyFirstPerson){
			EntityID id = new EntityID(EntityType.PLAYER, entityID);

			if(this.create)
				cl.voxelLightHandler.Add(id, animatorParent.transform.position, this.voxelLightIntensity, priority:true);
			else
				cl.voxelLightHandler.Remove(id);
		}

		if(!this.firstPerson){
			NetMessage message = new NetMessage(NetCode.ENTITYVOXELLIGHT);
			message.EntityVoxelLight(operation, this.voxelLightIntensity);
			cl.client.Send(message);
		}
	}
}