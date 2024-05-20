using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoalOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public CoalOreblock_Item(){
		this.SetName("Coal Ore Block");
		this.SetDescription("Primary source of fuel");
		this.SetID(ItemID.COAL_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(6);
		this.SetPriceVar(4);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Potentia, 2}, {ThaumicAspect.Ignis, 2}, {ThaumicAspect.Terra, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

