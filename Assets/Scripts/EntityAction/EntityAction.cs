using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class EntityAction {
	public string name;
	private ushort id;

	// Behaviours ---------------------
	// Hold
	private List<EntityActionBehaviour> onHoldPlayerBehaviour;
	private List<EntityActionBehaviour> onHoldClientBehaviour;
	private List<EntityActionBehaviour> onHoldServerBehaviour;

	// Unhold
	private List<EntityActionBehaviour> onUnholdPlayerBehaviour;
	private List<EntityActionBehaviour> onUnholdClientBehaviour;
	private List<EntityActionBehaviour> onUnholdServerBehaviour;

	// Primary
	private List<EntityActionBehaviour> onPrimaryPlayerBehaviour;
	private List<EntityActionBehaviour> onPrimaryClientBehaviour;
	private List<EntityActionBehaviour> onPrimaryServerBehaviour;

	// Primary Hold
	private List<EntityActionBehaviour> onPrimaryHoldPlayerBehaviour;
	private List<EntityActionBehaviour> onPrimaryHoldClientBehaviour;
	private List<EntityActionBehaviour> onPrimaryHoldServerBehaviour;

	// Secondary
	private List<EntityActionBehaviour> onSecondaryPlayerBehaviour;
	private List<EntityActionBehaviour> onSecondaryClientBehaviour;
	private List<EntityActionBehaviour> onSecondaryServerBehaviour;

	// Secondary Hold
	private List<EntityActionBehaviour> onSecondaryHoldPlayerBehaviour;
	private List<EntityActionBehaviour> onSecondaryHoldClientBehaviour;
	private List<EntityActionBehaviour> onSecondaryHoldServerBehaviour;

	// Terciary
	private List<EntityActionBehaviour> onTerciaryPlayerBehaviour;
	private List<EntityActionBehaviour> onTerciaryClientBehaviour;
	private List<EntityActionBehaviour> onTerciaryServerBehaviour;

	// Basic functions ---------------------

	public void SetID(ushort id){this.id = id;}
	public ushort GetID(){return this.id;}

	// GET and SET functions ---------------
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