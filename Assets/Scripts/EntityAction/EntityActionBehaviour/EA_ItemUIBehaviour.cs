using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class EA_ItemUIBehaviour : EntityActionBehaviour {
	private static string BASE_ITEM_SYMBOL = "Inventory/item_action";
	private static Texture2D SYMBOL;

	public override EntityActionBehaviour Copy(){
		return new EA_ItemUIBehaviour();
	}

	public override void OnIconDraw(ChunkLoader cl, EntityAction ea, ItemStack its, ref Texture2D symbol, ref Texture2D itemIcon){
		if(its == null){
			symbol = null;
			itemIcon = null;
			return;
		}
		
		itemIcon = ItemLoader.GetSprite(its);

		if(SYMBOL == null){
			SYMBOL = Resources.Load<Texture2D>(BASE_ITEM_SYMBOL);

			if(SYMBOL == null)
				throw new DeserializationErrorException($"[EAItemUIBehaviour] Failed to load default Item Symbol");
		}

		symbol = SYMBOL;
	}
	
	public override string OnStackDraw(ChunkLoader cl, EntityAction ea, ItemStack its){ 
		if(its == null)
			return "";

		int amount = cl.playerInventoryManager.GetItemCount(its.GetID());

		if(amount <= 1)
			return "";
		if(amount >= 1000)
			return "999+";
		return $"{amount}"; 
	}
}