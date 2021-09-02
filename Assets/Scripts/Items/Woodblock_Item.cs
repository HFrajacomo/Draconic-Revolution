using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Woodblock_Item : Item
{
	public Woodblock_Item(){
		this.SetName("Wood Block");
		this.SetDescription("Basic log from nature");
		this.SetID(ItemID.WOODBLOCK);
		this.SetIconID(3, 0);
		this.SetStackSize(50);
		this.SetPrice(10);
		this.SetPriceVar(6);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Arbor, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
	}
}
