using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InventorySerializer
{
    public static byte[] buffer = new byte[30000];


    // Creates Inventory and Hotbar given a byte[] received from server
    // out are the output inventories
    public static void BuildPlayerInventory(byte[] data, int init, out Inventory hotbar, out Inventory inv){
        int bytesRead = 0;
        int currentSlot = 0;
        MemoryStorageType mst;
        ushort id;
        byte quantity;
        uint currentDur;
        byte refineLv;
        EnchantmentType enchant;
        ItemStack its;
        Item item;
        Weapon weapon;
        //ushort inventorySize;

        hotbar = new Inventory(InventoryType.HOTBAR);
        inv = new Inventory(InventoryType.PLAYER);

        while(currentSlot < hotbar.GetLimit()){
            mst = (MemoryStorageType)data[init+bytesRead];
            bytesRead++;

            switch(mst){
                case MemoryStorageType.EMPTY:
                    break;
                case MemoryStorageType.ITEM:
                    id = NetDecoder.ReadUshort(data, init+bytesRead);
                    bytesRead += 2;
                    quantity = data[init+bytesRead];
                    bytesRead++;
                    item = ItemLoader.GetCopy(id);
                    its = new ItemStack(item, quantity);
                    AddToInventory(its, hotbar, inv, currentSlot);
                    break;
                case MemoryStorageType.WEAPON:
                    id = NetDecoder.ReadUshort(data, init+bytesRead);
                    bytesRead += 2;
                    currentDur = NetDecoder.ReadUint(data, init+bytesRead);
                    bytesRead += 4;
                    refineLv = data[init+bytesRead];
                    bytesRead++;
                    enchant = (EnchantmentType)data[init+bytesRead];
                    weapon = (Weapon)ItemLoader.GetCopy(id);;
                    weapon.SetDurability(currentDur);
                    weapon.SetExtraEffects(enchant);
                    weapon.SetRefineLevel(refineLv);
                    its = new ItemStack(weapon, 1);
                    AddToInventory(its, hotbar, inv, currentSlot);
                    break;
                case MemoryStorageType.STORAGE:
                    //id = NetDecoder.ReadUshort(data, init+bytesRead);
                    //bytesRead += 2;
                    //inventorySize = NetDecoder.ReadUshort(data, init+bytesRead);
                    /*
                    Create inventory Items first
                    Add inventory byte size to bytesRead
                    Create ItemStack
                    AddToInventory
                    */
                    break;
            }

            currentSlot++;
        }

        while(currentSlot < hotbar.GetLimit() + inv.GetLimit()){
            mst = (MemoryStorageType)data[init+bytesRead];
            bytesRead++;

            switch(mst){
                case MemoryStorageType.EMPTY:
                    break;
                case MemoryStorageType.ITEM:
                    id = NetDecoder.ReadUshort(data, init+bytesRead);
                    bytesRead += 2;
                    quantity = data[init+bytesRead];
                    bytesRead++;
                    item = ItemLoader.GetCopy(id);
                    its = new ItemStack(item, quantity);
                    AddToInventory(its, hotbar, inv, currentSlot);
                    break;
                case MemoryStorageType.WEAPON:
                    id = NetDecoder.ReadUshort(data, init+bytesRead);
                    bytesRead += 2;
                    currentDur = NetDecoder.ReadUint(data, init+bytesRead);
                    bytesRead += 4;
                    refineLv = data[init+bytesRead];
                    bytesRead++;
                    enchant = (EnchantmentType)data[init+bytesRead];
                    weapon = (Weapon)ItemLoader.GetCopy(id);
                    weapon.SetDurability(currentDur);
                    weapon.SetExtraEffects(enchant);
                    weapon.SetRefineLevel(refineLv);
                    its = new ItemStack(weapon, 1);
                    AddToInventory(its, hotbar, inv, currentSlot);
                    break;
                case MemoryStorageType.STORAGE:
                    //id = NetDecoder.ReadUshort(data, init+bytesRead);
                    //bytesRead += 2;
                    //inventorySize = NetDecoder.ReadUshort(data, init+bytesRead);
                    /*
                    Create inventory Items first
                    Add inventory byte size to bytesRead
                    Create ItemStack
                    AddToInventory
                    */
                    break;
            }

            currentSlot++;
        }
        
    }

    /*
    Turns player inventory into a serialized version in InventorySerializer.buffer
    and returns the amount of written bytes
    */
    public static int SerializePlayerInventory(Inventory hotbar, Inventory inv){
        ItemStack its;
        int bytesWritten = 0;

        for(ushort i=0; i < hotbar.GetLimit(); i++){
            if(hotbar.GetSlot(i) == null){
                InventorySerializer.buffer[bytesWritten] = (byte)MemoryStorageType.EMPTY;
                bytesWritten++;
            }
            else{
                its = hotbar.GetSlot(i);
                bytesWritten += its.ConvertToMemory(InventorySerializer.buffer, bytesWritten);
            }
        }

        for(ushort i=0; i < inv.GetLimit(); i++){
            if(inv.GetSlot(i) == null){
                InventorySerializer.buffer[bytesWritten] = (byte)MemoryStorageType.EMPTY;
                bytesWritten++;
            }
            else{
                its = inv.GetSlot(i);
                bytesWritten += its.ConvertToMemory(InventorySerializer.buffer, bytesWritten);
            }
        }   
        return bytesWritten;
    }

    private static void AddToInventory(ItemStack its, Inventory hotbar, Inventory inv, int currentSlot){
        if(currentSlot < 9)
            hotbar.ForceAddStack(its, (ushort)currentSlot);
        else
            inv.ForceAddStack(its, (ushort)(currentSlot-9));
    }
}
