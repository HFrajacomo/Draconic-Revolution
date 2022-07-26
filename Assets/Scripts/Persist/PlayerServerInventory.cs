using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerServerInventory{
    public static int playerInventorySize = 30;
    private Dictionary<ulong, PlayerServerInventorySlot[]> inventories = new Dictionary<ulong, PlayerServerInventorySlot[]>();
    private InventoryFileHandler inventoryHandler;
    
    public PlayerServerInventory(){
        this.inventoryHandler = new InventoryFileHandler();
    }

}
