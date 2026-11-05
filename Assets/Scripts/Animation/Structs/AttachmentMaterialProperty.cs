using System;
using UnityEngine;

/*
A single Material property override applied through a MaterialPropertyBlock on an attachment
*/
[Serializable]
public class AttachmentMaterialProperty {
	public string name;
	public string type;
	public float floatValue;
	public Color colorValue;
	public string textureValue;

	private AttachmentMaterialPropertyType propertyType;
	private int propertyID;

	public void PostDeserializationSetup(){
		if(string.IsNullOrEmpty(this.name))
			throw new DeserializationErrorException("[AttachmentMaterialProperty] Property has no name");

		if(!Enum.TryParse<AttachmentMaterialPropertyType>(this.type, true, out AttachmentMaterialPropertyType parsed))
			throw new DeserializationErrorException($"[AttachmentMaterialProperty] Property {this.name} has unknown type: {this.type}");

		this.propertyType = parsed;

		if(this.propertyType == AttachmentMaterialPropertyType.TEXTURE && string.IsNullOrEmpty(this.textureValue))
			throw new DeserializationErrorException($"[AttachmentMaterialProperty] Texture2D property {this.name} has no textureValue");

		this.propertyID = Shader.PropertyToID(this.name);
	}

	public AttachmentMaterialPropertyType GetPropertyType(){return this.propertyType;}
	public int GetPropertyID(){return this.propertyID;}
}

public enum AttachmentMaterialPropertyType : byte {
	FLOAT,
	COLOR,
	TEXTURE
}
