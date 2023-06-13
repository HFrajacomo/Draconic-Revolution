using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using Random = System.Random;

public abstract class Item
{
	private static Random rng = new Random((int)DateTime.Now.Ticks);

	// Basic Identification
	public string name;
	public string description;
	public ItemID id;
	public uint iconID;

	// Storage
	public MemoryStorageType memoryStorageType;

	// Inventory
	public byte stacksize;
	public uint price;
	public uint priceVariation;

	public Dictionary<ThaumicAspect, byte> aspects;
	public HashSet<ItemTag> tags;
	public bool hasDurability;

	// Returns the item's Icon
	public string GetItemIconName(){
		return "icon_" + this.iconID.ToString();
	}

	// Checks if this item contains a given tag
	public bool ContainsTag(ItemTag tag){
		return this.tags.Contains(tag);
	}

	// Returns this item's tags as a list
	public List<ItemTag> GetTags(){
		return new List<ItemTag>(this.tags);
	}

	// Returns a string array with name and description to use in Details UI
	public virtual string[] GetDetails(){
		return new string[2]{this.name, this.description};
	}

	// Generate the force vector for new Item Entities
	public static float3 GenerateForceVector(){
		float upwards, xForce, zForce;
		int yMitigator = 6;
		int xzMitigator = 10;

		upwards = RandomDecimal() / yMitigator;
		xForce = RandomMirrored() / xzMitigator;
		zForce = RandomMirrored() / xzMitigator;

		return new float3(xForce, upwards, zForce);
	}

	public static byte RandomizeDropQuantity(byte min, byte max){
		if(min == max)
			return max;
		return (byte)Item.rng.Next(min, max+1);
	}

	private static float RandomDecimal(){
		return (float)Item.rng.NextDouble();
	}

	private static float RandomMirrored(){
		return (float)(Item.rng.NextDouble())*2-1;
	}



	public virtual void SetName(string s){this.name = s;}
	public virtual void SetDescription(string s){this.description = s;}
	public virtual void SetID(ItemID i){this.id = i;}
	public virtual void SetIconID(ushort atlasX, ushort atlasY){this.iconID = (uint)(atlasY*Icon.iconAtlasX + atlasX);}
	public virtual void SetStackSize(byte b){this.stacksize = b;}
	public virtual void SetPrice(uint u){this.price = u;}
	public virtual void SetPriceVar(uint u){this.priceVariation = u;}
	public virtual void SetAspects(Dictionary<ThaumicAspect, byte> d){this.aspects = d;}
	public virtual void SetTags(List<ItemTag> lit){this.tags = new HashSet<ItemTag>(lit);}
	public virtual void SetDurability(bool b){this.hasDurability = b;}
	public virtual int Use(){return 0;}
	public virtual void Hold(){}
	public virtual string GetIconName(){return "icon_" + this.iconID.ToString();}
	/*
	ADD TO THIS LIST EVERYTIME A NEW ITEM IS ADDED
	*/
	public static Item GenerateItem(ushort code){
		return GenerateItem((ItemID)code);
	}

	public static Item GenerateItem(ItemID code){
		switch(code){
			case ItemID.GRASSBLOCK:
				return new Grassblock_Item();
			case ItemID.DIRTBLOCK:
				return new Dirtblock_Item();
			case ItemID.STONEBLOCK:
				return new Stoneblock_Item();
			case ItemID.WOODBLOCK:
				return new Woodblock_Item();
			case ItemID.METALBLOCK:
				return new Metalblock_Item();
			case ItemID.WATERBLOCK:
				return new Waterblock_Item();
			case ItemID.TORCH:
				return new Torch_Item();
			case ItemID.LEAFBLOCK:
				return new Leafblock_Item();
			case ItemID.SANDBLOCK:
				return new Sandblock_Item();
			case ItemID.SNOWBLOCK:
				return new Snowblock_Item();
			case ItemID.ICEBLOCK:
				return new Iceblock_Item();
			case ItemID.BASALTBLOCK:
				return new Basaltblock_Item();
			case ItemID.CLAYBLOCK:
				return new Clayblock_Item();
			case ItemID.STONEBRICKBLOCK:
				return new StoneBrickblock_Item();
			case ItemID.WOODENPLANKSREGULARBLOCK:
				return new WoodenPlankRegularblock_Item();
			case ItemID.WOODENPLANKSPINEBLOCK:
				return new WoodenPlankPineblock_Item();
			case ItemID.BONEBLOCK:
				return new Boneblock_Item();
			case ItemID.SANDSTONEBRICKBLOCK:
				return new SandstoneBrickblock_Item();
			case ItemID.SANDSTONEBLOCK:
				return new Sandstoneblock_Item();
			case ItemID.PINEWOODBLOCK:
				return new Pinewoodblock_Item();
			case ItemID.PINELEAFBLOCK:
				return new Pineleafblock_Item();
			case ItemID.GRAVELBLOCK:
				return new Gravelblock_Item();
			case ItemID.MOONSTONEBLOCK:
				return new Moonstoneblock_Item();
			case ItemID.LAVABLOCK:
				return new Lavablock_Item();
			case ItemID.HELLMARBLEBLOCK:
				return new HellMarbleblock_Item();
			case ItemID.ACASTERBLOCK:
				return new Acasterblock_Item();
			case ItemID.COAL_ORE_BLOCK:
				return new CoalOreblock_Item();
			case ItemID.MAGNETITE_ORE_BLOCK:
				return new MagnetiteOreblock_Item();
			case ItemID.ALUMINIUM_ORE_BLOCK:
				return new AluminiumOreblock_Item();
			case ItemID.COPPER_ORE_BLOCK:
				return new CopperOreblock_Item();
			case ItemID.TIN_ORE_BLOCK:
				return new TinOreblock_Item();
			case ItemID.GOLD_ORE_BLOCK:
				return new GoldOreblock_Item();
			case ItemID.EMERIUM_ORE_BLOCK:
				return new EmeriumOreblock_Item();
			case ItemID.URANIUM_ORE_BLOCK:
				return new UraniumOreblock_Item();
			case ItemID.EMERALD_ORE_BLOCK:
				return new EmeraldOreblock_Item();
			case ItemID.RUBY_ORE_BLOCK:
				return new RubyOreblock_Item();
			case ItemID.COBALT_ORE_BLOCK:
				return new CobaltOreblock_Item();
			case ItemID.ARDITE_ORE_BLOCK:
				return new ArditeOreblock_Item();
			case ItemID.GRANDIUM_ORE_BLOCK:
				return new GrandiumOreblock_Item();
			case ItemID.STEONYX_ORE_BLOCK:
				return new SteonyxOreblock_Item();
			case ItemID.WHITEMARBLEBLOCK:
				return new WhiteMarbleblock_Item();
			case ItemID.WHITEMARBLEBRICKBLOCK:
				return new WhiteMarbleBrickblock_Item();
			case ItemID.BLACKMARBLEBLOCK:
				return new BlackMarbleblock_Item();
			case ItemID.BLACKMARBLEBRICKBLOCK:
				return new BlackMarbleBrickblock_Item();
			case ItemID.HELLMARBLEBRICKBLOCK:
				return new HellMarbleBrickblock_Item();
			case ItemID.SUNSTONEBLOCK:
				return new Sunstoneblock_Item();
			case ItemID.COBBLESTONEBLOCK:
				return new Cobblestoneblock_Item();
			case ItemID.VINTEUMOREBLOCK:
				return new VinteumOreblock_Item();
			case ItemID.SILVERWOODBLOCK:
				return new SilverWoodblock_Item();
			case ItemID.SILVERWOODLEAFBLOCK:
				return new SilverWoodLeafblock_Item();
			case ItemID.SILVERWOODPLANKBLOCK:
				return new SilverWoodPlankblock_Item();
			case ItemID.IRONFLOORBLOCK:
				return new IronFloorblock_Item();
			case ItemID.IRONWALLBLOCK:
				return new IronWallblock_Item();
			case ItemID.GABBROBLOCK:
				return new Gabbroblock_Item();
			case ItemID.GABBROBRICKBLOCK:
				return new GabbroBrickblock_Item();
			case ItemID.BRICKBLOCK:
				return new Brickblock_Item();
			case ItemID.SANDSTONEBRICK2BLOCK:
				return new SandstoneBrick2block_Item();
			case ItemID.GRAVEL2BLOCK:
				return new Gravel2block_Item();
			case ItemID.LIMESTONEBLOCK:
				return new Limestoneblock_Item();
			case ItemID.LIMESTONEBRICKBLOCK:
				return new LimestoneBrickblock_Item();
			case ItemID.QUARTZBLOCK:
				return new Quartzblock_Item();
			case ItemID.QUARTZBRICKBLOCK:
				return new QuartzBrickblock_Item();
			case ItemID.BASALTBRICKBLOCK:
				return new BasaltBrickblock_Item();
			case ItemID.OBSIDIANBLOCK:
				return new Obsidianblock_Item();
			default:
				return null;
		}
	}
}

