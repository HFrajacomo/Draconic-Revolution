using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using Random = System.Random;

[Serializable]
public class Item {
	protected static Random rng = new Random((int)DateTime.Now.Ticks);

	// Basic Identification
	public string codename;
	public string name;
	public string description;

	protected ushort id;

	// Storage
	public byte memoryType;
	protected MemoryStorageType memoryStorageType;

	// Inventory
	public byte stacksize;
	public bool hasDurability = false;

	// Behaviours
	protected List<ItemBehaviour> onHoldPlayerBehaviour;
	protected List<ItemBehaviour> onHoldClientBehaviour;
	protected List<ItemBehaviour> onHoldServerBehaviour;
	protected List<ItemBehaviour> onUnholdPlayerBehaviour;
	protected List<ItemBehaviour> onUnholdClientBehaviour;
	protected List<ItemBehaviour> onUnholdServerBehaviour;
	protected List<ItemBehaviour> onUseClientBehaviour;
	protected List<ItemBehaviour> onUseServerBehaviour;


	public Item Copy(){
		return (Item)this.MemberwiseClone();
	}

	public override string ToString(){
		return $"{this.codename}:{this.id}";
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

	protected static float RandomDecimal(){
		return (float)Item.rng.NextDouble();
	}

	protected static float RandomMirrored(){
		return (float)(Item.rng.NextDouble())*2-1;
	}

	// EVENT GET/SET
	public List<ItemBehaviour> GetOnHoldPlayer() { return onHoldPlayerBehaviour; }
    public void SetOnHoldPlayer(List<ItemBehaviour> val) { onHoldPlayerBehaviour = val; }
	public List<ItemBehaviour> GetOnHoldClient() { return onHoldClientBehaviour; }
    public void SetOnHoldClient(List<ItemBehaviour> val) { onHoldClientBehaviour = val; }
	public List<ItemBehaviour> GetOnHoldServer() { return onHoldServerBehaviour; }
    public void SetOnHoldServer(List<ItemBehaviour> val) { onHoldServerBehaviour = val; }

	public List<ItemBehaviour> GetOnUnholdPlayer() { return onUnholdPlayerBehaviour; }
    public void SetOnUnholdPlayer(List<ItemBehaviour> val) { onUnholdPlayerBehaviour = val; }
	public List<ItemBehaviour> GetOnUnholdClient() { return onUnholdClientBehaviour; }
    public void SetOnUnholdClient(List<ItemBehaviour> val) { onUnholdClientBehaviour = val; }
	public List<ItemBehaviour> GetOnUnholdServer() { return onUnholdServerBehaviour; }
    public void SetOnUnholdServer(List<ItemBehaviour> val) { onUnholdServerBehaviour = val; }

	public List<ItemBehaviour> GetOnUseClient() { return onUseClientBehaviour; }
    public void SetOnUseClient(List<ItemBehaviour> val) { onUseClientBehaviour = val; }
	public List<ItemBehaviour> GetOnUseServer() { return onUseServerBehaviour; }
    public void SetOnUseServer(List<ItemBehaviour> val) { onUseServerBehaviour = val; }

    // Properties Set
	public void SetID(ushort i){this.id = i;}
	public void SetDurability(bool b){this.hasDurability = b;}
	public ushort GetID(){return this.id;}
	public void SetMemoryStorageType(){this.memoryStorageType = (MemoryStorageType)this.memoryType;}
	public MemoryStorageType GetMemoryStorageType(){return (MemoryStorageType)this.memoryType;}

	// Basic Operations
	public virtual void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onHoldPlayerBehaviour == null || this.onHoldPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onHoldPlayerBehaviour.Count; i++){
			this.onHoldPlayerBehaviour[i].OnHoldPlayer(cl, its, code);
		}
	}
	public virtual void OnHoldClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onHoldClientBehaviour == null || this.onHoldClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onHoldClientBehaviour.Count; i++){
			this.onHoldClientBehaviour[i].OnHoldClient(cl, its, code);
		}
	}
	public virtual void OnHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onHoldServerBehaviour == null || this.onHoldServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onHoldServerBehaviour.Count; i++){
			this.onHoldServerBehaviour[i].OnHoldServer(cl, its, code);
		}
	}
	public virtual void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onUnholdPlayerBehaviour == null || this.onUnholdPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnholdPlayerBehaviour.Count; i++){
			this.onUnholdPlayerBehaviour[i].OnUnholdPlayer(cl, its, code);
		}
	}
	public virtual void OnUnholdClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onUnholdClientBehaviour == null || this.onUnholdClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnholdClientBehaviour.Count; i++){
			this.onUnholdClientBehaviour[i].OnUnholdClient(cl, its, code);
		}
	}
	public virtual void OnUnholdServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onUnholdServerBehaviour == null || this.onUnholdServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnholdServerBehaviour.Count; i++){
			this.onUnholdServerBehaviour[i].OnUnholdServer(cl, its, code);
		}
	}
	public virtual void OnUseClient(ChunkLoader cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){
		if(this.onUseClientBehaviour == null || this.onUseClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUseClientBehaviour.Count; i++){
			this.onUseClientBehaviour[i].OnUseClient(cl, its, usagePos, targetBlock, referencePoint1, referencePoint2, referencePoint3);
		}
	}
	public virtual void OnUseServer(ChunkLoader_Server cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){
		if(this.onUseServerBehaviour == null || this.onUseServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUseServerBehaviour.Count; i++){
			this.onUseServerBehaviour[i].OnUseServer(cl, its, usagePos, targetBlock, referencePoint1, referencePoint2, referencePoint3);
		}
	}

	public void SetupAfterSerialize(bool isClient){
		if(this.onHoldPlayerBehaviour != null){
			for(int i=0; i < onHoldPlayerBehaviour.Count; i++){
				onHoldPlayerBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onHoldClientBehaviour != null){
			for(int i=0; i < onHoldClientBehaviour.Count; i++){
				onHoldClientBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onHoldServerBehaviour != null){
			for(int i=0; i < onHoldServerBehaviour.Count; i++){
				onHoldServerBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onUnholdPlayerBehaviour != null){
			for(int i=0; i < onUnholdPlayerBehaviour.Count; i++){
				onUnholdPlayerBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onUnholdClientBehaviour != null){
			for(int i=0; i < onUnholdClientBehaviour.Count; i++){
				onUnholdClientBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onUnholdServerBehaviour != null){
			for(int i=0; i < onUnholdServerBehaviour.Count; i++){
				onUnholdServerBehaviour[i].PostDeserializationSetup(isClient);
			}
		}

		if(this.onUseClientBehaviour != null){
			for(int i=0; i < onUseClientBehaviour.Count; i++){
				onUseClientBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
		if(this.onUseServerBehaviour != null){
			for(int i=0; i < onUseServerBehaviour.Count; i++){
				onUseServerBehaviour[i].PostDeserializationSetup(isClient);
			}
		}
	}
}

