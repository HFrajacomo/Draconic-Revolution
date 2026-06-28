using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class BoneAnchorPoint {
	public string type;
	public string bonePath;
	private BoneAnchorType baType;

	public BoneAnchorType GetAnchorType(){return this.baType;}

	public void PostDeserializationSetup(){
		if(Enum.TryParse<BoneAnchorType>(this.type, false, out BoneAnchorType parsed))
			this.baType = parsed;
		else
			Debug.LogError($"Unknown BoneAnchorType: {type}");
	}

	public override string ToString(){return $"{this.baType}: {this.bonePath}";}
}