using System.Collections;
using System.Collections.Generic;

public class Pinewoodblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Pinewoodblock_Item(){
		this.SetName("Pine Wood Block");
		this.SetDescription("Has a darker bark");
		this.SetID(ItemID.PINEWOODBLOCK);
		this.SetIconID(3, 1);
		this.SetStackSize(50);
		this.SetPrice(10);
		this.SetPriceVar(6);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ignis, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Wood});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

