using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerServerInventory{
    public static int playerInventorySize = 45;
    private Dictionary<ulong, PlayerServerInventorySlot[]> inventories = new Dictionary<ulong, PlayerServerInventorySlot[]>();
    private InventoryFileHandler inventoryHandler;
    private byte[] buffer = new byte[16000];

    private PlayerServerInventorySlot[] emptyInventory;
    private byte[] emptyInventoryBuffer;
    
    public PlayerServerInventory(){
        this.inventoryHandler = new InventoryFileHandler();

        this.emptyInventoryBuffer = new byte[playerInventorySize];
        this.emptyInventory = new PlayerServerInventorySlot[playerInventorySize];
        CacheEmptyInventory();
    }

    /*
    Adds and updates inventories of players
    Used whenever a slot changes type (goes empty or picks something)
    */
    public void AddInventory(ulong playerId, byte[] data){
        int refVoid = 0;

        // If it's a pre-existing inventory in RAM that is being changed
        if(this.inventories.ContainsKey(playerId)){
            this.inventories[playerId] = PlayerServerInventorySlot.BuildInventory(data, 1, playerInventorySize, ref refVoid);
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
        // If player doesn't exist yet
        else{
            this.inventories.Add(playerId, PlayerServerInventorySlot.BuildInventory(data, 1, playerInventorySize, ref refVoid));
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
    }

    public void AddInventory(ulong playerId, PlayerServerInventorySlot[] inv){
        // If it's a pre-existing inventory in RAM that is being changed
        if(this.inventories.ContainsKey(playerId)){
            this.inventories[playerId] = inv;
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
        // If player doesn't exist yet
        else{
            this.inventories.Add(playerId, inv);
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
    }

    public int ConvertInventoryToBytes(ulong playerId){
        int bytesRead = 0;

        if(this.inventories.ContainsKey(playerId)){
            for(int i=0; i < playerInventorySize; i++){
                bytesRead += this.inventories[playerId][i].SaveToBuffer(this.buffer, bytesRead);
            }
        }

        return bytesRead;
    }

    // Loads player inventory from memory if it exists or creates and saves a new empty inventory
    // (out bool) exists to tell the user whether to use the normal buffer or the empty one
    public int LoadInventoryIntoBuffer(ulong playerId, out bool isEmpty){
        // If exists in RAM
        if(this.inventories.ContainsKey(playerId)){
            isEmpty = false;
            return ConvertInventoryToBytes(playerId);
        }
        // If exists in Disk
        else if(this.inventoryHandler.IsIndexed(playerId)){
            isEmpty = false;
            this.inventories.Add(playerId, this.inventoryHandler.LoadInventory(playerId));
            return ConvertInventoryToBytes(playerId);
        }
        // If is a new player
        else{
            isEmpty = true;
            AddInventory(playerId, this.emptyInventory);
            return playerInventorySize;
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

    public byte[] GetBuffer(){
        return this.buffer;
    }

    public byte[] GetEmptyBuffer(){
        return this.emptyInventoryBuffer;
    }

    public void Destroy(){
        this.inventoryHandler.Close();
    }

    private int CacheEmptyInventory(){
        EmptyPlayerInventorySlot empty = new EmptyPlayerInventorySlot();
        int bytesRead = 0;

        for(int i=0; i < PlayerServerInventory.playerInventorySize; i++){
            this.emptyInventory[i] = new EmptyPlayerInventorySlot();
            bytesRead += empty.SaveToBuffer(this.emptyInventoryBuffer, bytesRead);
        }

        return bytesRead;
    }
}
