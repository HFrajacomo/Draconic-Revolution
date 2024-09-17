using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerServerInventory{
    public static readonly int playerInventorySize = 45;
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

    // Fetches the Item in a slot directly from the buffer data
    public Item GetSlot(ulong playerCode, byte slot){
        return ItemLoader.GetItem((ushort)this.inventories[playerCode][slot].GetItemID());
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
            if(quantity == 0)
                this.inventories[playerId][slotId] = new EmptyPlayerInventorySlot();
            else
                this.inventories[playerId][slotId].SetQuantity(quantity);
        }
    }

    public byte GetQuantity(ulong playerId, byte slotId){
        return (byte)(this.inventories[playerId][slotId].GetQuantity());
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

    // Returns a pair (index, currentIndexAmount) of the player Inventory that fits the given ItemStack
    // Returns (-1,0) if there's no room in player inventory
    public int2 CheckFits(ulong playerCode, ItemStack its){
        PlayerServerInventorySlot aux;

        for(int i=0; i < playerInventorySize; i++){
            aux = this.inventories[playerCode][i];
            if(aux.GetItemID() == (int)its.GetID()){
                if(its.GetStacksize() != aux.GetQuantity()){
                    return new int2(i, aux.GetQuantity());
                }
            }
            if(aux.GetItemID() == -1){
                return new int2(i, 0);
            }
        }

        return new int2(-1, 0);
    }

    public void CreateSlotAt(byte slotIndex, ulong playerCode, PlayerServerInventorySlot slot){
        this.inventories[playerCode][slotIndex] = slot;
    }
}
