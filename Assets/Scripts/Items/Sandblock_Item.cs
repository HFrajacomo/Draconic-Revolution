using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sandblock_Item : Item, IPlaceable
{
    public ushort placeableBlockID {get; set;}

    public Sandblock_Item(){
        this.SetName("Sand Block");
        this.SetDescription("Taken straight from a beach or something");
        this.SetID(ItemID.SANDBLOCK);
        this.SetIconID(8, 0);
        this.SetStackSize(50);
        this.SetPrice(3);
        this.SetPriceVar(2);
        this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 1}, {ThaumicAspect.Perditio, 1}});
        this.SetTags(new List<ItemTag>(){ItemTag.Placeable});
        this.SetDurability(false);
        this.placeableBlockID = 8;
    }

    public override int Use(){
        return this.placeableBlockID;
    }
}
