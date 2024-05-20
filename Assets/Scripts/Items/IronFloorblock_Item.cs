using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronFloorblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public IronFloorblock_Item(){
		this.SetName("Iron Floor Block");
		this.SetDescription("Industrial flooring metal");
		this.SetID(ItemID.IRONFLOORBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 3}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

