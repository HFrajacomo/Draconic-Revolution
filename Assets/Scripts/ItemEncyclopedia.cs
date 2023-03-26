using System.Collections;
using System.Collections.Generic;

public class ItemEncyclopedia
{
	public Item[] items = new Item[ItemID.GetNames(typeof(ushort)).Length];

    void Awake()
    {
    	for(ushort i=0; i<items.Length; i++)
    		this.items[i] = Item.GenerateItem(i);
    }

    public Item GetItem(ushort code){
    	if(this.ValidateCode(code))
    		return this.items[code];
    	return null;
    	
    }

    // Checks if code input is in bounds of ItemEncyclopedia.items
    private bool ValidateCode(ushort code){
    	if(code >= items.Length)
    		return false;
    	return true;
    }

}
