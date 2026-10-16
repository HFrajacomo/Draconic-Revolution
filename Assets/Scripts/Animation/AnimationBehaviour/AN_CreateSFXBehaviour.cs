using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Creates an SFX in a Player entity. Optionally attached to a bone anchor.
Looping SFX persist until AN_DestroySFXBehaviour is run, while non-looping SFX are destroyed once they finish playing.

This behaviour only supports Players so far. 
Animation and movement mechanics must be added to AI before this leveraging this class for AI.
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
		if((this.ifPlayerBuildOnlyFirstPerson && this.firstPerson) || !this.ifPlayerBuildOnlyFirstPerson || !isPlayer){
			EntityID id = new EntityID(EntityType.PLAYER, entityID);
			Transform parent = null;

			if(this.hasAnchor){
				GameObject attachmentObject = animationHandler.GetAttachment(this.anchor, this.firstPerson);

				if(attachmentObject != null)
					parent = attachmentObject.transform;
			}

			cl.sfx.LoadEntitySFX(this.sfxName, id, parent:parent, localPosition:new Vector3(this.offsetX, this.offsetY, this.offsetZ));
		}
	}

	private BoneAnchorType Resolve(string name){
		if(Enum.TryParse<BoneAnchorType>(name, false, out BoneAnchorType parsed))
			return parsed;
		else
			throw new TypeParsingException($"[AN_CreateSFXBehaviour] Failed to load boneAnchor: {name}");
	}
}
