using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Destroys a VFX previously created by AN_CreateVFXBehaviour in a Player entity

VFX are owner-authoritative: only the client that owns the player runs this behaviour.
	ifPlayerBuildOnlyFirstPerson = true  -> only the First Person event removes the VFX locally
	ifPlayerBuildOnlyFirstPerson = false -> both events remove the VFX locally
The Third Person event also sends an ENTITYVFX message, so the Server can track and relay it to other clients.
Animations replayed on other players' models do nothing.
*/

[Serializable]
public class AN_DestroyVFXBehaviour : AnimationBehaviour {
	public string vfxGameObjectName;
	public bool ifPlayerBuildOnlyFirstPerson;

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(!isPlayer)
			return;

		if(this.firstPerson || !this.ifPlayerBuildOnlyFirstPerson)
			cl.vfx.Remove(entityID, this.vfxGameObjectName);

		if(!this.firstPerson){
			NetMessage message = new NetMessage(NetCode.ENTITYVFX);
			message.EntityVFX(EntityEffectOperation.DESTROY, new EntityVFXData(this.vfxGameObjectName));
			cl.client.Send(message);
		}
	}
}