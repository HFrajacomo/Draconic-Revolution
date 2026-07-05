using System;
using UnityEngine;

[Serializable]
public class SwitchAttachmentAnchorBehaviour : AnimationBehaviour {
	public string originAnchor;
	public string targetAnchor;
	
	private BoneAnchorType origin;
	private BoneAnchorType target;

	public override void PostDeserializationSetup(){
		this.origin = Resolve(originAnchor);
		this.target = Resolve(targetAnchor);
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		animationHandler.SwitchAttachments(this.firstPerson, this.origin, this.target);
	}

	public override string ToString(){return $"[SwitchAttachmentAnchor] {this.origin} -> {this.target}";}

	private BoneAnchorType Resolve(string name){
		if(Enum.TryParse<BoneAnchorType>(name, false, out BoneAnchorType parsed))
			return parsed;
		else
			Debug.LogError($"Unknown BoneAnchorType: {name}");

		return BoneAnchorType.BASE_CHARACTER_BELT_CURVED_RIGHT;
	}
}