using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metalblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Metalblock_Item(){
		this.SetName("Metal Block");
		this.SetDescription("Pure metal ore");
		this.SetID(ItemID.METALBLOCK);
		this.SetIconID(4, 0);
		this.SetStackSize(50);
		this.SetPrice(16);
		this.SetPriceVar(10);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 1}, {ThaumicAspect.Terra, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 5;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
