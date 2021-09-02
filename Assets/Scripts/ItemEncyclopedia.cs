using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEncyclopedia : MonoBehaviour
{
	public Item[] items = new Item[Item.totalItems];

    void Awake()
    {
    	for(ushort i=0; i<Item.totalItems; i++)
    		this.items[i] = Item.PopulateEncyclopedia(i);
    }


    public Item GetItem(ushort code){
    	if(this.ValidateCode(code))
    		return this.items[code];
    	return null;
    	
    }

    // Checks if code input is in bounds of ItemEncyclopedia.items
    private bool ValidateCode(ushort code){
    	if(code >= Item.totalItems)
    		return false;
    	return true;
    }

}
