using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waterblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Waterblock_Item(){
		this.SetName("Water Block");
		this.SetDescription("How are you holding this?");
		this.SetID(ItemID.WATERBLOCK);
		this.SetIconID(5, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Aqua, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Forbidden});
		this.SetDurability(false);
		this.placeableBlockID = 6;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
