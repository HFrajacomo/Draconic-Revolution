using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InventorySerializer
{
    // Creates Inventory and Hotbar given a byte[] received from server
    // out are the output inventories
    public static void BuildPlayerInventory(byte[] data, int init, out Inventory hotbar, out Inventory inv){
        int bytesRead = 0;
        int currentSlot = 0;
        MemoryStorageType mst;
        ItemID id;
        byte quantity;
        ulong currentDur;
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
                    id = (ItemID)NetDecoder.ReadUshort(data, init+bytesRead);
                    bytesRead += 2;
                    quantity = data[init+bytesRead];
                    bytesRead++;
                    item = Item.GenerateItem((ushort)id);
                    its = new ItemStack(item, quantity);
                    AddToInventory(its, hotbar, inv, currentSlot);
                    break;
                case MemoryStorageType.WEAPON:
                    id = (ItemID)NetDecoder.ReadUshort(data, init+bytesRead);
                    bytesRead += 2;
                    currentDur = NetDecoder.ReadUlong(data, init+bytesRead);
                    bytesRead += 8;
                    refineLv = data[init+bytesRead];
                    bytesRead++;
                    enchant = (EnchantmentType)data[init+bytesRead];
                    weapon = (Weapon)Item.GenerateItem((ushort)id);
                    weapon.SetDurability(currentDur);
                    weapon.SetExtraEffects(enchant);
                    weapon.SetRefineLevel(refineLv);
                    its = new ItemStack(weapon, 1);
                    AddToInventory(its, hotbar, inv, currentSlot);
                    break;
                case MemoryStorageType.STORAGE:
                    //id = (ItemID)NetDecoder.ReadUshort(data, init+bytesRead);
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

    public static void AddToInventory(ItemStack its, Inventory hotbar, Inventory inv, int currentSlot){
        if(currentSlot < 9)
            hotbar.ForceAddStack(its, (ushort)currentSlot);
        else
            inv.ForceAddStack(its, (ushort)currentSlot);
    }
}
