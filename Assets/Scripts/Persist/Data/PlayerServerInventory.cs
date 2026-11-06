using System;
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
            byte lastType = this.inventories[playerId][0].GetInventoryType();

            this.buffer[bytesRead] = lastType;
            bytesRead++;

            for(int i=0; i < this.inventories[playerId].Count; i++){
                psis = this.inventories[playerId][i];

                if(psis.GetInventoryType() != lastType){
                    lastType = psis.GetInventoryType();
                    this.buffer[bytesRead] = lastType;
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
        // The freshly created slots are serialized like any other inventory, so the client receives the inventory type headers and the default Action
        else{
            isEmpty = true;
            AddInventory(playerId, GetEmptySlots());
            return ConvertInventoryToBytes(playerId);
        }
    }

    // Fetches the Item in a slot directly from the buffer data
    public PlayerServerInventorySlot GetSlot(ulong playerCode, byte slot){
        return this.inventories[playerCode][slot];
    }

    // Fetches the Item in a slot directly from the buffer data
    public PlayerServerInventorySlot GetSlot(ulong playerCode, int inventoryCode, byte slot){
        int index = GetFlatIndex(playerCode, inventoryCode, slot);

        if(index < 0)
            throw new IndexOutOfRangeException($"[PlayerServerInventory] GetSlot(ushort, int, byte) couldn't find the indicated slot for player {playerCode} in inventory {inventoryCode} and slot {slot}");

        return this.inventories[playerCode][index];
    }

    // Converts an (inventoryCode, slot) pair into the index of the flattened inventory list
    // Returns -1 if it couldn't be found
    private int GetFlatIndex(ulong playerCode, int inventoryCode, byte slot){
        int i=0;
        int typeCounter = 0;
        byte currentInventoryType = this.inventories[playerCode][0].GetInventoryType();

        if(inventoryCode == 0)
            return slot < this.inventories[playerCode].Count ? slot : -1;

        while(i < this.inventories[playerCode].Count){
            if(currentInventoryType != this.inventories[playerCode][i].GetInventoryType()){
                currentInventoryType = this.inventories[playerCode][i].GetInventoryType();
                typeCounter++;

                if(typeCounter == inventoryCode){
                    if(slot + i >= this.inventories[playerCode].Count || this.inventories[playerCode][slot + i].GetInventoryType() != currentInventoryType)
                        return -1;
                    return slot + i;
                }
            }

            i++;
        }

        return -1;
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
            if(quantity == 0){
                this.inventories[playerId][slotId] = new EmptyPlayerInventorySlot(this.inventories[playerId][slotId].GetInventoryType(), this.inventories[playerId][slotId].GetSlotID());
            }
            else{
                this.inventories[playerId][slotId].SetQuantity(quantity);
            }

            SaveInventory(playerId);
        }
    }

    // Changes quantity of a slot given by the client's (inventoryCode, slot) pair
    public void ChangeQuantity(ulong playerId, int inventoryCode, byte slot, byte quantity){
        if(!this.inventories.ContainsKey(playerId))
            return;

        int index = GetFlatIndex(playerId, inventoryCode, slot);

        if(index < 0)
            return;

        if(quantity == 0){
            this.inventories[playerId][index] = new EmptyPlayerInventorySlot(this.inventories[playerId][index].GetInventoryType(), this.inventories[playerId][index].GetSlotID());
        }
        else{
            this.inventories[playerId][index].SetQuantity(quantity);
        }

        SaveInventory(playerId);
    }

    public byte GetQuantity(ulong playerId, byte slotId){
        return (byte)(this.inventories[playerId][slotId].GetQuantity());
    }

    public void ChangeDurability(ulong playerId, byte slotId, uint durability){
        if(this.inventories.ContainsKey(playerId)){
            ((WeaponPlayerInventorySlot)this.inventories[playerId][slotId]).SetDurability(durability);
            SaveInventory(playerId);
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
            if(!InventoryLoader.GetPickupTarget(aux.GetInventoryType()))
                continue;

            if(!aux.IsInGlobalWhitelist(its))
                continue;

            if(!aux.IsInLocalWhitelist(its))
                continue;        

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
        SaveInventory(playerCode);
    }
    
    private List<PlayerServerInventorySlot> GetEmptySlots(){
        List<PlayerServerInventorySlot> slots = new List<PlayerServerInventorySlot>();

        slots.Add(new ActionInventorySlot(ActionLoader.GetID("BASE_EquipmentAttack"), false, 0, 0, 0, 0, InventoryLoader.GetActionInventoryID("ACTION_HOTBAR"), 0));
        for(byte i=1; i < InventoryLoader.GetInventorySize("ACTION_HOTBAR"); i++){
            slots.Add(new EmptyPlayerInventorySlot(InventoryLoader.GetActionInventoryID("ACTION_HOTBAR"), i));
        }
        for(byte i=0; i < InventoryLoader.GetInventorySize("HOTBAR"); i++){
            slots.Add(new EmptyPlayerInventorySlot(InventoryLoader.GetInventoryID("HOTBAR"), i));
        }
        for(byte i=0; i < InventoryLoader.GetInventorySize("PLAYER"); i++){
            slots.Add(new EmptyPlayerInventorySlot(InventoryLoader.GetInventoryID("PLAYER"), i));
        }
        for(byte i=0; i < InventoryLoader.GetInventorySize("EQUIPMENT"); i++){
            slots.Add(new EmptyPlayerInventorySlot(InventoryLoader.GetInventoryID("EQUIPMENT"), i));
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
