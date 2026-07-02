using System;
using UnityEngine;

[Serializable]
public class AttachmentData {
	public string fbxName;
	public string type;
	public Vector3 offset = Vector3.zero;
	public Vector3 rotation = Vector3.zero;
	private BoneAnchorType baType;

	public void PostDeserializationSetup(){
		if(Enum.TryParse<BoneAnchorType>(this.type, false, out BoneAnchorType parsed))
			this.baType = parsed;
		else
			Debug.LogError($"Unknown BoneAnchorType: {type}");
	}

	public BoneAnchorType GetAnchorType(){return this.baType;}
}