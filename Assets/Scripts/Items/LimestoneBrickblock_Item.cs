using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimestoneBrickblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public LimestoneBrickblock_Item(){
		this.SetName("Limestone Bricks Block");
		this.SetDescription("Ornamental Bricks with calcite");
		this.SetID(ItemID.LIMESTONEBRICKBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.LIMESTONE_BRICK;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}