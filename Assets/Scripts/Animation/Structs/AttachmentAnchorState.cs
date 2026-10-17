using System.Collections.Generic;

/*
Current placement of a BattleStyle attachment that was moved away from its default anchor.
Attachments are identified by their default BoneAnchorType in the BattleStyle, used as key in the state dictionaries.
*/
public struct AttachmentAnchorState{
	public BoneAnchorType anchor;
	public bool flip;
	public float heightOffset;

	public AttachmentAnchorState(BoneAnchorType anchor, bool flip, float heightOffset){
		this.anchor = anchor;
		this.flip = flip;
		this.heightOffset = heightOffset;
	}

	/*
	Applies an attachment switch from origin to target into the states dictionary (defaultAnchor -> current state)
		Only attachments away from their default anchor are kept
		Returns false if the switch should be ignored
	*/
	public static bool Apply(Dictionary<BoneAnchorType, AttachmentAnchorState> states, BoneAnchorType origin, BoneAnchorType target, bool flip, float heightOffset){
		// SwitchAttachments would destroy the attachment itself
		if(origin == target)
			return false;

		BoneAnchorType key = origin;
		BoneAnchorType? occupantKey = null;

		foreach(KeyValuePair<BoneAnchorType, AttachmentAnchorState> item in states){
			if(item.Value.anchor == origin)
				key = item.Key;
			else if(item.Value.anchor == target)
				occupantKey = item.Key;
		}

		// SwitchAttachments destroys any attachment occupying the target anchor
		if(occupantKey != null && (BoneAnchorType)occupantKey != key)
			states.Remove((BoneAnchorType)occupantKey);

		if(target == key)
			states.Remove(key);
		else
			states[key] = new AttachmentAnchorState(target, flip, heightOffset);

		return true;
	}
}
