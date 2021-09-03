using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item
{
	// INCREMENT THIS EVERYTIME A NEW ITEM IS ADDED
	public static readonly ushort totalItems = 8;
	public static readonly ushort iconAtlasSizeX = 16;

	// Basic Identification
	public string name;
	public string description;
	public ItemID id;
	public uint iconID;

	// Inventory
	public byte stacksize;
	public uint price;
	public uint priceVariation;

	public Dictionary<ThaumicAspect, byte> aspects;
	public HashSet<ItemTag> tags;
	public bool hasDurability;



	// Returns the item's Icon as UVs
	public string GetItemIconName(){
		return "icon_" + this.iconID.ToString();
	}

	// Checks if this item contains a given tag
	public bool ContainsTag(ItemTag tag){
		return this.tags.Contains(tag);
	}

	// Returns this item's tags as a list
	public List<ItemTag> GetTags(){
		return new List<ItemTag>(this.tags);
	}

	public virtual void SetName(string s){this.name = s;}
	public virtual void SetDescription(string s){this.description = s;}
	public virtual void SetID(ItemID i){this.id = i;}
	public virtual void SetIconID(ushort atlasX, ushort atlasY){this.iconID = (uint)(atlasY*Item.iconAtlasSizeX + atlasX);}
	public virtual void SetStackSize(byte b){this.stacksize = b;}
	public virtual void SetPrice(uint u){this.price = u;}
	public virtual void SetPriceVar(uint u){this.priceVariation = u;}
	public virtual void SetAspects(Dictionary<ThaumicAspect, byte> d){this.aspects = d;}
	public virtual void SetTags(List<ItemTag> lit){this.tags = new HashSet<ItemTag>(lit);}
	public virtual void SetDurability(bool b){this.hasDurability = b;}


	/*
	ADD TO THIS LIST EVERYTIME A NEW ITEM IS ADDED
	*/
	public static Item GenerateItem(ushort code){
		ItemID codeID = (ItemID)code; // DEBUG ItemID.Parse(typeof(ItemID), code.ToString());

		switch(codeID){
			case ItemID.GRASSBLOCK:
				return new Grassblock_Item();
			case ItemID.DIRTBLOCK:
				return new Dirtblock_Item();
			case ItemID.STONEBLOCK:
				return new Stoneblock_Item();
			case ItemID.WOODBLOCK:
				return new Woodblock_Item();
			case ItemID.METALBLOCK:
				return new Metalblock_Item();
			case ItemID.WATERBLOCK:
				return new Waterblock_Item();
			case ItemID.TORCH:
				return new Torch_Item();
			case ItemID.LEAFBLOCK:
				return new Leafblock_Item();
			default:
				return null;
		}
	}
}

