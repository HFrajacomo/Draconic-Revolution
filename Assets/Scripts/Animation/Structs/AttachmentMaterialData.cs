using System;

/*
Lightweight descriptor of the Materials of an attachment, defined in Item JSON files (--->Attachment section) or as BattleStyle defaults
	Each entry of "materials" maps to the renderer material slot of the same index
	No Material or Texture is created from this data until an attachment that uses it is instantiated
*/
[Serializable]
public class AttachmentMaterialData {
	public AttachmentMaterialEntry[] materials;

	public bool IsEmpty(){return this.materials == null || this.materials.Length == 0;}

	public void PostDeserializationSetup(){
		if(IsEmpty())
			return;

		for(int i=0; i < this.materials.Length; i++){
			this.materials[i].PostDeserializationSetup();
		}
	}
}
