using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmeriumOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public EmeriumOreblock_Item(){
		this.SetName("Emerium Ore Block");
		this.SetDescription("Contains traces of emerium");
		this.SetID(ItemID.EMERIUM_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(45);
		this.SetPriceVar(13);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 1}, {ThaumicAspect.Terra, 1}, {ThaumicAspect.Bestia, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

