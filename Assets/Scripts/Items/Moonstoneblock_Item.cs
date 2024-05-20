using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moonstoneblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Moonstoneblock_Item(){
		this.SetName("Moonstone Block");
		this.SetDescription("It's very weird to guess where that came from");
		this.SetID(ItemID.MOONSTONEBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(3);
		this.SetPriceVar(1);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Alienis, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

