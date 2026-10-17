using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This behaviour only supports Players so far. 
Animation and movement mechanics must be added to AI before this leveraging this class for AI.
*/

[Serializable]
public class AN_CreateVFXBehaviour : AnimationBehaviour {
	public string vfxGameObjectName;
	public string boneAnchor;
	public float offsetX, offsetY, offsetZ;
	public float rotationX, rotationY, rotationZ;
	public string vfxPath;
	public bool ifPlayerBuildOnlyFirstPerson;

	private GameObject vfxPrefab;
	private BoneAnchorType anchor;

	public override void PostDeserializationSetup(){
		this.anchor = Resolve(this.boneAnchor);
		this.vfxPrefab = GameObject.Find(this.vfxPath);
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if((this.ifPlayerBuildOnlyFirstPerson && this.firstPerson) || !this.ifPlayerBuildOnlyFirstPerson || !isPlayer){
			// Prefab lives in the Game scene and is destroyed whenever the scene is reloaded
			if(this.vfxPrefab == null)
				this.vfxPrefab = GameObject.Find(this.vfxPath);

			if(this.vfxPrefab == null){
				Debug.LogError($"[AN_CreateVFXBehaviour] Could not find VFX prefab: {this.vfxPath}");
				return;
			}

			GameObject attachmentObject = animationHandler.GetAttachment(this.anchor, this.firstPerson);

			if(attachmentObject == null)
				return;

			GameObject visualEffect = GameObject.Instantiate(this.vfxPrefab);
			visualEffect.name = this.vfxGameObjectName;
			visualEffect.transform.parent = attachmentObject.transform;
			visualEffect.transform.localPosition = new Vector3(this.offsetX, this.offsetY, this.offsetZ);
			visualEffect.transform.localEulerAngles = new Vector3(this.rotationX, this.rotationY, this.rotationZ);
			visualEffect.transform.localScale = Vector3.one;

			if(this.firstPerson){
				visualEffect.layer = 12;
			}

			cl.vfx.Add(entityID, visualEffect);
		}
	}

	private BoneAnchorType Resolve(string name){
		if(Enum.TryParse<BoneAnchorType>(name, false, out BoneAnchorType parsed))
			return parsed;
		else
			throw new TypeParsingException($"[AN_CreateVFXBehaviour] Failed to load boneAnchor: {name}");
	}
}