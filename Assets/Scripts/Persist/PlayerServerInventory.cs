using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerServerInventory{
    public static int playerInventorySize = 45;
    private Dictionary<ulong, PlayerServerInventorySlot[]> inventories = new Dictionary<ulong, PlayerServerInventorySlot[]>();
    private InventoryFileHandler inventoryHandler;
    
    public PlayerServerInventory(){
        this.inventoryHandler = new InventoryFileHandler();
    }

    /*
    Adds and updates inventories of players
    Used whenever a slot changes type (goes empty or picks something)
    */
    public void AddInventory(ulong playerId, byte[] data){
        int refVoid = 0;

        // If it's a pre-existing inventory in RAM that is being changed
        if(this.inventories.ContainsKey(playerId)){
            this.inventories[playerId] = PlayerServerInventorySlot.BuildInventory(data, 0, playerInventorySize, ref refVoid);
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
        // If player doesn't exist yet
        else{
            this.inventories.Add(playerId, PlayerServerInventorySlot.BuildInventory(data, 0, playerInventorySize, ref refVoid));
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
        
    }

    /*
    Removes inventory from this handler
    */
    public void RemoveInventory(ulong playerId){
        this.inventories.Remove(playerId);
        this.inventoryHandler.UnloadIndex();
    }


    public void ChangeQuantity(ulong playerId, byte slotId, byte quantity){
        if(this.inventories.ContainsKey(playerId)){
            ((ItemPlayerInventorySlot)this.inventories[playerId][slotId]).SetQuantity(quantity);
        }
    }

    public void ChangeDurability(ulong playerId, byte slotId, uint durability){
        if(this.inventories.ContainsKey(playerId)){
            ((WeaponPlayerInventorySlot)this.inventories[playerId][slotId]).SetDurability(durability);
        }        
    }

    public void Destroy(){
        this.inventoryHandler.Close();
    }
}