public static class InventoryStaticMessage {
	public static Inventory playerInventory;
	public static Inventory hotbarInventory;
	public static Inventory equipmentInventory;

	public static void SetPlayerInventory(Inventory inv){InventoryStaticMessage.playerInventory = inv;}
	public static void SetHotbarInventory(Inventory inv){InventoryStaticMessage.hotbarInventory = inv;}
	public static void SetEquipmentInventory(Inventory inv){InventoryStaticMessage.equipmentInventory = inv;}

	public static Inventory GetInventory(){return InventoryStaticMessage.hotbarInventory;}
}
