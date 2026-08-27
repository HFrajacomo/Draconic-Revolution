using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Object = System.Object;
using UnityEngine;

[Serializable]
public class EntityAction : ClickableSlot {
	public string name;
	public bool notConnectedToSpecificStack = false;
	public bool keepInHotbar = false;
	private ushort id;

	// Storage
	public bool connectedToStack = true;
	private ushort currentCooldown;
	private ushort totalCooldown;
	private byte connectedStackInventory;
	private byte connectedStackSlot;

	private ItemStack its;
	private ushort connectedItemID;

	// Behaviours ---------------------
	// UI
	protected List<EntityActionBehaviour> onIconDrawBehaviour;
	protected List<EntityActionBehaviour> onStackDrawBehaviour;

	// Hold
	protected List<EntityActionBehaviour> onHoldPlayerBehaviour;
	protected List<EntityActionBehaviour> onHoldClientBehaviour;
	protected List<EntityActionBehaviour> onHoldServerBehaviour;

	// Unhold
	protected List<EntityActionBehaviour> onUnholdPlayerBehaviour;
	protected List<EntityActionBehaviour> onUnholdClientBehaviour;
	protected List<EntityActionBehaviour> onUnholdServerBehaviour;

	// Primary
	protected List<EntityActionBehaviour> onPrimaryPlayerBehaviour;
	protected List<EntityActionBehaviour> onPrimaryClientBehaviour;
	protected List<EntityActionBehaviour> onPrimaryServerBehaviour;

	// Primary Hold
	protected List<EntityActionBehaviour> onPrimaryHoldPlayerBehaviour;
	protected List<EntityActionBehaviour> onPrimaryHoldClientBehaviour;
	protected List<EntityActionBehaviour> onPrimaryHoldServerBehaviour;

	// Secondary
	protected List<EntityActionBehaviour> onSecondaryPlayerBehaviour;
	protected List<EntityActionBehaviour> onSecondaryClientBehaviour;
	protected List<EntityActionBehaviour> onSecondaryServerBehaviour;

	// Secondary Hold
	protected List<EntityActionBehaviour> onSecondaryHoldPlayerBehaviour;
	protected List<EntityActionBehaviour> onSecondaryHoldClientBehaviour;
	protected List<EntityActionBehaviour> onSecondaryHoldServerBehaviour;

	// Terciary
	protected List<EntityActionBehaviour> onTerciaryPlayerBehaviour;
	protected List<EntityActionBehaviour> onTerciaryClientBehaviour;
	protected List<EntityActionBehaviour> onTerciaryServerBehaviour;

	// Basic functions ---------------------

	public void SetID(ushort id){this.id = id;}
	public override ushort GetID(){return this.id;}
	public ItemStack GetItemStack(PlayerInventoryManager pim){
		if(pim == null){
			return this.its;
		}

		if((this.its == null || this.its.GetAmount() == 0)){
			this.its = null;

			if(this.notConnectedToSpecificStack){
				ItemStack newStack = pim.GetNextItemStack(this.connectedItemID, ref this.connectedStackInventory, ref this.connectedStackSlot);

				if(newStack != null){
					SetItemStack(newStack);
				}
				
				return newStack;
			}
		}

		return this.its;
	}
	public void SetItemStack(ItemStack its){
		if(its != null)
			this.connectedItemID = its.GetID();

		this.its = its;
	}
	public void SetItemConnection(byte inventory, byte slot, ItemStack its){
		this.connectedStackInventory = inventory;
		this.connectedStackSlot = slot;
		this.its = its;
	}

	public byte GetConnectedStackSlot(){return this.connectedStackSlot;}
	public byte GetConnectedStackInventory(){return this.connectedStackInventory;}

	public virtual EntityAction Copy(){
		return new EntityAction {
			name = this.name,
			id = this.id,
			isItemStack = false,
			notConnectedToSpecificStack = this.notConnectedToSpecificStack,
			keepInHotbar = this.keepInHotbar,

			// Deep copy lists (new lists with same elements)
			onIconDrawBehaviour = CopyList(this.onIconDrawBehaviour),
			onStackDrawBehaviour = CopyList(this.onStackDrawBehaviour),
			onHoldPlayerBehaviour = CopyList(this.onHoldPlayerBehaviour),
			onHoldClientBehaviour = CopyList(this.onHoldClientBehaviour),
			onHoldServerBehaviour = CopyList(this.onHoldServerBehaviour),
			onUnholdPlayerBehaviour = CopyList(this.onUnholdPlayerBehaviour),
			onUnholdClientBehaviour = CopyList(this.onUnholdClientBehaviour),
			onUnholdServerBehaviour = CopyList(this.onUnholdServerBehaviour),
			onPrimaryPlayerBehaviour = CopyList(this.onPrimaryPlayerBehaviour),
			onPrimaryClientBehaviour = CopyList(this.onPrimaryClientBehaviour),
			onPrimaryServerBehaviour = CopyList(this.onPrimaryServerBehaviour),
			onPrimaryHoldPlayerBehaviour = CopyList(this.onPrimaryHoldPlayerBehaviour),
			onPrimaryHoldClientBehaviour = CopyList(this.onPrimaryHoldClientBehaviour),
			onPrimaryHoldServerBehaviour = CopyList(this.onPrimaryHoldServerBehaviour),
			onSecondaryPlayerBehaviour = CopyList(this.onSecondaryPlayerBehaviour),
			onSecondaryClientBehaviour = CopyList(this.onSecondaryClientBehaviour),
			onSecondaryServerBehaviour = CopyList(this.onSecondaryServerBehaviour),
			onSecondaryHoldPlayerBehaviour = CopyList(this.onSecondaryHoldPlayerBehaviour),
			onSecondaryHoldClientBehaviour = CopyList(this.onSecondaryHoldClientBehaviour),
			onSecondaryHoldServerBehaviour = CopyList(this.onSecondaryHoldServerBehaviour),
			onTerciaryPlayerBehaviour = CopyList(this.onTerciaryPlayerBehaviour),
			onTerciaryClientBehaviour = CopyList(this.onTerciaryClientBehaviour),
			onTerciaryServerBehaviour = CopyList(this.onTerciaryServerBehaviour)
		};
	}

	// Sets EntityAction data that comes specifically from Server byte array reconstruction
	public void SetMemoryData(bool connectedToStack, ushort currentCooldown, ushort totalCooldown, byte connectedStackInventory, byte connectedStackSlot){
		this.connectedToStack = connectedToStack;
		this.currentCooldown = currentCooldown;
		this.totalCooldown = totalCooldown;
		this.connectedStackInventory = connectedStackInventory;
		this.connectedStackSlot = connectedStackSlot;
		this.isItemStack = false;
	}

	// Serializes this EntityAction object to send over byte array to Server
	public int ConvertToMemory(byte[] data, int pos){
		NetDecoder.WriteByte((byte)MemoryStorageType.ACTION, data, pos);
		NetDecoder.WriteUshort(GetID(), data, pos+1);
		NetDecoder.WriteBool(this.connectedToStack, data, pos+3);
		NetDecoder.WriteUshort(this.currentCooldown, data, pos+4);
		NetDecoder.WriteUshort(this.totalCooldown, data, pos+6);
		NetDecoder.WriteByte(this.connectedStackInventory, data, pos+8);
		NetDecoder.WriteByte(this.connectedStackSlot, data, pos+9);
		return 10;
	}

	public override string ToString(){return $"Action: {GetID()} -- Item Connection: ({this.connectedStackInventory} | {this.connectedStackSlot}) -- Item: {this.its}";}

	// GET and SET functions ---------------
	// UI
	public List<EntityActionBehaviour> GetOnIconDraw() { return onIconDrawBehaviour; }
	public void SetOnIconDraw(List<EntityActionBehaviour> val) { onIconDrawBehaviour = val; }

	public List<EntityActionBehaviour> GetStackDraw() { return onStackDrawBehaviour; }
	public void SetOnStackDraw(List<EntityActionBehaviour> val) { onStackDrawBehaviour = val; }

	// Hold
	public List<EntityActionBehaviour> GetOnHoldPlayer() { return onHoldPlayerBehaviour; }
	public void SetOnHoldPlayer(List<EntityActionBehaviour> val) { onHoldPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnHoldClient() { return onHoldClientBehaviour; }
	public void SetOnHoldClient(List<EntityActionBehaviour> val) { onHoldClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnHoldServer() { return onHoldServerBehaviour; }
	public void SetOnHoldServer(List<EntityActionBehaviour> val) { onHoldServerBehaviour = val; }

	// Unhold
	public List<EntityActionBehaviour> GetOnUnholdPlayer() { return onUnholdPlayerBehaviour; }
	public void SetOnUnholdPlayer(List<EntityActionBehaviour> val) { onUnholdPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnUnholdClient() { return onUnholdClientBehaviour; }
	public void SetOnUnholdClient(List<EntityActionBehaviour> val) { onUnholdClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnUnholdServer() { return onUnholdServerBehaviour; }
	public void SetOnUnholdServer(List<EntityActionBehaviour> val) { onUnholdServerBehaviour = val; }

	// Primary
	public List<EntityActionBehaviour> GetOnPrimaryPlayer() { return onPrimaryPlayerBehaviour; }
	public void SetOnPrimaryPlayer(List<EntityActionBehaviour> val) { onPrimaryPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnPrimaryClient() { return onPrimaryClientBehaviour; }
	public void SetOnPrimaryClient(List<EntityActionBehaviour> val) { onPrimaryClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnPrimaryServer() { return onPrimaryServerBehaviour; }
	public void SetOnPrimaryServer(List<EntityActionBehaviour> val) { onPrimaryServerBehaviour = val; }

	// Primary Hold
	public List<EntityActionBehaviour> GetOnPrimaryHoldPlayer() { return onPrimaryHoldPlayerBehaviour; }
	public void SetOnPrimaryHoldPlayer(List<EntityActionBehaviour> val) { onPrimaryHoldPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnPrimaryHoldClient() { return onPrimaryHoldClientBehaviour; }
	public void SetOnPrimaryHoldClient(List<EntityActionBehaviour> val) { onPrimaryHoldClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnPrimaryHoldServer() { return onPrimaryHoldServerBehaviour; }
	public void SetOnPrimaryHoldServer(List<EntityActionBehaviour> val) { onPrimaryHoldServerBehaviour = val; }

	// Secondary
	public List<EntityActionBehaviour> GetOnSecondaryPlayer() { return onSecondaryPlayerBehaviour; }
	public void SetOnSecondaryPlayer(List<EntityActionBehaviour> val) { onSecondaryPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnSecondaryClient() { return onSecondaryClientBehaviour; }
	public void SetOnSecondaryClient(List<EntityActionBehaviour> val) { onSecondaryClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnSecondaryServer() { return onSecondaryServerBehaviour; }
	public void SetOnSecondaryServer(List<EntityActionBehaviour> val) { onSecondaryServerBehaviour = val; }

	// Secondary Hold
	public List<EntityActionBehaviour> GetOnSecondaryHoldPlayer() { return onSecondaryHoldPlayerBehaviour; }
	public void SetOnSecondaryHoldPlayer(List<EntityActionBehaviour> val) { onSecondaryHoldPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnSecondaryHoldClient() { return onSecondaryHoldClientBehaviour; }
	public void SetOnSecondaryHoldClient(List<EntityActionBehaviour> val) { onSecondaryHoldClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnSecondaryHoldServer() { return onSecondaryHoldServerBehaviour; }
	public void SetOnSecondaryHoldServer(List<EntityActionBehaviour> val) { onSecondaryHoldServerBehaviour = val; }

	// Terciary
	public List<EntityActionBehaviour> GetOnTerciaryPlayer() { return onTerciaryPlayerBehaviour; }
	public void SetOnTerciaryPlayer(List<EntityActionBehaviour> val) { onTerciaryPlayerBehaviour = val; }

	public List<EntityActionBehaviour> GetOnTerciaryClient() { return onTerciaryClientBehaviour; }
	public void SetOnTerciaryClient(List<EntityActionBehaviour> val) { onTerciaryClientBehaviour = val; }

	public List<EntityActionBehaviour> GetOnTerciaryServer() { return onTerciaryServerBehaviour; }
	public void SetOnTerciaryServer(List<EntityActionBehaviour> val) { onTerciaryServerBehaviour = val; }

	public List<EntityActionBehaviour> GetAllBehaviours(){
		List<EntityActionBehaviour> all = new List<EntityActionBehaviour>();

		// UI
		if (this.onIconDrawBehaviour != null) all.AddRange(this.onIconDrawBehaviour);
		if (this.onStackDrawBehaviour != null) all.AddRange(this.onStackDrawBehaviour);

		// Hold
		if (this.onHoldPlayerBehaviour != null) all.AddRange(this.onHoldPlayerBehaviour);
		if (this.onHoldClientBehaviour != null) all.AddRange(this.onHoldClientBehaviour);
		if (this.onHoldServerBehaviour != null) all.AddRange(this.onHoldServerBehaviour);

		// Unhold
		if (this.onUnholdPlayerBehaviour != null) all.AddRange(this.onUnholdPlayerBehaviour);
		if (this.onUnholdClientBehaviour != null) all.AddRange(this.onUnholdClientBehaviour);
		if (this.onUnholdServerBehaviour != null) all.AddRange(this.onUnholdServerBehaviour);

		// Primary
		if (this.onPrimaryPlayerBehaviour != null) all.AddRange(this.onPrimaryPlayerBehaviour);
		if (this.onPrimaryClientBehaviour != null) all.AddRange(this.onPrimaryClientBehaviour);
		if (this.onPrimaryServerBehaviour != null) all.AddRange(this.onPrimaryServerBehaviour);

		// Primary Hold
		if (this.onPrimaryHoldPlayerBehaviour != null) all.AddRange(this.onPrimaryHoldPlayerBehaviour);
		if (this.onPrimaryHoldClientBehaviour != null) all.AddRange(this.onPrimaryHoldClientBehaviour);
		if (this.onPrimaryHoldServerBehaviour != null) all.AddRange(this.onPrimaryHoldServerBehaviour);

		// Secondary
		if (this.onSecondaryPlayerBehaviour != null) all.AddRange(this.onSecondaryPlayerBehaviour);
		if (this.onSecondaryClientBehaviour != null) all.AddRange(this.onSecondaryClientBehaviour);
		if (this.onSecondaryServerBehaviour != null) all.AddRange(this.onSecondaryServerBehaviour);

		// Secondary Hold
		if (this.onSecondaryHoldPlayerBehaviour != null) all.AddRange(this.onSecondaryHoldPlayerBehaviour);
		if (this.onSecondaryHoldClientBehaviour != null) all.AddRange(this.onSecondaryHoldClientBehaviour);
		if (this.onSecondaryHoldServerBehaviour != null) all.AddRange(this.onSecondaryHoldServerBehaviour);

		// Terciary
		if (this.onTerciaryPlayerBehaviour != null) all.AddRange(this.onTerciaryPlayerBehaviour);
		if (this.onTerciaryClientBehaviour != null) all.AddRange(this.onTerciaryClientBehaviour);
		if (this.onTerciaryServerBehaviour != null) all.AddRange(this.onTerciaryServerBehaviour);

		return all;
	}

	// Run Events ------------------------------
	// UI
	public virtual void OnIconDraw(ChunkLoader cl, ItemStack its, out Texture2D symbol, out Texture2D itemIcon){
		symbol = null;
		itemIcon = null;

		if(this.onIconDrawBehaviour == null || this.onIconDrawBehaviour.Count == 0){
			return;
		}

		this.onIconDrawBehaviour[0].OnIconDraw(cl, this, its, ref symbol, ref itemIcon);
	}

	public virtual string OnStackDraw(ChunkLoader cl, ItemStack its){
		if(this.onStackDrawBehaviour == null || this.onStackDrawBehaviour.Count == 0){
			return "";
		}

		return this.onStackDrawBehaviour[0].OnStackDraw(cl, this, its);
	}

	// Hold
	public virtual void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onHoldPlayerBehaviour == null || this.onHoldPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onHoldPlayerBehaviour.Count; i++){
			this.onHoldPlayerBehaviour[i].OnHoldPlayer(cl, this, its, code);
		}
	}

	public virtual void OnHoldClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onHoldClientBehaviour == null || this.onHoldClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onHoldClientBehaviour.Count; i++){
			this.onHoldClientBehaviour[i].OnHoldClient(cl, this, its, code);
		}
	}

	public virtual void OnHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onHoldServerBehaviour == null || this.onHoldServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onHoldServerBehaviour.Count; i++){
			this.onHoldServerBehaviour[i].OnHoldServer(cl, this, its, code);
		}
	}

	// Unhold
	public virtual void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onUnholdPlayerBehaviour == null || this.onUnholdPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnholdPlayerBehaviour.Count; i++){
			this.onUnholdPlayerBehaviour[i].OnUnholdPlayer(cl, this, its, code);
		}
	}

	public virtual void OnUnholdClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onUnholdClientBehaviour == null || this.onUnholdClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnholdClientBehaviour.Count; i++){
			this.onUnholdClientBehaviour[i].OnUnholdClient(cl, this, its, code);
		}
	}

	public virtual void OnUnholdServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onUnholdServerBehaviour == null || this.onUnholdServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onUnholdServerBehaviour.Count; i++){
			this.onUnholdServerBehaviour[i].OnUnholdServer(cl, this, its, code);
		}
	}

	// Primary
	public virtual void OnPrimaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryPlayerBehaviour == null || this.onPrimaryPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryPlayerBehaviour.Count; i++){
			this.onPrimaryPlayerBehaviour[i].OnPrimaryPlayer(cl, this, its, code);
		}
	}

	public virtual void OnPrimaryClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryClientBehaviour == null || this.onPrimaryClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryClientBehaviour.Count; i++){
			this.onPrimaryClientBehaviour[i].OnPrimaryClient(cl, this, its, code);
		}
	}

	public virtual void OnPrimaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onPrimaryServerBehaviour == null || this.onPrimaryServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryServerBehaviour.Count; i++){
			this.onPrimaryServerBehaviour[i].OnPrimaryServer(cl, this, its, code);
		}
	}

	// Primary Hold
	public virtual void OnPrimaryHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryHoldPlayerBehaviour == null || this.onPrimaryHoldPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryHoldPlayerBehaviour.Count; i++){
			this.onPrimaryHoldPlayerBehaviour[i].OnPrimaryHoldPlayer(cl, this, its, code);
		}
	}

	public virtual void OnPrimaryHoldClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryHoldClientBehaviour == null || this.onPrimaryHoldClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryHoldClientBehaviour.Count; i++){
			this.onPrimaryHoldClientBehaviour[i].OnPrimaryHoldClient(cl, this, its, code);
		}
	}

	public virtual void OnPrimaryHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onPrimaryHoldServerBehaviour == null || this.onPrimaryHoldServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryHoldServerBehaviour.Count; i++){
			this.onPrimaryHoldServerBehaviour[i].OnPrimaryHoldServer(cl, this, its, code);
		}
	}

	// Secondary
	public virtual void OnSecondaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryPlayerBehaviour == null || this.onSecondaryPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryPlayerBehaviour.Count; i++){
			this.onSecondaryPlayerBehaviour[i].OnSecondaryPlayer(cl, this, its, code);
		}
	}

	public virtual void OnSecondaryClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryClientBehaviour == null || this.onSecondaryClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryClientBehaviour.Count; i++){
			this.onSecondaryClientBehaviour[i].OnSecondaryClient(cl, this, its, code);
		}
	}

	public virtual void OnSecondaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onSecondaryServerBehaviour == null || this.onSecondaryServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryServerBehaviour.Count; i++){
			this.onSecondaryServerBehaviour[i].OnSecondaryServer(cl, this, its, code);
		}
	}

	// Secondary Hold
	public virtual void OnSecondaryHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryHoldPlayerBehaviour == null || this.onSecondaryHoldPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryHoldPlayerBehaviour.Count; i++){
			this.onSecondaryHoldPlayerBehaviour[i].OnSecondaryHoldPlayer(cl, this, its, code);
		}
	}

	public virtual void OnSecondaryHoldClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryHoldClientBehaviour == null || this.onSecondaryHoldClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryHoldClientBehaviour.Count; i++){
			this.onSecondaryHoldClientBehaviour[i].OnSecondaryHoldClient(cl, this, its, code);
		}
	}

	public virtual void OnSecondaryHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onSecondaryHoldServerBehaviour == null || this.onSecondaryHoldServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryHoldServerBehaviour.Count; i++){
			this.onSecondaryHoldServerBehaviour[i].OnSecondaryHoldServer(cl, this, its, code);
		}
	}

	// Terciary
	public virtual void OnTerciaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onTerciaryPlayerBehaviour == null || this.onTerciaryPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onTerciaryPlayerBehaviour.Count; i++){
			this.onTerciaryPlayerBehaviour[i].OnTerciaryPlayer(cl, this, its, code);
		}
	}

	public virtual void OnTerciaryClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onTerciaryClientBehaviour == null || this.onTerciaryClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onTerciaryClientBehaviour.Count; i++){
			this.onTerciaryClientBehaviour[i].OnTerciaryClient(cl, this, its, code);
		}
	}

	public virtual void OnTerciaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onTerciaryServerBehaviour == null || this.onTerciaryServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onTerciaryServerBehaviour.Count; i++){
			this.onTerciaryServerBehaviour[i].OnTerciaryServer(cl, this, its, code);
		}
	}

	public virtual void PostDeserializationSetup() { return; }

	private List<EntityActionBehaviour> CopyList(List<EntityActionBehaviour> list){
		if(list == null)
			return null;

		List<EntityActionBehaviour> outputList = new List<EntityActionBehaviour>();

		foreach(EntityActionBehaviour eab in list){
			outputList.Add(eab.Copy());
		}

		return outputList;
	}
}