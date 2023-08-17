using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VinteumOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public VinteumOreblock_Item(){
		this.SetName("Vinteum Ore");
		this.SetDescription("Magic stone containing mana powder");
		this.SetID(ItemID.VINTEUMOREBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Precantatio, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.VINTEUM_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
