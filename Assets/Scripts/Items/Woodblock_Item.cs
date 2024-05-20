using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Woodblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Woodblock_Item(){
		this.SetName("Wood Block");
		this.SetDescription("Basic log from nature");
		this.SetID(ItemID.WOODBLOCK);
		this.SetIconID(3, 0);
		this.SetStackSize(50);
		this.SetPrice(10);
		this.SetPriceVar(6);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Arbor, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

