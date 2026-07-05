using System;
using UnityEngine;

[Serializable]
public class AttachmentData {
	public string fbxName;
	public string type;
	public Vector3 offset = Vector3.zero;
	public Vector3 rotation = Vector3.zero;
	private BoneAnchorType baType;
	private bool firstPerson = false;


	public void PostDeserializationSetup(bool firstPerson){
		if(Enum.TryParse<BoneAnchorType>(this.type, false, out BoneAnchorType parsed))
			this.baType = parsed;
		else
			Debug.LogError($"Unknown BoneAnchorType: {type}");

		this.firstPerson = firstPerson;
	}

	public BoneAnchorType GetAnchorType(){return this.baType;}
}