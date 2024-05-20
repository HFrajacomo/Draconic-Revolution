using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CobaltOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public CobaltOreblock_Item(){
		this.SetName("Cobalt Ore Block");
		this.SetDescription("Contains traces of cobalt");
		this.SetID(ItemID.COBALT_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(80);
		this.SetPriceVar(20);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Ignis, 1}, {ThaumicAspect.Metallum, 2}, {ThaumicAspect.Mortus, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

