using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pineleafblock_Item : Item, IPlaceable
{

	public ushort placeableBlockID {get; set;}

	public Pineleafblock_Item(){
		this.SetName("Pine Leaf Block");
		this.SetDescription("Some are covered in snow");
		this.SetID(ItemID.PINELEAFBLOCK);
		this.SetIconID(4, 1);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Herba, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

