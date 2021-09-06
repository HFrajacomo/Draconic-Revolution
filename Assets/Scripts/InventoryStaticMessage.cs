public static class InventoryStaticMessage
{
	public static Inventory playerInventory;
	public static Inventory specialInventory;

	public static void SetPlayerInventory(Inventory inv){InventoryStaticMessage.playerInventory = inv;}
	public static void SetInventory(Inventory inv){InventoryStaticMessage.specialInventory = inv;}
	public static Inventory GetInventory(){return InventoryStaticMessage.specialInventory;}
}
