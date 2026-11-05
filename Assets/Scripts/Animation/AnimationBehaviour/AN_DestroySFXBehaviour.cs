using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Destroys an SFX previously created by AN_CreateSFXBehaviour in a Player entity

SFX are owner-authoritative: only the client that owns the player runs this behaviour, removing the SFX locally and
sending an ENTITYSFX message so the Server relays it to other clients. Animations replayed on other players' models do nothing.

Since the local player runs both a First Person and a Third Person animator, only one of them triggers the removal:
	ifPlayerBuildOnlyFirstPerson = true  -> the First Person event is used
	ifPlayerBuildOnlyFirstPerson = false -> the Third Person event is used

This behaviour only supports Players so far. 
Animation and movement mechanics must be added to AI before leveraging this class for AI.
*/

[Serializable]
public class AN_DestroySFXBehaviour : AnimationBehaviour {
	public string sfxName;
	public bool ifPlayerBuildOnlyFirstPerson;

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(!isPlayer)
			return;
		if(this.ifPlayerBuildOnlyFirstPerson != this.firstPerson)
			return;

		cl.sfx.RemoveEntitySFX(new EntityID(EntityType.PLAYER, entityID), this.sfxName);

		NetMessage message = new NetMessage(NetCode.ENTITYSFX);
		message.EntitySFX(entityID, EntitySFXOperation.DESTROY, new EntitySFXData(this.sfxName));
		cl.client.Send(message);
	}
}
