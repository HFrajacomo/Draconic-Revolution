using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Creates a VFX in a BattleStyle attachment of a Player entity

VFX are owner-authoritative: only the client that owns the player runs this behaviour.
	ifPlayerBuildOnlyFirstPerson = true  -> only the First Person event builds the VFX locally
	ifPlayerBuildOnlyFirstPerson = false -> both events build the VFX locally
The Third Person event also sends an ENTITYVFX message, so the Server can track and relay it to other clients,
which build it in their Third Person model. Animations replayed on other players' models do nothing.
*/

[Serializable]
public class AN_CreateVFXBehaviour : AnimationBehaviour {
	public string vfxGameObjectName;
	public string boneAnchor;
	public float offsetX, offsetY, offsetZ;
	public float rotationX, rotationY, rotationZ;
	public string vfxPath;
	public bool ifPlayerBuildOnlyFirstPerson;

	private EntityVFXData vfxData;

	public override void PostDeserializationSetup(){
		this.vfxData = new EntityVFXData(this.vfxGameObjectName, Resolve(this.boneAnchor), this.offsetX, this.offsetY, this.offsetZ, this.rotationX, this.rotationY, this.rotationZ, this.vfxPath);
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(!isPlayer)
			return;

		if(this.firstPerson || !this.ifPlayerBuildOnlyFirstPerson){
			GameObject visualEffect = this.vfxData.Build(animationHandler.GetAttachment(this.vfxData.anchor, this.firstPerson), this.firstPerson);

			if(visualEffect != null)
				cl.vfx.Add(entityID, visualEffect);
		}

		if(!this.firstPerson){
			NetMessage message = new NetMessage(NetCode.ENTITYVFX);
			message.EntityVFX(EntityEffectOperation.CREATE, this.vfxData);
			cl.client.Send(message);
		}
	}

	private BoneAnchorType Resolve(string name){
		if(Enum.TryParse<BoneAnchorType>(name, false, out BoneAnchorType parsed))
			return parsed;
		else
			throw new TypeParsingException($"[AN_CreateVFXBehaviour] Failed to load boneAnchor: {name}");
	}
}