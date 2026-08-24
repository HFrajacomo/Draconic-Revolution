using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class EA_EquipmentBehaviour : EntityActionBehaviour {
	private static string BASE_ITEM_SYMBOL = "Inventory/equipment_action";
	private static Texture2D SYMBOL;

	public override EntityActionBehaviour Copy(){
		return new EA_EquipmentBehaviour();
	}

	public override void OnIconDraw(ChunkLoader cl, EntityAction ea, ItemStack its, ref Texture2D symbol, ref Texture2D itemIcon){
		// PLACEHOLDER
		itemIcon = ItemLoader.GetSprite(ItemLoader.GetItem("BASE_Bastard_Sword").GetID());

		if(SYMBOL == null){
			SYMBOL = Resources.Load<Texture2D>(BASE_ITEM_SYMBOL);

			if(SYMBOL == null)
				throw new DeserializationErrorException($"[EA_EquipmentBehaviour] Failed to load default Equipment Symbol");
		}

		symbol = SYMBOL;
	}
	
	public override void OnPrimaryPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){

	}

	public override void OnHoldServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){
		//cl.server.SendBattleStyle(code, this.styleCode);
	}
}