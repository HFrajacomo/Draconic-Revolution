using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Creates an SFX in a Player entity. Optionally attached to a bone anchor.
Looping SFX persist until AN_DestroySFXBehaviour is run, while non-looping SFX are destroyed once they finish playing.

SFX are owner-authoritative: only the client that owns the player runs this behaviour, playing the SFX locally and
sending an ENTITYSFX message so the Server relays it to other clients. Animations replayed on other players' models do nothing.

Since the local player runs both a First Person and a Third Person animator, only one of them triggers the SFX:
	ifPlayerBuildOnlyFirstPerson = true  -> the First Person event is used
	ifPlayerBuildOnlyFirstPerson = false -> the Third Person event is used
(Differs from AN_CreateVFXBehaviour, where false builds in both, since a sound must not exist twice)

This behaviour only supports Players so far. 
Animation and movement mechanics must be added to AI before leveraging this class for AI.
*/

[Serializable]
public class AN_CreateSFXBehaviour : AnimationBehaviour {
	public string sfxName;
	public string boneAnchor;
	public float offsetX, offsetY, offsetZ;
	public bool ifPlayerBuildOnlyFirstPerson;

	private BoneAnchorType anchor;
	private bool hasAnchor;

	public override void PostDeserializationSetup(){
		if(!AudioLoader.IsSound(this.sfxName))
			throw new TypeParsingException($"[AN_CreateSFXBehaviour] Sound does not exist: {this.sfxName}");

		AudioUsecase usecase = AudioLoader.GetSound(this.sfxName).GetUsecaseType();

		if(usecase != AudioUsecase.SFX_3D && usecase != AudioUsecase.SFX_3D_LOOP)
			throw new TypeParsingException($"[AN_CreateSFXBehaviour] Sound {this.sfxName} is not a SFX_3D or SFX_3D_LOOP");

		this.hasAnchor = !string.IsNullOrEmpty(this.boneAnchor);

		if(this.hasAnchor)
			this.anchor = Resolve(this.boneAnchor);
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(!isPlayer)
			return;
		if(this.ifPlayerBuildOnlyFirstPerson != this.firstPerson)
			return;

		EntityID id = new EntityID(EntityType.PLAYER, entityID);
		Transform parent = null;

		if(this.hasAnchor){
			GameObject attachmentObject = animationHandler.GetAttachment(this.anchor, this.firstPerson);

			if(attachmentObject != null)
				parent = attachmentObject.transform;
		}

		cl.sfx.LoadEntitySFX(this.sfxName, id, parent:parent, localPosition:new Vector3(this.offsetX, this.offsetY, this.offsetZ));

		NetMessage message = new NetMessage(NetCode.ENTITYSFX);
		message.EntitySFX(entityID, EntitySFXOperation.CREATE, new EntitySFXData(this.sfxName, AudioLoader.IsLoop(this.sfxName), this.hasAnchor, this.anchor, this.offsetX, this.offsetY, this.offsetZ));
		cl.client.Send(message);
	}

	private BoneAnchorType Resolve(string name){
		if(Enum.TryParse<BoneAnchorType>(name, false, out BoneAnchorType parsed))
			return parsed;
		else
			throw new TypeParsingException($"[AN_CreateSFXBehaviour] Failed to load boneAnchor: {name}");
	}
}
