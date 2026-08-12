using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public abstract class EntityActionBehaviour {
	public override string ToString(){return GetType().Name;}

	public virtual void PostDeserializationSetup(){ return; }

	// UI
	public virtual void OnIconDraw(ChunkLoader cl, EntityAction ea, ItemStack its, out Texture2D symbol, out Texture2D itemIcon){symbol = null; itemIcon = null; return; }
	public virtual string OnStackDraw(ChunkLoader cl, EntityAction ea, ItemStack its){ return ""; }

	// Hold
	public virtual void OnHoldPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnHoldClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnHoldServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }

	// Unhold
	public virtual void OnUnholdPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnUnholdClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnUnholdServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }

	// Primary
	public virtual void OnPrimaryPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }

	// Primary Hold
	public virtual void OnPrimaryHoldPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryHoldClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryHoldServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }

	// Secondary
	public virtual void OnSecondaryPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }

	// Secondary Hold
	public virtual void OnSecondaryHoldPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryHoldClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryHoldServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }

	// Terciary
	public virtual void OnTerciaryPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnTerciaryClient(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){ return; }
	public virtual void OnTerciaryServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){ return; }
}