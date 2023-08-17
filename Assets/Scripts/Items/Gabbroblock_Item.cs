using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gabbroblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Gabbroblock_Item(){
		this.SetName("Gabbro Block");
		this.SetDescription("Dark bluish stone");
		this.SetID(ItemID.GABBROBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.GABBRO;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
