using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandstoneBrick2block_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public SandstoneBrick2block_Item(){
		this.SetName("Sandstone Bricks");
		this.SetDescription("Pretty chiseled sandstone");
		this.SetID(ItemID.SANDSTONEBRICK2BLOCK);
		this.SetIconID(1, 1);
		this.SetStackSize(50);
		this.SetPrice(11);
		this.SetPriceVar(2);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SANDSTONE_BRICK2;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
