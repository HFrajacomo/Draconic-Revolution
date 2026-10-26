using System;
using UnityEngine;

[Serializable]
public class AttachmentData {
	public string fbxName;
	public string type;
	public bool flipModel = false;
	public float heightOffset = 0f;
	public string weaponType;
	public AttachmentMaterialData defaultMaterial;
	private BoneAnchorType baType;
	private bool firstPerson = false;
	private bool hasWeaponType = false;
	private WeaponType wType;


	public void PostDeserializationSetup(bool firstPerson, bool isClient){
		if(Enum.TryParse<BoneAnchorType>(this.type, false, out BoneAnchorType parsed))
			this.baType = parsed;
		else
			Debug.LogError($"Unknown BoneAnchorType: {type}");

		if(!string.IsNullOrEmpty(this.weaponType)){
			if(Enum.TryParse<WeaponType>(this.weaponType, false, out WeaponType parsedWeapon)){
				this.wType = parsedWeapon;
				this.hasWeaponType = true;
			}
			else{
				throw new DeserializationErrorException($"[AttachmentData] Attachment {this.fbxName} has unknown weaponType: {this.weaponType}");
			}
		}

		// Material data is only used for rendering
		if(!isClient || (this.defaultMaterial != null && this.defaultMaterial.IsEmpty()))
			this.defaultMaterial = null;
		else if(this.defaultMaterial != null)
			this.defaultMaterial.PostDeserializationSetup();

		this.firstPerson = firstPerson;
	}

	public BoneAnchorType GetAnchorType(){return this.baType;}
	public bool HasWeaponType(){return this.hasWeaponType;}
	public WeaponType GetWeaponType(){return this.wType;}
	public AttachmentMaterialData GetDefaultMaterial(){return this.defaultMaterial;}
}