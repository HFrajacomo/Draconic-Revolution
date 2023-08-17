using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quartzblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Quartzblock_Item(){
		this.SetName("Quartz Block");
		this.SetDescription("A stylish white crystal from the depths");
		this.SetID(ItemID.QUARTZBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.QUARTZ;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
