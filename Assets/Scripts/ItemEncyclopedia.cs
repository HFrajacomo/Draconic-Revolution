using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemEncyclopedia
{
	public static Item[] items = new Item[ItemID.GetNames(typeof(ItemID)).Length];
    private static bool isInitialized = false;

    public static void Initialize()
    {
    	for(ushort i=0; i<items.Length; i++)
    		items[i] = Item.GenerateItem(i);
    }

    public static Item GetItem(ushort code){
        if(!isInitialized)
            Initialize();

    	if(ValidateCode(code))
    		return items[code];
    	return null;
    }

    public static Item GetItem(ItemID code){
        return GetItem((ushort)code);
    }

    // Checks if code input is in bounds of ItemEncyclopedia.items
    private static bool ValidateCode(ushort code){
    	if(code >= items.Length)
    		return false;
    	return true;
    }

}
