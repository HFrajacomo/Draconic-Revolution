using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopperOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public CopperOreblock_Item(){
		this.SetName("Copper Ore Block");
		this.SetDescription("Contains traces of copper");
		this.SetID(ItemID.COPPER_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(8);
		this.SetPriceVar(3);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 1}, {ThaumicAspect.Terra, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

