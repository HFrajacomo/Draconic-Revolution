using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilverWoodblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public SilverWoodblock_Item(){
		this.SetName("Silverwood Block");
		this.SetDescription("Wood taken from the magical silver tree");
		this.SetID(ItemID.SILVERWOODBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Arbor, 2}, {ThaumicAspect.Precantatio, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SILVERWOOD;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
