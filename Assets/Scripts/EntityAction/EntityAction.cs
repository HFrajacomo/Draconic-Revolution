using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntityAction {
	public string name;
	private ushort id;

	// Storage
	private bool connectedToStack;
	private ushort currentCooldown;
	private ushort totalCooldown;
	private byte connectedStackInventory;
	private byte connectedStackSlot;

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
	public ushort GetID(){return this.id;}

	public virtual EntityAction Copy(){
		return new EntityAction {
			name = this.name,
			id = this.id,

			// Deep copy lists (new lists with same elements)
			onIconDrawBehaviour   = this.onIconDrawBehaviour   != null ? new List<EntityActionBehaviour>(this.onIconDrawBehaviour)   : null,
			onStackDrawBehaviour   = this.onStackDrawBehaviour   != null ? new List<EntityActionBehaviour>(this.onStackDrawBehaviour)   : null,
			onHoldPlayerBehaviour   = this.onHoldPlayerBehaviour   != null ? new List<EntityActionBehaviour>(this.onHoldPlayerBehaviour)   : null,
			onHoldClientBehaviour   = this.onHoldClientBehaviour   != null ? new List<EntityActionBehaviour>(this.onHoldClientBehaviour)   : null,
			onHoldServerBehaviour   = this.onHoldServerBehaviour   != null ? new List<EntityActionBehaviour>(this.onHoldServerBehaviour)   : null,
			onUnholdPlayerBehaviour = this.onUnholdPlayerBehaviour != null ? new List<EntityActionBehaviour>(this.onUnholdPlayerBehaviour) : null,
			onUnholdClientBehaviour = this.onUnholdClientBehaviour != null ? new List<EntityActionBehaviour>(this.onUnholdClientBehaviour) : null,
			onUnholdServerBehaviour = this.onUnholdServerBehaviour != null ? new List<EntityActionBehaviour>(this.onUnholdServerBehaviour) : null,
			onPrimaryPlayerBehaviour = this.onPrimaryPlayerBehaviour != null ? new List<EntityActionBehaviour>(this.onPrimaryPlayerBehaviour) : null,
			onPrimaryClientBehaviour = this.onPrimaryClientBehaviour != null ? new List<EntityActionBehaviour>(this.onPrimaryClientBehaviour) : null,
			onPrimaryServerBehaviour = this.onPrimaryServerBehaviour != null ? new List<EntityActionBehaviour>(this.onPrimaryServerBehaviour) : null,
			onPrimaryHoldPlayerBehaviour = this.onPrimaryHoldPlayerBehaviour != null ? new List<EntityActionBehaviour>(this.onPrimaryHoldPlayerBehaviour) : null,
			onPrimaryHoldClientBehaviour = this.onPrimaryHoldClientBehaviour != null ? new List<EntityActionBehaviour>(this.onPrimaryHoldClientBehaviour) : null,
			onPrimaryHoldServerBehaviour = this.onPrimaryHoldServerBehaviour != null ? new List<EntityActionBehaviour>(this.onPrimaryHoldServerBehaviour) : null,
			onSecondaryPlayerBehaviour = this.onSecondaryPlayerBehaviour != null ? new List<EntityActionBehaviour>(this.onSecondaryPlayerBehaviour) : null,
			onSecondaryClientBehaviour = this.onSecondaryClientBehaviour != null ? new List<EntityActionBehaviour>(this.onSecondaryClientBehaviour) : null,
			onSecondaryServerBehaviour = this.onSecondaryServerBehaviour != null ? new List<EntityActionBehaviour>(this.onSecondaryServerBehaviour) : null,
			onSecondaryHoldPlayerBehaviour = this.onSecondaryHoldPlayerBehaviour != null ? new List<EntityActionBehaviour>(this.onSecondaryHoldPlayerBehaviour) : null,
			onSecondaryHoldClientBehaviour = this.onSecondaryHoldClientBehaviour != null ? new List<EntityActionBehaviour>(this.onSecondaryHoldClientBehaviour) : null,
			onSecondaryHoldServerBehaviour = this.onSecondaryHoldServerBehaviour != null ? new List<EntityActionBehaviour>(this.onSecondaryHoldServerBehaviour) : null,
			onTerciaryPlayerBehaviour = this.onTerciaryPlayerBehaviour != null ? new List<EntityActionBehaviour>(this.onTerciaryPlayerBehaviour) : null,
			onTerciaryClientBehaviour = this.onTerciaryClientBehaviour != null ? new List<EntityActionBehaviour>(this.onTerciaryClientBehaviour) : null,
			onTerciaryServerBehaviour = this.onTerciaryServerBehaviour != null ? new List<EntityActionBehaviour>(this.onTerciaryServerBehaviour) : null
		};
	}

	// Sets EntityAction data that comes specifically from Server byte array reconstruction
	public void SetMemoryData(bool connectedToStack, ushort currentCooldown, ushort totalCooldown, byte connectedStackInventory, byte connectedStackSlot){
		this.connectedToStack = connectedToStack;
		this.currentCooldown = currentCooldown;
		this.totalCooldown = totalCooldown;
		this.connectedStackInventory = connectedStackInventory;
		this.connectedStackSlot = connectedStackSlot;
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

	// GET and SET functions ---------------
	// UI
	public List<EntityActionBehaviour> GetOnIconDraw() { return onIconDrawBehaviour; }
	public void SetOnIconDraw(List<EntityActionBehaviour> val) { onIconDrawBehaviour = val; }

	public List<EntityActionBehaviour> GetStackDraw() { return onStackDrawBehaviour; }
	public void SetStackDraw(List<EntityActionBehaviour> val) { onStackDrawBehaviour = val; }

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

	// Run Events ------------------------------
	// UI
	public virtual void OnIconDraw(ChunkLoader cl, ItemStack its, out Texture2D symbol, out Texture2D itemIcon){
		symbol = null;
		itemIcon = null;

		if(this.onIconDrawBehaviour == null || this.onIconDrawBehaviour.Count == 0)
			return;

		this.onIconDrawBehaviour[0].OnIconDraw(cl, its, out symbol, out itemIcon);
	}

	public virtual string OnStackDraw(ChunkLoader cl, ItemStack its){
		if(this.onStackDrawBehaviour == null || this.onStackDrawBehaviour.Count == 0)
			return "";

		return this.onStackDrawBehaviour[0].OnStackDraw(cl, its);
	}

	// Hold
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

	// Unhold
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

	// Primary
	public virtual void OnPrimaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryPlayerBehaviour == null || this.onPrimaryPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryPlayerBehaviour.Count; i++){
			this.onPrimaryPlayerBehaviour[i].OnPrimaryPlayer(cl, its, code);
		}
	}

	public virtual void OnPrimaryClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryClientBehaviour == null || this.onPrimaryClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryClientBehaviour.Count; i++){
			this.onPrimaryClientBehaviour[i].OnPrimaryClient(cl, its, code);
		}
	}

	public virtual void OnPrimaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onPrimaryServerBehaviour == null || this.onPrimaryServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryServerBehaviour.Count; i++){
			this.onPrimaryServerBehaviour[i].OnPrimaryServer(cl, its, code);
		}
	}

	// Primary Hold
	public virtual void OnPrimaryHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryHoldPlayerBehaviour == null || this.onPrimaryHoldPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryHoldPlayerBehaviour.Count; i++){
			this.onPrimaryHoldPlayerBehaviour[i].OnPrimaryHoldPlayer(cl, its, code);
		}
	}

	public virtual void OnPrimaryHoldClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onPrimaryHoldClientBehaviour == null || this.onPrimaryHoldClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryHoldClientBehaviour.Count; i++){
			this.onPrimaryHoldClientBehaviour[i].OnPrimaryHoldClient(cl, its, code);
		}
	}

	public virtual void OnPrimaryHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onPrimaryHoldServerBehaviour == null || this.onPrimaryHoldServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onPrimaryHoldServerBehaviour.Count; i++){
			this.onPrimaryHoldServerBehaviour[i].OnPrimaryHoldServer(cl, its, code);
		}
	}

	// Secondary
	public virtual void OnSecondaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryPlayerBehaviour == null || this.onSecondaryPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryPlayerBehaviour.Count; i++){
			this.onSecondaryPlayerBehaviour[i].OnSecondaryPlayer(cl, its, code);
		}
	}

	public virtual void OnSecondaryClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryClientBehaviour == null || this.onSecondaryClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryClientBehaviour.Count; i++){
			this.onSecondaryClientBehaviour[i].OnSecondaryClient(cl, its, code);
		}
	}

	public virtual void OnSecondaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onSecondaryServerBehaviour == null || this.onSecondaryServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryServerBehaviour.Count; i++){
			this.onSecondaryServerBehaviour[i].OnSecondaryServer(cl, its, code);
		}
	}

	// Secondary Hold
	public virtual void OnSecondaryHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryHoldPlayerBehaviour == null || this.onSecondaryHoldPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryHoldPlayerBehaviour.Count; i++){
			this.onSecondaryHoldPlayerBehaviour[i].OnSecondaryHoldPlayer(cl, its, code);
		}
	}

	public virtual void OnSecondaryHoldClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onSecondaryHoldClientBehaviour == null || this.onSecondaryHoldClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryHoldClientBehaviour.Count; i++){
			this.onSecondaryHoldClientBehaviour[i].OnSecondaryHoldClient(cl, its, code);
		}
	}

	public virtual void OnSecondaryHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onSecondaryHoldServerBehaviour == null || this.onSecondaryHoldServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onSecondaryHoldServerBehaviour.Count; i++){
			this.onSecondaryHoldServerBehaviour[i].OnSecondaryHoldServer(cl, its, code);
		}
	}

	// Terciary
	public virtual void OnTerciaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onTerciaryPlayerBehaviour == null || this.onTerciaryPlayerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onTerciaryPlayerBehaviour.Count; i++){
			this.onTerciaryPlayerBehaviour[i].OnTerciaryPlayer(cl, its, code);
		}
	}

	public virtual void OnTerciaryClient(ChunkLoader cl, ItemStack its, ulong code){
		if(this.onTerciaryClientBehaviour == null || this.onTerciaryClientBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onTerciaryClientBehaviour.Count; i++){
			this.onTerciaryClientBehaviour[i].OnTerciaryClient(cl, its, code);
		}
	}

	public virtual void OnTerciaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		if(this.onTerciaryServerBehaviour == null || this.onTerciaryServerBehaviour.Count == 0)
			return;

		for(int i=0; i < this.onTerciaryServerBehaviour.Count; i++){
			this.onTerciaryServerBehaviour[i].OnTerciaryServer(cl, its, code);
		}
	}

	public virtual void PostDeserializationSetup(){ return; }

}