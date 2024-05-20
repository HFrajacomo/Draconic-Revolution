using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackMarbleblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public BlackMarbleblock_Item(){
		this.SetName("Black Marble Block");
		this.SetDescription("An aesthetic black stone from the underground");
		this.SetID(ItemID.BLACKMARBLEBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

