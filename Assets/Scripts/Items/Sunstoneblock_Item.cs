using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sunstoneblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Sunstoneblock_Item(){
		this.SetName("Sunstone Block");
		this.SetDescription("Bright magma stone that emanates light");
		this.SetID(ItemID.SUNSTONEBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Lux, 10}, {ThaumicAspect.Ignis, 2}, {ThaumicAspect.Terra, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.SUNSTONE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
