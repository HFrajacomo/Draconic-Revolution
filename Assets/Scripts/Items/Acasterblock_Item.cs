using System.Collections;
using System.Collections.Generic;

public class Acasterblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Acasterblock_Item(){
		this.SetName("Acaster Rock Block");
		this.SetDescription("The rock guarding the deepest parts of the world");
		this.SetID(ItemID.ACASTERBLOCK);
		this.SetIconID(11, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Tutamen, 20}, {ThaumicAspect.Terra, 5}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone, ItemTag.Forbidden});
		this.SetDurability(false);
		this.placeableBlockID = 0;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}

