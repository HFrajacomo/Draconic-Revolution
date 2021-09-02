using System.Collections;
using System.Collections.Generic;

public class Dirtblock_Item : Item
{
	public Dirtblock_Item(){
		this.SetName("Dirt Block");
		this.SetDescription("There may be worms");
		this.SetID(ItemID.DIRTBLOCK);
		this.SetIconID(1, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
		this.SetDurability(false);
	}
}
