using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public GoldOreblock_Item(){
		this.SetName("Gold Ore Block");
		this.SetDescription("Contains traces of gold");
		this.SetID(ItemID.GOLD_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(20);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 1}, {ThaumicAspect.Terra, 1}, {ThaumicAspect.Lucrum, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.GOLD_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
