using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}
	
	public Torch_Item(){
		this.SetName("Torch");
		this.SetDescription("Lights the way");
		this.SetID(ItemID.TORCH);
		this.SetIconID(6, 0);
		this.SetStackSize(50);
		this.SetPrice(10);
		this.SetPriceVar(6);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Lux, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Light});
		this.SetDurability(false);
		this.placeableBlockID = ushort.MaxValue;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
