using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteonyxOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public SteonyxOreblock_Item(){
		this.SetName("Steonyx Ore Block");
		this.SetDescription("Contains traces of steonyx");
		this.SetID(ItemID.STEONYX_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(320);
		this.SetPriceVar(60);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Alienis, 1}, {ThaumicAspect.Metallum, 4}, {ThaumicAspect.Tenebrae, 1}, {ThaumicAspect.Tutamen, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.STEONYX_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
