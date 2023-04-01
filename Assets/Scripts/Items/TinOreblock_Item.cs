using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TinOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public TinOreblock_Item(){
		this.SetName("Tin Ore Block");
		this.SetDescription("Contains traces of tin");
		this.SetID(ItemID.TIN_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(10);
		this.SetPriceVar(5);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 1}, {ThaumicAspect.Terra, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.TIN_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
