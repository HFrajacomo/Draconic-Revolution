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

	public override void OnHoldPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){
		cl.playerActionController.Sheathe(false);
	}

	public override void OnUnholdPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){
		cl.playerActionController.Sheathe(true);
	}

	public override void OnHoldServer(ChunkLoader_Server cl, EntityAction ea, ItemStack its, ulong code){
		PlayerServerInventorySlot slot1 = cl.playerServerInventory.GetSlot(code, 3, 0);
		EmptyPlayerInventorySlot slot2 = new EmptyPlayerInventorySlot(3, 0); // PLACEHOLDER SLOT SINCE SLOT2 IN EQUIPMENT INV IS NOT IMPLEMENTED YET

		string style = BattleStyleDeterminator.Resolve(slot1, slot2);
		Debug.Log(slot1);
		int styleCode = AnimationLoader.GetBattleStyle(style).GetCode();

		cl.server.SendBattleStyle(code, styleCode);
	}
}