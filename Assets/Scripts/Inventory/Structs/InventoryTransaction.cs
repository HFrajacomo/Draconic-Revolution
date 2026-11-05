using System;

public struct InventoryTransaction{
	public ushort slotNumber;
	public byte amount;

	public InventoryTransaction(ushort slot, byte x){
		this.slotNumber = slot;
		this.amount = x;
	}

	public override string ToString(){
		return "Slot: " + this.slotNumber.ToString() + " | Amount: " + this.amount.ToString();
	}
}