using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UraniumOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public UraniumOreblock_Item(){
		this.SetName("Uranium Ore Block");
		this.SetDescription("Contains traces of uranium");
		this.SetID(ItemID.URANIUM_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(120);
		this.SetPriceVar(24);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 1}, {ThaumicAspect.Terra, 1}, {ThaumicAspect.Potentia, 20}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

