using System.Collections;
using System.Collections.Generic;

public class Clayblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Clayblock_Item(){
		this.SetName("Clay Block");
		this.SetDescription("Can be molded into other things");
		this.SetID(ItemID.CLAYBLOCK);
		this.SetIconID(12, 0);
		this.SetStackSize(50);
		this.SetPrice(5);
		this.SetPriceVar(1);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Aqua, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.CLAY;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
