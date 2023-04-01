using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrandiumOreblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public GrandiumOreblock_Item(){
		this.SetName("Grandium Ore Block");
		this.SetDescription("Contains traces of grandium");
		this.SetID(ItemID.GRANDIUM_ORE_BLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(300);
		this.SetPriceVar(70);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Alienis, 1}, {ThaumicAspect.Metallum, 4}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Ore});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.GRANDIUM_ORE;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
