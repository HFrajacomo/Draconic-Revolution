using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmeraldOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public EmeraldOreblock_Item(){
		this.SetName("Emerald Ore Block");
		this.SetDescription("Contains traces of emerald");
		this.SetID(ItemID.EMERALD_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(45);
		this.SetPriceVar(15);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Vitrus, 1}, {ThaumicAspect.Lucrum, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

