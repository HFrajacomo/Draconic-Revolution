using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenPlankPineblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public WoodenPlankPineblock_Item(){
		this.SetName("Pine Planks");
		this.SetDescription("Pine planks made from pine trees");
		this.SetID(ItemID.WOODENPLANKSPINEBLOCK);
		this.SetIconID(15, 0);
		this.SetStackSize(50);
		this.SetPrice(4);
		this.SetPriceVar(3);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Arbor, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.WOODEN_PLANKS_PINE;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
