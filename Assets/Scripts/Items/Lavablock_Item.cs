using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lavablock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Lavablock_Item(){
		this.SetName("Lava Block");
		this.SetDescription("How are you holding this???");
		this.SetID(ItemID.LAVABLOCK);
		this.SetIconID(5, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Ignis, 3}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Forbidden});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

