using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneBrickblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public StoneBrickblock_Item(){
		this.SetName("Stone Bricks");
		this.SetDescription("Ordered stone");
		this.SetID(ItemID.STONEBRICKBLOCK);
		this.SetIconID(13, 0);
		this.SetStackSize(50);
		this.SetPrice(1);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.STONE_BRICK;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
