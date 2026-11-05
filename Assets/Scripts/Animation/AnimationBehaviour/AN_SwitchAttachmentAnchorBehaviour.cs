using System;
using UnityEngine;

/*
Moves a BattleStyle attachment from one bone anchor to another

Attachment anchors are owner-authoritative: only the client that owns the player runs this behaviour.
Both the First Person and Third Person events switch their own attachments locally, and the Third Person event also
sends a SENDATTACHMENTANCHOR message so the Server can track and relay it to other clients.
Animations replayed on other players' models do nothing.
*/

[Serializable]
public class AN_SwitchAttachmentAnchorBehaviour : AnimationBehaviour {
	public string originAnchor;
	public string targetAnchor;
	public bool flip = false;
	public float heightOffset = 0;
	
	private BoneAnchorType origin;
	private BoneAnchorType target;

	public override void PostDeserializationSetup(){
		this.origin = Resolve(originAnchor);
		this.target = Resolve(targetAnchor);
	}

	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		if(!isPlayer)
			return;

		animationHandler.SwitchAttachments(this.firstPerson, this.origin, this.target, this.heightOffset, this.flip);

		if(!this.firstPerson && this.origin != this.target){
			NetMessage message = new NetMessage(NetCode.SENDATTACHMENTANCHOR);
			message.SendAttachmentAnchor(entityID, this.origin, this.target, this.flip, this.heightOffset);
			cl.client.Send(message);
		}
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