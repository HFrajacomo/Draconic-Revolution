using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilverWoodPlankblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public SilverWoodPlankblock_Item(){
		this.SetName("Silverwood Plank Block");
		this.SetDescription("Processed planks from a silver tree");
		this.SetID(ItemID.SILVERWOODPLANKBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Arbor, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SILVERWOOD_PLANKS;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
