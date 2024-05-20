using System.Collections;
using System.Collections.Generic;

public class Obsidianblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Obsidianblock_Item(){
		this.SetName("Obsidian Block");
		this.SetDescription("Greenish glass from the depths");
		this.SetID(ItemID.OBSIDIANBLOCK);
		this.SetIconID(2, 0);
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

