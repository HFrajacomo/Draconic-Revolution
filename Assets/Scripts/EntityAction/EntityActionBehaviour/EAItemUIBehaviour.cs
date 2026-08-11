using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class EAItemUIBehaviour : EntityActionBehaviour {
	private static string BASE_ITEM_SYMBOL = "Inventory/item_action";
	private static Texture2D SYMBOL;
	
	public override void OnIconDraw(ChunkLoader cl, ItemStack its, out Texture2D symbol, out Texture2D itemIcon){
		itemIcon = ItemLoader.GetSprite(its);

		if(SYMBOL == null){
			SYMBOL = Resources.Load<Texture2D>(BASE_ITEM_SYMBOL);

			if(SYMBOL == null)
				throw new DeserializationErrorException($"[EntityActionBehaviour] Failed to load default Item Symbol");
		}

		symbol = SYMBOL;
	}
	
	public override string OnStackDraw(ChunkLoader cl, ItemStack its){ 
		int amount = cl.playerInventoryManager.GetItemCount(its.GetID());

		if(amount <= 1)
			return "";
		if(amount >= 100)
			return "99+";
		return $"{amount}"; 
	}
}