/*
Links an instantiated attachment to the AttachmentData it was created from
	Main attachments are skinned by claiming equipped items through their weaponType
	Carry attachments are always skinned by the off-hand item
*/
public struct AttachmentSource {
	public AttachmentData[] attachments;
	public int index;
	public bool isCarry;

	public AttachmentSource(AttachmentData[] attachments, int index, bool isCarry){
		this.attachments = attachments;
		this.index = index;
		this.isCarry = isCarry;
	}

	public AttachmentData GetData(){return this.attachments[this.index];}

	public AttachmentMaterialData ResolveSkin(AttachmentItems items){
		if(this.isCarry)
			return AttachmentLoader.ResolveCarryDescriptor(this.attachments, this.index, items);
		return AttachmentLoader.ResolveDescriptor(this.attachments, this.index, items);
	}
}
