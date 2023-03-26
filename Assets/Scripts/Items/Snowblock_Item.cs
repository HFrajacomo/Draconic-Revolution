using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snowblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Snowblock_Item(){
		this.SetName("Snow Block");
		this.SetDescription("Safe enough to step on");
		this.SetID(ItemID.SNOWBLOCK);
		this.SetIconID(9, 0);
		this.SetStackSize(50);
		this.SetPrice(1);
		this.SetPriceVar(1);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Aqua, 1}, {ThaumicAspect.Gelum, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SNOW;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
