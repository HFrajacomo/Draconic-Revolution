using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenPlankRegularblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public WoodenPlankRegularblock_Item(){
		this.SetName("Wooden Planks");
		this.SetDescription("Basic planks made from basic trees");
		this.SetID(ItemID.WOODENPLANKSREGULARBLOCK);
		this.SetIconID(14, 0);
		this.SetStackSize(50);
		this.SetPrice(4);
		this.SetPriceVar(3);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Arbor, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.WOODEN_PLANKS_REGULAR;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
