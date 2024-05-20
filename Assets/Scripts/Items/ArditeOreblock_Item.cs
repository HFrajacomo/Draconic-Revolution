using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArditeOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public ArditeOreblock_Item(){
		this.SetName("Ardite Ore Block");
		this.SetDescription("Contains traces of ardite");
		this.SetID(ItemID.ARDITE_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(100);
		this.SetPriceVar(25);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Ignis, 1}, {ThaumicAspect.Metallum, 2}, {ThaumicAspect.Precantatio, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

