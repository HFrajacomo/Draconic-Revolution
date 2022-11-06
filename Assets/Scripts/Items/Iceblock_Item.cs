using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Iceblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Iceblock_Item(){
		this.SetName("Ice Block");
		this.SetDescription("Frozen water");
		this.SetID(ItemID.ICEBLOCK);
		this.SetIconID(10, 0);
		this.SetStackSize(50);
		this.SetPrice(2);
		this.SetPriceVar(1);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Gelum, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.COAL_ORE;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
