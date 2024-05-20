using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HellMarbleblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public HellMarbleblock_Item(){
		this.SetName("Hell Marble Block");
		this.SetDescription("Red broken marble from hell");
		this.SetID(ItemID.HELLMARBLEBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Ignis, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

