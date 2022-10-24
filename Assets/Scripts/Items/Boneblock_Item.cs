using System.Collections;
using System.Collections.Generic;

public class Boneblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public Boneblock_Item(){
		this.SetName("Bone Block");
		this.SetDescription("It was a living thing at some moment");
		this.SetID(ItemID.BONEBLOCK);
		this.SetIconID(0, 1);
		this.SetStackSize(50);
		this.SetPrice(14);
		this.SetPriceVar(2);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Mortus, 6}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.BONE;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}
