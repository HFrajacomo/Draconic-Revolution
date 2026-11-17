using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerEntityRadar : EntityRadar{
	private ChunkLoader_Server cl;
	private PlayerServerInventory psi;
	private DroppedItemAI cachedItemAI;
	public bool HAS_RECEIVED_ITEMS;

	public PlayerEntityRadar(Vector3 pos, Vector3 dir, CastCoord coords, EntityID entityID, EntityHandler_Server ehs, PlayerServerInventory psi, ChunkLoader_Server cl){
		this.SetTransform(ref pos, ref dir, ref coords);
		this.entityHandler = ehs;
		this.FOV = 180;
		this.visionDistance = 1.4f;
		this.entitySubscription = new HashSet<EntityType>(){EntityType.DROP};
		this.psi = psi;
		this.ID = entityID;
		this.cl = cl;
	}

	protected override bool PreAnalysisAI(AbstractAI ai){
		this.cachedItemAI = (DroppedItemAI)ai;

		if(this.cachedItemAI.IsOnPickupMode())
			return false;

		if(this.cachedItemAI.markedForDelete || this.cachedItemAI.markedForChange)
			return false;

		if(this.cachedItemAI.IsCreatedByPlayer() && this.cachedItemAI.playerCode == this.ID.code){
			return false;
		}

		return true;
	}

	protected override bool PostAnalysisAI(AbstractAI ai){
		this.cachedItemAI = (DroppedItemAI)ai;

		ItemStack aiItem = this.cachedItemAI.GetItemStack();
		byte taken = this.psi.AddPickup(this.ID.code, aiItem, out int createdSlot);

		if(taken == 0)
			return false;

		this.HAS_RECEIVED_ITEMS = true;

		// Only a previously empty slot can change the item in hand, which triggers the OnHold event
		if(createdSlot == this.entityHandler.GetSheet(this.ID.code).GetHotbarSlot()){
			ItemStack heldStack = this.psi.GetSlot(this.ID.code, (byte)createdSlot).GetItemStack();
			heldStack.GetItem().OnHoldServer(this.cl, heldStack, this.ID.code);

			NetMessage message = new NetMessage(NetCode.SENDITEMINHAND);
			message.SendItemInHand(this.cl.server.GetPlayerNetID(this.ID.code), true, heldStack.GetID(), heldStack.GetAmount());
			this.cl.server.SendToClientsExcept(this.ID.code, message);
		}

		// If could take the entire stack
		if(taken == aiItem.GetAmount()){
			this.cachedItemAI.SetPickupMode();
			return true;
		}

		// The remainder stays on the ground
		this.cachedItemAI.SetItemStackAmount((byte)(aiItem.GetAmount() - taken));
		return false;
	}

	protected override void CreateEntityRadarEvent(AbstractAI ai, ref List<EntityEvent> ieq){
		ai.AddToInboundEventQueue(new EntityEvent(EntityEventType.ITEM_PICKUP, false, new EntityRadarEvent(ai.GetID(), ai.GetPosition(), ai.GetPosition(), ai), this.position - ai.position));
	}
}