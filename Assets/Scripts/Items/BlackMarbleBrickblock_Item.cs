using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackMarbleBrickblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public BlackMarbleBrickblock_Item(){
		this.SetName("Black Marble Bricks");
		this.SetDescription("Pleasing black bricks");
		this.SetID(ItemID.BLACKMARBLEBRICKBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.BLACK_MARBLE_BRICKS;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
