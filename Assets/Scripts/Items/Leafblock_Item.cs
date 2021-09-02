using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leafblock_Item : Item
{
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
	}
}
