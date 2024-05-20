using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cobblestoneblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Cobblestoneblock_Item(){
		this.SetName("Cobblestone Block");
		this.SetDescription("Basic broken stone");
		this.SetID(ItemID.COBBLESTONEBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Perditio, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

