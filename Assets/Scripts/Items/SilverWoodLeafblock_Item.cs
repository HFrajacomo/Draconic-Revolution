using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilverWoodLeafblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public SilverWoodLeafblock_Item(){
		this.SetName("Silverwood Leaf Block");
		this.SetDescription("Blue leaves taken from a silver tree");
		this.SetID(ItemID.SILVERWOODLEAFBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Herba, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SILVERWOOD_LEAF;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
