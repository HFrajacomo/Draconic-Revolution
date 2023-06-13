using System.Collections;
using System.Collections.Generic;

public class BasaltBrickblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public BasaltBrickblock_Item(){
		this.SetName("Basalt Bricks");
		this.SetDescription("From a volcano to construction sites");
		this.SetID(ItemID.BASALTBRICKBLOCK);
		this.SetIconID(11, 0);
		this.SetStackSize(50);
		this.SetPrice(3);
		this.SetPriceVar(1);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ignis, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.BASALT_BRICK;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
