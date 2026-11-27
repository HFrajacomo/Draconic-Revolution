/*
A change made by a client to a container slot, sent in CONTAINEROP
Before and After are the slot written by InventorySlotCodec. The Server only applies it if the slot is still Before
*/
public struct ContainerOpEntry{
	public ushort slot;
	public byte[] before;
	public byte[] after;

	public ContainerOpEntry(ushort slot, byte[] before, byte[] after){
		this.slot = slot;
		this.before = before;
		this.after = after;
	}
}
