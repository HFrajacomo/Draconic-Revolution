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
	public List<string> tags;

	protected ushort id;


	// Storage
	public byte memoryType;
	protected MemoryStorageType memoryStorageType;
	protected HashSet<string> itemTags;

	// Inventory
	public byte stacksize;
	public bool hasDurability = false;

	// Behaviours
	protected List<ItemBehaviour> onCreateActionBehaviour;
	protected List<ItemBehaviour> onHoldPlayerBehaviour;
	protected List<ItemBehaviour> onHoldClientBehaviour;
	protected List<ItemBehaviour> onHoldServerBehaviour;
	protected List<ItemBehaviour> onUnholdPlayerBehaviour;
	protected List<ItemBehaviour> onUnholdClientBehaviour;
	protected List<ItemBehaviour> onUnholdServerBehaviour;
	protected List<ItemBehaviour> onUseClientBehaviour;
	protected List<ItemBehaviour> onUseServerBehaviour;
	protected List<ItemBehaviour> onEquipPlayerBehaviour;
	protected List<ItemBehaviour> onUnequipPlayerBehaviour;
	protected List<ItemBehaviour> onEquipClientBehaviour;
	protected List<ItemBehaviour> onUnequipClientBehaviour;
	protected List<ItemBehaviour> onEquipServerBehaviour;
	protected List<ItemBehaviour> onUnequipServerBehaviour;

	public virtual void PostDeserializationSetup(){
		this.itemTags = new HashSet<string>(this.tags);
		this.tags.Clear();
		this.tags = null;
	}

	public virtual Item Copy(){
		return new Item {
			codename = this.codename,
			name = this.name,
			description = this.description,
			id = this.id,
			memoryType = this.memoryType,
			memoryStorageType = this.memoryStorageType,
			stacksize = this.stacksize,
			hasDurability = this.hasDurability,
			itemTags = this.itemTags,

			// Deep copy lists (new lists with same elements)
			onCreateActionBehaviour = this.onCreateActionBehaviour != null ? new List<ItemBehaviour>(this.onCreateActionBehaviour) : null,
			onHoldPlayerBehaviour = this.onHoldPlayerBehaviour != null ? new List<ItemBehaviour>(this.onHoldPlayerBehaviour) : null,
			onHoldClientBehaviour = this.onHoldClientBehaviour != null ? new List<ItemBehaviour>(this.onHoldClientBehaviour) : null,
			onHoldServerBehaviour = this.onHoldServerBehaviour != null ? new List<ItemBehaviour>(this.onHoldServerBehaviour) : null,
			onUnholdPlayerBehaviour = this.onUnholdPlayerBehaviour != null ? new List<ItemBehaviour>(this.onUnholdPlayerBehaviour) : null,
			onUnholdClientBehaviour = this.onUnholdClientBehaviour != null ? new List<ItemBehaviour>(this.onUnholdClientBehaviour) : null,
			onUnholdServerBehaviour = this.onUnholdServerBehaviour != null ? new List<ItemBehaviour>(this.onUnholdServerBehaviour) : null,
			onUseClientBehaviour = this.onUseClientBehaviour != null ? new List<ItemBehaviour>(this.onUseClientBehaviour) : null,
			onUseServerBehaviour = this.onUseServerBehaviour != null ? new List<ItemBehaviour>(this.onUseServerBehaviour) : null,
			onEquipClientBehaviour = this.onEquipClientBehaviour != null ? new List<ItemBehaviour>(this.onEquipClientBehaviour) : null,
			onUnequipClientBehaviour = this.onUnequipClientBehaviour != null ? new List<ItemBehaviour>(this.onUnequipClientBehaviour) : null,
			onEquipServerBehaviour = this.onEquipServerBehaviour != null ? new List<ItemBehaviour>(this.onEquipServerBehaviour) : null,
			onUnequipServerBehaviour = this.onUnequipServerBehaviour != null ? new List<ItemBehaviour>(this.onUnequipServerBehaviour) : null,
			onEquipPlayerBehaviour = this.onEquipPlayerBehaviour != null ? new List<ItemBehaviour>(this.onEquipPlayerBehaviour) : null,
			onUnequipPlayerBehaviour = this.onUnequipPlayerBehaviour != null ? new List<ItemBehaviour>(this.onUnequipPlayerBehaviour) : null
		};
	}

	public bool ContainsTag(string tag){
		if(this.itemTags == null)
			return false;

		return this.itemTags.Contains(tag);
	}
	public bool ContainsAnyTag(HashSet<string> tags){
		if(this.itemTags == null)
			return false;

		return this.itemTags.Overlaps(tags);
	}
	public bool ContainsAnyTag(string[] tags){
		for(int i=0; i < tags.Length; i++){
			if(this.itemTags.Contains(tags[i])){
				return true;
			}
		}
		return false;
	}

	public HashSet<string> GetTags(){return ItemLoader.GetItem(this.id).itemTags;}

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
	public List<ItemBehaviour> GetOnCreateAction() { return onCreateActionBehaviour; }
	public void SetOnCreateAction(List<ItemBehaviour> val) { onCreateActionBehaviour = val; }

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

	public List<ItemBehaviour> GetOnEquipPlayer() { return onEquipPlayerBehaviour; }
	public void SetOnEquipPlayer(List<ItemBehaviour> val) { onEquipPlayerBehaviour = val; }
	public List<ItemBehaviour> GetOnEquipClient() { return onEquipClientBehaviour; }
	public void SetOnEquipClient(List<ItemBehaviour> val) { onEquipClientBehaviour = val; }
	public List<ItemBehaviour> GetOnEquipServer() { return onEquipServerBehaviour; }
	public void SetOnEquipServer(List<ItemBehaviour> val) { onEquipServerBehaviour = val; }

	public List<ItemBehaviour> GetOnUnequipPlayer() { return onUnequipPlayerBehaviour; }
	public void SetOnUnequipPlayer(List<ItemBehaviour> val) { onUnequipPlayerBehaviour = val; }
	public List<ItemBehaviour> GetOnUnequipServer() { return onUnequipServerBehaviour; }
	public void SetOnUnequipServer(List<ItemBehaviour> val) { onUnequipServerBehaviour = val; }
	public List<ItemBehaviour> GetOnUnequipClient() { return onUnequipClientBehaviour; }
	public void SetOnUnequipClient(List<ItemBehaviour> val) { onUnequipClientBehaviour = val; }

	// Properties Set
	public void SetID(ushort i){this.id = i;}
	public void SetDurability(bool b){this.hasDurability = b;}
	public ushort GetID(){return this.id;}
	public void SetMemoryStorageType(){this.memoryStorageType = (MemoryStorageType)this.memoryType;}
	public MemoryStorageType GetMemoryStorageType(){return (MemoryStorageType)this.memoryType;}

	// Basic Operations
	public virtual EntityAction OnCreateAction(ChunkLoader cl, ItemStack its){
		if(this.onCreateActionBehaviour == null || this.onCreateActionBehaviour.Count == 0)
			return null;

		return this.onCreateActionBehaviour[0].OnCreateAction(cl, its);
	}
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
	public virtual void OnEquipPlayer(ChunkLoader cl, Item it){
		if(this.onEquipPlayerBehaviour == null || this.onEquipPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onEquipPlayerBehaviour.Count; i++){
			this.onEquipPlayerBehaviour[i].OnEquipPlayer(cl, it);
		}
	}
	public virtual void OnUnequipPlayer(ChunkLoader cl, Item it){
		if(this.onUnequipPlayerBehaviour == null || this.onUnequipPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnequipPlayerBehaviour.Count; i++){
			this.onUnequipPlayerBehaviour[i].OnUnequipPlayer(cl, it);
		}
	}
	public virtual void OnEquipClient(ChunkLoader cl, Item it, ulong code){
		if(this.onEquipClientBehaviour == null || this.onEquipClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onEquipClientBehaviour.Count; i++){
			this.onEquipClientBehaviour[i].OnEquipClient(cl, it, code);
		}
	}
	public virtual void OnUnequipClient(ChunkLoader cl, Item it, ulong code){
		if(this.onUnequipClientBehaviour == null || this.onUnequipClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnequipClientBehaviour.Count; i++){
			this.onUnequipClientBehaviour[i].OnUnequipClient(cl, it, code);
		}
	}
	public virtual void OnEquipServer(ChunkLoader_Server cl, Item it, ulong code){
		if(this.onEquipServerBehaviour == null || this.onEquipServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onEquipServerBehaviour.Count; i++){
			this.onEquipServerBehaviour[i].OnEquipServer(cl, it, code);
		}
	}
	public virtual void OnUnequipServer(ChunkLoader_Server cl, Item it, ulong code){
		if(this.onUnequipServerBehaviour == null || this.onUnequipServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnequipServerBehaviour.Count; i++){
			this.onUnequipServerBehaviour[i].OnUnequipServer(cl, it, code);
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

