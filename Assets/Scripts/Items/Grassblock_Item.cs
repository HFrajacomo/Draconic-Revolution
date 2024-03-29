using System.Collections;
using System.Collections.Generic;

public class Grassblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Grassblock_Item(){
		this.SetName("Grass Block");
		this.SetDescription("Nature's floormat");
		this.SetID(ItemID.GRASSBLOCK);
		this.SetIconID(0, 0);
		this.SetStackSize(50);
		this.SetPrice(1);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Herba, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = 1;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
