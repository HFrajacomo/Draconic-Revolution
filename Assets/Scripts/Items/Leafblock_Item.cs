using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leafblock_Item : Item, IPlaceable
{

	public ushort placeableBlockID {get; set;}

	public Leafblock_Item(){
		this.SetName("Leaf Block");
		this.SetDescription("Spring's delight");
		this.SetID(ItemID.LEAFBLOCK);
		this.SetIconID(7, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Herba, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = ushort.MaxValue-1;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
