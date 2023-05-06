using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronWallblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public IronWallblock_Item(){
		this.SetName("Iron Wall Block");
		this.SetDescription("Industrial wall metal material");
		this.SetID(ItemID.IRONWALLBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Metallum, 3}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.IRON_WALL;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
