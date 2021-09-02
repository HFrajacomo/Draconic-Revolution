using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item
{
	// Image
	public static readonly int atlasSizeX = 16;
	public static readonly int atlasSizeY = 1;

	public uint itemAtlasPosX;
	public uint itemAtlasPosY;

	// Basic Identification
	public string name;
	public string description;
	public ItemID id;

	// Inventory
	public byte stacksize;
	public uint price;
	public uint priceVariation;

	// Thaumic Aspects
	public HashSet<ItemTag> tags;
	public bool hasDurability;


	// Returns the item's Icon as UVs
	public Vector2[] GetItemIcon(){
		Vector2[] UVs = new Vector2[4];
		float x,y;
 
		x = this.itemAtlasPosX * (1f / Item.atlasSizeX);
		y = this.itemAtlasPosY * (1f / Item.atlasSizeY);

		UVs[0] = new Vector2(x,y+(1f/Item.atlasSizeY));
		UVs[1] = new Vector2(x+(1f/Item.atlasSizeX),y+(1f/Item.atlasSizeY));
		UVs[2] = new Vector2(x+(1f/Item.atlasSizeX),y);
		UVs[3] = new Vector2(x,y);

		return UVs;
	}

	// Checks if this item contains a given tag
	public bool ContainsTag(ItemTag tag){
		return this.tags.Contains(tag);
	}

	// Returns this item's tags as a list
	public List<ItemTag> GetTags(){
		return new List<ItemTag>(this.tags);
	}
}

public enum ItemID : ushort {
	GRASSBLOCK,
	DIRTBLOCK,
	STONEBLOCK,
	WOODBLOCK,
	METALBLOCK,
	LEAFBLOCK,
	WATERBLOCK,
	TORCH
}