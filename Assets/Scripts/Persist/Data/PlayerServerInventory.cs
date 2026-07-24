using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerServerInventory{
    private Dictionary<ulong, List<PlayerServerInventorySlot>> inventories = new Dictionary<ulong, List<PlayerServerInventorySlot>>();
    private InventoryFileHandler inventoryHandler;
    private byte[] buffer = new byte[16000];

    private byte[] emptyInventoryBuffer;
    
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
            this.inventories[playerId] = PlayerServerInventorySlot.BuildInventory(data, 1, ref refVoid);
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
        // If player doesn't exist yet
        else{
            this.inventories.Add(playerId, PlayerServerInventorySlot.BuildInventory(data, 1, ref refVoid));
            this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
        }
    }

    public void AddInventory(ulong playerId, List<PlayerServerInventorySlot> inv){
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
    public void AddInventory(ulong playerId, PlayerServerInventorySlot[] inv){AddInventory(playerId, new List<PlayerServerInventorySlot>(inv));}

    // Manually saves the current inventory without changing anything
    public void SaveInventory(ulong playerId){
        this.inventoryHandler.SaveInventory(playerId, this.inventories[playerId]);
    }

    public int ConvertInventoryToBytes(ulong playerId){
        PlayerServerInventorySlot psis;
        int bytesRead = 0;

        if(this.inventories.ContainsKey(playerId)){
            InventoryType lastType = this.inventories[playerId][0].GetInventoryType();

            this.buffer[bytesRead] = (byte)lastType;
            bytesRead++;

            for(int i=0; i < this.inventories[playerId].Count; i++){
                psis = this.inventories[playerId][i];

                if(psis.GetInventoryType() != lastType){
                    lastType = psis.GetInventoryType();
                    this.buffer[bytesRead] = (byte)lastType;
                    bytesRead++;
                }

                bytesRead += psis.SaveToBuffer(this.buffer, bytesRead);
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
            AddInventory(playerId, GetEmptySlots());
            return InventoryLoader.GetInventorySize(InventoryType.HOTBAR) + InventoryLoader.GetInventorySize(InventoryType.PLAYER) + InventoryLoader.GetInventorySize(InventoryType.EQUIPMENT);
        }
    }

    // Fetches the Item in a slot directly from the buffer data
    public PlayerServerInventorySlot GetSlot(ulong playerCode, byte slot){
        return this.inventories[playerCode][slot];
    }

    public bool HasInventory(ulong playerCode){
        return this.inventories.ContainsKey(playerCode);
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
                this.inventories[playerId][slotId] = new EmptyPlayerInventorySlot(GetInventoryTypeBySlot(slotId));
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

    public byte[] GetEmptyBuffer(int size){
        return CacheEmptyInventory(size);
    }

    public void Destroy(){
        this.inventoryHandler.Close();
    }

    // Returns a pair (index, currentIndexAmount) of the player Inventory that fits the given ItemStack
    // Returns (-1,0) if there's no room in player inventory
    public int2 CheckFits(ulong playerCode, ItemStack its){
        PlayerServerInventorySlot aux;

        for(int i=0; i < this.inventories[playerCode].Count; i++){
            aux = this.inventories[playerCode][i];
            if(aux.GetItemID() == (int)its.GetID() || aux.GetItemID() == 0){
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

    // CHANGE THIS ONCE INVENTORY ITEMS ARE IMPLEMENTED
    private InventoryType GetInventoryTypeBySlot(byte slotId){
        if(slotId < InventoryLoader.GetInventorySize(InventoryType.HOTBAR))
            return InventoryType.HOTBAR;
        else if(slotId < InventoryLoader.GetInventorySize(InventoryType.HOTBAR) + InventoryLoader.GetInventorySize(InventoryType.PLAYER))
            return InventoryType.PLAYER;
        return InventoryType.EQUIPMENT;
    }

    private List<PlayerServerInventorySlot> GetEmptySlots(){
        List<PlayerServerInventorySlot> slots = new List<PlayerServerInventorySlot>();

        for(int i=0; i < InventoryLoader.GetInventorySize(InventoryType.HOTBAR) + InventoryLoader.GetInventorySize(InventoryType.PLAYER) + InventoryLoader.GetInventorySize(InventoryType.EQUIPMENT); i++){
            slots.Add(new EmptyPlayerInventorySlot(GetInventoryTypeBySlot((byte)i)));
        }

        return slots;
    }

    private byte[] CacheEmptyInventory(int size){
        byte[] emptyInventoryBuffer = new byte[size];

        int bytesRead = 0;

        for(int i=0; i < size; i++){
            bytesRead += EmptyPlayerInventorySlot.WriteBlank(emptyInventoryBuffer, bytesRead);
        }

        return emptyInventoryBuffer;
    }
}
