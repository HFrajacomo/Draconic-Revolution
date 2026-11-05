using System;

/*
Describes the Material applied to a single renderer material slot of an attachment
	material: name of the base Material in Resources/ItemModels/Materials/
	properties: overrides applied through a MaterialPropertyBlock
*/
[Serializable]
public class AttachmentMaterialEntry {
	public string material;
	public AttachmentMaterialProperty[] properties;

	public void PostDeserializationSetup(){
		if(string.IsNullOrEmpty(this.material))
			throw new DeserializationErrorException("[AttachmentMaterialEntry] Material entry has no base material name");

		if(this.properties == null)
			this.properties = new AttachmentMaterialProperty[0];

		for(int i=0; i < this.properties.Length; i++){
			this.properties[i].PostDeserializationSetup();
		}
	}
}
