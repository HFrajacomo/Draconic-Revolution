using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public abstract class EntityActionBehaviour {
	private static string BASE_ITEM_SYMBOL = "";
	private static Texture2D SYMBOL;

	public override string ToString(){return GetType().Name;}

	public virtual void PostDeserializationSetup(){ return; }

	// UI
	public virtual void OnIconDraw(ChunkLoader cl, ItemStack its, out Texture2D symbol, out Texture2D itemIcon){
		itemIcon = ItemLoader.GetSprite(its);

		if(SYMBOL == null){
			SYMBOL = Resources.Load<Texture2D>(BASE_ITEM_SYMBOL);

			if(SYMBOL == null)
				throw new DeserializationErrorException($"[EntityActionBehaviour] Failed to load default Item Symbol");
		}

		symbol = SYMBOL;
	}
	public virtual string OnStackDraw(ChunkLoader cl, ItemStack its){ 
		return $"{its.GetAmount()}"; 
	}

	// Hold
	public virtual void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnHoldClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }

	// Unhold
	public virtual void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnUnholdClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnUnholdServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }

	// Primary
	public virtual void OnPrimaryPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }

	// Primary Hold
	public virtual void OnPrimaryHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryHoldClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnPrimaryHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }

	// Secondary
	public virtual void OnSecondaryPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }

	// Secondary Hold
	public virtual void OnSecondaryHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryHoldClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnSecondaryHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }

	// Terciary
	public virtual void OnTerciaryPlayer(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnTerciaryClient(ChunkLoader cl, ItemStack its, ulong code){ return; }
	public virtual void OnTerciaryServer(ChunkLoader_Server cl, ItemStack its, ulong code){ return; }
}