using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetiteOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public MagnetiteOreblock_Item(){
		this.SetName("Magnetite Ore Block");
		this.SetDescription("Powered by the world's magnetite field");
		this.SetID(ItemID.MAGNETITE_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(28);
		this.SetPriceVar(12);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Potentia, 6}, {ThaumicAspect.Terra, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.MAGNETITE_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
