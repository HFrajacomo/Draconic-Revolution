using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sandstoneblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Sandstoneblock_Item(){
		this.SetName("Sandstone");
		this.SetDescription("Sand hardened by nature");
		this.SetID(ItemID.SANDSTONEBLOCK);
		this.SetIconID(2, 1);
		this.SetStackSize(50);
		this.SetPrice(5);
		this.SetPriceVar(3);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SANDSTONE;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
