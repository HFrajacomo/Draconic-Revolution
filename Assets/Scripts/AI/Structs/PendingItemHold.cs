/*
Item OnHoldClient of a remote player waiting for that player's Sheathe animation to finish.
Prevents the Item's Hold effects from resolving before the Sheathe animation events (e.g. the weapon's light being removed).
*/
public struct PendingItemHold{
	public ushort id;
	public byte quantity;
	public float elapsed;
	public bool sawSheathe;

	public PendingItemHold(ushort id, byte quantity){
		this.id = id;
		this.quantity = quantity;
		this.elapsed = 0f;
		this.sawSheathe = false;
	}
}
