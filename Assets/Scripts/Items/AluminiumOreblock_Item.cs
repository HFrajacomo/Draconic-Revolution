using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AluminiumOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public AluminiumOreblock_Item(){
		this.SetName("Aluminium Ore Block");
		this.SetDescription("Contains traces of aluminium");
		this.SetID(ItemID.ALUMINIUM_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(30);
		this.SetPriceVar(10);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Volatus, 1}, {ThaumicAspect.Metallum, 2}, {ThaumicAspect.Terra, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.ALUMINIUM_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
