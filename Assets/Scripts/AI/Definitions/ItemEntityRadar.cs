using System.Collections.Generic;
using UnityEngine;

public class ItemEntityRadar : EntityRadar{
	private ItemStack its;
	private byte quantityToTransfer;

	public ItemEntityRadar(Vector3 pos, Vector3 dir, CastCoord coords, ItemStack its, EntityID entityID, EntityHandler_Server ehs){
		this.SetTransform(ref pos, ref dir, ref coords);
		this.entityHandler = ehs;
		this.FOV = 180;
		this.visionDistance = 1.25f;
		this.entitySubscription = new HashSet<EntityType>(){EntityType.DROP};
		this.its = its;
		this.ID = entityID;
	}

	protected override bool PreAnalysisAI(AbstractAI ai){
		DroppedItemAI itemAI = (DroppedItemAI)ai;
		ItemStack aiItem = itemAI.GetItemStack();

		if(itemAI.IsOnPickupMode())
			return false;

		// If has the same ID
		if(this.its.GetID() == aiItem.GetID()){
			if(!itemAI.IsStanding())
				return false;

			// And not full
			if(!aiItem.IsFull()){
				this.quantityToTransfer = (byte)(its.GetStacksize() - aiItem.GetAmount());
				return true;
			}
		}
		return false;
	}

	protected override void CreateEntityRadarEvent(AbstractAI ai, ref List<EntityEvent> ieq){
		ieq.Add(new EntityEvent(EntityEventType.VISION, true, this.quantityToTransfer, new EntityRadarEvent(ai.GetID(), ai.GetPosition(), ai.GetPosition(), ai)));
	}
}