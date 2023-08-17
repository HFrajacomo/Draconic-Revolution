using System.Collections;
using System.Collections.Generic;

public class Brickblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Brickblock_Item(){
		this.SetName("Bricks Block");
		this.SetDescription("Hard fired brick blocks");
		this.SetID(ItemID.BRICKBLOCK);
		this.SetIconID(12, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Aqua, 1}, {ThaumicAspect.Ordo, 1}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.BRICK;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
