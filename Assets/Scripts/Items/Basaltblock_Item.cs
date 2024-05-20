using System.Collections;
using System.Collections.Generic;

public class Basaltblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Basaltblock_Item(){
		this.SetName("Basalt Block");
		this.SetDescription("Straight from a volcano");
		this.SetID(ItemID.BASALTBLOCK);
		this.SetIconID(11, 0);
		this.SetStackSize(50);
		this.SetPrice(3);
		this.SetPriceVar(1);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ignis, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

