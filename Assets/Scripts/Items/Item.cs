using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using Random = System.Random;

[Serializable]
public class Item
{
	private static Random rng = new Random((int)DateTime.Now.Ticks);

	// Basic Identification
	public string codename;
	public string name;
	public string description;

	public ushort id;

	// Storage
	public byte memoryType;
	private MemoryStorageType memoryStorageType;

	// Inventory
	public byte stacksize;
	public uint price;
	public uint priceVariation;

	private HashSet<string> tags;
	public bool hasDurability;

	// Behaviours
	private ItemBehaviour onHoldBehaviour;
	private ItemBehaviour onUseClientBehaviour;
	private ItemBehaviour onUseServerBehaviour;


	// Checks if this item contains a given tag
	public bool ContainsTag(string tag){
		return this.tags.Contains(tag);
	}

	// Returns this item's tags as a list
	public List<string> GetTags(){
		return new List<string>(this.tags);
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

	// EVENT GET/SET
	public ItemBehaviour GetOnHold() { return onHoldBehaviour; }
    public void SetOnHold(ItemBehaviour val) { onHoldBehaviour = val; }
	public ItemBehaviour GetOnUse() { return onUseBehaviour; }
    public void SetOnUse(ItemBehaviour val) { onUseBehaviour = val; }

    // Properties Set
	public void SetID(ushort i){this.id = i;}
	public void SetPrice(uint u){this.price = u;}
	public void SetPriceVar(uint u){this.priceVariation = u;}
	public void SetTags(List<string> lit){this.tags = new HashSet<string>(lit);}
	public void SetDurability(bool b){this.hasDurability = b;}

	// Basic Operations
	public virtual void OnHold(ChunkLoader cl, ItemStack its){
		if(this.onHoldBehaviour == null)
			return;
		this.onHoldBehaviour.OnHold(cl);
	}
	public virtual void OnUseClient(ChunkLoader cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){
		if(this.onUseClientBehaviour == null)
			return;
		this.onUseClientBehaviour.OnUseClient(cl, its, usagePos, referencePoint, targetBlock);
	}
	public virtual void OnUseServer(ChunkLoader_Server cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){
		if(this.onUseServerBehaviour == null)
			return;
		this.onUseServerBehaviour.OnUseClient(cl, its, usagePos, referencePoint, targetBlock);
	}

	public void SetupAfterSerialize(bool isClient){
		if(this.onHoldBehaviour != null)
			onHoldBehaviour.PostDeserializationSetup(isClient);
		if(this.onUseClientBehaviour != null)
			onUseClientBehaviour.PostDeserializationSetup(isClient);
		if(this.onUseServerBehaviour != null)
			onUseServerBehaviour.PostDeserializationSetup(isClient);
	}
}

