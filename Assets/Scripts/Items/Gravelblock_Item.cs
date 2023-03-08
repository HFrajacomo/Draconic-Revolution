using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravelblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Gravelblock_Item(){
		this.SetName("Gravel Block");
		this.SetDescription("Granular chips of stone");
		this.SetID(ItemID.GRAVELBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.GRAVEL;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
