using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteMarbleblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public WhiteMarbleblock_Item(){
		this.SetName("White Marble Block");
		this.SetDescription("An aesthetic white stone from the underground");
		this.SetID(ItemID.WHITEMARBLEBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.WHITE_MARBLE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}